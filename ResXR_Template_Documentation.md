# ResXR Research Template - Component Documentation

## Table of Contents

1. [Introduction](#introduction)
2. [Core Components](#core-components)
   - [Base Scene](#21-base-scene)
   - [ResXRPlayer](#211-ResXRPlayer)
   - [ResXRSceneManager](#212-ResXRSceneManager)
   - [ResXRDataManager_V2](#213-ResXRDataManager_V2)
   - [ResXR_RoomCalibrator](#214-ResXR_RoomCalibrator)
   - [ResXR Scene Template](#215-ResXR-scene-template)
3. [Flow Management System](#3-flow-management-system)
4. [Utilities](#4-utilities)
   - [General Scripts](#41-general-scripts)
   - [Shared Utils](#42-shared-utils-demo-experiments)
   - [Extensions](#43-extensions)
   - [Juice Animations](#44-juice-animations)
   - [Shapes Alternatives](#45-shapes-alternatives)
5. [Detectors](#5-detectors)
6. [Demo Experiments](#6-demo-experiments)
7. [Meta Components](#7-meta-components)

---

## Introduction

### Overview

The **ResXR Research Template** is a comprehensive Unity XR template designed specifically for Meta Quest research experiments. It provides a complete framework for building VR research applications with built-in support for:

- Hand tracking and gesture recognition
- Eye tracking and gaze analysis
- Face expression tracking
- Comprehensive data collection and export
- Scene management and flow control
- Room-scale calibration
- Multiple interaction paradigms (pinching, controllers, touch)

**Core Philosophy - "Clear Box" Design**: Unlike other solutions that provide black-box classes you use from outside, ResXR is designed as a **transparent, clear box** where researchers **own and modify every part of their experiment**. The template provides structure, base classes, and examples, but you are expected to copy, modify, and customize the code for your specific research needs. Everything is open and transparent - you understand exactly how your experiment runs under the hood.

### Prerequisites

- Unity 2021.3 or later
- Meta Quest SDK (OVR) - Automatically installed via Unity Package Manager
- Meta Quest headset (Quest 2, Quest Pro, Quest 3)
- Basic knowledge of Unity and C#

**Note**: The Meta XR SDK is not included in this repository. It will be automatically downloaded and installed by Unity when you open the project, based on the package dependencies defined in `Packages/manifest.json`.

**Third-party open-source plugins**: The template vendors several free/open-source Unity libraries under `Assets/` (for example **UniTask** for async/await, **NaughtyAttributes** for Inspector attributes, and **DOTween** for tweening). Licenses and attributions are summarized in **[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)** (see also **Third-Party Dependencies** in [README.md](README.md)).

### Project Structure

The template is organized into several main directories:

```
Assets/ResXR/
├── Base Scene/          # Core persistent scene and systems
├── Flow Management/     # Session/Task/Trial flow control
├── Utilities/           # Helper scripts and utilities
├── Detectors/           # Interaction detection system
├── Demo Experiments/    # Example implementations
└── Meta components/     # Meta-specific integrations

Assets/Project Folder/
└── New ResXRScene [Duplicate].unity  # Template for creating experiment scenes
```

### Key Design Patterns

- **Singleton Pattern**: Most managers use `ResXRSingleton<T>` for global access
- **Async/Await**: Uses UniTask for asynchronous operations
- **Component-Based**: Modular design with clear separation of concerns
- **Clear Box Philosophy**: You own and modify your experiment code - everything is transparent and open

---

## Core Components

### 2.1 Base Scene

**Location**: `Assets/ResXR/Base Scene/`

The Base Scene is the foundational scene that loads first and persists throughout the entire VR experience. It contains all core systems that need to remain active across different experiment scenes.

#### Key Files

- `Base Scene.unity` / `Base Scene With Meta Interactions.unity` - The base scene files
- `ProjectInitializer.cs` - Entry point that initializes the experience

#### ProjectInitializer

**Location**: `Assets/ResXR/Base Scene/ProjectInitializer.cs`

The entry point of the entire application. Coordinates calibration and scene loading.

**Key Properties**:
- `_shouldProjectUseCalibration` - Whether the project should be calibrated to physical space
- `_shouldCalibrateOnEditor` - Whether to run calibration in the Unity Editor

**Usage**:
```csharp
// ProjectInitializer automatically starts the experience
// It handles:
// 1. Room calibration (if enabled)
// 2. Scene manager initialization
// 3. First scene loading
```

**Dependencies**:
- `EnvironmentCalibrator` - For room calibration
- `ResXRSceneManager` - For scene management

#### Sub-Components

The Base Scene contains several major subsystems:

1. **ResXRPlayer** - Player controller and tracking systems
2. **ResXRSceneManager** - Scene loading and transitions
3. **ResXRDataManager_V2** - Data collection and export
4. **ResXR_RoomCalibrator** - Room-scale calibration

---

### 2.1.1 ResXRPlayer

**Location**: `Assets/ResXR/Base Scene/ResXRPlayer/`

**Purpose**: Central player controller managing all player-related systems including hand tracking, eye tracking, face tracking, and input management.

**Key Script**: `ResXRPlayer.cs`

**Inheritance**: `ResXRSingleton<ResXRPlayer>`

#### Overview

ResXRPlayer is the main singleton that provides access to all player-related functionality. It initializes and manages hand tracking, eye tracking, face tracking, and input systems. **Face tracking**: OVRFaceExpressions is included on the ResXRPlayer prefab and assigned via the Inspector; ResXRPlayer exposes it via the `OVRFace` property for convenience.

#### Key Properties

```csharp
// Player Trackables
public Transform PlayerHead { get; }
public Transform RightHand { get; }
public Transform LeftHand { get; }

// Hand Tracking
public ResXRHand HandLeft { get; }
public ResXRHand HandRight { get; }

// Eye Tracking
public ResXREyeTracker EyeTracker { get; }
public bool IsEyeTrackingEnabled { get; }
public Transform FocusedObject { get; }  // Object currently being looked at
public Vector3 EyeGazeHitPosition { get; }  // World position of gaze hit
public Transform RightEye { get; }
public Transform LeftEye { get; }

// Face Tracking (OVRFaceExpressions is on the ResXRPlayer prefab and assigned in the Inspector)
// Note: If Face Tracking is disabled, the reference may exist but will not produce valid weights.
public OVRFaceExpressions OVRFace { get; }
public bool IsFaceTrackingEnabled { get; }

// Input Managers
public ControllersInputManager ControllersInputManager { get; }
public PinchingInputManager PinchingInputManager { get; }
```

#### Key Methods

```csharp
// View fading
public async UniTask FadeViewToColor(Color targetColor, float duration)

// Player positioning
public void RepositionPlayer(PlayerRepositioner repositioner)

// Passthrough control
public void SetPassthrough(bool state)

// Hand utilities
public Transform GetHandFingerCollider(HandType handType, FingerType fingerType)
```

#### Usage Example

```csharp
// Access player instance
ResXRPlayer player = ResXRPlayer.Instance;

// Fade to black
await player.FadeViewToColor(Color.black, 1.0f);

// Check if player is looking at something
if (player.FocusedObject != null)
{
    Debug.Log($"Looking at: {player.FocusedObject.name}");
}

// Get finger position
Transform indexFinger = player.GetHandFingerCollider(HandType.Right, FingerType.Index);
```

#### Sub-Components

##### ResXR Hand

**Location**: `Assets/ResXR/Base Scene/ResXRPlayer/ResXR Hand/`

**Key Script**: `ResXRHand.cs`

Manages hand tracking via OVR Skeleton, hand colliders for interaction, pinch detection, and hand visibility.

**Key Properties**:
```csharp
public HandType HandType { get; }
public Pincher Pincher { get; }
public PinchManager PinchManager { get; }
public SkinnedMeshRenderer _handSMR { get; }
```

**Key Methods**:
```csharp
public void Init()  // Initialize hand tracking
public void UpdateHand()  // Update hand state (called every frame)
public Transform GetFingerCollider(FingerType fingerType)  // Get finger collider transform
public void SetHandVisibility(bool state)  // Show/hide hand mesh
```

**Sub-Components**:
- **HandCollider** (`HandCollider.cs`) - Individual finger colliders that track hand bone positions
- **Pincher** (`Pincher.cs`) - Calculates pinch strength and position between thumb and index finger

**Pincher Details**:
- Tracks distance between thumb tip (bone index 5) and index tip (bone index 10)
- Calculates pinch strength (0-1) based on finger distance
- Positioned at midpoint between thumb and index finger
- Uses sphere collider to detect pinchable objects

**Usage Example**:
```csharp
// Check if hand is pinching
bool isPinching = ResXRPlayer.Instance.HandRight.PinchManager.IsHandPinchingThisFrame();

// Get current pinch strength (0-1)
float strength = ResXRPlayer.Instance.HandRight.Pincher.Strength;
```

##### ResXREyeTracker

**Location**: `Assets/ResXR/Base Scene/ResXRPlayer/ResXR Eye Tracker/`

**Key Script**: `ResXREyeTracker.cs`

Provides eye gaze tracking, focused object detection via raycast, and eye position information. It can run up to **three raycasts per frame**: one for the left eye, one for the right eye, and one combined (cyclopean) ray. The **combined ray always runs** and drives `FocusedObject` and `EyeGazeHitPosition`. Per-eye raycasts are optional and controlled by the Data Manager recording option (see below).

**Key Properties**:
```csharp
// Combined (cyclopean) gaze - always computed when both eyes are confident
public Transform FocusedObject { get; }       // Object under binocular gaze
public Vector3 EyeGazeHitPosition { get; }    // World position where combined ray hits
public Vector3 EyePosition { get; }          // Midpoint of both eyes

// Per-eye gaze (only valid when EnableSeparateEyeRaycasts is true)
public Vector3 LeftEyeGazeHitPosition { get; }
public Vector3 RightEyeGazeHitPosition { get; }
public Transform LeftFocusedObject { get; }
public Transform RightFocusedObject { get; }
public bool HasLeftEyeHit { get; }
public bool HasRightEyeHit { get; }

public Transform RightEye { get; }
public Transform LeftEye { get; }
```

**Key Methods**:
```csharp
public void Init()  // Initialize eye tracking
public void UpdateEyeTracker()  // Update eye tracking (called every frame)
public bool EnableSeparateEyeRaycasts { get; set; }  // Set by Data Manager from recordingOptions.includeSeparateEyesGaze
```

**How It Works (raycasts)**:
1. **Combined ray (always run)**: When both eyes have OVR confidence ≥ threshold (default 0.5), a single raycast is cast from the midpoint of the two eye positions, in the direction of the normalized sum of left and right eye forward vectors. Result: `FocusedObject`, `EyeGazeHitPosition`. If either eye is below threshold, combined output is cleared (no raycast).
2. **Per-eye rays (only when `EnableSeparateEyeRaycasts` is true)**: If the Data Manager recording option "Include Separate Eyes Gaze" is enabled, two additional raycasts are run each frame — one from the left eye position along its forward, one from the right—giving left/right hit positions and focused objects. Each per-eye ray uses only that eye's confidence.
3. **Performance**: With the option off, only the combined ray runs (1 raycast). With the option on, up to 3 raycasts run (left, right, combined). In heavy scenes with many colliders, turning off per-eye raycasts can improve performance.

**Recording options and ResXREyeTracker**: At runtime, `ResXRDataManager_V2` sets `EyeTracker.EnableSeparateEyeRaycasts` from `recordingOptions.includeSeparateEyesGaze` in its `Start()`. When that option is false, ResXREyeTracker skips the left and right raycasts and clears per-eye outputs; the combined ray still runs. When true, all three raycasts run. The combined ray is always computed when both eyes are confident—it does not depend on the recording option.

**Usage Example**:
```csharp
// Check what player is looking at
if (ResXRPlayer.Instance.FocusedObject != null)
{
    Debug.Log($"Looking at: {ResXRPlayer.Instance.FocusedObject.name}");
    Debug.Log($"Hit position: {ResXRPlayer.Instance.EyeGazeHitPosition}");
}
```

##### Controllers

**Location**: `Assets/ResXR/Base Scene/ResXRPlayer/Controllers/`

**Key Script**: `ControllersInputManager.cs`

**Inheritance**: `AInputManager`

Handles controller input (Quest controllers) including trigger presses and haptic feedback.

**Key Methods**:
```csharp
public override bool IsLeftHeld()  // Is left trigger held
public override bool IsRightHeld()  // Is right trigger held
public async UniTask SetControllersVibration(float vibrationStrength, float duration)
```

**Usage Example**:
```csharp
// Check if controller trigger is pressed
if (ResXRPlayer.Instance.ControllersInputManager.IsRightHeld())
{
    Debug.Log("Right trigger held");
}

// Provide haptic feedback
await ResXRPlayer.Instance.ControllersInputManager.SetControllersVibration(0.5f, 0.2f);
```

##### Pinching System

**Location**: `Assets/ResXR/Base Scene/ResXRPlayer/Pinching/`

The pinching system provides hand-based interaction through pinch gestures.

**Key Components**:

1. **PinchingInputManager** (`PinchingInputManager.cs`)
   - Wraps pinch detection for use as input manager
   - Inherits from `AInputManager`
   - Provides `IsLeftHeld()` and `IsRightHeld()` based on pinch state

2. **PinchManager** (`PinchManager.cs`)
   - Manages pinch detection and pinchable object interaction
   - Tracks pinchable objects in range
   - Handles pinch enter/exit events
   - Selects which object to pinch based on priority

**Key Properties**:
```csharp
public APinchable PinchedObject { get; }  // Currently pinched object
public PinchingConfiguration Configuration { get; }  // Pinch thresholds and settings
public Pincher Pincher { get; }  // Pincher component
public HandType HandType { get; }  // Left or Right
```

**Key Methods**:
```csharp
public void HandlePinching()  // Called every frame to update pinch state
public APinchable ChooseObjectToPinch()  // Selects object to pinch from range
public void AddPinchableInRange(APinchable pinchable)  // Register pinchable object
public void RemovePinchableInRange(APinchable pinchable)  // Unregister pinchable object
public bool IsHandPinchingThisFrame()  // Check if currently pinching
```

3. **APinchable** (`APinchable.cs`)
   - Abstract base class for objects that can be pinched
   - Requires `Collider` and `Rigidbody` components
   - Implements priority-based selection system

**Key Properties**:
```csharp
public PinchManager PinchingHandPinchManager { get; set; }  // Which hand is pinching
public Collider Collider { get; }  // Object's collider
public int Priority { get; set; }  // Selection priority (higher = selected first)
public virtual float PinchExitThreshold => 0.97f  // Pinch strength to release
```

**Virtual Methods** (override in subclasses):
```csharp
public virtual void OnHoverEnter(PinchManager pinchManager)  // Hand enters range
public virtual void OnHoverStay(PinchManager pinchManager)  // Hand stays in range
public virtual void OnHoverExit(PinchManager pinchManager)  // Hand exits range
public virtual bool CanBePinched(PinchManager pinchManager)  // Check if can be pinched
public virtual void OnPinchEnter(PinchManager pinchManager)  // Pinch started
public virtual void OnPinchExit()  // Pinch released
```

**Usage Example**:
```csharp
// Create a pinchable object
public class MyPinchableObject : APinchable
{
    public override void OnPinchEnter(PinchManager pinchManager)
    {
        Debug.Log("Object pinched!");
        // Your custom logic here
    }
    
    public override void OnPinchExit()
    {
        Debug.Log("Object released!");
    }
}

// Wait for pinch gesture
await ResXRPlayer.Instance.PinchingInputManager.WaitForHoldAndRelease(HandType.Right, 1.0f);
```

**PinchingConfiguration**:
- `PinchEnterThreshold` - Pinch strength to start pinching (default: ~0.7)
- `PinchExitThreshold` - Pinch strength to release (default: ~0.3)
- `PinchMinDistance` - Minimum distance for full pinch
- `PinchMaxDistance` - Maximum distance for no pinch
- `MinimumTimeBetweenPinches` - Cooldown between pinches

---

### 2.1.2 ResXRSceneManager

**Location**: `Assets/ResXR/Base Scene/SceneManagement/`

**Purpose**: Manages scene loading, unloading, and transitions with fade effects.

**Key Script**: `ResXRSceneManager.cs`

**Inheritance**: `ResXRSingleton<ResXRSceneManager>`

#### Overview

ResXRSceneManager handles additive scene loading, allowing the Base Scene to persist while experiment scenes are loaded and unloaded. It provides smooth transitions with fade effects and automatic player repositioning.

#### Key Properties

```csharp
public int BaseSceneIndex { get; set; }  // Build index of base scene
public int FirstSceneToLoadIndex { get; set; }  // First scene to load after base
public string CurrentSceneName { get; }  // Name of currently active scene
```

#### Key Methods

```csharp
// Initialize scene manager
public void Init(bool isProjectUsingCalibration)

// Load a scene by build index
private async UniTask LoadActiveScene(int sceneBuildIndex)

// Load a scene by name
private async UniTask LoadActiveScene(string sceneName)

// Switch to a different scene (unloads current, loads new)
public async UniTask SwitchActiveScene(string sceneName)

// Unload current active scene
private async UniTask UnloadActiveScene()

// Restart the active scene
public async void RestartActiveScene()
```

#### Features

1. **Additive Scene Loading**: Loads experiment scenes additively, keeping Base Scene active
2. **Fade Transitions**: Automatic fade to black/clear during scene transitions
3. **Player Repositioning**: Automatically repositions player using `PlayerRepositioner` components
4. **Editor Support**: Handles both editor and build scenarios differently

#### Usage Example

```csharp
// Initialize (usually called by ProjectInitializer)
ResXRSceneManager.Instance.Init(false);  // false = not using calibration

// Switch to a different scene
await ResXRSceneManager.Instance.SwitchActiveScene("MyExperimentScene");

// Get current scene name
string current = ResXRSceneManager.Instance.CurrentSceneName;
```

#### PlayerRepositioner

**Location**: `Assets/ResXR/Base Scene/SceneManagement/PlayerRepositioner.cs`

A component placed in experiment scenes to define where the player should be positioned when the scene loads.

**Properties**:
- `Type` - `ERepositionType` (FloorLevel or FullPosition)
- Transform position and rotation define target player position

**Usage**: Simply add this component to a GameObject in your experiment scene at the desired player spawn location.

---

### 2.1.5 ResXR Scene Template

**Location**: `Assets/Project Folder/New ResXRScene [Duplicate].unity`

**Purpose**: Template scene that serves as the starting point for creating new experiment scenes. This scene is designed to be duplicated and opened additively with a Base Scene.

#### Overview

The ResXR Scene Template is a pre-configured scene template that contains the essential components needed for any experiment scene. **This is your starting point - you own and modify everything in your duplicated scene.**

**Important Philosophy**: ResXR is a **"clear box"** template, not a black box. Unlike other solutions where you use classes from outside, ResXR is designed for researchers to **own and modify every part of their experiment**. The template provides structure and examples, but you are expected to customize, extend, and modify the scripts to fit your research needs.

#### How to Use

1. **Duplicate the Template**:
   - In Unity Project window, navigate to `Assets/Project Folder/`
   - Right-click on `New ResXRScene [Duplicate].unity`
   - Select "Duplicate"
   - Rename it to your experiment name (e.g., `MyExperimentScene.unity`)

2. **Add to Build Settings**:
   - Open `File → Build Settings`
   - Click "Add Open Scenes" or drag your new scene into the build list
   - Ensure it's placed after the Base Scene in the build order

3. **Configure Scene Manager**:
   - In the Base Scene, select the `SceneManager` GameObject
   - Set `FirstSceneToLoadIndex` to the build index of your new scene
   - Or use `SwitchActiveScene()` with your scene name

4. **Own Your Scripts**:
   - The template includes empty `SceneReferencer` and Flow Management scripts
   - **You own these scripts** - modify them directly for your experiment
   - No need to create new classes - just fill in the existing ones

#### What's Included

The template scene comes pre-configured with:

1. **SceneReferencer** - An empty singleton component for storing scene-specific references
   - **You own this script** - add your experiment-specific references directly
   - Example: stimulus objects, target positions, UI panels
   - Located in: `Assets/ResXR/Utilities/General Scripts/SceneReferencer.cs` (base class)
   - Your scene will have its own instance - modify it as needed

2. **Flow Management Scripts** - Template stubs for Session/Task/Trial management
   - **You own these scripts** - edit `SessionManager.cs`, `TaskManager.cs`, and `TrialManager.cs` in `Assets/ResXR/Flow Management/` directly
   - Implement the placeholder methods (StartSession, EndSession, BetweenTasksFlow, etc.) with your experiment logic
   - Note: Demo experiments use renamed copies (e.g., `Maze_SessionManager`) for convenience when shipping multiple experiments in one project

3. **PlayerRepositioner** (optional) - For player spawn positioning
   - Add this component to a GameObject where you want the player to spawn
   - Set position and rotation
   - Choose `FloorLevel` or `FullPosition` reposition type

4. **Basic Scene Setup**:
   - Proper lighting settings
   - Render settings configured for VR
   - Empty hierarchy ready for your experiment content

#### Owning Your Experiment Code

**The ResXR Philosophy**:

ResXR is designed as a **transparent, clear box** where researchers have full control:

- ✅ **You own your SceneReferencer** - Modify it directly, add your references
- ✅ **You own your Flow Management** - Edit the Flow Management scripts directly; they are stubs you implement
- ✅ **You modify everything** - All scripts are open and transparent
- ✅ **You understand your experiment** - No hidden magic, everything is visible

**What You Should Do**:

1. **SceneReferencer**: Open the `SceneReferencer.cs` file in your scene and add your fields:
   ```csharp
   public class SceneReferencer : ResXRSingleton<SceneReferencer>
   {
       [Header("My Experiment Objects")]
       public GameObject stimulus;
       public InstructionsPanel instructions;
       public Transform targetPosition;
       
       [Header("My Configuration")]
       public float trialDuration = 10f;
   }
   ```

2. **Flow Management**: Open `SessionManager.cs`, `TaskManager.cs`, and `TrialManager.cs` in `Assets/ResXR/Flow Management/` and implement the placeholder methods with your experiment logic. If you ship multiple experiments in one project, you may duplicate and rename the scripts (as the demo experiments do with prefixes like `Maze_SessionManager`); otherwise, edit them in place.

**What's Transparent but API-Based**:

Some systems like `ResXRDataManager_V2` provide simplified APIs (like custom data classes) to make common tasks easier, but the underlying code is still **completely transparent and open** for you to understand and modify if needed. The API is a convenience, not a black box.

#### Base Scene Compatibility

The template works with both Base Scene variants:
- `Base Scene.unity` - Standard base scene
- `Base Scene With Meta Interactions.unity` - Base scene with Meta Interactions SDK

Both will load your experiment scene additively using `ResXRSceneManager`.

#### Best Practices

1. **Always duplicate the template** - Don't modify the original template scene
2. **Own your scripts** - Modify SceneReferencer and Flow Management directly
3. **No unnecessary inheritance** - You don't need to create wrapper classes, just modify what you have
4. **Keep it additive** - Never make your experiment scene the active scene in Build Settings (Base Scene should be first)
5. **Name clearly** - Use descriptive names for your experiment scenes and scripts
6. **Understand the code** - Read the base classes to understand how they work, then modify as needed

#### Troubleshooting

**Scene doesn't load**:
- Verify scene is in Build Settings
- Check `FirstSceneToLoadIndex` matches your scene's build index
- Ensure scene name matches exactly (case-sensitive)

**Player spawns in wrong location**:
- Add `PlayerRepositioner` component to a GameObject at desired spawn point
- Set the reposition type appropriately
- Ensure `ResXRSceneManager` is configured to reposition players

**Scripts not working**:
- Verify you've modified the scripts in your scene (not just the template)
- Check that your modified SceneReferencer is assigned in the scene
- Ensure Flow Management scripts are in your scene and properly configured

---

### 2.1.3 ResXRDataManager_V2

**Location**: `Assets/ResXR/Base Scene/ResXRDataManager_V2/`

**Purpose**: Comprehensive data collection and export system for VR research data.

**Key Script**: `ResXRDataManager_V2.cs`

**Inheritance**: `ResXRSingleton<ResXRDataManager_V2>`

#### Overview

ResXRDataManager_V2 is a complete data logging system that automatically collects VR tracking data (head, hands, eyes, body, face) and allows researchers to add custom event logging. Data is exported to CSV files with automatic schema generation.

**Transparency Note**: While ResXRDataManager_V2 provides convenient APIs (like custom data classes) to simplify data logging, the entire codebase is **completely transparent and open**. You can read, understand, and modify any part of it if needed. The API is a convenience, not a black box - everything is visible and understandable.

#### Key Features

- **Automatic Continuous Data Collection**: Head, hands, eyes, body, face tracking
- **Gaze recording options**: "Include Gaze" records the combined (cyclopean) hit point and focused object. "Include Separate Eyes Gaze" adds per-eye hit positions and focused objects and enables 3 raycasts per frame in ResXREyeTracker (left, right, combined); when off, only the combined ray runs (1 raycast). The combined ray always runs when both eyes are confident.
- **Custom Event Logging**: Create custom data classes for experiment-specific events
- **Events table**: Template includes `ReportEvent` (`Events.csv`) with columns `name`, `onset`, and `duration`; call `ResXRDataManager_V2.Instance.ReportEvent(...)`. Use `Time.realtimeSinceStartup` for `onset` to match continuous data and downstream pipelines.
- **CSV Export**: All data exported to organized CSV files
- **Metadata**: Automatic session metadata generation
- **Live Monitor**: Real-time data visualization (optional)

#### How to Use

##### 1. Custom Data Classes (Events)

**Template-provided `ReportEvent`** (writes `{sessionTime}_Events.csv`): public fields `name`, `onset`, `duration`. Onset is expected to use **`Time.realtimeSinceStartup`** (seconds since app start), same clock as continuous CSV `timeSinceStartup`; `duration` is in seconds (use `0` for point events). This supports pipeline-friendly event markers and output standardization.

```csharp
using ResXRData;
using UnityEngine;

// From anywhere with ResXRDataManager_V2 in the scene:
ResXRDataManager_V2.Instance.ReportEvent("stimulus_onset", Time.realtimeSinceStartup, 0f);
```

You can also add your own types implementing `CustomDataClass` (see `ChoiceEvent`, `StimulusBounds`, etc. in `ResXRDataManager_V2.cs`).

**Guidelines**:
- Must implement `CustomDataClass` interface
- Must have `TableName` property (read-only string)
- Use public fields (not properties) for data columns
- Always prefer `Time.realtimeSinceStartup` for time fields so they align with continuous data
- Add a constructor to set default values

##### 2. Reporter Functions

Add helper functions in `ResXRDataManager_V2.cs` to log your events, or call `LogCustom` directly. The template includes:

```csharp
public void ReportEvent(string name, float onset, float duration)
{
    LogCustom(new ReportEvent(name, onset, duration));
}

public void LogChoice(string task, int trial, string optionAName, string optionBName, string choice,
    string chosenOption, string handUsed, float reactionTime, float displayTime, float choiceTime)
{
    var choiceEvent = new ChoiceEvent(task, trial, optionAName, optionBName, choice, chosenOption, handUsed, reactionTime, displayTime, choiceTime);
    LogCustom(choiceEvent);
}
```

##### 3. Custom Transforms

In the Unity Inspector, assign transforms you want to track continuously:

1. Select `ResXR_DataManager_V2` prefab in scene
2. Find "Custom Transforms To Record" list
3. Drag objects you want to track (e.g., stimuli, targets)
4. Their positions/rotations will appear in `ContinuousData.csv`

#### Sub-Components

##### Collectors

**Location**: `Assets/ResXR/Base Scene/ResXRDataManager_V2/Collectors/`

Collectors pull data from the VR system every physics tick and write to CSV files.

1. **OVRNodesCollector** - Head, controllers, hand positions
2. **OVREyesCollector** - Eye gaze angles, validity, confidence; combined gaze (FocusedObject, EyeGazeHitPosition); and when **includeSeparateEyesGaze** is enabled, per-eye hit positions and focused objects (Left/RightEyeGazeHitPosition, Left/RightFocusedObject, HasLeftEyeHit, HasRightEyeHit)
3. **OVRHandsCollector** - Hand tracking, bone positions, confidence
4. **OVRBodyCollector** - Body joint positions and calibration
5. **OVRFaceCollector** - Face expression weights and validity
6. **CustomTransformsCollector** - Custom object positions/rotations
7. **SystemStatusCollector** - Recenter, tracking origin change, user presence, tracking loss
8. **OVRPerformanceCollector** - Reserved for performance metrics (optional; see project)

##### Core Infrastructure

**Location**: `Assets/ResXR/Base Scene/ResXRDataManager_V2/Core Infrastructure/`

- **SchemaBuilder.cs** - Defines column names for each CSV
- **ColumnIndex.cs** - Stores ordered column names for lookup
- **RowBuffer.cs** - Staging area for one row of data
- **CsvRowWriter.cs** - Writes CSV files (header + rows)
- **CustomCsvFromDataClass.cs** - Automatic CSV generation from data classes

##### Metadata

**Location**: `Assets/ResXR/Base Scene/ResXRDataManager_V2/Metadata/`

- **BuildInfoLoader.cs** - Loads build information at runtime
- **SessionMetaWriter.cs** - Writes `session_metadata.json` with session info (designed to support later Motion-BIDS export; includes device/platform provenance, tracking origin, reference frames for pipeline)

##### Live Monitor

**Location**: `Assets/ResXR/Base Scene/ResXRDataManager_V2/Live Monitor/`

Real-time data visualization system for monitoring data collection during experiments.

#### Data Collection Flow

```
Collectors → RowBuffer → CsvRowWriter → CSV files
```

- Collectors fill `RowBuffer` every physics tick
- `RowBuffer` flushes to `CsvRowWriter`
- `CsvRowWriter` writes to disk (CSV file)
- Metadata scripts write JSON files in parallel

#### Output Files

- **ContinuousData.csv** - All continuous tracking data (head, hands, eyes, body, face, custom transforms). Gaze columns include combined (FocusedObject, EyeGazeHitPosition) and, when the "Include Separate Eyes Gaze" recording option is enabled, per-eye hit points and focused objects.
- **FaceExpressions.csv** - Face expression weights
- **Custom Event CSVs** - One CSV per custom data class (e.g., `ChoiceEvents.csv`, `{sessionTime}_Events.csv` from the template `ReportEvent` type)
- **session_metadata.json** - Session information, schema details, device/platform provenance (manufacturers_model_name_raw, software_versions_raw), tracking_origin_type (Meta), reference_frames (UnityWorld/HandLocal for later *_channels.json), and device_utc_offset (DST-aware). Includes **build_info_available**: when true, build_id, git_commit, and utc_build_iso8601 are set from build_info.json; when false, those three fields are left empty (no placeholders). A Python pipeline can use this to generate BIDS motion files.

#### FAQ

**Q: How often is data logged?**  
A: Continuous data is logged once per physics tick (default: 50 Hz). Custom events are logged whenever your reporter function is called.

**Q: Where are files saved?**  
A: Files are saved to device storage. Path is logged in console at startup.

**Q: Do I need to edit DataManager_V2 or SchemaBuilder?**  
A: Typically no - you use the provided APIs (custom data classes, reporter functions). However, all code is transparent and open - you can read and modify it if your research requires it. The template is a "clear box" - nothing is hidden.

For detailed information, see: `Assets/ResXR/Base Scene/ResXRDataManager_V2/Doc/ResXRDataManager_V2_README.txt`

---

### 2.1.4 ResXR_RoomCalibrator

**Location**: `Assets/ResXR/Base Scene/ResXR_RoomCalibrator/`

**Purpose**: Calibrates the VR experience to physical space for room-scale experiments.

**Key Script**: `EnvironmentCalibrator.cs`

**Inheritance**: `ResXRSingleton<EnvironmentCalibrator>`

#### Overview

The Room Calibrator allows researchers to align the virtual environment with physical space. This is essential for experiments that require participants to move in real space.

#### How It Works

1. **Calibration Process**:
   - Enables passthrough so user can see real world
   - User pinches to mark two reference points in physical space
   - System calculates rotation and position offset
   - Aligns virtual environment to match physical space

2. **Reference Points**:
   - **Position Point**: First pinch location (defines position)
   - **Rotation Point**: Second pinch location (defines rotation/direction)

#### Key Methods

```csharp
// Start calibration process
public async UniTask CalibrateRoom()

// Align virtual to physical room (called automatically)
public void AlignVirtualToPhysicalRoom()

// Button callbacks
public void OnRedoCalibration()
public void OnConfirmCalibration()
```

#### Usage

Calibration is typically triggered automatically by `ProjectInitializer` if `_shouldProjectUseCalibration` is true.

**Manual Usage**:
```csharp
// Start calibration
await EnvironmentCalibrator.Instance.CalibrateRoom();
```

#### Configuration

In the Unity Inspector:
- `centerModel` - Virtual model to align
- `virtualReferencePointPosition` - Virtual position reference point
- `virtualReferencePointRotation` - Virtual rotation reference point
- `calibrationMark` - Prefab to show at marked points
- `_btnConfirm` / `_btnRedo` - Confirmation buttons

---

## Flow Management System

**Location**: `Assets/ResXR/Flow Management/`

**Purpose**: Hierarchical experiment flow control system (Session → Task → Trial).

### Overview

The Flow Management system provides a structured way to organize experiments into Sessions, Tasks, and Trials. This hierarchy allows for flexible experiment design while maintaining clear structure.

**Important**: The Flow Management scripts in `Assets/ResXR/Flow Management/` are **intentionally simple template stubs you edit directly** for your experiment. They are part of the clear-box philosophy: you own them and edit them. By default some methods are placeholders; implement your own Start/End/Between logic directly in the scripts. If you want experiment-specific names (e.g. when shipping multiple experiments in one project), you may duplicate and rename the scripts; the default intended approach is direct modification.

### Hierarchy

```
Session
  └── Task[]
        └── Trial[]
```

### Components

#### SessionManager

**Location**: `Assets/ResXR/Flow Management/SessionManager.cs`

**Inheritance**: `ResXRSingleton<SessionManager>`

Top-level manager that controls the entire session, iterating through tasks.

**Key Properties**:
```csharp
[SerializeField] private Task[] _tasks;  // Array of tasks in this session
private int _currentTask;  // Current task index
```

**Key Methods**:
```csharp
// Run the entire session flow
public async UniTask RunSessionFlow()

// Edit these directly in SessionManager.cs — implement your logic here
private void StartSession()  // Called at session start
private void EndSession()  // Called at session end
private async UniTask BetweenTasksFlow()  // Called between tasks
```

**Usage**: Open `SessionManager.cs` and implement `StartSession`, `EndSession`, and `BetweenTasksFlow` by editing the script. Example of what you might add inside those methods:

```csharp
// In SessionManager.cs — edit StartSession():
private void StartSession()
{
    Debug.Log("Session started!");
    // Initialize session-level data
}

// In SessionManager.cs — edit BetweenTasksFlow():
private async UniTask BetweenTasksFlow()
{
    await instructionsPanel.Show();
    await UniTask.Delay(5000);
    await instructionsPanel.Hide();
}
```

#### TaskManager

**Location**: `Assets/ResXR/Flow Management/TaskManager.cs`

**Inheritance**: `ResXRSingleton<TaskManager>`

Manages individual tasks, iterating through trials.

**Key Properties**:
```csharp
[SerializeField] private Trial[] _trials;  // Array of trials in this task
private int _currentTrial;  // Current trial index
private Task _currentTask;  // Current task data
```

**Key Methods**:
```csharp
// Run a task's flow
public async UniTask RunTaskFlow(Task task)

// Edit these directly in TaskManager.cs — implement your logic here
private void StartTask()  // Called at task start
private void EndTask()  // Called at task end
private async UniTask BetweenTrialsFlow()  // Called between trials
```

**Usage**: Typically called by `SessionManager`, but can be used independently.

#### TrialManager

**Location**: `Assets/ResXR/Flow Management/TrialManager.cs`

**Inheritance**: `ResXRSingleton<TrialManager>`

Manages individual trials.

**Key Properties**:
```csharp
private Trial _currentTrial;  // Current trial data
```

**Key Methods**:
```csharp
// Run a trial's flow
public async UniTask RunTrialFlow(Trial trial)

// Edit these directly in TrialManager.cs — implement your logic here
private void StartTrial()  // Called at trial start
private void EndTrial()  // Called at trial end
```

**Usage**: Typically called by `TaskManager`.

#### Task and Trial Data Structures

**Location**: 
- `Assets/ResXR/Flow Management/Task.cs`
- `Assets/ResXR/Flow Management/Trial.cs`

These are simple data container classes. Extend them with your own properties:

```csharp
[System.Serializable]
public class MyTask : Task
{
    public string taskName;
    public int difficulty;
    public bool isPractice;
}

[System.Serializable]
public class MyTrial : Trial
{
    public string trialName;
    public GameObject stimulus;
    public float duration;
}
```

### Usage Pattern

1. **Edit the scripts directly**: Open `SessionManager.cs`, `TaskManager.cs`, and `TrialManager.cs` in `Assets/ResXR/Flow Management/` and implement the placeholder methods (`StartSession`, `EndSession`, `BetweenTasksFlow`, `StartTask`, `EndTask`, `BetweenTrialsFlow`, `StartTrial`, `EndTrial`) with your experiment logic.
2. **Define Data Structures**: Extend `Task` and `Trial` with your data (or create new ones) as needed.
3. **Configure in Inspector**: Assign tasks and trials in the Unity Inspector.
4. **Optional**: If you ship multiple experiments in one project, you may duplicate the scripts and give them experiment-specific names (e.g. `Maze_SessionManager`); the demo experiments do this for convenience. The default approach is to edit the Flow Management scripts in place.

### Example Flow

```csharp
// Modify directly - you own this script
public class MyExperimentSessionManager : ResXRSingleton<MyExperimentSessionManager>
{
    [SerializeField] private Task[] _tasks;
    private int _currentTask;

    private void Start()
    {
        RunSessionFlow().Forget();
    }

    public async UniTask RunSessionFlow()
    {
        StartSession();

        while (_currentTask < _tasks.Length)
        {
            await TaskManager.Instance.RunTaskFlow(_tasks[_currentTask]);
            await BetweenTasksFlow();
            _currentTask++;
        }

        EndSession();
    }

    private void StartSession()
    {
        // Your session initialization logic
        ResXRDataManager_V2.Instance.LogCustom("Session started");
    }

    private async UniTask BetweenTasksFlow()
    {
        // Your between-tasks logic
        await breakScreen.Show();
        await UniTask.Delay(30000);  // 30 second break
        await breakScreen.Hide();
    }

    private void EndSession()
    {
        // Your session end logic
    }
}


// Modify directly - you own this script
public class TrialManager : ResXRSingleton<TrialManager>
{
    private Trial _currentTrial;

    public async UniTask RunTrialFlow(Trial trial)
    {
        _currentTrial = trial;
        StartTrial();

        // Your trial logic here
        await YourTrialLogic();

        EndTrial();
    }

    private void StartTrial()
    {
        MyTrial trial = _currentTrial as MyTrial;
        // Activate stimulus
        trial.stimulus.SetActive(true);
    }

    private void EndTrial()
    {
        MyTrial trial = _currentTrial as MyTrial;
        // Deactivate stimulus
        trial.stimulus.SetActive(false);
        // Log trial end
        ResXRDataManager_V2.Instance.LogTrialEnd(trial.trialName);
    }

    private async UniTask YourTrialLogic()
    {
        // Your experiment-specific trial logic
    }
}
```

---

## Utilities

### 4.1 General Scripts

**Location**: `Assets/ResXR/Utilities/General Scripts/`

#### ResXRSingleton

**Location**: `Assets/ResXR/Utilities/General Scripts/ResXRSingleton.cs`

Base class for singleton pattern implementation. Ensures only one instance exists and provides global access.

**Usage**:
```csharp
public class MyManager : ResXRSingleton<MyManager>
{
    protected override void DoInAwake()
    {
        // Initialization code
    }
}

// Access from anywhere
MyManager.Instance.DoSomething();
```

#### ResXRUtilities

**Location**: `Assets/ResXR/Utilities/General Scripts/ResXRUtilities.cs`

Static utility functions for common operations.

**Key Methods**:
```csharp
// Date/time formatting
public static string GetFormattedDateTime(bool includeTime = false)

// Line math utilities
public static Vector3 GetPointOnLineFromNormalizedValue(Vector3 lineStart, Vector3 lineEnd, float valueNormalized)
public static float GetNormalizedValueFromPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
public static Vector3 GetClosestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)

// Object serialization
public static Dictionary<string, string> SerializeObject(object obj)
```

#### SceneReferencer

**Location**: `Assets/ResXR/Utilities/General Scripts/SceneReferencer.cs`

**Inheritance**: `ResXRSingleton<SceneReferencer>`

Container for scene-specific references. **You own this script** - modify it directly in your experiment scene to store references to objects.

**Usage**: The template scene includes an empty `SceneReferencer`. Simply open the script and add your fields directly:

```csharp
// In your experiment scene's SceneReferencer.cs
public class SceneReferencer : ResXRSingleton<SceneReferencer>
{
    [Header("Experiment Objects")]
    public GameObject stimulus;
    public Transform targetPosition;
    public InstructionsPanel instructions;
    public GameButton startButton;
    
    [Header("Configuration")]
    public float trialDuration = 10f;
}
```

**No need to create a new class** - just modify the existing one in your scene. The base class provides the singleton pattern, you add your experiment-specific references.

#### ResXRHeadsetServices

**Location**: `Assets/ResXR/Utilities/General Scripts/ResXRHeadsetServices.cs`

**Inheritance**: `ResXRSingleton<ResXRHeadsetServices>`

Provides access to headset services like passthrough.

**Key Methods**:
```csharp
public void SetPassthrough(bool state)
```

#### ToucherDetector

**Location**: `Assets/ResXR/Utilities/General Scripts/ToucherDetector.cs`

Detects when objects tagged as "Toucher" (typically hand colliders) enter/exit trigger zones.

**Key Events**:
```csharp
public UnityEvent<Transform> ToucherEnter;
public UnityEvent<Transform> ToucherExited;
public UnityEvent HeadEnter;
public UnityEvent HeadExit;
```

**Usage**: Attach to objects with colliders set as triggers. Useful for button interactions.

#### FollowTransform

**Location**: `Assets/ResXR/Utilities/General Scripts/FollowTransform.cs`

Makes an object follow another transform's position.

**Key Methods**:
```csharp
public void Init(Transform target)  // Set target to follow
public Vector3 Position { get; }  // Current position
```

#### Randomizer

**Location**: `Assets/ResXR/Utilities/General Scripts/Randomizer.cs`

Utilities for randomization operations.

#### ConfigurableIterator

**Location**: `Assets/ResXR/Utilities/General Scripts/ConfigurableIterator/`

Provides configurable iteration patterns for lists.

**Key Components**:
- `ConfigurableIterator.cs` - Main iterator class
- `EIterationOrder.cs` - Enum for iteration order (Sequential, Random, etc.)

---

### 4.2 Shared Utils (Demo Experiments)

**Location**: `Assets/ResXR/Demo Experiments/Shared Utils/`

Reusable components for building experiments.

#### Instructions Panel

**Location**: `Assets/ResXR/Demo Experiments/Shared Utils/Instructions Panel/`

**Key Script**: `InstructionsPanel.cs`

A panel system for displaying instructions to participants with show/hide animations and automatic back panel resizing.

**Key Properties**:
```csharp
public GameObject backPanel;  // Background panel
public TextMeshPro title;  // Title text
public TextMeshPro text;  // Body text
public TMPBackPanelResizer backPanelResizer;  // Auto-resizer
public bool hideOnAwake = true;  // Hide on start
public bool useAnimations = true;  // Use scale animations
public bool collectEyeGaze = true;  // Track eye gaze on panel
```

**Key Methods**:
```csharp
public async UniTask Show(bool doResizeBackPanel = true)
public async UniTask Hide()
public async UniTask ShowForSeconds(float seconds, bool doResizeBackPanel = true)
```

**Usage Example**:
```csharp
InstructionsPanel instructions = GetComponent<InstructionsPanel>();

// Show instructions
await instructions.Show();

// Show for specific duration
await instructions.ShowForSeconds(5.0f);

// Hide instructions
await instructions.Hide();
```

##### Back Panel Resizer

**Location**: `Assets/ResXR/Demo Experiments/Shared Utils/Instructions Panel/Back Panel/TMPBackPanelResizer.cs`

Automatically resizes a background panel (Quad) to fit TextMeshPro text bounds.

**Key Properties**:
```csharp
public List<TextMeshPro> tmps;  // TextMeshPro components to measure
public Transform backPanel;  // Background panel to resize
public Vector2 padding;  // Padding around text (x = horizontal, y = vertical)
public bool resizeOnAwake = true;
public bool resizeEveryFrame = false;
```

**Key Methods**:
```csharp
public void ResizeBackPanel()  // Manually trigger resize
```

**Usage**: Automatically called by `InstructionsPanel`, but can be used standalone.

#### Game Button

**Location**: `Assets/ResXR/Demo Experiments/Shared Utils/Game Button/`

**Key Scripts**: 
- `GameButton.cs` - Main button controller
- `GameButtonCollider.cs` - Collider-based press detection

Interactive 3D button system with press/release events and haptic feedback.

**Key Properties**:
```csharp
public UnityEvent onPress;  // Fired when button pressed
public UnityEvent onRelease;  // Fired when button released
public bool alwaysPressable = false;  // If false, only pressable when WaitForButtonPress() is called
public bool useSound = true;  // Play sound on press
```

**Key Methods**:
```csharp
public async UniTask WaitForButtonPress()  // Wait for button to be pressed
```

**Usage Example**:
```csharp
GameButton button = GetComponent<GameButton>();

// Wait for button press
await button.WaitForButtonPress();
Debug.Log("Button was pressed!");

// Or use events
button.onPress.AddListener(() => {
    Debug.Log("Button pressed via event");
});
```

#### Player Position Mark

**Location**: `Assets/ResXR/Demo Experiments/Shared Utils/Player Position Mark/`

**Key Script**: `PlayerPositionMark.cs`

Visual markers that indicate where players should stand, with trigger zones to detect when player arrives.

**Key Properties**:
```csharp
public GameObject visualMark;  // Visual marker to show
public float fadeInDuration = 1f;  // Fade duration when player arrives
```

**Key Methods**:
```csharp
public async UniTask WaitForPlayerAsync(bool fadeToblack = true, CancellationToken cancellationToken = default)
```

**Usage Example**:
```csharp
PlayerPositionMark positionMark = GetComponent<PlayerPositionMark>();

// Wait for player to reach position
await positionMark.WaitForPlayerAsync(fadeToblack: true);
Debug.Log("Player reached position!");
```

---

### 4.3 Extensions

**Location**: `Assets/ResXR/Utilities/Extensions/`

Extension methods for common Unity types.

#### TransformExtensions

**Location**: `Assets/ResXR/Utilities/Extensions/TransformExtensions.cs`

Extension methods for Transform operations.

#### ColorExtensions

**Location**: `Assets/ResXR/Utilities/Extensions/ColorExtensions.cs`

Extension methods for Color manipulation.

#### ListExtensions

**Location**: `Assets/ResXR/Utilities/Extensions/ListExtensions.cs`

Extension methods for List operations.

---

### 4.4 Juice Animations

**Location**: `Assets/ResXR/Utilities/JuiceAnimations/`

Simple animation components for visual feedback.

#### HoverAnimation

**Location**: `Assets/ResXR/Utilities/JuiceAnimations/HoverAnimation.cs`

Animation that plays when object is hovered.

#### ScaleEffect

**Location**: `Assets/ResXR/Utilities/JuiceAnimations/ScaleEffect.cs`

Scaling animation effects.

#### UIShaker

**Location**: `Assets/ResXR/Utilities/JuiceAnimations/UIShaker.cs`

Shake animation for UI elements.

#### RotatingObject

**Location**: `Assets/ResXR/Utilities/JuiceAnimations/RotatingObject.cs`

Continuous rotation animation.

---

### 4.5 Shapes Alternatives

**Location**: `Assets/ResXR/Utilities/ShapesAlternatives/`

Procedural shape generation components.

#### Line, Rectangle, Sphere

Simple components for generating procedural shapes at runtime.

---

## Detectors

**Location**: `Assets/ResXR/Detectors/`

**Purpose**: Interaction detection system for collision and trigger events.

### Overview

The Detector system provides a unified way to detect interactions through Unity's collision system.

### Components

#### ADetector

**Location**: `Assets/ResXR/Detectors/ADetector.cs`

Abstract base class for all detectors.

**Key Events**:
```csharp
public Action InteractionStarted;
public Action InteractionEnded;
```

#### CollisionDetector

**Location**: `Assets/ResXR/Detectors/CollisionDetector.cs`

**Inheritance**: `ADetector`

Detects collisions and trigger events, firing events when interactions start/end.

**Key Properties**:
```csharp
[SerializeField] private bool _detectSpecificCollision;  // Only detect specific tag
[SerializeField] private string _colliderTag;  // Tag to detect
```

**How It Works**:
- Listens to `OnCollisionEnter/Exit` and `OnTriggerEnter/Exit`
- Fires `InteractionStarted` on enter
- Fires `InteractionEnded` on exit
- Can filter by collider tag if `_detectSpecificCollision` is true

**Usage Example**:
```csharp
CollisionDetector detector = GetComponent<CollisionDetector>();

detector.InteractionStarted += () => {
    Debug.Log("Interaction started!");
};

detector.InteractionEnded += () => {
    Debug.Log("Interaction ended!");
};
```

---

## Demo Experiments

**Location**: `Assets/ResXR/Demo Experiments/`

**Purpose**: Example implementations showing how to use the template components.

### Overview

The Demo Experiments folder contains complete, working examples of experiments built with the template. These serve as reference implementations.

### Examples

#### Binary Choice

**Location**: `Assets/ResXR/Demo Experiments/Binary Choice/`

A two-choice decision-making experiment where participants choose between two options.

**Key Components**:
- `Choice.cs` - Individual choice option
- `ChoicesManager.cs` - Manages choice presentation
- `FixationCross.cs` - Fixation cross display
- Flow management: `BinaryChoice_SessionManager`, `BinaryChoice_TaskManager`, `BinaryChoice_TrialManager`

**Features**:
- Stimulus pair loading
- Reaction time measurement
- Choice logging

**Note**: Demo experiments use prefixes (e.g., `BinaryChoice_`) for organizational clarity, but when you create your own experiment, you should name your scripts clearly without prefixes since you own them.

#### Maze

**Location**: `Assets/ResXR/Demo Experiments/Maze/`

A navigation experiment where participants navigate a maze to collect coins.

**Key Components**:
- `Maze.cs` - Maze generation and management
- `Coin.cs` - Collectible coin objects
- Flow management: `Maze_SessionManager`, `Maze_TaskManager`, `Maze_TrialManager`

**Features**:
- Procedural maze generation
- Coin collection tracking
- Navigation data logging

#### Museum

**Location**: `Assets/ResXR/Demo Experiments/Museum/`

An art viewing experiment in a virtual museum gallery.

**Key Components**:
- Art piece prefabs
- Label system
- Flow management: `Museum_SessionManager`, `Museum_TaskManager`, `Museum_TrialManager`

**Features**:
- Art piece viewing
- Gaze tracking on artworks
- Viewing time measurement

### Learning from Examples

Each demo experiment demonstrates:
1. **Flow Management**: How to structure Session/Task/Trial (note: they use prefixes, but you should own your scripts)
2. **Data Logging**: How to log custom events
3. **Interaction**: How to use pinching, buttons, and other interaction methods
4. **Scene Setup**: How to organize experiment scenes
5. **Component Integration**: How components work together

**Important**: These are examples to learn from. When creating your own experiment:
- Copy the base Flow Management classes and modify them directly
- You own your scripts - no need for prefixes unless you have multiple experiments in one project
- All code is transparent - read and understand, then modify as needed

---

## Meta Components

**Location**: `Assets/ResXR/Meta components/`

Meta-specific integrations and utilities for Quest headsets.

### Components

#### Meta Interactions

Various Meta SDK integrations and utilities.

---

## Component Relationship Diagram

```mermaid
graph TD
    ProjectInitializer[ProjectInitializer] --> EnvironmentCalibrator[EnvironmentCalibrator]
    ProjectInitializer --> ResXRSceneManager[ResXRSceneManager]
    
    ResXRSceneManager --> ResXRPlayer[ResXRPlayer]
    
    ResXRPlayer --> ResXRHandLeft[ResXRHand Left]
    ResXRPlayer --> ResXRHandRight[ResXRHand Right]
    ResXRPlayer --> ResXREyeTracker[ResXREyeTracker]
    ResXRPlayer --> ControllersInputManager[ControllersInputManager]
    ResXRPlayer --> PinchingInputManager[PinchingInputManager]
    
    ResXRHandLeft --> PinchManager[PinchManager]
    ResXRHandRight --> PinchManager
    PinchManager --> Pincher[Pincher]
    PinchManager --> APinchable[APinchable Objects]
    
    ResXRPlayer --> ResXRDataManager[ResXRDataManager_V2]
    ResXRDataManager --> Collectors[Data Collectors]
    ResXRDataManager --> CustomCsvWriter[Custom CSV Writer]
    
    SessionManager[SessionManager] --> TaskManager[TaskManager]
    TaskManager --> TrialManager[TrialManager]
    
    InstructionsPanel[InstructionsPanel] --> TMPBackPanelResizer[TMPBackPanelResizer]
    GameButton[GameButton] --> GameButtonCollider[GameButtonCollider]
    PlayerPositionMark[PlayerPositionMark] --> PositionMarkTriggerZone[TriggerZone]
```

---

## Common Usage Patterns

### Creating a New Experiment Scene

```csharp
// 1. Duplicate "New ResXRScene [Duplicate].unity" → "MyExperiment.unity"
// 2. Add scene to Build Settings (after Base Scene)
// 3. Modify SceneReferencer directly - add your experiment references
// 4. Edit Flow Management scripts directly and implement your experiment logic
// 5. Add PlayerRepositioner for spawn point
// 6. Build your experiment content
// 7. Configure ResXRSceneManager to load your scene
```

### Starting an Experiment

```csharp
// 1. ProjectInitializer automatically starts
// 2. Calibration (if enabled)
// 3. SceneManager loads first scene (your experiment scene)
// 4. Your experiment code runs
```

### Logging Data

```csharp
// Log custom event
ResXRDataManager_V2.Instance.LogChoice(trialNum, option1, option2, choice, reactionTime);

// Custom transforms are automatically tracked if added to inspector
```

### Handling Input

```csharp
// Pinch gesture
await ResXRPlayer.Instance.PinchingInputManager.WaitForHoldAndRelease(HandType.Right, 1.0f);

// Controller trigger
if (ResXRPlayer.Instance.ControllersInputManager.IsRightHeld())
{
    // Do something
}
```

### Scene Transitions

```csharp
// Switch scenes
await ResXRSceneManager.Instance.SwitchActiveScene("NextExperimentScene");
```

### Showing Instructions

```csharp
// Show instructions panel
await instructionsPanel.Show();
await UniTask.Delay(5000);  // Show for 5 seconds
await instructionsPanel.Hide();
```

---

## Best Practices

1. **Always use ResXRSingleton**: For managers that need global access
2. **Use UniTask for async**: Prefer UniTask over Coroutines for async operations
3. **Own your scripts**: Edit the Flow Management scripts and SceneReferencer directly - you own your experiment code
4. **No unnecessary inheritance**: Don't create wrapper classes when you can modify directly
5. **Log custom events**: Use ResXRDataManager_V2 for all experiment data
6. **Follow flow hierarchy**: Use Session → Task → Trial structure
7. **Test in Editor**: Use editor calibration option for faster iteration
8. **Organize scenes**: Keep Base Scene separate from experiment scenes
9. **Understand the code**: Read base classes to understand how they work, then modify as needed
10. **Transparency**: All code is open and transparent - understand it, then customize it

---

## Troubleshooting

### Hand Tracking Not Working
- Ensure hand tracking is enabled in OVR settings
- Check that `ResXRHand` components are properly initialized
- Verify OVR Skeleton components are present

### Eye Tracking Not Working
- Ensure eye tracking is enabled in headset settings
- Check `IsEyeTrackingEnabled` is true on ResXRPlayer
- Verify confidence threshold (default 0.5)

### Data Not Logging
- Check ResXRDataManager_V2 is in scene
- Verify custom transforms are assigned in inspector
- Check file permissions on device

### Scene Not Loading
- Verify scene is in Build Settings
- Check BaseSceneIndex and FirstSceneToLoadIndex in ResXRSceneManager
- Ensure scene name matches exactly

---

## Additional Resources

- **Third-party notices**: `THIRD_PARTY_NOTICES.md` (Meta XR SDK, UniTask, NaughtyAttributes, DOTween, and other bundled dependencies)
- **Data Manager Documentation**: `Assets/ResXR/Base Scene/ResXRDataManager_V2/Doc/ResXRDataManager_V2_README.txt`
- **Data Sources**: `Assets/ResXR/Base Scene/ResXRDataManager_V2/Doc/data_sources_README.txt`
- **Demo Experiments**: Reference implementations in `Assets/ResXR/Demo Experiments/`

---

## Conclusion

This documentation covers the major components of the ResXR Research Template. For specific implementation details, refer to the source code and demo experiments. 

**Remember**: ResXR is a **"clear box"** template. Unlike other solutions that provide black-box classes, ResXR is designed for researchers to **own and modify every part of their experiment**. The template provides structure, base classes, and examples, but you are expected to:

- **Edit** the Flow Management scripts and SceneReferencer directly for your experiment
- **Understand the code** - everything is transparent and open
- **Customize directly** - no need for unnecessary inheritance or wrappers
- **Own your experiment** - you have full control and visibility

Some systems like `ResXRDataManager_V2` provide convenient APIs (like custom data classes) to simplify common tasks, but the underlying code is still completely transparent for you to understand and modify if needed.

For questions or issues, refer to the demo experiments as working examples, but remember: you own your experiment code - modify it as needed for your research.

