# ResXR Research Template

A comprehensive Unity XR template designed specifically for Meta Quest research experiments. Build VR research applications with built-in support for hand tracking, eye tracking, face expression tracking, comprehensive data collection, and more.

> **⚠️ Note**: This project is still under construction. For inquiries, help, or support, please contact: **resxr.toolkit@gmail.com**

## 🎯 Overview

ResXR is a complete framework for building VR research applications on Meta Quest headsets. It provides:

- **Hand Tracking & Gesture Recognition** - Full hand tracking with pinch detection
- **Eye Tracking & Gaze Analysis** - Real-time eye gaze tracking and focused object detection
- **Face Expression Tracking** - Face expression weights and validity tracking
- **Comprehensive Data Collection** - Automatic CSV export of all tracking data
- **Scene Management** - Additive scene loading with smooth transitions

## 🧠 Core Philosophy: "Clear Box" Design

Unlike other solutions that provide black-box classes, ResXR is designed as a **transparent, clear box** where researchers **own and modify every part of their experiment**. The template provides structure, base classes, and examples, but you are expected to copy, modify, and customize the code for your specific research needs. Everything is open and transparent - you understand exactly how your experiment runs under the hood.

## 📋 Prerequisites

- **Unity 2021.3 or later**
- **Meta Quest SDK (OVR)** - Automatically installed via Unity Package Manager (see below)
- **Meta Quest headset** (Quest 2, Quest Pro, Quest 3)
- **Basic knowledge** of Unity and C#

## 🚀 Quick Start

### 1. Create a New Repository from Template

1. Click the **"Use this template"** button on GitHub
2. Create a new repository with your desired name
3. Clone your new repository locally:

```bash
git clone <your-repository-url>
cd <your-repository-name>
```

### 2. Open in Unity

1. Open Unity Hub
2. Click "Add" and select the cloned project folder
3. Unity will detect the project and open it

### 3. SDK Installation (Automatic)

This project requires the Meta XR SDK, which is automatically installed via Unity Package Manager based on package dependencies defined in `Packages/manifest.json`. The SDK itself is not included in this repository. When you open the project in Unity for the first time, Unity will automatically download and install the required Meta XR packages (version 78.0.0).

### 4. Create Your First Experiment

1. Navigate to `Assets/Project Folder/`
2. Duplicate `New ResXRScene [Duplicate].unity`
3. Rename it to your experiment name
4. Add it to Build Settings (after Base Scene)
5. **Important**: The Base Scene must be opened **with** your experiment scene additively. The Base Scene contains the player (`ResXRPlayer`) and data manager (`ResXRDataManager`) which run continuously throughout your experiment, even when scenes are changed. Your experiment scene will be loaded additively on top of the Base Scene.
6. Edit the `SceneReferencer` and Flow Management scripts (SessionManager, TaskManager, TrialManager) directly to add your experiment references and logic
7. Build and run!

## 📁 Project Structure

```
Assets/ResXR/
├── Base Scene/              # Core persistent scene and systems
│   ├── ResXRPlayer/        # Player controller, hand/eye/face tracking
│   ├── ResXRDataManager/# Data collection and export system
│   ├── SceneManagement/    # Scene loading and transitions
│   └── ResXR_RoomCalibrator/# Room-scale calibration
├── Flow Management/        # Session/Task/Trial flow control
├── Utilities/              # Helper scripts and utilities
│   ├── EditorUtilities/    # Editor tools
│   ├── General Scripts/    # Singleton, utilities, extensions
├── Detectors/              # Interaction detection system
├── Demo Experiments/       # Example implementations
│   ├── Binary Choice/      # Two-choice decision experiment
│   ├── Maze/               # Navigation experiment
│   └── Museum/             # Art viewing experiment
└── Meta components/        # Meta-specific integrations
```

## ✨ Key Features

### Data Collection
- **Automatic Continuous Data**: Head, hands, eyes, body, face tracking at 50Hz
- **Gaze**: Combined (cyclopean) gaze hit point and focused object always recorded when eye tracking is on. Optional **per-eye** hit points and focused objects (left/right) via the "Include Separate Eyes Gaze" recording option—enables 3 raycasts per frame instead of 1; turn off in heavy scenes to save performance.
- **Custom Event Logging**: Create custom data classes for experiment-specific events
- **Events table** (`Events.csv`): Template provides `ReportEvent` rows with `name`, `onset`, and `duration` (seconds). Use `Time.realtimeSinceStartup` for `onset` to match continuous data and downstream pipelines; call `ResXRDataManager.Instance.ReportEvent(...)`.
- **CSV Export**: All data exported to organized CSV files
- **Metadata**: Automatic session metadata generation (supports later Motion-BIDS export; includes device offset, tracking origin, reference frames; `build_info_available` flags whether build provenance fields are present, otherwise they are left empty)


