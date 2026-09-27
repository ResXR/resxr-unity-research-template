#!/usr/bin/env python3
"""
perf_report.py — analyzes pulled ResXRDataManager perf-test session folders and produces a
markdown report: latency (ProfilerMarker breakdown, frame time), resource consumption (GC
allocation, Mono heap trend), and payload/efficiency (bytes/row, KB/s, MB per 30-min session)
per recording-option condition.

Usage:
    python perf_report.py --results-dir ./results --out perf_report.md

Point --results-dir at a folder containing one or more pulled session folders, anywhere in its
tree (searched recursively) — each is identified by containing a *_ResXRDataPerfSamples.csv
file. Each session's condition label comes from its folder name (the part after the
{sessionTime}_ prefix, e.g. "2026.09.10_10-05_continuous_off" -> "continuous_off") — this is
deliberate: if you're building one APK per condition and hand-naming the pulled folder (rather
than driving everything through one build + PerfTestConfig.txt), RecordingConfigOverride never
runs, so every session's Events.csv reports the same perf_preset:Default fallback regardless of
what you actually configured — that marker is NOT usable as the grouping key in that workflow.
It's still read and shown as a cross-check note (a mismatch there is a genuine signal something's
off, e.g. RecordingConfigOverride left in the scene with a stale config), just never trusted over
the folder name.

Self-contained: no dependency on a resxr-python-pipeline checkout. If one exists and already has
session-folder-discovery or CSV-loading utilities, find_session_folders/load_session and the
column-classification heuristic in packed_bytes_estimate are the parts most worth upstreaming
there later — nothing else here assumes that repo exists.
"""

import argparse
import glob
import os
import sys
from dataclasses import dataclass

import numpy as np
import pandas as pd

PERF_SUFFIX = "_ResXRDataPerfSamples.csv"
CONTINUOUS_SUFFIX = "_ContinuousData.csv"
FACE_SUFFIX = "_FaceExpressionData.csv"
EVENTS_SUFFIX = "_Events.csv"

CSV_ENCODING = "utf-8-sig"  # CsvRowWriter writes a UTF-8 BOM

# Startup/scene-load transient excluded from the payload-rate regression, in seconds.
STEADY_STATE_WARMUP_SECONDS = 15.0

# Condition display order for the report; anything else found is appended after these.
PRESET_ORDER = ["FullyOff", "AllOff", "Minimal", "Default", "Maximum"]


@dataclass
class SessionData:
    label: str                  # raw folder name, for reference/debugging
    session_dir: str
    session_time: str
    condition: str               # grouping key: folder name minus the {sessionTime}_ prefix
    events_preset_marker: str    # perf_preset:<name> from Events.csv — cross-check only, see module docstring
    perf: pd.DataFrame
    continuous_row_count: int
    face_row_count: int
    continuous_header: list


def find_session_folders(results_dir):
    session_dirs = {
        os.path.dirname(p)
        for p in glob.glob(os.path.join(results_dir, "**", f"*{PERF_SUFFIX}"), recursive=True)
    }
    return sorted(session_dirs)


def _session_time_from_perf_path(perf_path):
    return os.path.basename(perf_path)[: -len(PERF_SUFFIX)]


def _condition_from_folder(session_dir, session_time):
    """Folder name minus the '{session_time}_' prefix. Falls back to the full folder name if it
    doesn't start with that prefix (e.g. a folder you renamed without the timestamp)."""
    folder_name = os.path.basename(session_dir)
    prefix = f"{session_time}_"
    return folder_name[len(prefix):] if folder_name.startswith(prefix) else folder_name


def _read_preset_from_events(session_dir, session_time):
    events_path = os.path.join(session_dir, f"{session_time}{EVENTS_SUFFIX}")
    if not os.path.exists(events_path):
        return "UNKNOWN"

    events = pd.read_csv(events_path, encoding=CSV_ENCODING)
    preset_rows = events[events["name"].astype(str).str.startswith("perf_preset:")]
    if preset_rows.empty:
        return "UNKNOWN"
    return preset_rows.iloc[0]["name"].split(":", 1)[1]


