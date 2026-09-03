# Third Party Notices

BovineLabs Core includes or adapts the following third-party software. Each component remains under its original license.

## CodeGenHelpers

CodeGenHelpers is copyright © 2020 Dan Siegel and is licensed under the MIT License. The complete license text is retained alongside both the
[`runtime code`](BovineLabs.Core/CodeGenHelpers/LICENSE) and the [`source-generator source`](SourceGenerators~/CodeGenHelpers/LICENSE).

## BlobCurve

BlobCurve is copyright © 2020 Lieene@ShadeRealm and is licensed under the MIT License. The complete license text is retained at
[`BovineLabs.Core/Collections/Blobs/Curve/LICENSE-3RD-PARTY.md`](BovineLabs.Core/Collections/Blobs/Curve/LICENSE-3RD-PARTY.md).

## BlobHashMap

BlobHashMap is copyright © 2021 Bart van de Sande and is licensed under the MIT License. The complete license text is retained at
[`BovineLabs.Core/Collections/Blobs/HashMap/LICENSE-3RD-PARTY.md`](BovineLabs.Core/Collections/Blobs/HashMap/LICENSE-3RD-PARTY.md).

## LZ4

LZ4 is copyright © 2011-2016 Yann Collet and is licensed under its BSD-style license. The complete license text is retained at
[`Libraries/lz4-1.9.1/LICENSE`](Libraries/lz4-1.9.1/LICENSE).

## LibraryLoader

`BovineLabs.Core/Utility/LibraryLoader.cs` is adapted from
[`LLMUnity/Runtime/LLMLib.cs`](https://github.com/undreamai/LLMUnity/blob/b64c24566fb8ec17bfb426cb5e4728393af0e9b3/Runtime/LLMLib.cs),
which is copyright (c) 2023 Undream AI and licensed under the MIT License. That upstream source is itself adapted from
[`SkiaForUnity/LibraryLoader.cs`](https://github.com/ammariqais/SkiaForUnity/blob/f43322218c736d1c41f3a3df9355b90db4259a07/SkiaUnity/Assets/SkiaSharp/SkiaSharp-Bindings/SkiaSharp.HarfBuzz.Shared/HarfBuzzSharp.Shared/LibraryLoader.cs),
which is copyright (c) 2023 Qais Ammari and licensed under the MIT License. Both complete license texts are retained in
[`BovineLabs.Core/Utility/LibraryLoader.LICENSE.md`](BovineLabs.Core/Utility/LibraryLoader.LICENSE.md).

## UnityMeshSimplifier / Fast Quadric Mesh Simplification

`BovineLabs.Core/Utility/Mesh/MeshSimplifier.cs` is adapted from
[`UnityMeshSimplifier`](https://github.com/Whinarn/UnityMeshSimplifier), copyright (c) 2017-2021 Mattias Edlund, which is licensed under the MIT
License. UnityMeshSimplifier is itself based on
[`Fast Quadric Mesh Simplification`](https://github.com/sp4cerat/Fast-Quadric-Mesh-Simplification), copyright (c) 2014 Sven Forstmann, which is
also licensed under the MIT License. Both complete notices are retained at
[`BovineLabs.Core/Utility/Mesh/MeshSimplifier.LICENSE.md`](BovineLabs.Core/Utility/Mesh/MeshSimplifier.LICENSE.md).

## Unity Technologies

The following Core source includes code adapted from Unity packages:

- `BovineLabs.Core/Utility/Mesh/TerrainToMesh.cs` from `com.unity.render-pipelines.core`.
- `BovineLabs.Core/Collections/BitArray.cs` from `com.unity.render-pipelines.core`.
- `BovineLabs.Core.Editor/SearchWindow` from `com.unity.platforms`.
- `BovineLabs.Core.Editor/Inspectors/BaseFieldInspector.cs` and part of `DynamicListElement.cs` from `com.unity.entities`.
- `BovineLabs.Core/Utility/SpinLock.cs` from `com.unity.collections`.
- `BovineLabs.Core/Utility/TypeManagerUtil.cs` from `com.unity.entities`.
- `SourceGenerators~/CodeGenHelpers/SourceGenHelpers.cs` from Netcode for Entities.

The corresponding upstream package copyright notices are:

- `com.unity.render-pipelines.core` copyright © 2020 Unity Technologies ApS.
- `com.unity.platforms` copyright © 2020 Unity Technologies ApS.
- `com.unity.collections` copyright © 2024 Unity Technologies.
- `com.unity.entities` copyright © 2024 Unity Technologies.
- Netcode for Entities copyright © 2025 Unity Technologies.

These portions are licensed under the [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license) for Unity-dependent
projects. Unless expressly provided otherwise, the software under this license is made available strictly on an “AS IS” basis, without warranty
of any kind, express or implied. Review the license for these and other terms and conditions.