### ResXRPlayer API - Easy Access to Tracking Components
While every VR app has tracking, ResXR provides a **simple, unified API** through `ResXRPlayer` singleton that gives you easy access to all tracking components without digging through OVR internals:

- **Hand Tracking**: `ResXRPlayer.Instance.HandLeft/HandRight` - Direct access to hand tracking, pinch detection, and finger colliders
- **Eye Tracking**: `ResXRPlayer.Instance.FocusedObject`, `EyeGazeHitPosition` (combined gaze, always when both eyes confident). Per-eye: `LeftEyeGazeHitPosition`, `RightEyeGazeHitPosition`, `LeftFocusedObject`, `RightFocusedObject`, `HasLeftEyeHit`, `HasRightEyeHit` when the separate-eyes recording option is enabled. The Data Manager sets whether ResXREyeTracker runs 1 raycast (combined only) or 3 (left, right, combined) via that option.
- **Face Tracking**: `ResXRPlayer.Instance.OVRFace` - Direct access to face expression weights and validity (OVRFaceExpressions is on the ResXRPlayer prefab and assigned in the Inspector)
- **Body Tracking**: Body joint positions and calibration
- **Player Transforms**: `PlayerHead`, `RightHand`, `LeftHand` - Easy access to player transforms
- **Input Managers**: `ControllersInputManager`, `PinchingInputManager` - Unified input handling

See `ResXRPlayer.cs` for the complete API. Access everything through `ResXRPlayer.Instance` - no need to find OVR components manually!

### Scene Management
- **Additive Loading**: Experiment scenes are loaded additively on top of the Base Scene, which must remain open throughout your experiment
- **Persistent Base Scene**: The Base Scene contains the player (`ResXRPlayer`) and data manager (`ResXRDataManager`) that run continuously, even when switching between experiment scenes
- **Smooth Transitions**: Automatic fade effects during scene changes
- **Player Repositioning**: Automatic player positioning per scene

### Interaction Systems
- **Pinching**: Hand-based pinch interaction with priority-based selection
- **Controllers**: Quest controller input with haptic feedback
- **Touch**: Collider-based touch detection

### Flow Management
- **Hierarchical Structure**: Session → Task → Trial organization
- **Edit the scripts directly**: Flow Management scripts (`SessionManager`, `TaskManager`, `TrialManager`) are intentionally simple stubs you modify directly for your experiment. They are part of the clear-box philosophy: you own them and edit them. By default some methods are placeholders; implement your own Start/End/Between logic directly in the scripts. The demo experiments ship with copies named e.g. `Maze_SessionManager` for convenience when including multiple experiments in one project.
- **Clear Ownership**: You own and modify your experiment code

## 📚 Documentation

- **[Full Component Documentation](ResXR_Template_Documentation.md)** - Comprehensive guide to all components
- **[Data Manager Documentation](Assets/ResXR/Base%20Scene/ResXRDataManager/Doc/ResXRDataManager_README.txt)** - Data collection system details
- **Demo Experiments** - Working examples in `Assets/ResXR/Demo Experiments/`

## 🎓 Learning Resources

### Demo Experiments

The template includes three complete demo experiments:

1. **Binary Choice** - Two-choice decision-making experiment
2. **Maze** - Navigation experiment with coin collection
3. **Museum** - Art viewing experiment with gaze tracking

Each demo shows:
- Flow Management structure (Session/Task/Trial)
- Custom data logging
- Interaction patterns
- Scene organization

### Getting Started Guide

1. **Read the Documentation** - Start with `ResXR_Template_Documentation.md`
2. **Explore Demo Experiments** - See working examples in `Assets/ResXR/Demo Experiments/`
3. **Duplicate the Template Scene** - Use `Assets/Project Folder/New ResXRScene [Duplicate].unity`
4. **Modify Directly** - Own your experiment code - modify scripts directly
5. **Build Your Experiment** - Add your research logic to the template structure

## 🔧 Usage Example

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

// Wait for pinch gesture
await player.PinchingInputManager.WaitForHoldAndRelease(HandType.Right, 1.0f);

// Pipeline-friendly event marker (Events.csv); onset uses same clock as continuous CSVs
ResXRDataManager.Instance.ReportEvent("stimulus_on", Time.realtimeSinceStartup, 0f);