def _row_count(session_dir, session_time, suffix):
    path = os.path.join(session_dir, f"{session_time}{suffix}")
    if not os.path.exists(path):
        return 0
    # Line count only — Body-on configs push ContinuousData.csv to ~1300 columns, not worth
    # loading fully into memory just to count rows.
    with open(path, "r", encoding=CSV_ENCODING) as f:
        return max(sum(1 for _ in f) - 1, 0)  # -1 for header


def _read_header_columns(session_dir, session_time, suffix):
    path = os.path.join(session_dir, f"{session_time}{suffix}")
    if not os.path.exists(path):
        return []
    return list(pd.read_csv(path, nrows=0, encoding=CSV_ENCODING).columns)


def load_session(session_dir):
    perf_paths = glob.glob(os.path.join(session_dir, f"*{PERF_SUFFIX}"))
    if not perf_paths:
        raise FileNotFoundError(f"No {PERF_SUFFIX} found in {session_dir}")

    perf_path = perf_paths[0]
    session_time = _session_time_from_perf_path(perf_path)
    perf = pd.read_csv(perf_path, encoding=CSV_ENCODING)

    return SessionData(
        label=os.path.basename(session_dir),
        session_dir=session_dir,
        session_time=session_time,
        condition=_condition_from_folder(session_dir, session_time),
        events_preset_marker=_read_preset_from_events(session_dir, session_time),
        perf=perf,
        continuous_row_count=_row_count(session_dir, session_time, CONTINUOUS_SUFFIX),
        face_row_count=_row_count(session_dir, session_time, FACE_SUFFIX),
        continuous_header=_read_header_columns(session_dir, session_time, CONTINUOUS_SUFFIX),
    )


def _safe_ratio(numerator, denominator):
    return numerator / denominator if denominator else float("nan")


def latency_summary(session):
    perf = session.perf
    fixed_ms = perf["FixedUpdate_ns"] / 1e6

    collector_cols = [c for c in perf.columns if c.startswith("Collect_") and c.endswith("_ns")]
    collector_totals_ms = (perf[collector_cols].sum() / 1e6).sort_values(ascending=False)

    return {
        "FixedUpdate_ms_avg": fixed_ms.mean(),
        "FixedUpdate_ms_p95": fixed_ms.quantile(0.95),
        "FixedUpdate_ms_max": fixed_ms.max(),
        "FrameTime_ms_p95_avg": (perf["FrameTime_P95"] * 1000).mean(),
        "FrameTime_ms_max": (perf["FrameTime_Max"] * 1000).max(),
        "CsvFlush_share_of_FixedUpdate": _safe_ratio(perf["CsvFlush_ns"].sum(), perf["FixedUpdate_ns"].sum()),
        "CsvWriteRow_share_of_FixedUpdate": _safe_ratio(perf["CsvWriteRow_ns"].sum(), perf["FixedUpdate_ns"].sum()),
        "collector_ms_total": {
            col.removeprefix("Collect_").removesuffix("_ns"): total
            for col, total in collector_totals_ms.items()
        },
    }


def resource_summary(session):
    perf = session.perf
    window_seconds = perf["timeSinceStartup"].diff().median()
    window_seconds = window_seconds if window_seconds and window_seconds > 0 else 1.0

    return {
        "GC_alloc_KBps_avg": (perf["GCAllocBytesInWindow"] / window_seconds).mean() / 1024,
        "GC_gen0_collections_per_sec_avg": (perf["GCGen0CollectionsInWindow"] / window_seconds).mean(),
        "MonoHeap_MB_start": perf["MonoHeapUsedBytes"].iloc[0] / (1024 * 1024) if len(perf) else float("nan"),
        "MonoHeap_MB_end": perf["MonoHeapUsedBytes"].iloc[-1] / (1024 * 1024) if len(perf) else float("nan"),
    }


def _steady_state(perf):
    """Rows after the startup/scene-load transient, for the byte-rate regression."""
    if len(perf) == 0:
        return perf
    t0 = perf["timeSinceStartup"].iloc[0]
    steady = perf[perf["timeSinceStartup"] - t0 >= STEADY_STATE_WARMUP_SECONDS]
    return steady if len(steady) >= 2 else perf


