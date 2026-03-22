# Tracking vs World Space Test Script

## Purpose

This test script empirically verifies the relationship between **tracking space** and **world space** in VR applications. It helps you understand:

- How OVRPlugin tracking space coordinates relate to Unity world space
- What happens when the OVRCameraRig is positioned at different locations
- How recenter events affect coordinate systems

## Quick Start

### 1. Setup

1. Open your VR scene with an OVRCameraRig
2. Create an empty GameObject (e.g., "TrackingSpaceTest")
3. Add the `TrackingSpaceTest` component
4. In the Inspector, drag the **CenterEyeAnchor** from your OVRCameraRig hierarchy into the `Center Eye Anchor` field

### 2. Run Test

1. Press Play
2. Move around in VR (walk forward, backward, turn, etc.)
3. (Optional) Trigger a recenter event using the Oculus button
4. Press **Escape** to stop recording and save the CSV

### 3. Analyze Results

The CSV file will be saved to `Application.persistentDataPath` by default (check Unity console for exact path).

Open the CSV in Excel, Python, or your preferred tool to analyze:

**OVRPlugin Data (Right-Handed Tracking Space):**
- `HeadNode_x/y/z` - Position in tracking space (raw from OVRPlugin.GetNodePose)
- `TrackingOrigin_x/y/z` - Tracking origin from OVRPlugin.GetTrackingCalibratedOrigin()
- `TrackingOriginType` - Tracking origin type (EyeLevel, FloorLevel, Stage)

**Unity Transform Data (Left-Handed World Space):**
- `CenterEye_x/y/z` - CenterEyeAnchor world position (Transform.position)
- `TrackingSpace_x/y/z` - TrackingSpace (parent) world position
- `CameraRig_x/y/z` - OVRCameraRig (grandparent) world position

**Calculated Values:**
- `Diff_x/y/z` - Difference: CenterEye - HeadNode (shows coordinate conversion + offset)
- `Diff_Magnitude` - Distance between tracking and world positions

**Recenter Detection:**
- `shouldRecenter` - Current recenter signal (0 or 1)
- `recenterEvent` - Flags when recenter happens (1 on rising edge)

**Note:** All Unity Transform values use `.position` (world space), NOT `.localPosition`

## Settings

### Public Fields (Inspector)

- **Center Eye Anchor** - Assign the CenterEyeAnchor transform from OVRCameraRig
- **Is Recording** - Toggle to pause/resume recording without stopping
- **Stop Key** - Key to stop and save (default: Escape)
- **Save Path** - Custom save location (leave empty for Application.persistentDataPath)

## Expected Results

### Test Case 1: OVRCameraRig at Origin (0, 0, 0)

When your OVRCameraRig is at world position (0, 0, 0):

```
HeadNode position ≈ CenterEye position
Diff ≈ (0, 0, 0)
```

**What this means:** Tracking space and world space are aligned. Your head position in tracking space equals your head position in world space.

### Test Case 2: OVRCameraRig Offset (e.g., at 5, 2, 10)

When your OVRCameraRig is at world position (5, 2, 10):

```
CenterEye position = HeadNode position + (5, 2, 10)
Diff ≈ (5, 2, 10) - constant offset
```

**What this means:** There's a fixed offset between tracking and world space. The CenterEyeAnchor's world position is the tracking space position plus the rig's offset.

### Test Case 3: After Recenter Event

When you trigger a recenter (Oculus button):

```
Before recenter:
- HeadNode: (0.5, 1.62, 2.0)
- shouldRecenter: 0

During recenter frame:
- shouldRecenter: 1
- recenterEvent: 1  ← Rising edge!

After recenter:
- HeadNode: (~0, 1.6, ~0) ← Reset to origin!
- CenterEye: may teleport or stay depending on setup
- Diff: changes to new offset
- recenterEvent: 0 ← Back to normal
```

**What this means:** Tracking space resets to origin, but world space positions depend on whether your OVRCameraRig compensates for the recenter. The `Diff` column shows this clearly.

## Understanding Coordinate Spaces

### Tracking Space
- **Origin:** Set at app start or when user recenters
- **Reference:** VR hardware's internal coordinate system
- **Behavior:** Resets when user recenters
- **Data source:** OVRPlugin.GetNodePose()

### World Space
- **Origin:** Fixed at Unity scene origin (0, 0, 0)
- **Reference:** Your Unity scene's global coordinates
- **Behavior:** Never resets (unless you move the rig)
- **Data source:** Unity Transform.position

### The Offset
The difference between them (`Diff` columns) represents the OVRCameraRig's world position. This is the "secret offset" that makes tracking space ≠ world space!

## Common Use Cases

### Use Case 1: Verify Static Rig Setup
**Goal:** Confirm tracking and world space are aligned

1. Set OVRCameraRig to (0, 0, 0)
2. Run test
3. Check CSV: `Diff_Magnitude` should be ~0.0

### Use Case 2: Test Offset Rig
**Goal:** Understand coordinate transformation

