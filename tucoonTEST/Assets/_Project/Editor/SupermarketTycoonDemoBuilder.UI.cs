using SupermarketTycoon.Buildings;
using SupermarketTycoon.UI;
using UnityEditor;
using UnityEngine;
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

            CreateText(
                "Loading Label",
                root.transform,
                "LOADING SUPERMARKET...",
                44,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f),
                new Vector2(850f, 80f));

            var slider = CreateSlider(
                "Loading Progress",
                root.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -15f),
                new Vector2(620f, 28f));
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
            Camera camera)
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
            canvasRect.sizeDelta = new Vector2(330f, 125f);

            var button = CreateButton(
                "Build Button",
                canvasObject.transform,
                "+ BUILD",
                26,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(320f, 116f));
            var label = button.GetComponentInChildren<Text>();
            var view = canvasObject.AddComponent<BuildSpotView>();
            view.Configure(button, label, canvasObject);

            var spot = root.AddComponent<BuildSpot>();
            spot.Configure(id, definition, placement, view);
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

            CreateText(
                "Title",
                menuRoot.transform,
                "SUPERMARKET\nTYCOON",
                74,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                Color.white,
                new Vector2(0.5f, 0.82f),
                new Vector2(0.5f, 0.82f),
                Vector2.zero,
                new Vector2(900f, 190f));

            var subtitle = CreateText(
                "Subtitle",
                menuRoot.transform,
                "BUILD  |  SERVE  |  GROW",
                26,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                AccentYellow,
                new Vector2(0.5f, 0.68f),
                new Vector2(0.5f, 0.68f),
                Vector2.zero,
                new Vector2(700f, 55f));
            subtitle.horizontalOverflow = HorizontalWrapMode.Overflow;

            var play = CreateButton("Play", menuRoot.transform, "PLAY", 34, new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(420f, 112f));
            var settings = CreateButton("Settings", menuRoot.transform, "SETTINGS", 28, new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(350f, 92f));
            var quit = CreateButton("Quit", menuRoot.transform, "QUIT", 26, new Vector2(0.5f, 0.24f), Vector2.zero, new Vector2(300f, 82f));

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
            out PauseMenuView pauseMenu,
            out SettingsView settingsView)
        {
            var hud = CreateUiObject("HUD", canvas);
            Stretch(hud.rectTransform);

            var topBar = CreateUiObject("Top Bar", hud.transform);
            SetRect(topBar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-30f, 118f));
            var topImage = topBar.gameObject.AddComponent<Image>();
            topImage.color = new Color(0.025f, 0.13f, 0.14f, 0.94f);

            var coin = CreateUiObject("Coin", topBar.transform);
            SetRect(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(70f, 70f));
            var coinImage = coin.gameObject.AddComponent<Image>();
            coinImage.sprite = LoadSprite(CoinSpritePath);
            coinImage.color = coinImage.sprite != null ? Color.white : AccentYellow;

            var money = CreateText("Money", topBar.transform, "$150", 40, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(105f, 0f), new Vector2(260f, 76f));
            var level = CreateText("Level", topBar.transform, "LEVEL 1", 30, FontStyle.Bold, TextAnchor.MiddleCenter, AccentYellow,
                new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0f, 0f), new Vector2(300f, 48f));
            var xp = CreateSlider("XP Bar", topBar.transform, new Vector2(0.5f, 0.34f), Vector2.zero, new Vector2(420f, 24f));
            xp.interactable = false;
            var xpLabel = CreateText("XP Label", topBar.transform, "0 / 100 XP", 17, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.34f), new Vector2(0f, -28f), new Vector2(300f, 30f));
            var customers = CreateText("Customers", topBar.transform, "CUSTOMERS  0", 23, FontStyle.Bold, TextAnchor.MiddleRight, Color.white,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(300f, 60f));
            var pause = CreateButton("Pause", topBar.transform, "II", 28, new Vector2(1f, 0.5f), new Vector2(-55f, 0f), new Vector2(80f, 76f));

            var objective = CreateText(
                "Objective",
                hud.transform,
                "Build a shelf and a checkout",
                24,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                new Color(0.04f, 0.2f, 0.2f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -145f),
                new Vector2(620f, 56f));
            var objectiveBackground = objective.gameObject.AddComponent<Shadow>();
            objectiveBackground.effectColor = new Color(1f, 1f, 1f, 0.7f);
            objectiveBackground.effectDistance = new Vector2(2f, -2f);

            var floatingRoot = CreateUiObject("Floating Income Root", hud.transform);
            SetRect(floatingRoot.rectTransform, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 180f));
            var floatingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FloatingIncomePrefabPath)?.GetComponent<FloatingIncomeView>();

            hudView = hud.gameObject.AddComponent<GameHudView>();
            hudView.Configure(money, level, xp, xpLabel, customers, objective, pause, floatingPrefab, floatingRoot.rectTransform);

            pauseMenu = CreatePauseMenu(canvas);
            settingsView = CreateSettingsUi(canvas);
        }

        private static PauseMenuView CreatePauseMenu(Transform canvas)
        {
            var root = CreateOverlay("Pause Overlay", canvas, new Color(0.01f, 0.04f, 0.05f, 0.78f));
            var panel = CreatePanel("Pause Panel", root.transform, new Vector2(520f, 580f));
            CreateText("Title", panel.transform, "PAUSED", 48, FontStyle.Bold, TextAnchor.MiddleCenter, StoreGreen,
                new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(420f, 80f));
            var resume = CreateButton("Resume", panel.transform, "RESUME", 30, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(340f, 90f));
            var settings = CreateButton("Settings", panel.transform, "SETTINGS", 27, new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(320f, 82f));
            var mainMenu = CreateButton("Main Menu", panel.transform, "MAIN MENU", 25, new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(300f, 78f));

            var view = root.gameObject.AddComponent<PauseMenuView>();
            view.Configure(root.gameObject, resume, settings, mainMenu);
            root.gameObject.SetActive(false);
            return view;
        }

        private static SettingsView CreateSettingsUi(Transform canvas)
        {
            var root = CreateOverlay("Settings Overlay", canvas, new Color(0.01f, 0.04f, 0.05f, 0.82f));
            var panel = CreatePanel("Settings Panel", root.transform, new Vector2(720f, 820f));
            CreateText("Title", panel.transform, "SETTINGS", 46, FontStyle.Bold, TextAnchor.MiddleCenter, StoreGreen,
                new Vector2(0.5f, 0.89f), new Vector2(0.5f, 0.89f), Vector2.zero, new Vector2(550f, 70f));

            var master = CreateLabeledSlider(panel.transform, "MASTER VOLUME", 0.71f);
            var music = CreateLabeledSlider(panel.transform, "MUSIC VOLUME", 0.57f);
            var sfx = CreateLabeledSlider(panel.transform, "SFX VOLUME", 0.43f);
            var fullscreen = CreateLabeledToggle(panel.transform, "FULLSCREEN", 0.3f);
            var reset = CreateButton("Reset Progress", panel.transform, "RESET PROGRESS", 22, new Vector2(0.5f, 0.17f), Vector2.zero, new Vector2(310f, 70f));
            var back = CreateButton("Back", panel.transform, "BACK", 25, new Vector2(0.5f, 0.07f), Vector2.zero, new Vector2(250f, 64f));

            var confirmation = CreateOverlay("Reset Confirmation", root.transform, new Color(0.01f, 0.04f, 0.05f, 0.88f));
            var confirmPanel = CreatePanel("Confirm Panel", confirmation.transform, new Vector2(560f, 360f));
            CreateText("Question", confirmPanel.transform, "RESET ALL GAMEPLAY PROGRESS?\nSettings will be kept.", 26, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.08f, 0.18f, 0.18f),
                new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(470f, 120f));
            var confirm = CreateButton("Confirm", confirmPanel.transform, "RESET", 24, new Vector2(0.34f, 0.25f), Vector2.zero, new Vector2(200f, 66f));
            var cancel = CreateButton("Cancel", confirmPanel.transform, "CANCEL", 24, new Vector2(0.66f, 0.25f), Vector2.zero, new Vector2(200f, 66f));
            confirmation.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<SettingsView>();
            view.Configure(root.gameObject, master, music, sfx, fullscreen, back, reset, confirmation.gameObject, confirm, cancel);
            root.gameObject.SetActive(false);
            return view;
        }

        private static Slider CreateLabeledSlider(Transform parent, string label, float anchorY)
        {
            CreateText(label + " Label", parent, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.08f, 0.18f, 0.18f),
                new Vector2(0.16f, anchorY), new Vector2(0.16f, anchorY), Vector2.zero, new Vector2(250f, 45f));
            return CreateSlider(label, parent, new Vector2(0.68f, anchorY), Vector2.zero, new Vector2(330f, 30f));
        }

        private static Toggle CreateLabeledToggle(Transform parent, string label, float anchorY)
        {
            CreateText(label + " Label", parent, label, 22, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.08f, 0.18f, 0.18f),
                new Vector2(0.16f, anchorY), new Vector2(0.16f, anchorY), Vector2.zero, new Vector2(250f, 45f));

            var root = CreateUiObject(label, parent);
            SetRect(root.rectTransform, new Vector2(0.75f, anchorY), new Vector2(0.75f, anchorY), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));
            var background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.82f, 0.88f, 0.86f);

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
                disabledSprite = null
            };

            CreateText("Label", root.transform, label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);
            return button;
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
