# CONTINUOUSDATA - column <- source
# NOTE: All columns are float unless otherwise specified. Types noted as: (int), (double), (string), (bool as 0/1)

# - Timing (Unity clock kept for continuity) -
# [Collector: ResXRDataManager (direct)]
timeSinceStartup                          <- Unity: Time.realTimeSinceStartup                        (double: seconds since app start)

# ============================================================
# COORDINATE SPACE CONVERSION
# ============================================================
# NOTE: All OVRPlugin APIs return data in TRACKING SPACE (right-handed coordinates,
# relative to VR play area origin). This system automatically converts spatial data
# to UNITY WORLD SPACE (left-handed coordinates, Unity scene's global system) using
# TrackingSpaceConverter.ToWorldSpacePosition() and ToWorldSpaceRotation().
#
# This conversion:
#   1. Applies Z-axis flip (right-handed → left-handed)
#   2. Rotates by OVRCameraRig's rotation
#   3. Adds OVRCameraRig's position offset
#
# Velocities and angular velocities are rotated (steps 1-2) but NOT offset.
# Hand bones remain in hand-local space (relative to hand root).
# ============================================================

# - Gaze: combined (cyclopean) hit point / raycast -
# [Collector: OVREyesCollector]
# ResXREyeTracker always runs the combined ray when BOTH eyes have confidence >= threshold.
#   - Origin: midpoint between left and right eye positions
#   - Direction: normalized sum of left and right gaze directions (eye transform forward)
# When either eye is below threshold, FocusedObject and EyeGazeHitPosition are cleared (no raycast).
FocusedObject                              <- ResXRPlayer.Instance.FocusedObject                      (string: GameObject name or empty "")
EyeGazeHitPosition_X                       <- ResXRPlayer.Instance.EyeGazeHitPosition.x  [world space]
EyeGazeHitPosition_Y                       <- ResXRPlayer.Instance.EyeGazeHitPosition.y  [world space]
EyeGazeHitPosition_Z                       <- ResXRPlayer.Instance.EyeGazeHitPosition.z  [world space]

# - Gaze: per-eye hit points and focused objects (optional) -
# [Collector: OVREyesCollector; columns only when RecordingOptions.includeSeparateEyesGaze is true]
# When this option is enabled, ResXREyeTracker runs up to 3 raycasts per frame:
#   (1) left eye: from LeftEye position along LeftEye.forward
#   (2) right eye: from RightEye position along RightEye.forward
#   (3) combined: as above (always run when both eyes confident)
# The Data Manager sets EyeTracker.EnableSeparateEyeRaycasts from this option at Start();
# when false, only the combined ray runs (1 raycast), which can help performance in heavy scenes.
LeftEyeGazeHitPosition_X                    <- ResXRPlayer.Instance.EyeTracker.LeftEyeGazeHitPosition.x  [world space]
LeftEyeGazeHitPosition_Y                    <- ResXRPlayer.Instance.EyeTracker.LeftEyeGazeHitPosition.y  [world space]
LeftEyeGazeHitPosition_Z                    <- ResXRPlayer.Instance.EyeTracker.LeftEyeGazeHitPosition.z  [world space]
RightEyeGazeHitPosition_X                   <- ResXRPlayer.Instance.EyeTracker.RightEyeGazeHitPosition.x [world space]
RightEyeGazeHitPosition_Y                   <- ResXRPlayer.Instance.EyeTracker.RightEyeGazeHitPosition.y [world space]
RightEyeGazeHitPosition_Z                   <- ResXRPlayer.Instance.EyeTracker.RightEyeGazeHitPosition.z [world space]
LeftFocusedObject                           <- ResXRPlayer.Instance.EyeTracker.LeftFocusedObject          (string: name or "")
RightFocusedObject                          <- ResXRPlayer.Instance.EyeTracker.RightFocusedObject        (string: name or "")
HasLeftEyeHit                               <- ResXRPlayer.Instance.EyeTracker.HasLeftEyeHit             (bool as 0/1)
HasRightEyeHit                              <- ResXRPlayer.Instance.EyeTracker.HasRightEyeHit            (bool as 0/1)

