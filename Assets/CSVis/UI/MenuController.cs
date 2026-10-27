using System;
using System.Collections.Generic;
using CSVis.Core;
using CSVis.ExampleLoader;
using CSVis.IO;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CSVis.UI
{
    /// <summary>
    /// The app's main menu: a single floating panel with a navigation stack (Home, Examples, categories), a grab handle,
    /// pin, minimise, and a footer with Back / Home. It is rebuilt from data on every navigation so adding an example
    /// is one entry in <see cref="BuildScreens"/>.
    /// </summary>
    public sealed class MenuController : MonoBehaviour
    {
        public const string HomeScreen = "home";
        public const string ExamplesScreen = "examples";
        public const string StaticScreen = "static";
        public const string DynamicScreen = "dynamic";
        public const string MapsScreen = "maps";

        private const float ConfirmWindowSeconds = 3.5f;

        private sealed class CardDef
        {
            public string Key;
            public string Title;
            public string Description;
            public Sprite Icon;
            public Action OnClick;
        }

        private sealed class ScreenDef
        {
            public string Title;
            public string Subtitle;
            public List<CardDef> Cards = new List<CardDef>();
        }

        private readonly Dictionary<string, ScreenDef> screens = new Dictionary<string, ScreenDef>();
        private readonly List<string> history = new List<string>();
        private readonly List<GameObject> cardObjects = new List<GameObject>();

        private CSVisUIAssets assets;
        private Canvas panelCanvas;
        private RectTransform panelRect;
        private Canvas miniCanvas;

        private TextMeshProUGUI title;
        private TextMeshProUGUI subtitle;
        private Image divider;
        private Image footerDivider;
        private UIButton backButton;
        private UIButton homeButton;
        private UIButton pinButton;
        private TextMeshProUGUI status;

        private SolverHandler solverHandler;
        private bool pinned;
        private bool minimized;

        private string current = HomeScreen;
        private bool confirmingClear;
        private float confirmUntil;

        public static MenuController Instance { get; private set; }

        public string CurrentScreen => current;

        public bool IsPinned => pinned;

        public bool IsMinimized => minimized;

        public static MenuController Create(CSVisUIAssets assets)
        {
            var go = new GameObject("CSVis Menu");
            var menu = go.AddComponent<MenuController>();
            menu.Build(assets);
            return menu;
        }

        private void Build(CSVisUIAssets uiAssets)
        {
            Instance = this;
            assets = uiAssets;

            BuildScreens();
            BuildChrome();
            PlaceInFrontOfUser();
            ConfigureFollowing();
            Show(HomeScreen, false);

            PlotInstance.CountChanged += OnPlotsChanged;
        }

        private System.Collections.IEnumerator Start()
        {
            // On a headset the head pose is only valid once tracking has started a moment after launch
            yield return new WaitForSeconds(0.75f);
            if (!pinned)
            {
                PlaceInFrontOfUser();
            }
        }

        private void OnDestroy()
        {
            PlotInstance.CountChanged -= OnPlotsChanged;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ---------------------------------------------------------------- screens

        private void BuildScreens()
        {
            screens[HomeScreen] = new ScreenDef
            {
                Title = "CSVis",
                Subtitle = "Mixed reality data visualization",
                Cards =
                {
                    new CardDef
                    {
                        Key = "OpenFile", Title = "Open a CSV file", Description = "Plot your own data in the room around you",
                        Icon = IconFactory.Get(UIIcon.Folder), OnClick = OnOpenFile,
                    },
                    new CardDef
                    {
                        Key = "Examples", Title = "Example plots", Description = "Explore ready-made plots and maps",
                        Icon = assets.logo, OnClick = () => Show(ExamplesScreen),
                    },
                    new CardDef
                    {
                        Key = "ClearAll", Title = "Clear all plots", Description = string.Empty,
                        Icon = IconFactory.Get(UIIcon.Trash), OnClick = OnClearAll,
                    },
                },
            };

            screens[ExamplesScreen] = new ScreenDef
            {
                Title = "Examples",
                Subtitle = "Choose a category",
                Cards =
                {
                    new CardDef
                    {
                        Key = "Static", Title = "Static plots", Description = "2D and 3D scatter plots of real datasets",
                        Icon = assets.staticPlots, OnClick = () => Show(StaticScreen),
                    },
                    new CardDef
                    {
                        Key = "Dynamic", Title = "Dynamic plots", Description = "Watch data move through space over time",
                        Icon = assets.dynamicPlots, OnClick = () => Show(DynamicScreen),
                    },
                    new CardDef
                    {
                        Key = "Maps", Title = "Geographic maps", Description = "Data placed on real-world maps",
                        Icon = assets.geographicMaps, OnClick = () => Show(MapsScreen),
                    },
                },
            };

            screens[StaticScreen] = new ScreenDef
            {
                Title = "Static plots",
                Subtitle = "Tap one to place it in front of you",
                Cards =
                {
                    Example("SineWave", "Sine wave", "A simple 2D sine wave", assets.sineWave, typeof(SineWavePlotLoader)),
                    Example("Iris", "Iris data set", "Flower measurements clustered in 3D", assets.iris, typeof(IrisDataSetPlotLoader)),
                    Example("Electron", "Electron scattering", "A distribution of 2,500 points", assets.electron,
                        typeof(ElectronScatteringDistributionPlotHelper)),
                },
            };

            screens[DynamicScreen] = new ScreenDef
            {
                Title = "Dynamic plots",
                Subtitle = "Animated with the dynamic plotting library",
                Cards =
                {
                    Example("DynSine", "Animated sine wave", "The static sine wave, in motion", assets.sineWave,
                        typeof(DynamicSineWavePlotLoader)),
                    Example("DynGeo", "Geometric rotation", "20 points of a dodecahedron rotating", assets.dodecahedron,
                        typeof(DynamicGeometricRotationExample)),
                    Example("DynDrone", "Drone flight", "A real drone flight path, animated", assets.drone,
                        typeof(DynamicDronePlotLoader)),
                },
            };

            screens[MapsScreen] = new ScreenDef
            {
                Title = "Geographic maps",
                Subtitle = "Needs an internet connection",
                Cards =
                {
                    Example("MapCampus", "Campus map", "Review ratings on a map of the campus", assets.geographicMaps,
                        typeof(CampusMapPlotLoader), true),
                    Example("MapDrone", "Drone altitude", "Flight altitudes as a histogram", assets.drone,
                        typeof(DroneMapPlotLoader), true),
                    Example("MapParliament", "Parliament of Canada", "3D terrain of Parliament Hill", assets.parliament,
                        typeof(Parliament3DTerrainMapLoader), true),
                },
            };
        }

        private static CardDef Example(string key, string title, string description, Sprite icon, Type loader, bool map = false)
        {
            return new CardDef
            {
                Key = key,
                Title = title,
                Description = description,
                Icon = icon,
                OnClick = () => PlotCommands.RunExample(loader, title, map),
            };
        }

        // ---------------------------------------------------------------- chrome (the parts every screen shares)

        private void BuildChrome()
        {
            panelCanvas = UIBuilder.CreateCanvas("Panel", transform, new Vector2(UITheme.PanelWidth, 400f));
            panelRect = (RectTransform)panelCanvas.transform;
            UIBuilder.Backplate(assets, panelCanvas.transform, UITheme.PanelFill);

            BuildGrabHandle();

            var pad = UITheme.PanelPadding;
            var headerY = 24f;
            var buttonsWidth = 2f * UITheme.IconButton + 6f;

            title = UIBuilder.Text(assets, panelCanvas.transform, "Title", string.Empty, UITheme.TitleSize, UITheme.TextPrimary, true);
            UIBuilder.Place(title.rectTransform, pad, headerY, UITheme.PanelWidth - 2f * pad - buttonsWidth - 8f, 32f);

            subtitle = UIBuilder.Text(assets, panelCanvas.transform, "Subtitle", string.Empty, UITheme.SubtitleSize, UITheme.TextSecondary);
            UIBuilder.Place(subtitle.rectTransform, pad, headerY + 33f, UITheme.PanelWidth - 2f * pad - buttonsWidth - 8f, 20f);

            var minimize = UIBuilder.IconButton(assets, panelCanvas.transform, "Minimize",
                UITheme.PanelWidth - pad - UITheme.IconButton, headerY + 2f, UITheme.IconButton, UIIcon.Minimize, UITheme.TextPrimary);
            minimize.AddListener(() => SetMinimized(true));

            pinButton = UIBuilder.IconButton(assets, panelCanvas.transform, "Pin",
                UITheme.PanelWidth - pad - 2f * UITheme.IconButton - 6f, headerY + 2f, UITheme.IconButton, UIIcon.Pin, UITheme.TextSecondary);
            pinButton.AddListener(() => SetPinned(!pinned));

            divider = UIBuilder.Bar(panelCanvas.transform, "Divider", UITheme.Divider);
            UIBuilder.Place(divider.rectTransform, pad, headerY + UITheme.HeaderHeight, UITheme.PanelWidth - 2f * pad, 1.5f);

            footerDivider = UIBuilder.Bar(panelCanvas.transform, "FooterDivider", UITheme.Divider);

            backButton = UIBuilder.LabelButton(assets, panelCanvas.transform, "Back", pad, 0f,
                UITheme.FooterButtonWidth, 40f, "Back", UIIcon.Back, UITheme.TextPrimary);
            backButton.AddListener(Back);

            homeButton = UIBuilder.LabelButton(assets, panelCanvas.transform, "Home", pad + UITheme.FooterButtonWidth + 8f, 0f,
                UITheme.FooterButtonWidth, 40f, "Home", UIIcon.Home, UITheme.TextPrimary);
            homeButton.AddListener(() => Show(HomeScreen));

            status = UIBuilder.Text(assets, panelCanvas.transform, "Status", string.Empty, UITheme.FooterSize, UITheme.TextSecondary,
                false, TextAlignmentOptions.MidlineRight);

            // Minimised state: a single logo button
            miniCanvas = UIBuilder.CreateCanvas("Mini", transform, new Vector2(84f, 84f));
            UIBuilder.Backplate(assets, miniCanvas.transform, UITheme.PanelFill);
            var mini = UIBuilder.SpriteButton(assets, miniCanvas.transform, "Maximize", 6f, 6f, 72f, assets.logo, Color.white);
            mini.AddListener(() => SetMinimized(false));
            miniCanvas.gameObject.SetActive(false);
        }

        private void BuildGrabHandle()
        {
            var handle = UIBuilder.Empty("GrabHandle", panelCanvas.transform);
            UIBuilder.Place(handle, (UITheme.PanelWidth - 190f) / 2f, 0f, 190f, 26f);

            var pill = UIBuilder.Bar(handle, "Pill", new Color(1f, 1f, 1f, 0.35f));
            UIBuilder.Place(pill.rectTransform, 63f, 10f, 64f, 6f);

            // Components that read their settings in Awake are added to an inactive object first
            handle.gameObject.SetActive(false);
            var collider = handle.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(190f, 26f, 24f);
            collider.center = new Vector3(95f, -13f, 0f);

            var manipulator = handle.gameObject.AddComponent<ObjectManipulator>();
            manipulator.HostTransform = transform;
            manipulator.AllowedManipulations = TransformFlags.Move | TransformFlags.Rotate;
            manipulator.firstSelectEntered.AddListener(_ => SetPinned(true));
            handle.gameObject.SetActive(true);
        }

        private void PlaceInFrontOfUser()
        {
            var camera = Camera.main;
            var origin = camera != null ? camera.transform.position : new Vector3(0f, 1.6f, 0f);
            var forward = camera != null ? camera.transform.forward : Vector3.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;
            forward = Quaternion.AngleAxis(CSVis.Core.DesktopPreview.MenuYaw, Vector3.up) * forward;

            transform.SetPositionAndRotation(
                origin + forward * CSVis.Core.DesktopPreview.MenuDistance + Vector3.down * UITheme.MenuDropMeters,
                Quaternion.LookRotation(forward, Vector3.up));
        }

        /// <summary>The menu drifts into view when the user turns away, until they pin it or grab it.</summary>
        private void ConfigureFollowing()
        {
            solverHandler = gameObject.AddComponent<SolverHandler>();
            var radial = gameObject.AddComponent<RadialView>();
            radial.MinDistance = 0.7f;
            radial.MaxDistance = 1.0f;
            radial.MinViewDegrees = 0f;
            radial.MaxViewDegrees = 28f;
            radial.OrientToReferenceDirection = true;
        }

        // ---------------------------------------------------------------- navigation

        public void Show(string screenId, bool push = true)
        {
            if (!screens.TryGetValue(screenId, out var screen))
            {
                Debug.LogWarning("Unknown menu screen: " + screenId);
                return;
            }

            if (push && screenId != current)
            {
                history.Add(current);
            }

            if (screenId == HomeScreen)
            {
                history.Clear();
            }

            current = screenId;
            confirmingClear = false;
            Rebuild(screen);
        }

        public void Back()
        {
            if (history.Count == 0)
            {
                Show(HomeScreen, false);
                return;
            }

            var previous = history[history.Count - 1];
            history.RemoveAt(history.Count - 1);
            Show(previous, false);
        }

        private void Rebuild(ScreenDef screen)
        {
            foreach (var card in cardObjects)
            {
                if (card != null)
                {
                    // Deactivate immediately: Destroy is deferred to the end of the frame
                    card.SetActive(false);
                    Destroy(card);
                }
            }

            cardObjects.Clear();

            var pad = UITheme.PanelPadding;
            var contentWidth = UITheme.PanelWidth - 2f * pad;
            title.text = screen.Title;
            subtitle.text = screen.Subtitle;

            var y = 24f + UITheme.HeaderHeight + 14f;
            foreach (var card in screen.Cards)
            {
                var def = card;
                var button = UIBuilder.Card(assets, panelCanvas.transform, def.Key, pad, y, contentWidth, UITheme.CardHeight,
                    def.Title, def.Description, def.Icon, Color.white);
                button.AddListener(() => ErrorReporter.Guard("complete that action", def.OnClick));
                cardObjects.Add(button.gameObject);
                y += UITheme.CardHeight + UITheme.CardGap;
            }

            // Home has nothing to go back to, so it gets a slimmer footer with just the plot counter
            var atHome = current == HomeScreen;
            y += 4f;
            footerDivider.gameObject.SetActive(!atHome);
            if (!atHome)
            {
                UIBuilder.Place(footerDivider.rectTransform, pad, y, contentWidth, 1.5f);
                y += 9f;
            }

            backButton.gameObject.SetActive(!atHome);
            homeButton.gameObject.SetActive(!atHome);
            UIBuilder.Place((RectTransform)backButton.transform, pad, y, UITheme.FooterButtonWidth, 40f);
            UIBuilder.Place((RectTransform)homeButton.transform, pad + UITheme.FooterButtonWidth + 8f, y, UITheme.FooterButtonWidth, 40f);
            UIBuilder.Place(status.rectTransform, UITheme.PanelWidth - pad - 150f, y, 150f, atHome ? 24f : 40f);

            panelRect.sizeDelta = new Vector2(UITheme.PanelWidth, y + (atHome ? 24f : 40f) + pad);
            RefreshDynamic();
        }

        // ---------------------------------------------------------------- actions

        private void OnOpenFile()
        {
            FilePicker.PickAndPlot();
        }

        private void OnClearAll()
        {
            var count = PlotManager.Count;
            if (count == 0)
            {
                Notifier.Info("There are no plots to clear.");
                return;
            }

            if (!confirmingClear)
            {
                // Destructive: ask for a second tap rather than a modal dialog
                confirmingClear = true;
                confirmUntil = Time.unscaledTime + ConfirmWindowSeconds;
                RefreshDynamic();
                return;
            }

            confirmingClear = false;
            PlotManager.ClearAll();
            Notifier.Success("Cleared " + count + (count == 1 ? " plot." : " plots."));
        }

        public void SetPinned(bool value)
        {
            pinned = value;
            if (solverHandler != null)
            {
                solverHandler.UpdateSolvers = !value;
            }

            pinButton.SetIconColor(value ? UITheme.Accent : UITheme.TextSecondary);
        }

        public void SetMinimized(bool value)
        {
            minimized = value;
            panelCanvas.gameObject.SetActive(!value);
            miniCanvas.gameObject.SetActive(value);
        }

        // ---------------------------------------------------------------- live values

        private void OnPlotsChanged()
        {
            RefreshDynamic();
        }

        private void Update()
        {
            if (confirmingClear && Time.unscaledTime > confirmUntil)
            {
                confirmingClear = false;
                RefreshDynamic();
            }
        }

        /// <summary>Updates the bits of the menu that depend on live state (plot count, clear confirmation).</summary>
        private void RefreshDynamic()
        {
            if (status == null)
            {
                return;
            }

            var count = PlotManager.Count;
            status.text = count == 0 ? string.Empty : count + " of " + PlotManager.MaxPlots + " plots";

            foreach (var card in cardObjects)
            {
                if (card == null || card.name != "ClearAll")
                {
                    continue;
                }

                var button = card.GetComponent<UIButton>();
                button.SetEnabled(count > 0);
                button.Title.color = confirmingClear ? UITheme.Danger : UITheme.TextPrimary;
                button.SetTitle(confirmingClear ? "Tap again to confirm" : "Clear all plots");
                button.SetSubtitle(count == 0
                    ? "There are no plots in the scene"
                    : confirmingClear
                        ? "This removes " + count + (count == 1 ? " plot" : " plots") + " from the room"
                        : "Remove " + count + (count == 1 ? " plot" : " plots") + " from the room");
                button.SetIconColor(confirmingClear ? UITheme.Danger : Color.white);
            }
        }
    }
}
