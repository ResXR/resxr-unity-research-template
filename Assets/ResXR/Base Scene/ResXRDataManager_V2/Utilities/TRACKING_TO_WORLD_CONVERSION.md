# Converting OVRPlugin Tracking Space to Unity World Space

## The Problem

You want to:
1. ✅ Use `OVRPlugin.GetNodePose()` / `GetNodePoseStateRaw()` (for precise timestamps)
2. ✅ Get positions in **Unity world space** (for scene context)
3. ❌ NOT rely on Unity Transform hierarchy (which may update at different times)

## The Solution: `TrackingSpaceConverter`

This utility converts OVRPlugin tracking space coordinates to Unity world space **without** using Transform updates.

### Key Benefits

- **Precise Timestamps**: Use OVRPlugin's `PoseStatef.Time` directly
- **World Space Positions**: Converted for scene context (raycast hits, object positions, etc.)
- **No Transform Dependency**: Direct conversion using cached rig position
- **Both Coordinate Systems**: Can log both tracking space (raw) and world space (converted)

## How It Works

The conversion has 3 steps:

### Step 1: Coordinate System Conversion (Z-Flip)
```csharp
// OVRPlugin returns RIGHT-HANDED coordinates
// Unity uses LEFT-HANDED coordinates
Vector3 unityCoords = new Vector3(
    trackingPose.Position.x,
    trackingPose.Position.y,
    -trackingPose.Position.z  // ← Z-FLIP
);
```

### Step 2: Apply Rig Rotation
```csharp
// If OVRCameraRig is rotated in scene
Vector3 rotated = rigRotation * unityCoords;
```

### Step 3: Apply Rig Position Offset
```csharp
// Add OVRCameraRig's world position
Vector3 worldSpace = rigPosition + rotated;
```

## Usage

### 1. Initialize at Startup

In `ResXRDataManager_V2.Awake()`:

```csharp
OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
TrackingSpaceConverter.Initialize(cameraRig.trackingSpace);
```

### 2. Convert in Collectors

```csharp
// Get raw pose with timestamp from OVRPlugin
PoseStatef headState = OVRPlugin.GetNodePoseStateRaw(Node.Head, Step.Render);

// Convert to world space
Vector3 worldPos = TrackingSpaceConverter.ToWorldSpacePosition(headState.Pose);

// Log both!
row.Set(_idxHeadTrackingX, headState.Pose.Position.x);  // Raw tracking space
row.Set(_idxHeadWorldX, worldPos.x);                     // World space
row.Set(_idxHeadTime, headState.Time);                   // OVRPlugin timestamp
```

### 3. Update Cache (Optional)

If your OVRCameraRig moves during gameplay:

```csharp
void Update()
{
    TrackingSpaceConverter.UpdateCache();  // Update rig position
}
```

## Recommended Schema Changes

### Option A: Add World Space Columns (Keep Tracking Space)

```
# Current (tracking space only):
Head_Position_x    ← trackingPose.Position.x
Head_Height        ← trackingPose.Position.y  
Head_Position_z    ← trackingPose.Position.z

# Add world space columns:
Head_World_x       ← TrackingSpaceConverter.ToWorldSpacePosition(pose).x
Head_World_y       ← ...
Head_World_z       ← ...
Head_Time          ← poseState.Time (from OVRPlugin)
```

### Option B: Replace with World Space + Add Rig Offset

```
# World space positions:
Head_Position_x    ← TrackingSpaceConverter.ToWorldSpacePosition(pose).x
Head_Height        ← ...
Head_Position_z    ← ...

# Add rig offset columns (once per frame, not per node):
RigOffset_x        ← TrackingSpaceConverter.GetRigOffset().x
RigOffset_y        ← ...
RigOffset_z        ← ...
```

This way you can reconstruct tracking space if needed: `tracking = world - rigOffset` (with Z-flip)

## Verification

Test with your `TrackingSpaceTest.cs`:

```csharp
Posef headPose = OVRPlugin.GetNodePose(Node.Head, Step.Render);

// Method 1: Unity Transform (what you tested)
Vector3 method1 = centerEyeAnchor.position;

// Method 2: TrackingSpaceConverter (new)
Vector3 method2 = TrackingSpaceConverter.ToWorldSpacePosition(headPose);

// Should match!
Debug.Log($"Method 1: {method1}, Method 2: {method2}, Diff: {(method1-method2).magnitude}");
```

Expected: `Diff < 0.001m` (sub-millimeter precision)

## Performance

- **Initialization**: Once per session (negligible)
- **UpdateCache**: 1 Vector3 + 1 Quaternion read per frame (negligible)
- **Conversion**: 1 vector multiply + 1 vector add per pose (~5 CPU ops)

Much faster than Transform hierarchy traversal!

## Coordinate Spaces Summary

| Data Source | Coordinate System | When to Use |
|-------------|-------------------|-------------|
| `OVRPlugin.GetNodePose().Position` | Right-handed tracking space | Raw VR hardware data |
| `Transform.position` | Left-handed Unity world space | Scene objects, raycasts |
| `TrackingSpaceConverter.ToWorldSpacePosition()` | Left-handed Unity world space | OVRPlugin data in world coordinates |

## See Also

- `TrackingSpaceConverter.cs` - The converter implementation
- `TrackingSpaceConverter_Example.cs` - Usage examples
- `data_sources_README.txt` - Current schema documentation
- `OVRCommon.cs:236` - The Z-flip conversion reference