# - Eye gazes (dedicated API) -
# [Collector: OVREyesCollector]
RightEye_qx                                <- OVRPlugin.GetEyeGazesState(step, frameIndex, ref state).EyeGazes[(int)OVRPlugin.Eye.Right].Pose.Orientation.x  [converted to world space]
RightEye_qy                                <- ...EyeGazes[(int)OVRPlugin.Eye.Right].Pose.Orientation.y  [converted to world space]
RightEye_qz                                <- ...EyeGazes[(int)OVRPlugin.Eye.Right].Pose.Orientation.z  [converted to world space]
RightEye_qw                                <- ...EyeGazes[(int)OVRPlugin.Eye.Right].Pose.Orientation.w  [converted to world space]
LeftEye_qx                                 <- ...EyeGazes[(int)OVRPlugin.Eye.Left].Pose.Orientation.x  [converted to world space]
LeftEye_qy                                 <- ...EyeGazes[(int)OVRPlugin.Eye.Left].Pose.Orientation.y  [converted to world space]
LeftEye_qz                                 <- ...EyeGazes[(int)OVRPlugin.Eye.Left].Pose.Orientation.z  [converted to world space]
LeftEye_qw                                 <- ...EyeGazes[(int)OVRPlugin.Eye.Left].Pose.Orientation.w  [converted to world space]
LeftEye_IsValid                            <- state.EyeGazes[(int)OVRPlugin.Eye.Left].IsValid         (bool as 0/1)
LeftEye_Confidence                         <- state.EyeGazes[(int)OVRPlugin.Eye.Left].Confidence      (float: confidence value, typically 0.0 to 1.0)
RightEye_IsValid                           <- state.EyeGazes[(int)OVRPlugin.Eye.Right].IsValid        (bool as 0/1)
RightEye_Confidence                        <- state.EyeGazes[(int)OVRPlugin.Eye.Right].Confidence     (float: confidence value, typically 0.0 to 1.0)
Eyes_Time                                  <- state.Time                                              (double: seconds; shared timestamp for both eyes)

# - System Status (recenter, tracking space, user presence, tracking loss) -
# [Collector: SystemStatusCollector]
# NOTE: Collector renamed from RecenterCollector
shouldRecenter                             <- OVRPlugin.shouldRecenter                                (bool as 0/1 if available, else empty)
recenterEvent                              <- Derived: rising edge of shouldRecenter                  (bool as 0/1: pulse on 0->1 transition)
RecenterCount                              <- OVRPlugin.GetLocalTrackingSpaceRecenterCount()         (int: cumulative counter, increments on each recenter)
TrackingOriginChange_Event                 <- OVRManager.TrackingOriginChangePending event           (bool as 0/1: pulse on recenter event - most precise detection)
TrackingOriginChange_PrevPose_px/_py/_pz   <- Event: poseInPreviousSpace.Position.{x,y,z}            [converted to world space]
TrackingOriginChange_PrevPose_qx/_qy/_qz/_qw <- Event: poseInPreviousSpace.Orientation.{x,y,z,w}    [converted to world space]
                                              (previous tracking origin pose before recenter, only written when event fires)
TrackingTransform_px/_py/_pz               <- GetTrackingTransformRawPose().Position.{x,y,z}         [converted to world space]
TrackingTransform_qx/_qy/_qz/_qw           <- GetTrackingTransformRawPose().Orientation.{x,y,z,w}    [converted to world space]
                                              (tracking space origin pose in world coordinates; polled every frame)
UserPresent                                <- OVRPlugin.userPresent                                   (bool as 0/1: true when user wears headset)
TrackingLost                               <- !GetNodePositionTracked(Node.EyeCenter)                (bool as 0/1: true when SLAM tracking is lost)

# - Device nodes -
# [Collector: OVRNodesCollector]
# For each node, the following fields are collected:
Node_<Node>_Present                        <- OVRPlugin.GetNodePresent(<Node>)                        (bool as 0/1)
Node_<Node>_px / _py / _pz                 <- GetNodePose(<Node>, step).Position.{x,y,z}              [converted to world space]
Node_<Node>_qx / _qy / _qz / _qw           <- ...Posef.Orientation.{x,y,z,w}                          [converted to world space]
                                              (collected for: EyeCenter, Head, HandLeft, HandRight, ControllerLeft, ControllerRight)
