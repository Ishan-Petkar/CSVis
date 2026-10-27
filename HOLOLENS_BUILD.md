# Building CSVis for HoloLens 2

CSVis was upgraded from Unity 2018.4 + MRTK 2.1 to **Unity 6 (6000.4.0f1) + MRTK3 + OpenXR** on the `unity6-mrtk3` branch.
Everything up to the final build works on macOS. The last step (building the UWP app and deploying it) needs Windows,
because Unity's Universal Windows Platform build support only exists for the Windows editor.

## What changed

| Area | Before | Now |
|---|---|---|
| Engine | Unity 2018.4.11f1 | Unity 6000.4.0f1 |
| XR | Legacy Windows MR (`com.unity.xr.windowsmr.metro`) | OpenXR + Microsoft Mixed Reality OpenXR plugin (HoloLens feature group) |
| Toolkit | MRTK 2.1 (copied into `Assets/`) | MRTK3 4.0.0-pre.2 (embedded under `Packages/`) |
| Menus | Hand-authored MRTK2 prefabs | Built in code from data with MRTK3 canvas buttons (`Assets/CSVis/UI/`): navigation stack, grab handle, pin, minimise, toasts, per-plot toolbar |
| Plot manipulation | `BoundingBox` + `ManipulationHandler` | `BoundsControl` + `ObjectManipulator` (`Assets/DataVisualizationToolkit/Utilities/Manipulation.cs`) |
| Shaders | MRTK Standard | Graphics Tools Standard (same look) |
| Scripting backend (UWP) | .NET | IL2CPP, ARM64 |

Notes on the packages under `Packages/`:
- `org.mixedrealitytoolkit.*` 4.0.0-pre.2 and `com.microsoft.mrtk.graphicstools.unity` 0.8.1 are embedded from the
  [MRTK GitHub releases](https://github.com/MixedRealityToolkit/MixedRealityToolkit-Unity/releases). Unity 6.4 forces
  XR Interaction Toolkit 3.x, which only MRTK 4.x supports, and the published 4.0 pre-releases declare a dependency on a
  `4.0.0` that does not exist. The embedded copies have that dependency corrected.
- `com.microsoft.mixedreality.openxr` 1.11.2 is embedded with a one-line patch in
  `Runtime/API/ARMarker/ARMarkerManager.cs` (AR Foundation 6 introduced clashing `XRMarker` type names).

Binary assets (DLLs, images, models, audio) are stored in **Git LFS**. After cloning, always run `git lfs pull`.

## Trying it on the Mac

1. Open the project in Unity 6000.4.0f1 and open `Assets/Scenes/MenuScene.unity`.
2. Press Play. The MRTK input simulator drives the scene:
   - hold **Space** (right hand) or **Left Shift** (left hand) and move the mouse to move a hand; **left click** to pinch/press
   - **W A S D / Q E** to move, hold **right mouse** to look around
3. Automated checks: **Window > General > Test Runner > PlayMode > Run All**.
   (`GeographicMapExamplesSpawn` needs internet; it is tagged `Network`.)

## Building for HoloLens 2 (Windows PC)

### One-time setup
1. **Unity Hub**: install **6000.4.0f1** with the modules **Universal Windows Platform Build Support** and
   **Windows Build Support (IL2CPP)**.
2. **Visual Studio 2022** with the workloads **Universal Windows Platform development** and
   **Desktop development with C++**, plus the components **C++ (v143) Universal Windows Platform tools**,
   **MSVC v143 ARM64 build tools** and a **Windows 11 SDK**.
3. **Git + Git LFS**, then:
   ```
   git clone https://github.com/Ishan-Petkar/CSVis.git
   cd CSVis
   git checkout unity6-mrtk3
   git lfs pull
   ```
4. On the HoloLens: **Settings > Update & Security > For developers** → enable **Developer Mode** and
   **Device Portal**. Note the device's IP address (Settings > Network & Internet).

### In Unity
1. Open the project and wait for the import to finish.
2. **File > Build Profiles > Universal Windows Platform > Switch Platform.**
3. Run **CSVis > Setup for HoloLens 2** from the menu bar. On the Mac the UWP OpenXR settings could not be
   created (no UWP module there); this fills them in. Then check
   **Edit > Project Settings > XR Plug-in Management > Windows (UWP) tab**: **OpenXR** ticked and the
   **Microsoft HoloLens** feature group ticked. Open **Project Validation** on that page and click **Fix All**.
4. **Signing certificate**: the certificate in the repo expired in 2020. Go to
   **Project Settings > Player > Publishing Settings > Certificate > Create...**, enter a name, leave the password empty.
5. In the build profile set **Architecture: ARM64**, **Build Type: D3D Project**, **Target SDK: Latest installed**,
   **Build configuration: Release**, then **Build** into a new folder such as `Builds/UWP` (git-ignored).

### In Visual Studio
1. Open `Builds/UWP/CSVis.sln`.
2. Set the configuration to **Release** and the platform to **ARM64**.
3. Deploy target:
   - USB: choose **Device**, or
   - Wi-Fi: choose **Remote Machine** and enter the HoloLens IP (Project > Properties > Debugging).
4. **Debug > Start Without Debugging**. The first deploy asks for a pairing PIN, shown on the HoloLens under
   Settings > Update & Security > For developers > **Pair**.

The app appears as **CSVis** in the HoloLens Start menu afterwards.

## Using your own CSV files

On the HoloLens choose **Open a CSV file** (in the editor this opens a native file dialog). The layout is detected from the column count:

| Columns | Plot |
|---|---|
| 2 | 2D plot: X, Y |
| 3 | 3D plot: X, Y, Z |
| 1 + 3n | Animated: time, then X, Y, Z for each of n points |
| 6 | Map: latitude, longitude, height, R, G, B (colours 0-1 or 0-255) |

Files with or without a header row, comma / semicolon / tab separated, with quoted values and a BOM are all fine. Rows that are not numbers are skipped
and reported. Limits that protect the headset: 5,000 points per static plot, 50 animated points, 300 map locations, 15 MB per file, 8 plots at once.

## Robustness and diagnostics

- Every user action is guarded: failures show a plain-language message in the headset (a toast) instead of crashing, and half-built plots are cleaned up.
- Unexpected errors are written to `csvis.log` (rotated at 512 KB). On the HoloLens: Device Portal > File explorer > `LocalState` > `csvis.log`.
- **CSVis > Validate Project** (also run automatically before every build) blocks the build with a clear message if Git LFS files are placeholders,
  the UWP signing certificate is missing or expired, or the UI assets are incomplete, and warns if OpenXR or the Mapbox token are missing.
- Tests: **Window > General > Test Runner**. EditMode covers the CSV parser (25 tests); PlayMode drives the real menus, every example, the plot toolbar
  and all the bad-file paths (17 tests; the maps test needs internet).

## Known limitations
- MRTK3 4.0 is a pre-release. If Microsoft publishes a stable 4.x, replace the embedded `Packages/org.mixedrealitytoolkit.*`
  folders with registry entries in `Packages/manifest.json`.
- The Mapbox Unity SDK is deprecated by Mapbox. The geographic map examples work with the token in
  `Assets/Resources/Mapbox/MapboxConfiguration.txt` (valid as of October 2026), but the SDK will not get updates.
- `Assets/MainScene.unity` is an old, unused MRTK2 scene; the app uses `Assets/Scenes/MenuScene.unity`.
- **Select File** opens the Windows file picker and only works on the device (UWP), not in the Mac editor.
