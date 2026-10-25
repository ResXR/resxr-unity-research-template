# Changelog

All notable changes to the ResXR Unity Research Template are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.1]

### Changed

- The Maze reward coin is a new model of the same size, made for this release (`Coin.fbx`: a flat face, a raised rim and a beveled edge), with a new material, `Coin Gold.mat`.
- The popup text in both Base Scenes uses Nunito Bold and the Live Monitor titles use Rubik Light (both under the SIL Open Font License), instead of Neue Haas Grotesk Display.
- `bundleVersion` is `0.2.1` in the project settings and the four build profiles (it held a build stamp from 25 May 2026).

### Fixed

- README: the editor output folder for the shipped settings (`Application.persistentDataPath`, with Export In Editor off) and the Quest 3S in the device list.
- `ResXR_Template_Documentation.md`: the Unity version (6000.0.68f1), the Quest 3S, the pinch thresholds (0.99 and 0.97), logging examples that use the template's API (`ReportEvent`, `ChoiceEvents.Log`), the face file (`FaceExpressionData.csv`, separate from `ContinuousData.csv`) and the Maze's fixed layout.
- In-tree data manager docs: the hand-bone frame (the SDK's tracking space), `schema_rev` "3", the face file name and the Record Face Expressions toggle.

### Removed

- The Neue Haas Grotesk Display font files (a commercial typeface): four TTF files and four TextMesh Pro font assets.
- The previous Maze coin model, its five textures, and its material and shader graph.
- Unused assets: `hopper-mural.jpg` and its material; the unused Museum frame model (`Frame_V2.fbx`) with its material, shader graph and textures; a Windows desktop screenshot texture in the Base Scene room folder; `arrowleft.png`; `Pinch Illustration.png`; `Click_1.mp3` to `Click_3.mp3`; two unused Metal 046 B texture maps.
- Two `.meta` files of folders that do not exist.

### Licensing and project files

- LICENSE is the Apache License 2.0 text with the template's copyright line. The appended paragraph saying that the template includes the Meta XR SDK is removed: Unity Package Manager installs the SDK, and it is not in this repository. NOTICE points to THIRD_PARTY_NOTICES.md.
- THIRD_PARTY_NOTICES.md lists every bundled library and the bundled third-party fonts, model, textures, sound and images, with copyright lines and license terms, including the full MIT texts of UniTask and NaughtyAttributes.
- New `CITATION.cff` (version 0.2.1) and this changelog.
- `.gitignore` ignores Android keystore files (`*.keystore`, `*.jks`).

## [0.2.0] - 2026-06-23

- First tagged release of the ResXR Unity Research Template.