Node_<Node>_Valid_Position                 <- OVRPlugin.GetNodePositionValid(<Node>)                  (bool as 0/1)
Node_<Node>_Valid_Orientation              <- OVRPlugin.GetNodeOrientationValid(<Node>)               (bool as 0/1)
Node_<Node>_Tracked_Position               <- OVRPlugin.GetNodePositionTracked(<Node>)                (bool as 0/1)
Node_<Node>_Tracked_Orientation            <- OVRPlugin.GetNodeOrientationTracked(<Node>)             (bool as 0/1)
Node_<Node>_Time                           <- GetNodePoseStateRaw(<Node>, step).Time                  (double: seconds)

# Node list: EyeCenter, Head, HandLeft, HandRight, ControllerLeft, ControllerRight
# (EyeLeft, EyeRight removed - use dedicated eye gaze API from OVREyesCollector instead)

# - Hands (dedicated API; LEFT then RIGHT) -
# [Collector: OVRHandsCollector]
LeftHand_Status_HandTracked                <- OVRPlugin.GetHandState(step, Hand.HandLeft).Status      (bool as 0/1: HandTracked flag)
LeftHand_Status_InputStateValid            <- OVRPlugin.GetHandState(step, Hand.HandLeft).Status      (bool as 0/1: InputStateValid flag)
LeftHand_Status_SystemGestureInProgress    <- OVRPlugin.GetHandState(step, Hand.HandLeft).Status      (bool as 0/1: SystemGestureInProgress flag)
LeftHand_Status_DominantHand               <- OVRPlugin.GetHandState(step, Hand.HandLeft).Status      (bool as 0/1: DominantHand flag)
LeftHand_Status_MenuPressed                <- OVRPlugin.GetHandState(step, Hand.HandLeft).Status      (bool as 0/1: MenuPressed flag)
                                                                                                       Note: Multiple flags can be 1 simultaneously (flags can be combined)
LeftHand_Root_px/_py/_pz                   <- HandState.RootPose.Position.{x,y,z}                     [converted to world space]
LeftHand_Root_qx/_qy/_qz/_qw               <- HandState.RootPose.Orientation.{x,y,z,w}                [converted to world space]
LeftHand_HandScale                         <- HandState.HandScale
LeftHand_HandConfidence                    <- HandState.HandConfidence                                (bool as 0/1: 0=Low, 1=High)
LeftHand_FingerConf_Thumb                  <- HandState.FingerConfidences[Thumb]                      (bool as 0/1: 0=Low, 1=High)
LeftHand_FingerConf_Index                  <- HandState.FingerConfidences[Index]                      (bool as 0/1: 0=Low, 1=High)
LeftHand_FingerConf_Middle                 <- HandState.FingerConfidences[Middle]                     (bool as 0/1: 0=Low, 1=High)
LeftHand_FingerConf_Ring                   <- HandState.FingerConfidences[Ring]                       (bool as 0/1: 0=Low, 1=High)
LeftHand_FingerConf_Pinky                  <- HandState.FingerConfidences[Pinky]                      (bool as 0/1: 0=Low, 1=High)
LeftHand_RequestedTS                       <- HandState.RequestedTimeStamp                            (double: seconds)
LeftHand_SampleTS                          <- HandState.SampleTimeStamp                               (double: seconds)

# NOTE ON HAND ROOT vs PALM BONE:
# - LeftHand_Root represents the hand root pose in WORLD SPACE (converted from tracking space)
# - Left_XRHand_Palm (bone[0] below) represents the same physical pose but in HAND-LOCAL SPACE (relative to root)
# - The Palm bone in local space will typically be at origin (0,0,0) with identity rotation since it IS the root
# - Both Node_HandLeft and LeftHand_Root provide world-space hand pose but may differ slightly (different APIs)

# Per-bone (columns named by SDK BoneId enum; order = SDK bone enum order)
Left_<BoneName>_x/_y/_z                    <- HandState.BonePositions[boneIndex].{x,y,z}  (e.g., Left_Hand_Thumb0_x or Left_XRHand_Thumb_Metacarpal_x)  [hand-local space]
Left_<BoneName>_qx/_qy/_qz/_qw             <- HandState.BoneRotations[boneIndex].{x,y,z,w}  [hand-local space]

RightHand_*                                <- Same fields as LeftHand (Hand.HandRight)

