# Third-Party Dependencies

This project requires third-party components that are licensed separately and obtained through Unity Package Manager.

---

## Meta XR SDK

This project requires the Meta XR SDK (version 78.0.0), which is automatically installed via Unity Package Manager based on package dependencies defined in `Packages/manifest.json`. The SDK itself is not included in this repository.

Copyright © Meta Platform Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Meta SDK License Agreement.

The Meta XR SDK is not licensed under the Apache License 2.0 and is subject to its own license terms. The Meta SDK License Agreement can be found at the official Meta developer documentation or within the installed package.

---

## Bundled open-source Unity plugins (vendored under `Assets/`)

The following libraries are included in this repository (not only via Package Manager) and are licensed separately from the Apache 2.0 project license. See each package’s own files for full license text where provided.

| Library | Location | Notes |
|--------|----------|--------|
| **UniTask** | `Assets/Plugins/UniTask/` | Async/await for Unity (Cysharp). `package.json` declares **MIT**. Project: [UniTask](https://github.com/Cysharp/UniTask). |
| **NaughtyAttributes** | `Assets/NaughtyAttributes/` | Inspector attributes (e.g. `[ShowIf]`, `[Button]`). Commonly distributed under the **MIT License**; see [NaughtyAttributes](https://github.com/dbrizov/NaughtyAttributes). |
| **DOTween** | `Assets/Plugins/Demigiant/DOTween/` | Tweening library (Demigiant). License and terms: [DOTween license](http://dotween.demigiant.com/license.php); see also `readme.txt` in that folder. |

ResXR thanks the authors and maintainers of these tools for making them available to the community.
