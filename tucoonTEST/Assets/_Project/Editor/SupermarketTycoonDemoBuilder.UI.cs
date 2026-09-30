using SupermarketTycoon.Buildings;
using SupermarketTycoon.Expansion;
using SupermarketTycoon.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace SupermarketTycoon.Editor
{
    public static partial class SupermarketTycoonDemoBuilder
    {
        private const string ButtonSpritePath = "Assets/HONETi/mobile_cartoon_GUI/GUI Elements/Buttons/btn_big.png";
        private const string ButtonHoverSpritePath = "Assets/HONETi/mobile_cartoon_GUI/GUI Elements/Buttons/btn_big_hover.png";
        private const string ButtonPressedSpritePath = "Assets/HONETi/mobile_cartoon_GUI/GUI Elements/Buttons/btn_big_pressed.png";
        private const string PanelSpritePath = "Assets/HONETi/mobile_cartoon_GUI/GUI Elements/Panels/panel.png";
        private const string CoinSpritePath = "Assets/HONETi/mobile_cartoon_GUI/GUI Elements/Icons/icon_coins.png";

        private static Font cachedFont;

        private static Canvas CreateScreenCanvas(string name)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static LoadingScreenView CreateLoadingScreen(Transform parent)
        {
            var canvas = CreateScreenCanvas("Loading Canvas");
            canvas.sortingOrder = 1000;
            canvas.transform.SetParent(parent, false);

            var root = CreateUiObject("Loading Overlay", canvas.transform);
            Stretch(root.rectTransform);
            var image = root.gameObject.AddComponent<Image>();
            image.color = new Color(0.035f, 0.09f, 0.1f, 1f);
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var content = CreateUiObject("Loading Content", root.transform);
            SetRect(
                content.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(720f, 260f));
            ConfigureVerticalLayout(content.gameObject, new RectOffset(40, 40, 20, 20), 12f, TextAnchor.MiddleCenter);

            var title = CreateLayoutText(
                "Game Title",
                content.transform,
                "SUPERMARKET TYCOON",
                38,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                AccentYellow);
            SetLayout(title.gameObject, 0f, 70f, 1f);

            var loadingLabel = CreateLayoutText(
                "Loading Label",
                content.transform,
                "LOADING...",
                27,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
            SetLayout(loadingLabel.gameObject, 0f, 48f, 1f);

            var slider = CreateSlider(
                "Loading Progress",
                content.transform,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(560f, 30f));
            SetLayout(slider.gameObject, 560f, 32f, 0f);
            slider.interactable = false;

            var view = root.gameObject.AddComponent<LoadingScreenView>();
            view.Configure(group, slider);
            root.gameObject.SetActive(false);
            return view;
        }

        private static void CreateFloatingIncomePrefab()
        {
            var root = new GameObject("FloatingIncome", typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(220f, 70f);
                var text = root.AddComponent<Text>();
                text.font = GetFont();
                text.fontSize = 42;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(0.25f, 0.95f, 0.48f);
                text.raycastTarget = false;

                var view = root.AddComponent<FloatingIncomeView>();
                view.Configure(text, root.GetComponent<CanvasGroup>(), rect);
                PrefabUtility.SaveAsPrefabAsset(root, FloatingIncomePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static BuildSpot CreateWorldBuildSpot(
            string id,
            BuildingDefinition definition,
            Vector3 position,
            Quaternion rotation,
            Camera camera,
            string requiredExpansionId = null)
        {
            var root = new GameObject($"Build Spot - {id}");
            root.transform.SetPositionAndRotation(position, rotation);

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Build Area";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            marker.transform.localScale = new Vector3(2.2f, 0.04f, 1.55f);
            marker.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("BuildSpot", BuildSpotColor);
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());

            var placement = new GameObject("Building Placement").transform;
            placement.SetParent(root.transform, false);

            var canvasObject = new GameObject("Build Prompt", typeof(RectTransform));
            canvasObject.transform.SetParent(root.transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            canvasObject.transform.rotation = camera.transform.rotation;
            canvasObject.transform.localScale = Vector3.one * 0.008f;

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            canvasObject.AddComponent<GraphicRaycaster>();
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(350f, 124f);

            var button = CreateButton(
                "Build Button",
                canvasObject.transform,
                "+ BUILD",
                24,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(340f, 114f));
            var label = button.GetComponentInChildren<Text>();
            var view = canvasObject.AddComponent<BuildSpotView>();
            view.Configure(button, label, canvasObject);

            var spot = root.AddComponent<BuildSpot>();
            spot.Configure(id, definition, placement, view, requiredExpansionId);
            return spot;
        }

        private static StoreExpansionSpot CreateStoreExpansionSpot(
            StoreExpansionDefinition definition,
            Camera camera)
        {
            var root = new GameObject("Store Expansion");

            var lockedVisual = new GameObject("Locked Area");
            lockedVisual.transform.SetParent(root.transform, false);
            var barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "Expansion Barrier";
            barrier.transform.SetParent(lockedVisual.transform, false);
            barrier.transform.position = new Vector3(0f, 0.75f, 2.55f);
            barrier.transform.localScale = new Vector3(11.5f, 1.5f, 0.25f);
            barrier.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("ExpansionBarrier", AccentYellow);
            UnityEngine.Object.DestroyImmediate(barrier.GetComponent<Collider>());
            var obstacle = barrier.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            obstacle.carving = true;

            var unlockedVisual = new GameObject("Expanded Store Visual");
            unlockedVisual.transform.SetParent(root.transform, false);
            for (var i = -1; i <= 1; i += 2)
            {
                var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name = "Expansion Floor Trim";
                trim.transform.SetParent(unlockedVisual.transform, false);
                trim.transform.position = new Vector3(i * 5.8f, 0.08f, 4.65f);
                trim.transform.localScale = new Vector3(0.18f, 0.14f, 4f);
                trim.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("StoreGreen", StoreGreen);
                UnityEngine.Object.DestroyImmediate(trim.GetComponent<Collider>());
            }

            unlockedVisual.SetActive(false);

            var canvasObject = new GameObject("Expansion Prompt", typeof(RectTransform));
            canvasObject.transform.SetParent(root.transform, false);
            canvasObject.transform.position = new Vector3(0f, 2.1f, 1.8f);
            canvasObject.transform.rotation = camera.transform.rotation;
            canvasObject.transform.localScale = Vector3.one * 0.008f;
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 21;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(430f, 124f);
            var purchase = CreateButton(
                "Expansion Button",
                canvasObject.transform,
                "LOCKED AREA",
                23,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(420f, 114f));
            var label = purchase.GetComponentInChildren<Text>();

            var spot = root.AddComponent<StoreExpansionSpot>();
            spot.Configure(definition, purchase, label, lockedVisual, unlockedVisual);
            return spot;
        }

        private static void CreateMainMenuUi(
            Transform canvas,
            out MainMenuView mainMenuView,
            out SettingsView settingsView)
        {
            var tint = CreateUiObject("Backdrop Tint", canvas);
            Stretch(tint.rectTransform);
            tint.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.09f, 0.1f, 0.45f);

            var menuRoot = CreateUiObject("Main Menu", canvas);
            Stretch(menuRoot.rectTransform);

            var content = CreateUiObject("Menu Content", menuRoot.transform);
            SetRect(
                content.rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(520f, 760f));
            ConfigureVerticalLayout(content.gameObject, new RectOffset(45, 45, 20, 20), 16f, TextAnchor.MiddleCenter);

            var title = CreateLayoutText(
                "Title",
                content.transform,
                "SUPERMARKET\nTYCOON",
                70,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white);
            title.lineSpacing = 0.86f;
            SetLayout(title.gameObject, 0f, 190f, 1f);

            var subtitle = CreateLayoutText(
                "Subtitle",
                content.transform,
                "BUILD  |  SERVE  |  GROW",
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                AccentYellow);
            SetLayout(subtitle.gameObject, 0f, 52f, 1f);

            CreateLayoutSpacer("Menu Spacer", content.transform, 76f);

            var play = CreateButton("Play", content.transform, "PLAY", 32, Vector2.one * 0.5f, Vector2.zero, new Vector2(390f, 88f));
            var settings = CreateButton("Settings", content.transform, "SETTINGS", 28, Vector2.one * 0.5f, Vector2.zero, new Vector2(390f, 82f));
            var quit = CreateButton("Quit", content.transform, "QUIT", 26, Vector2.one * 0.5f, Vector2.zero, new Vector2(390f, 78f));
            SetLayout(play.gameObject, 390f, 88f, 0f);
            SetLayout(settings.gameObject, 390f, 82f, 0f);
            SetLayout(quit.gameObject, 390f, 78f, 0f);

            mainMenuView = menuRoot.gameObject.AddComponent<MainMenuView>();
            mainMenuView.Configure(
                menuRoot.gameObject,
                play,
                play.GetComponentInChildren<Text>(),
                settings,
                quit);

            settingsView = CreateSettingsUi(canvas);
        }

        private static void CreateGameUi(
            Transform canvas,
            out GameHudView hudView,
            out BuildingPanelView buildingPanelView,
            out EmployeeView employeeView,
            out OfflineIncomeView offlineIncomeView,
            out PauseMenuView pauseMenu,
            out StatsView statsView,
            out SettingsView settingsView)
        {
            var hud = CreateUiObject("HUD", canvas);
            Stretch(hud.rectTransform);

            var topBar = CreateUiObject("Top Bar", hud.transform);
            SetRect(topBar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-64f, 128f));
            var topImage = topBar.gameObject.AddComponent<Image>();
            topImage.color = new Color(0.025f, 0.13f, 0.14f, 0.94f);
            ConfigureHorizontalLayout(topBar.gameObject, new RectOffset(22, 22, 12, 12), 18f, TextAnchor.MiddleCenter, true);

            var leftGroup = CreateUiObject("Left HUD", topBar.transform);
            SetLayout(leftGroup.gameObject, 360f, 0f, 1f, 1f, 300f, 0f);
            ConfigureHorizontalLayout(leftGroup.gameObject, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleLeft, false);

            var coin = CreateUiObject("Coin", leftGroup.transform);
            var coinImage = coin.gameObject.AddComponent<Image>();
            coinImage.sprite = LoadSprite(CoinSpritePath);
            coinImage.color = coinImage.sprite != null ? Color.white : AccentYellow;
            coinImage.preserveAspect = true;
            SetLayout(coin.gameObject, 58f, 58f, 0f);

            var money = CreateLayoutText("Money", leftGroup.transform, "$150", 38, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            SetLayout(money.gameObject, 250f, 70f, 1f, 0f, 190f, 0f);

            var centerGroup = CreateUiObject("Center HUD", topBar.transform);
            SetLayout(centerGroup.gameObject, 520f, 0f, 1f, 1f, 430f, 0f);
            ConfigureVerticalLayout(centerGroup.gameObject, new RectOffset(26, 26, 0, 0), 2f, TextAnchor.MiddleCenter);
            var level = CreateLayoutText("Level", centerGroup.transform, "LEVEL 1", 30, FontStyle.Bold, TextAnchor.MiddleCenter, AccentYellow);
            SetLayout(level.gameObject, 0f, 38f, 1f);
            var xp = CreateSlider("XP Bar", centerGroup.transform, Vector2.one * 0.5f, Vector2.zero, new Vector2(420f, 22f));
            SetLayout(xp.gameObject, 420f, 22f, 1f);
            xp.interactable = false;
            var xpLabel = CreateLayoutText("XP Label", centerGroup.transform, "0 / 100 XP", 17, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetLayout(xpLabel.gameObject, 0f, 28f, 1f);

            var rightGroup = CreateUiObject("Right HUD", topBar.transform);
            SetLayout(rightGroup.gameObject, 520f, 0f, 1f, 1f, 450f, 0f);
            ConfigureHorizontalLayout(rightGroup.gameObject, new RectOffset(0, 0, 0, 0), 14f, TextAnchor.MiddleRight, true);
            var customers = CreateLayoutText("Customers", rightGroup.transform, "CUSTOMERS\n0", 21, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            SetLayout(customers.gameObject, 160f, 74f, 1f, 0f, 135f, 0f);
            var rating = CreateLayoutText("Rating", rightGroup.transform, "RATING\n3.0 / 5", 21, FontStyle.Bold, TextAnchor.MiddleCenter, AccentYellow);
            SetLayout(rating.gameObject, 155f, 74f, 1f, 0f, 130f, 0f);
            var pause = CreateButton("Pause", rightGroup.transform, "II", 27, Vector2.one * 0.5f, Vector2.zero, new Vector2(72f, 72f));
            SetLayout(pause.gameObject, 72f, 72f, 0f);

            var objectiveBand = CreateUiObject("Objective Band", hud.transform);
            SetRect(objectiveBand.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -162f), new Vector2(700f, 68f));
            objectiveBand.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.99f, 0.97f, 0.94f);
            var objective = CreateText(
                "Objective",
                objectiveBand.transform,
                "CURRENT GOAL  |  BUILD A SHELF  0 / 1",
                21,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.04f, 0.2f, 0.2f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                true);

            var floatingRoot = CreateUiObject("Floating Income Root", hud.transform);
            SetRect(floatingRoot.rectTransform, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 180f));
            var floatingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FloatingIncomePrefabPath)?.GetComponent<FloatingIncomeView>();

            var notification = CreateUiObject("Progress Notification", hud.transform);
            SetRect(notification.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 126f));
            notification.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.13f, 0.14f, 0.96f);
            var notificationGroup = notification.gameObject.AddComponent<CanvasGroup>();
            var notificationText = CreateText("Message", notification.transform, "OBJECTIVE COMPLETE", 27, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            notification.gameObject.SetActive(false);

            hudView = hud.gameObject.AddComponent<GameHudView>();
            hudView.Configure(
                money,
                level,
                xp,
                xpLabel,
                customers,
                rating,
                objective,
                pause,
                floatingPrefab,
                floatingRoot.rectTransform,
                notification.gameObject,
                notificationGroup,
                notificationText);

            buildingPanelView = CreateBuildingPanel(hud.transform);
            employeeView = CreateEmployeePanel(hud.transform);
            offlineIncomeView = CreateOfflineIncomePanel(canvas);

            pauseMenu = CreatePauseMenu(canvas);
            statsView = CreateStatsPanel(canvas);
            settingsView = CreateSettingsUi(canvas);
        }

        private static BuildingPanelView CreateBuildingPanel(Transform parent)
        {
            var panel = CreatePanel("Building Management", parent, new Vector2(640f, 300f));
            SetRect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(640f, 300f));
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            var transition = panel.gameObject.AddComponent<PanelTransition>();
            transition.Configure(panel.gameObject, group, panel);

            var title = CreateText("Title", panel, "SHELF", 31, FontStyle.Bold, TextAnchor.MiddleLeft, StoreGreen,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(38f, -44f), new Vector2(390f, 54f));
            var level = CreateText("Level", panel, "LEVEL 1", 21, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.1f, 0.25f, 0.25f),
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-112f, -44f), new Vector2(160f, 45f));
            var primary = CreateText("Primary Stat", panel, "INCOME", 22, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.08f, 0.2f, 0.2f),
                new Vector2(0f, 0.57f), new Vector2(0f, 0.57f), new Vector2(38f, 0f), new Vector2(560f, 42f));
            var secondary = CreateText("Secondary Stat", panel, "CAPACITY", 22, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.08f, 0.2f, 0.2f),
                new Vector2(0f, 0.39f), new Vector2(0f, 0.39f), new Vector2(38f, 0f), new Vector2(560f, 42f));
            var upgrade = CreateButton("Upgrade", panel, "UPGRADE", 23, new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(330f, 68f));
            var close = CreateButton("Close", panel, "X", 20, new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(48f, 48f));

            var view = panel.gameObject.AddComponent<BuildingPanelView>();
            view.Configure(panel.gameObject, title, level, primary, secondary, upgrade, upgrade.GetComponentInChildren<Text>(), close, transition);
            panel.gameObject.SetActive(false);
            return view;
        }

        private static EmployeeView CreateEmployeePanel(Transform parent)
        {
            var panel = CreatePanel("Employee Panel", parent, new Vector2(360f, 180f));
            SetRect(panel, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 28f), new Vector2(360f, 180f));
            var title = CreateText("Title", panel, "CASHIER", 26, FontStyle.Bold, TextAnchor.MiddleLeft, StoreGreen,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -38f), new Vector2(320f, 48f));
            var status = CreateText("Status", panel, "AVAILABLE AT LEVEL 2", 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.08f, 0.2f, 0.2f),
                new Vector2(0f, 0.55f), new Vector2(0f, 0.55f), new Vector2(28f, 0f), new Vector2(304f, 38f));
            var purchase = CreateButton("Purchase", panel, "REQUIRES LEVEL 2", 19, new Vector2(0.5f, 0.19f), Vector2.zero, new Vector2(300f, 56f));
            var view = panel.gameObject.AddComponent<EmployeeView>();
            view.Configure(panel.gameObject, title, status, purchase, purchase.GetComponentInChildren<Text>());
            return view;
        }

        private static OfflineIncomeView CreateOfflineIncomePanel(Transform canvas)
        {
            var root = CreateOverlay("Offline Income Overlay", canvas, new Color(0.01f, 0.04f, 0.05f, 0.82f));
            var panel = CreatePanel("Welcome Back Panel", root.transform, new Vector2(620f, 430f));
            CreateText("Title", panel, "WELCOME BACK", 42, FontStyle.Bold, TextAnchor.MiddleCenter, StoreGreen,
                new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), Vector2.zero, new Vector2(500f, 70f));
            var amount = CreateText("Amount", panel, "WHILE YOU WERE AWAY\n+$0", 30, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.08f, 0.2f, 0.2f),
                new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(500f, 120f));
            var collect = CreateButton("Collect", panel, "COLLECT", 27, new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(310f, 76f));
            var view = root.gameObject.AddComponent<OfflineIncomeView>();
            view.Configure(root.gameObject, amount, collect);
            root.gameObject.SetActive(false);
            return view;
        }

        private static PauseMenuView CreatePauseMenu(Transform canvas)
        {
            var root = CreateOverlay("Pause Overlay", canvas, new Color(0.01f, 0.04f, 0.05f, 0.78f));
            var rootGroup = root.gameObject.AddComponent<CanvasGroup>();
            var panel = CreatePanel("Pause Panel", root.transform, new Vector2(520f, 620f));
            ConfigureVerticalLayout(panel.gameObject, new RectOffset(72, 72, 54, 48), 18f, TextAnchor.MiddleCenter);
            var title = CreateLayoutText("Title", panel.transform, "PAUSED", 46, FontStyle.Bold, TextAnchor.MiddleCenter, StoreGreen);
            SetLayout(title.gameObject, 0f, 94f, 1f);
            CreateLayoutSpacer("Pause Spacer", panel.transform, 22f);
            var resume = CreateButton("Resume", panel.transform, "RESUME", 29, Vector2.one * 0.5f, Vector2.zero, new Vector2(360f, 78f));
            var stats = CreateButton("Stats", panel.transform, "STATS", 26, Vector2.one * 0.5f, Vector2.zero, new Vector2(360f, 72f));
            var settings = CreateButton("Settings", panel.transform, "SETTINGS", 26, Vector2.one * 0.5f, Vector2.zero, new Vector2(360f, 72f));
            var mainMenu = CreateButton("Main Menu", panel.transform, "MAIN MENU", 24, Vector2.one * 0.5f, Vector2.zero, new Vector2(360f, 68f));
            SetLayout(resume.gameObject, 360f, 78f, 0f);
            SetLayout(stats.gameObject, 360f, 72f, 0f);
            SetLayout(settings.gameObject, 360f, 72f, 0f);
            SetLayout(mainMenu.gameObject, 360f, 68f, 0f);
            var transition = root.gameObject.AddComponent<PanelTransition>();
            transition.Configure(root.gameObject, rootGroup, panel);

            var view = root.gameObject.AddComponent<PauseMenuView>();
            view.Configure(root.gameObject, resume, stats, settings, mainMenu, transition);
            root.gameObject.SetActive(false);
            return view;
        }

        private static StatsView CreateStatsPanel(Transform canvas)
        {
            var root = CreateOverlay("Stats Overlay", canvas, new Color(0.01f, 0.04f, 0.05f, 0.82f));
            var panel = CreatePanel("Stats Panel", root.transform, new Vector2(720f, 670f));
            CreateText("Title", panel, "STORE STATS", 44, FontStyle.Bold, TextAnchor.MiddleCenter, StoreGreen,
                new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(560f, 70f));
            var stats = CreateText("Stats", panel, "CUSTOMERS SERVED", 25, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.08f, 0.2f, 0.2f),
                new Vector2(0.5f, 0.53f), new Vector2(0.5f, 0.53f), Vector2.zero, new Vector2(580f, 330f));
            stats.lineSpacing = 1.45f;
            var back = CreateButton("Back", panel, "BACK", 25, new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(260f, 68f));
            var view = root.gameObject.AddComponent<StatsView>();
            view.Configure(root.gameObject, stats, back);
            root.gameObject.SetActive(false);
            return view;
        }

        private static SettingsView CreateSettingsUi(Transform canvas)
        {
            var root = CreateOverlay("Settings Overlay", canvas, new Color(0.01f, 0.04f, 0.05f, 0.82f));
            var rootGroup = root.gameObject.AddComponent<CanvasGroup>();
            var panel = CreatePanel("Settings Panel", root.transform, new Vector2(720f, 820f));
            ConfigureVerticalLayout(panel.gameObject, new RectOffset(64, 64, 42, 36), 13f, TextAnchor.UpperCenter);

            var title = CreateLayoutText("Title", panel.transform, "SETTINGS", 44, FontStyle.Bold, TextAnchor.MiddleCenter, StoreGreen);
            SetLayout(title.gameObject, 0f, 82f, 1f);
            CreateLayoutSpacer("Header Spacer", panel.transform, 10f);

            var master = CreateSettingsSliderRow(panel.transform, "MASTER VOLUME");
            var music = CreateSettingsSliderRow(panel.transform, "MUSIC VOLUME");
            var sfx = CreateSettingsSliderRow(panel.transform, "SFX VOLUME");
            var fullscreen = CreateSettingsToggleRow(panel.transform, "FULLSCREEN");
            CreateLayoutSpacer("Footer Spacer", panel.transform, 14f);

            var footer = CreateUiObject("Footer", panel.transform);
            SetLayout(footer.gameObject, 0f, 140f, 1f);
            ConfigureVerticalLayout(footer.gameObject, new RectOffset(0, 0, 0, 0), 12f, TextAnchor.MiddleCenter, false);
            var reset = CreateButton("Reset Progress", footer.transform, "RESET PROGRESS", 20, Vector2.one * 0.5f, Vector2.zero, new Vector2(280f, 58f));
            var back = CreateButton("Back", footer.transform, "BACK", 25, Vector2.one * 0.5f, Vector2.zero, new Vector2(340f, 68f));
            SetLayout(reset.gameObject, 280f, 58f, 0f);
            SetLayout(back.gameObject, 340f, 68f, 0f);

            var confirmation = CreateOverlay("Reset Confirmation", root.transform, new Color(0.01f, 0.04f, 0.05f, 0.88f));
            var confirmPanel = CreatePanel("Confirm Panel", confirmation.transform, new Vector2(560f, 360f));
            CreateText("Question", confirmPanel.transform, "RESET ALL GAMEPLAY PROGRESS?\nSettings will be kept.", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.08f, 0.18f, 0.18f),
                new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(470f, 120f));
            var confirm = CreateButton("Confirm", confirmPanel.transform, "RESET", 24, new Vector2(0.34f, 0.25f), Vector2.zero, new Vector2(200f, 66f));
            var cancel = CreateButton("Cancel", confirmPanel.transform, "CANCEL", 24, new Vector2(0.66f, 0.25f), Vector2.zero, new Vector2(200f, 66f));
            confirmation.gameObject.SetActive(false);

            var transition = root.gameObject.AddComponent<PanelTransition>();
            transition.Configure(root.gameObject, rootGroup, panel);

            var view = root.gameObject.AddComponent<SettingsView>();
            view.Configure(root.gameObject, master, music, sfx, fullscreen, back, reset, confirmation.gameObject, confirm, cancel, transition);
            root.gameObject.SetActive(false);
            return view;
        }

        private static Slider CreateSettingsSliderRow(Transform parent, string label)
        {
            var row = CreateUiObject(label + " Row", parent);
            SetLayout(row.gameObject, 0f, 68f, 1f);
            ConfigureHorizontalLayout(row.gameObject, new RectOffset(0, 0, 7, 7), 24f, TextAnchor.MiddleLeft, false);

            var rowLabel = CreateLayoutText(
                label + " Label",
                row.transform,
                label,
                21,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.08f, 0.18f, 0.18f));
            SetLayout(rowLabel.gameObject, 220f, 48f, 0f);

            var slider = CreateSlider(label, row.transform, Vector2.one * 0.5f, Vector2.zero, new Vector2(330f, 32f));
            SetLayout(slider.gameObject, 330f, 34f, 1f);
            return slider;
        }

        private static Toggle CreateSettingsToggleRow(Transform parent, string label)
        {
            var row = CreateUiObject(label + " Row", parent);
            SetLayout(row.gameObject, 0f, 68f, 1f);
            ConfigureHorizontalLayout(row.gameObject, new RectOffset(0, 0, 5, 5), 24f, TextAnchor.MiddleLeft, false);

            var rowLabel = CreateLayoutText(
                label + " Label",
                row.transform,
                label,
                21,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                new Color(0.08f, 0.18f, 0.18f));
            SetLayout(rowLabel.gameObject, 220f, 48f, 0f);

            var spacer = CreateUiObject("Control Spacer", row.transform);
            SetLayout(spacer.gameObject, 0f, 1f, 1f);

            var root = CreateUiObject(label, row.transform);
            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.82f, 0.88f, 0.86f);
            SetLayout(root.gameObject, 54f, 54f, 0f);

            var check = CreateUiObject("Checkmark", root.transform);
            Stretch(check.rectTransform, new Vector2(9f, 9f), new Vector2(-9f, -9f));
            var checkImage = check.gameObject.AddComponent<Image>();
            checkImage.color = StoreGreen;

            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkImage;
            toggle.isOn = true;
            return toggle;
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var root = CreateUiObject(name, parent);
            SetRect(root.rectTransform, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
            var slider = root.gameObject.AddComponent<Slider>();

            var background = CreateUiObject("Background", root.transform);
            Stretch(background.rectTransform, new Vector2(0f, 7f), new Vector2(0f, -7f));
            background.gameObject.AddComponent<Image>().color = new Color(0.17f, 0.29f, 0.29f, 0.65f);

            var fillArea = CreateUiObject("Fill Area", root.transform);
            Stretch(fillArea.rectTransform, new Vector2(6f, 7f), new Vector2(-6f, -7f));
            var fill = CreateUiObject("Fill", fillArea.transform);
            Stretch(fill.rectTransform);
            fill.gameObject.AddComponent<Image>().color = StoreGreen;

            var handleArea = CreateUiObject("Handle Slide Area", root.transform);
            Stretch(handleArea.rectTransform, new Vector2(8f, 0f), new Vector2(-8f, 0f));
            var handle = CreateUiObject("Handle", handleArea.transform);
            SetRect(handle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            handle.gameObject.AddComponent<Image>().color = AccentYellow;

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle.gameObject.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string label,
            int fontSize,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            var root = CreateUiObject(name, parent);
            SetRect(root.rectTransform, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
            var image = root.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(ButtonSpritePath);
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = image.sprite != null ? Color.white : StoreGreen;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = LoadSprite(ButtonHoverSpritePath),
                pressedSprite = LoadSprite(ButtonPressedSpritePath),
                selectedSprite = LoadSprite(ButtonHoverSpritePath),
                disabledSprite = LoadSprite(ButtonPressedSpritePath)
            };

            CreateText("Label", root.transform, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            return button;
        }

        private static Text CreateLayoutText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Color color)
        {
            var root = CreateUiObject(name, parent);
            var text = root.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = GetFont();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, fontSize / 2);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static void CreateLayoutSpacer(string name, Transform parent, float height)
        {
            var spacer = CreateUiObject(name, parent);
            SetLayout(spacer.gameObject, 0f, height, 1f);
        }

        private static void ConfigureHorizontalLayout(
            GameObject target,
            RectOffset padding,
            float spacing,
            TextAnchor alignment,
            bool forceExpandWidth)
        {
            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = forceExpandWidth;
            layout.childForceExpandHeight = false;
        }

        private static void ConfigureVerticalLayout(
            GameObject target,
            RectOffset padding,
            float spacing,
            TextAnchor alignment,
            bool forceExpandWidth = true)
        {
            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = forceExpandWidth;
            layout.childForceExpandHeight = false;
        }

        private static LayoutElement SetLayout(
            GameObject target,
            float preferredWidth,
            float preferredHeight,
            float flexibleWidth = 0f,
            float flexibleHeight = 0f,
            float minWidth = -1f,
            float minHeight = -1f)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            if (minWidth >= 0f)
            {
                element.minWidth = minWidth;
            }

            if (minHeight >= 0f)
            {
                element.minHeight = minHeight;
            }

            return element;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string value,
            int fontSize,
            FontStyle style,
            TextAnchor alignment,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            bool stretch = false)
        {
            var root = CreateUiObject(name, parent);
            if (stretch)
            {
                Stretch(root.rectTransform, new Vector2(10f, 8f), new Vector2(-10f, -8f));
            }
            else
            {
                SetRect(root.rectTransform, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), position, size);
            }

            var text = root.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = GetFont();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, fontSize / 2);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 size)
        {
            var root = CreateUiObject(name, parent);
            SetRect(root.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var image = root.gameObject.AddComponent<Image>();
            image.sprite = LoadSprite(PanelSpritePath);
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = image.sprite != null ? Color.white : new Color(0.95f, 0.98f, 0.96f);
            return root.rectTransform;
        }

        private static RectTransform CreateOverlay(string name, Transform parent, Color color)
        {
            var root = CreateUiObject(name, parent);
            Stretch(root.rectTransform);
            root.gameObject.AddComponent<Image>().color = color;
            return root.rectTransform;
        }

        private static (GameObject gameObject, RectTransform rectTransform, Transform transform) CreateUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (gameObject, gameObject.GetComponent<RectTransform>(), gameObject.transform);
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Font GetFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return cachedFont;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
