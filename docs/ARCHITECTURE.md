# CSVis architecture (Unity 6 / MRTK3 version)

CSVis turns CSV files and built-in example datasets into interactive 3D plots floating in the user's room on a Microsoft HoloLens 2.

```
                    AppBootstrap (scene entry point)
                       |
      +----------------+------------------+
      |                |                  |
  ErrorReporter   ToastView           MenuController  ----- navigation stack of screens
  (csvis.log)     (Notifier UI)       (MRTK3 canvas UI, pin / grab / minimise)
                       ^                  |
                       |                  v
                   Notifier  <-----  PlotCommands  (limits, prerequisites, never throws)
                                          |
                +-------------------------+----------------------+
                |                                                |
        Example loaders (PlotLoader subclasses)        CustomFilePlotLoader
        sine / iris / electron / drone / maps          CsvTable parser -> 2D / 3D / animated / map
                |                                                |
                +----------------------+-------------------------+
                                       v
                               DataVisualizationToolkit
                       DataPlotter | DynamicPlotter | mrMap (Mapbox)
                                       |
                              PlotManager / PlotInstance
                       (placement, 8-plot limit, toolbar, teardown)
```

## Folders

| Path | Purpose |
|---|---|
| `Assets/CSVis/Core` | App bootstrap, plot manager, commands, notifier, error reporter, desktop preview |
| `Assets/CSVis/UI` | Code-built MRTK3 UI: theme, procedural icons, menu, toasts, plot toolbar |
| `Assets/CSVis/Csv` | Dependency-free CSV parser (own assembly so it is unit-testable) |
| `Assets/CSVis/IO` | File picker (UWP / editor) and the CSV-to-plot loader |
| `Assets/CSVis/ExampleLoader` | The built-in example plots |
| `Assets/DataVisualizationToolkit` | Plot rendering library (static, animated, map) |
| `Assets/CSVis/Editor` | Setup menu, build validator, UI asset builder |
| `Assets/CSVis/Tests` | EditMode (parser) and PlayMode (UI, examples, bad-file paths) tests |
| `Packages/` | MRTK3 4.0.0-pre.2, Graphics Tools, Microsoft OpenXR plugin (embedded, patched) |

## Design decisions

- **Menus are built from data in code**, not hand-authored prefabs: adding an example is one entry in `MenuController.BuildScreens`.
- **Every user action goes through `PlotCommands`/`ErrorReporter.Guard`**: failures become plain-language toasts and half-built plots are removed.
- **Limits protect the headset**: 8 plots, 5,000 points per static plot, 50 animated points, 300 map locations, 15 MB files.
- **Build validator** blocks a build for the problems that only show up on the device (Git LFS placeholders, expired certificate, missing OpenXR).
- **Desktop preview** mode makes the app usable and presentable in the Unity editor without a headset.
