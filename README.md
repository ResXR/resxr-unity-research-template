# ResXR Unity Research Template

A Unity 6 project template for building XR behavioral experiments that run standalone on Meta Quest headsets. Records head, hand, eye, face, and body tracking at 100 Hz (Unity's `FixedUpdate`) alongside experiment events and custom data tables, writing everything to per-session CSV files and a `SessionMetadata.json` sidecar. A persistent Base Scene holds `ResXRPlayer` (tracking) and `ResXRDataManager` (recording); experiment scenes load additively on top and are organized with a Session → Task → Trial flow hierarchy. It is a "clear box" template: researchers own and modify every script directly rather than working around opaque base classes.

This template is the recording half of ResXR; the per-session packages it produces are processed by the [`resxr-python-pipeline`](https://github.com/ResXR/resxr-python-pipeline) into Motion-BIDS datasets. The two share no code — only the [output file format](https://docs.resxr.org/unity/data-output/).

## 📖 Full documentation

**[https://docs.resxr.org/unity/](https://docs.resxr.org/unity/)** — installation, quickstart, paradigms, recording options, data output, architecture, scripting API, and extension guide. The pages below are the source of truth; this README is only a quickstart.

## Install

1. Click **"Use this template"** on GitHub and clone your new repository.
2. Open Unity Hub, click **Add project from disk**, and select the folder.
   Unity 6000.0.68f1 with Android Build Support is required.
   Runs on Meta Quest 2/3/Pro; **eye and face tracking require Quest Pro.**
3. On first launch Unity auto-installs the Meta XR SDK (v 78.0.0) and all other package dependencies — no manual steps needed.

See [Installation](https://docs.resxr.org/unity/installation/) for eye/face tracking setup on Quest Pro and Quest Link / Meta XR Simulator configuration.

## Run

Open `Assets/ResXR/Base Scene/Base Scene.unity`, enable your target experiment in **File → Build Profiles** (Base Scene must be listed first), then press **Play** in the editor or use **File → Build And Run** for a device build.

Session data is written to `…/Temp/ResXR_EditorLogs/` in the editor, or `Application.persistentDataPath` (`/sdcard/Android/data/<bundle.id>/files/`) on device. Feed the session folder to `resxr-python-pipeline` for BIDS conversion.

Follow the [Quickstart](https://docs.resxr.org/unity/quickstart/) for a full walkthrough.

## Documentation map

| Topic | Page |
| ----- | ---- |
| Project setup and SDK configuration | [Installation](https://docs.resxr.org/unity/installation/) |
| First experiment walkthrough | [Quickstart](https://docs.resxr.org/unity/quickstart/) |
| Included demo experiments (Binary Choice, Maze, Museum) | [Paradigms](https://docs.resxr.org/unity/paradigms/) |
| Recording subsystems and toggle reference | [Recording](https://docs.resxr.org/unity/recording/) |
| Output folder layout, CSV/JSON schema | [Data Output](https://docs.resxr.org/unity/data-output/) |
| Base Scene, ResXRPlayer, ResXRDataManager, flow hierarchy | [Architecture](https://docs.resxr.org/unity/architecture/) |
| `ReportEvent`, `LogCustom`, `ResXRPlayer` API, flow hooks | [Scripting & API](https://docs.resxr.org/unity/scripting/) |
| Custom collectors, multi-experiment projects, performance tuning | [Extending the Template](https://docs.resxr.org/unity/extending/) |

## License

Apache License 2.0 — see [LICENSE](LICENSE).

The Meta XR SDK (auto-installed via UPM) is provided by Meta Platform Technologies, LLC under the Meta SDK License Agreement and is not covered by Apache 2.0.

Additional vendored libraries (UniTask, NaughtyAttributes, DOTween) are documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Contributing

Contributions are welcome. Fork the repository, create a feature branch, and open a pull request.

## Acknowledgments

ResXR builds on the early work of the [TAUXR Research Template](https://github.com/TAU-XR/TAUXR-Research-Template) by [TAU-XR Studio](https://github.com/TAU-XR) and [talmzip](https://github.com/talmzip).

## Citation

```bibtex
@software{resxr,
  title = {ResXR: XR Experiment Recording Template},
  year  = {2026},
  url   = {https://github.com/ResXR/resxr-unity-research-template}
}
```

## Support

**Email**: [resxr.toolkit@gmail.com](mailto:resxr.toolkit@gmail.com)