def packed_bytes_estimate(header_columns):
    """
    Illustrative only, NOT a rigorous benchmark: classifies each column by header-name suffix to
    estimate a packed ("binary") row-size floor, for a CSV-text-vs-packed ratio. Real packed size
    depends on chosen precision/delta-encoding/compression, none of which this accounts for.
    """
    flag_markers = ("_Present", "_Valid_Position", "_Valid_Orientation", "_Tracked_Position",
                     "_Tracked_Orientation", "Status_", "Flags_", "CalibrationStatus")
    packed_bytes = 0
    for col in header_columns:
        packed_bytes += 1 if any(marker in col for marker in flag_markers) else 4  # else: float32
    return packed_bytes


def payload_summary(session):
    perf = session.perf
    result = {}

    for label, file_col, row_count in (
        ("Continuous", "ContinuousFileBytes", session.continuous_row_count),
        ("Face", "FaceFileBytes", session.face_row_count),
    ):
        steady = _steady_state(perf)
        if len(steady) >= 2:
            slope_bytes_per_sec, _ = np.polyfit(steady["timeSinceStartup"], steady[file_col], 1)
        else:
            slope_bytes_per_sec = float("nan")

        final_bytes = perf[file_col].iloc[-1] if len(perf) else 0
        bytes_per_row = _safe_ratio(final_bytes, row_count) if row_count else float("nan")

        entry = {
            "bytes_per_row": bytes_per_row,
            "KBps": slope_bytes_per_sec / 1024,
            "MB_per_min": slope_bytes_per_sec * 60 / (1024 * 1024),
            "MB_per_30min_session": slope_bytes_per_sec * 1800 / (1024 * 1024),
            "final_MB": final_bytes / (1024 * 1024),
            "row_count": row_count,
        }

        if label == "Continuous" and session.continuous_header:
            packed = packed_bytes_estimate(session.continuous_header)
            entry["packed_bytes_estimate"] = packed
            entry["text_vs_packed_ratio"] = _safe_ratio(bytes_per_row, packed)

        result[label] = entry

    return result


def _ordered_conditions(sessions_by_condition):
    order_lookup = {name.lower(): name for name in PRESET_ORDER}
    canonical = [c for c in sessions_by_condition if c.lower() in order_lookup]
    canonical.sort(key=lambda c: PRESET_ORDER.index(order_lookup[c.lower()]))
    rest = sorted(c for c in sessions_by_condition if c.lower() not in order_lookup)
    return canonical + rest