1. Move OVRCameraRig to (10, 0, 5)
2. Run test
3. Check CSV: `Diff_x` ≈ 10, `Diff_z` ≈ 5

### Use Case 3: Analyze Recenter Behavior
**Goal:** See what happens during recenter

1. Run test
2. Walk around for a few seconds
3. Press Oculus button to recenter
4. Continue moving
5. Stop recording
6. In CSV, find row where `recenterEvent=1`
7. Compare HeadNode positions before and after

## Tips for Analysis

### Python Example

```python
import pandas as pd

# Load CSV
df = pd.read_csv('TrackingSpaceTest_2026-01-14_17-30-45.csv')

# Find recenter events
recenter_frames = df[df['recenterEvent'] == 1]
print(f"Recenter events: {len(recenter_frames)}")

# Plot difference over time
import matplotlib.pyplot as plt
plt.plot(df['timeSinceStartup'], df['Diff_Magnitude'])
plt.xlabel('Time (s)')
plt.ylabel('Tracking-World Offset (m)')
plt.title('Coordinate Space Offset Over Time')
plt.show()

# Check if rig is static
diff_std = df['Diff_x'].std()
print(f"Diff_x std dev: {diff_std:.6f} (< 0.001 = static rig)")
```

### Excel Analysis

1. Create scatter plot: X-axis = `timeSinceStartup`, Y-axis = `Diff_Magnitude`
2. Add conditional formatting to highlight `recenterEvent=1` rows
3. Calculate average of `Diff_x/y/z` to find rig offset

## Troubleshooting

### "CenterEyeAnchor is not assigned!"
- Make sure you dragged the CenterEyeAnchor transform into the Inspector field
- The CenterEyeAnchor should be under: OVRCameraRig > TrackingSpace > CenterEyeAnchor

### "No data to save"
- Recording didn't start or was disabled
- Check that `isRecording` is checked in Inspector

### File not found after saving
- Check Unity console for the exact save path
- By default, it saves to `Application.persistentDataPath`:
  - Windows: `C:\Users\<username>\AppData\LocalLow\<company>\<project>\`
  - Quest (Android): `/storage/emulated/0/Android/data/<package>/files/`

### All Diff values are 0
- This is normal if your OVRCameraRig is at world origin (0, 0, 0)
- Try moving the rig to (5, 0, 0) to see a non-zero offset

### shouldRecenter always 0
- This is normal on some platforms/backends
- OpenXR may not expose this flag
- Try manually recentering with Oculus button if available

## Understanding the Offset: Where Does It Come From?

The CSV now includes multiple data sources to empirically verify the coordinate transformation:

### Transform Hierarchy
```
OVRCameraRig (CameraRig_x/y/z)
  └─ TrackingSpace (TrackingSpace_x/y/z) 
      └─ CenterEyeAnchor (CenterEye_x/y/z)
```

### Theory 1: Offset is Pure Unity Transform Hierarchy ✓ (Most Likely)
**Expected observations:**
- `TrackingOrigin` = (0, 0, 0) always (OVRPlugin doesn't know about Unity transforms)
- `CameraRig` world position = the offset you set in Inspector
- `TrackingSpace` world position = `CameraRig` position (same, since TrackingSpace.localPosition = 0)
- `CenterEye` world position = `CameraRig` position + head movement (after Z-flip)
- `Diff` ≈ `CameraRig` position (after accounting for Z-axis flip)

### Theory 2: Offset is in OVRPlugin Internal State
**Expected observations:**
- `TrackingOrigin` changes when you move OVRCameraRig
- `TrackingOrigin` ≈ negative of `CameraRig` position
- `Diff` might be calculated from TrackingOrigin

### Test Plan

**Run 1: OVRCameraRig at (0, 0, 0)**
```
Expected if Theory 1:
  CameraRig     = (0, 0, 0)
  TrackingSpace = (0, 0, 0)
  TrackingOrigin = (0, 0, 0)
  Diff ≈ (0, 0, 0)
```

**Run 2: OVRCameraRig at (5, 0, 3)**
```
Expected if Theory 1:
  CameraRig     = (5, 0, 3)
  TrackingSpace = (5, 0, 3)
  TrackingOrigin = (0, 0, 0)  ← Stays zero!
  Diff_x = 5, Diff_z = -3 (Z-flip!)

Expected if Theory 2:
  CameraRig     = (5, 0, 3)
  TrackingOrigin = (-5, 0, -3) or similar
  Diff calculated from TrackingOrigin
```

### What to Compare

1. **CameraRig vs TrackingSpace**: Should be identical (TrackingSpace has no local offset)
2. **CameraRig vs TrackingOrigin**: Does OVRPlugin track the rig position?
3. **Diff vs CameraRig**: After Z-flip, does `Diff_z = -CameraRig_z`?
4. **CenterEye vs CameraRig**: CenterEye should = CameraRig + (HeadNode with Z-flipped)

## See Also

- `data_sources_README.txt` - Full documentation of all data sources and coordinate spaces
- ResXRDataManager_V2 - Production data logging system

## License

Part of the ResXR Research Template.