// Choice trials: see LogChoice on ResXRDataManager for the full signature

// Switch scenes
await ResXRSceneManager.Instance.SwitchActiveScene("NextExperimentScene");
```

## 🏗️ Building Your Experiment

### Step 1: Create Your Scene
1. Duplicate `Assets/Project Folder/New ResXRScene [Duplicate].unity`
2. Rename to your experiment name
3. Add to Build Settings (after Base Scene)
4. **Scene Architecture**: The Base Scene and your experiment scene must be opened **additively** together. The Base Scene contains:
   - `ResXRPlayer` - Player controller with hand/eye/face tracking
   - `ResXRDataManager` - Data collection system
   - Other core systems that persist throughout your experiment
   
   These systems run continuously and remain active even when you switch between experiment scenes. Your experiment scene is loaded additively on top of the Base Scene, allowing you to change experiment content while keeping the player and data collection systems running.

### Step 2: Modify SceneReferencer
Open `SceneReferencer.cs` and add your experiment references:

```csharp
public class SceneReferencer : ResXRSingleton<SceneReferencer>
{
    [Header("My Experiment Objects")]
    public GameObject stimulus;
    public InstructionsPanel instructions;
    public Transform targetPosition;
    
    [Header("Configuration")]
    public float trialDuration = 10f;
}
```

### Step 3: Set Up Flow Management
The Flow Management scripts in `Assets/ResXR/Flow Management/` are template stubs you **edit directly**. Add SessionManager, TaskManager, and TrialManager to your scene and implement `StartSession`, `EndSession`, `BetweenTasksFlow`, `StartTrial`, `EndTrial`, etc. in those scripts. The demo experiments use renamed copies (e.g. `Maze_SessionManager`) only for convenience when shipping multiple experiments in one project.

```csharp
// Open SessionManager.cs (and TaskManager.cs, TrialManager.cs) and implement the placeholder methods
// Add the components to a GameObject in your experiment scene and configure tasks/trials in the Inspector
```

### Step 4: Add Your Research Logic
- Edit the Flow Management scripts to add your Start/End/Between logic
- Configure tasks and trials in the Inspector
- Add custom data classes for logging
- Implement your experiment-specific logic directly in SessionManager, TaskManager, and TrialManager

## 🤝 Contributing

This is a research template. Contributions, improvements, and feedback are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📄 License

This project is licensed under the Apache License, Version 2.0.

### Third-Party Dependencies

This project requires the Meta XR SDK, which is automatically installed via Unity Package Manager based on package dependencies defined in `Packages/manifest.json`. The SDK itself is not included in this repository.

The Meta XR SDK is provided by Meta Platform Technologies, LLC and its affiliates, and is licensed under the Meta SDK License Agreement. The Meta XR SDK is not covered by the Apache License 2.0 and is subject to its own license terms.

Additional third-party components (including open-source Unity plugins vendored under `Assets/`, such as **UniTask**, **NaughtyAttributes**, and **DOTween**) are listed with license notes in **[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)**.

## 🙏 Acknowledgments

ResXR builds upon the early work of the [TAUXR Research Template](https://github.com/TAU-XR/TAUXR-Research-Template), developed by the [TAU-XR Studio](https://github.com/TAU-XR) and [talmzip](https://github.com/talmzip).

Thanks to the authors of open-source tools used in this template, including **UniTask** (Cysharp), **NaughtyAttributes** (Denis Rizov / community), and **DOTween** (Demigiant), as well as other dependencies documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

We would like to thank the original contributors and developers for their work on the initial template, which helped shape the early direction of this project.

## 📞 Support

For questions, issues, or inquiries:
- **Email**: resxr.toolkit@gmail.com
- Check the [Full Documentation](ResXR_Template_Documentation.md)
- Review the Demo Experiments for examples
- Examine the source code (it's all transparent!)

## 🎯 Best Practices

1. **Own Your Code** - Edit the Flow Management scripts (SessionManager, TaskManager, TrialManager) directly; they are stubs you implement for your experiment
2. **Understand the System** - Read the code to understand how it works
3. **Use Demo Experiments** - Learn from working examples
4. **Log Everything** - Use ResXRDataManager for all experiment data
5. **Follow Flow Hierarchy** - Use Session → Task → Trial structure
6. **Keep It Transparent** - All code is open - understand and customize

---

**Remember**: ResXR is a "clear box" template. You own and modify your experiment code. Everything is transparent and open for you to understand and customize.