def render_markdown(sessions_by_condition):
    ordered = _ordered_conditions(sessions_by_condition)
    summaries = {}
    for condition in ordered:
        session = sessions_by_condition[condition]
        summaries[condition] = (latency_summary(session), resource_summary(session), payload_summary(session))

    lines = ["# ResXRDataManager Perf Test Report", ""]

    # Cross-check: flag any session whose Events.csv perf_preset marker doesn't loosely match its
    # folder-derived condition name — a mismatch is a real signal something's off (e.g.
    # RecordingConfigOverride left in the scene, applying a stale/wrong preset silently).
    mismatches = [
        (c, sessions_by_condition[c].events_preset_marker)
        for c in ordered
        if sessions_by_condition[c].events_preset_marker.lower() not in (c.lower(), "unknown")
    ]
    if mismatches:
        lines.append("> **Note:** condition labels below come from each session's folder name, not "
                      "`Events.csv` (see script docstring for why). For reference, the "
                      "`perf_preset:` marker logged in `Events.csv` didn't match the folder name for: "
                      + ", ".join(f"`{c}` (logged as `{m}`)" for c, m in mismatches) + ".")
        lines.append("")

    lines += [
        "## Per-condition summary", "",
        "| Condition | FixedUpdate p95 (ms) | Flush share | GC alloc (KB/s) | "
        "Continuous bytes/row | Continuous KB/s | MB / 30-min session | Text-vs-packed ratio |",
        "|---|---|---|---|---|---|---|---|",
    ]
    for condition in ordered:
        lat, res, pay = summaries[condition]
        cont = pay["Continuous"]
        ratio = cont.get("text_vs_packed_ratio", float("nan"))
        lines.append(
            f"| {condition} | {lat['FixedUpdate_ms_p95']:.3f} | {lat['CsvFlush_share_of_FixedUpdate']:.1%} | "
            f"{res['GC_alloc_KBps_avg']:.1f} | {cont['bytes_per_row']:.0f} | {cont['KBps']:.2f} | "
            f"{cont['MB_per_30min_session']:.1f} | {ratio:.1f}x |"
        )

    lines += ["", "## Headline comparisons", ""]
    lower_keys = {c.lower(): c for c in summaries}
    headline_added = False
    if {"default", "alloff", "fullyoff"} <= lower_keys.keys():
        default_fu = summaries[lower_keys["default"]][0]["FixedUpdate_ms_p95"]
        alloff_fu = summaries[lower_keys["alloff"]][0]["FixedUpdate_ms_p95"]
        fulloff_fu = summaries[lower_keys["fullyoff"]][0]["FixedUpdate_ms_p95"]
        lines.append(f"- **Fixed I/O-skeleton tax** (AllOff − FullyOff): {alloff_fu - fulloff_fu:.3f} ms/tick p95.")
        lines.append(f"- **Collector cost** (Default − AllOff): {default_fu - alloff_fu:.3f} ms/tick p95.")
        headline_added = True
    if {"maximum", "default"} <= lower_keys.keys():
        max_fu = summaries[lower_keys["maximum"]][0]["FixedUpdate_ms_p95"]
        default_fu = summaries[lower_keys["default"]][0]["FixedUpdate_ms_p95"]
        lines.append(f"- **Body's marginal cost** (Maximum − Default): {max_fu - default_fu:.3f} ms/tick p95.")
        headline_added = True
    if not headline_added:
        lines.append("_None of the canonical Default/AllOff/FullyOff/Maximum condition names were found "
                      "among your folders, so no automatic comparison could be computed — see the "
                      "per-condition table above instead._")

    lines += ["", "## Per-collector cost breakdown", ""]
    for condition in ordered:
        collector_ms = summaries[condition][0]["collector_ms_total"]
        lines.append(f"### {condition}")
        lines.append("")
        if not collector_ms:
            lines.append("_No active collectors for this condition._")
        else:
            lines.append("| Collector | Total ms over session |")
            lines.append("|---|---|")
            for name, total_ms in collector_ms.items():
                lines.append(f"| {name} | {total_ms:.1f} |")
        lines.append("")

    lines += [
        "## Notes", "",
        "- `bytes_per_row` and row counts come from the actual pulled CSVs, not assumed from tick rate.",
        f"- KB/s and MB/30min are a least-squares slope over the steady-state region only "
        f"(first {STEADY_STATE_WARMUP_SECONDS:.0f}s of each session excluded as a scene-load transient).",
        "- The text-vs-packed ratio (`packed_bytes_estimate`) is illustrative only — a heuristic "
        "column classification, not a rigorous benchmark.",
        "- `_ns` columns in the source CSV are summed elapsed time per ~1s window, not an average — "
        "the actual aggregate cost incurred that second.",
    ]

    return "\n".join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--results-dir", required=True, help="Directory containing pulled session folders (searched recursively).")
    parser.add_argument("--out", default="perf_report.md", help="Output markdown file.")
    args = parser.parse_args()

    session_dirs = find_session_folders(args.results_dir)
    if not session_dirs:
        print(f"No *{PERF_SUFFIX} files found under {args.results_dir}", file=sys.stderr)
        sys.exit(1)

    sessions_by_condition = {}
    for session_dir in session_dirs:
        session = load_session(session_dir)
        if session.condition in sessions_by_condition:
            print(f"Warning: multiple sessions found for condition '{session.condition}' — "
                  f"using {session_dir}, ignoring {sessions_by_condition[session.condition].session_dir}.", file=sys.stderr)
        sessions_by_condition[session.condition] = session

    report = render_markdown(sessions_by_condition)
    with open(args.out, "w", encoding="utf-8") as f:
        f.write(report)

    print(f"Wrote {args.out} ({len(sessions_by_condition)} condition(s): {', '.join(sessions_by_condition)})")


if __name__ == "__main__":
    main()