# - Body (BodyState4; frame + joints) -
# [Collector: OVRBodyCollector]
Body_Time                                   <- OVRPlugin.GetBodyState4(step,...).Time                 (double: seconds)
Body_Confidence                             <- state.Confidence
Body_Fidelity                               <- state.Fidelity                                          (bool as 0/1: 0=Low, 1=High)
Body_CalibrationStatus_Invalid             <- state.CalibrationStatus                                 (bool as 0/1: Invalid flag)
Body_CalibrationStatus_Calibrating          <- state.CalibrationStatus                                 (bool as 0/1: Calibrating flag)
Body_CalibrationStatus_Valid                <- state.CalibrationStatus                                 (bool as 0/1: Valid flag)
                                                                                                       Note: Only one calibration status flag can be 1 at a time (mutually exclusive)
Body_SkeletonChangedCount                   <- state.SkeletonChangedCount                              (int: increments when skeleton changes)

# For each joint J in SDK BoneId enum order (names starting with "Body_"):
<Body_JointName>_px/_py/_pz                          <- state.JointLocations[jointIndex].Pose.Position.{x,y,z}  (e.g., Body_Root_px, Body_Hips_px)  [converted to world space]
<Body_JointName>_qx/_qy/_qz/_qw                      <- state.JointLocations[jointIndex].Pose.Orientation.{x,y,z,w}  [converted to world space]
<Body_JointName>_Flags_OrientationValid              <- state.JointLocations[jointIndex].LocationFlags          (bool as 0/1: OrientationValid flag - orientation can be used)
<Body_JointName>_Flags_PositionValid                 <- state.JointLocations[jointIndex].LocationFlags          (bool as 0/1: PositionValid flag - position can be used)
<Body_JointName>_Flags_OrientationTracked            <- state.JointLocations[jointIndex].LocationFlags          (bool as 0/1: OrientationTracked flag - orientation actively tracked)
<Body_JointName>_Flags_PositionTracked               <- state.JointLocations[jointIndex].LocationFlags          (bool as 0/1: PositionTracked flag - position actively tracked)
                                                                                                                  Note: Multiple flags can be 1 simultaneously (flags can be combined)

# - Custom transforms (experiment-specific; registered order) -
# [Collector: CustomTransformsCollector]
Custom_<Name>_px/_py/_pz                    <- Unity Transform.position.{x,y,z}  [world space]
Custom_<Name>_qx/_qy/_qz/_qw                <- Unity Transform.rotation.{x,y,z,w}  [world space]


# Coordinate Space Definitions:
# - [converted to world space]: Originally from OVRPlugin in tracking space, converted to Unity world space via TrackingSpaceConverter
# - [world space]: Native Unity world space data (Transform.position, raycast hits, etc.)
# - [hand-local space]: Relative to the hand's root (wrist). Each hand bone is relative to its root pose.


# FACEEXPRESSIONS - column <- source

# - Timing -
# [Collector: ResXRDataManager (direct)]
timeSinceStartup                          <- Unity: Time.realTimeSinceStartup                        (double: seconds since app start; renamed from "TimeFromStart")

# - Face state (dedicated API: FaceState2) -
# [Collector: OVRFaceCollector]
Face_Time                                 <- FaceState.Time                                          (double: seconds, SDK timestamp)
Face_Status                               <- FaceState.Status.IsValid                               (bool as 0/1)

# - Expression weights (OVRPlugin.FaceExpression2 names, one per entry) -
# [Collector: OVRFaceCollector]
Brow_Lowerer_L ... Tongue_Retreat           <- FaceState.ExpressionWeights[i]                         (float: 70 expression weights between 0.0 and 1.0, one per enum entry)

# - Region confidences (OVRPlugin.FaceRegionConfidence) -
# [Collector: OVRFaceCollector]
FaceRegionConfidence_Upper                  <- FaceState.ExpressionWeightConfidences[1]               (float: confidence value, typically 0.0 to 1.0)
FaceRegionConfidence_Lower                  <- FaceState.ExpressionWeightConfidences[0]               (float: confidence value, typically 0.0 to 1.0)


# Notes:
# - Indices map to the FaceExpression enum order in the MetaXR v78 SDK guide.
# - Total per frame = 70 weights + 2 confidences + status + time + timeSinceStartup.

