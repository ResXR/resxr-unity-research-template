# Third-Party Notices

This project uses third-party components that are licensed separately from the project's Apache License 2.0. The Meta XR SDK is installed through Unity Package Manager and is not included in this repository. The libraries and content listed after it are included in this repository.

---

## Meta XR SDK

This project requires the Meta XR SDK (version 78.0.0), which is automatically installed via Unity Package Manager based on package dependencies defined in `Packages/manifest.json`. The SDK itself is not included in this repository.

Copyright © Meta Platform Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Meta SDK License Agreement.

The Meta XR SDK is not licensed under the Apache License 2.0 and is subject to its own license terms. The Meta SDK License Agreement can be found at the official Meta developer documentation or within the installed package.

---

## Bundled libraries (vendored under `Assets/`)

The following libraries are included in this repository and are licensed separately from the Apache 2.0 project license.

| Library | Location | Copyright and license |
|--------|----------|--------|
| **UniTask** 2.5.10 | `Assets/Plugins/UniTask/` | Async/await for Unity. Copyright (c) 2019 Yoshifumi Kawai / Cysharp, Inc. MIT License (full text below). [UniTask](https://github.com/Cysharp/UniTask) |
| **NaughtyAttributes** 2.1.4 | `Assets/NaughtyAttributes/` | Inspector attributes (e.g. `[ShowIf]`, `[Button]`). Copyright (c) 2017 Denis Rizov. MIT License (full text below). [NaughtyAttributes](https://github.com/dbrizov/NaughtyAttributes) |
| **DOTween** | `Assets/Plugins/Demigiant/DOTween/` | Tweening library. DOTween and DOTween Pro are copyright (c) 2014-2018 Daniele Giardini - Demigiant. License and terms: [DOTween license](http://dotween.demigiant.com/license.php); see also `readme.txt` in that folder. |
| **Mobile Dependency Resolver** 1.2.185 | `Assets/MobileDependencyResolver/` | Copyright (C) 2014 Google Inc. Apache License 2.0 (see `Editor/LICENSE` in that folder). |

---

## Bundled content (fonts, models, textures, sounds, images)

| Content | Location | Copyright and license |
|--------|----------|--------|
| **TextMesh Pro Essential Resources** | `Assets/TextMesh Pro/`, `Assets/Plugins/TextMesh Pro/` | TextMesh Pro copyright (c) 2014-2019 Unity Technologies ApS. [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license). Includes the Liberation Sans font (Digitized data copyright (c) 2010 Google Corporation; Copyright (c) 2012 Red Hat, Inc.; SIL Open Font License 1.1, see `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`) and EmojiOne sample sprites (see `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt`). |
| **Nunito, Quicksand, Rubik** (Google Fonts) | `Assets/ResXR/Demo Experiments/Shared Utils/Google Fonts/` | Copyright 2014 The Nunito Project Authors; Copyright 2011 The Quicksand Project Authors, with Reserved Font Name "Quicksand"; Copyright 2015 The Rubik Project Authors. SIL Open Font License 1.1 (`OFL.txt` in each font's folder). |
| **Poppins** (Google Fonts) | `Assets/ResXR/Demo Experiments/Shared Utils/Google Fonts/Poppins/` | Copyright 2020 The Poppins Project Authors. SIL Open Font License 1.1 (`OFL.txt` in that folder). |
| **"The Thinker by Auguste Rodin"** 3D model | `Assets/ResXR/Demo Experiments/Museum/3D Models/Gallery Room/Statue/` | By Rigsters, [Sketchfab](https://sketchfab.com/3d-models/the-thinker-by-auguste-rodin-08a1e693c9674a3292dec2298b09e0ae), licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Changes: exported to FBX from Blender for use in Unity. |
| **Metal 046 B** texture | `Assets/ResXR/Demo Experiments/Museum/Textures/Metal/` | From [ambientCG](https://ambientcg.com/view?id=Metal046B), CC0 1.0. |
| **Button click sound** (`Click_4.mp3`) | `Assets/ResXR/Demo Experiments/Shared Utils/Game Button/sound/` | Edited from "click.wav" by swordmaster767, [Freesound](https://freesound.org/people/swordmaster767/sounds/365837/), CC0 1.0. |
| **Museum paintings** | `Assets/ResXR/Demo Experiments/Museum/Textures/Art Pieces Images/` | Generated with ChatGPT (GPT-4o); each image file carries its C2PA provenance record. |

---

## MIT License texts

### UniTask

```
The MIT License (MIT)

Copyright (c) 2019 Yoshifumi Kawai / Cysharp, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### NaughtyAttributes

```
MIT License

Copyright (c) 2017 Denis Rizov

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

ResXR thanks the authors and maintainers of these tools for making them available to the community.
