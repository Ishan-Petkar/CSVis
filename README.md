<img src="https://i.imgur.com/6l2zpmP.png" width="322" height="100"> 

## Unity 6 / HoloLens 2 update

This branch (`unity6-mrtk3`) modernises the original 2018 project: **Unity 6 + MRTK3 + OpenXR** for HoloLens 2, a rebuilt professional UI, a robust CSV pipeline and production hardening.

### Screenshots

| | |
|---|---|
| ![Home menu](docs/screenshots/home-menu.png) | ![Static plots and the Iris plot](docs/screenshots/static-plots-menu-and-iris-plot.png) |
| **Home menu**: open a CSV, browse examples, clear plots | **Static plots**: 3D Iris scatter plot with its toolbar |
| ![Electron scattering](docs/screenshots/electron-scattering-2500-points.png) | ![Animated drone flight](docs/screenshots/animated-drone-flight-plot.png) |
| **Electron scattering**: 2,500 points | **Animated drone flight path** |
| ![Opening a CSV file](docs/screenshots/opening-a-csv-file-3d-plot.png) | ![Friendly error message](docs/screenshots/friendly-error-message-for-bad-file.png) |
| **Your own CSV file**, plotted in 3D | **Clear messages** for bad files |
| ![Clear all confirmation](docs/screenshots/clear-all-asks-for-confirmation.png) | |
| **Clear all** asks for confirmation | |

- **Open a CSV file**: layout detected automatically (2 cols = 2D, 3 = 3D, 1+3n = animated, 6 = map).
- **Example plots**: static, animated and geographic map examples; grab, move, rotate and scale every plot.
- **Safe by design**: friendly error messages, plot limits, confirm-before-clear, error log, build validator.
- **Tested**: 25 EditMode + 17 PlayMode tests.

Docs: [Architecture](docs/ARCHITECTURE.md) | [HoloLens build and deploy guide](HOLOLENS_BUILD.md)

Try it without a headset: open `Assets/Scenes/MenuScene` in Unity 6000.4.0f1 and press Play (hold Space + mouse to move the hand, left-click to press). Binary assets use Git LFS: run `git lfs pull` after cloning.

---

## CSVis
  CSVis is a play on words for Comma Separated Values Visualisation. A Unity based CSV data visualizer application for creating and viewing data plots in mixed reality through the Microsoft HoloLens. 

<img align="right" src="https://i.imgur.com/QTaI4Gi.png" width="188" height="75">
<img align="right" src="https://i.imgur.com/CkOymcy.png" width="188" height="75">

  The application uses the open source [Data Visualization Toolkit](https://github.com/jman19/DataVisualizationPlotter) and the 
  [MixedRealityToolkit-Unity](https://github.com/Microsoft/MixedRealityToolkit-Unity/releases) by Microsoft to plot 3D data in Mixed Reality space to offer a different perspective in viewing 3D data.

## What can it do?
  1. Load premade example plots using the DVT porject and shocase it's useage through the HoloLnes
  1. CSVis can also plot static 2D / 3D data plots from predefined examples
  1. Plots created can also be manipulated with hand gestures for placement of the plots in your surroundings
 
The types of plots that are supported by the DVT are:

<img align="left" src="https://i.imgur.com/nZqD1B3.png" width="244" height="50"><br/><br/>

<img align="left" src="https://i.imgur.com/LTYmoCV.png" width="292" height="50"><br/><br/>

<img align="left" src="https://i.imgur.com/QoxnW74.png" width="351" height="50"><br/><br/>

---

<img src="https://i.imgur.com/45nOaU9.jpg" width="324" height="324"><img src="https://i.imgur.com/1Zo1bSG.jpg" width="324" height="324">
<img src="https://i.imgur.com/LyyuP3g.jpg" width="648" height="365">
