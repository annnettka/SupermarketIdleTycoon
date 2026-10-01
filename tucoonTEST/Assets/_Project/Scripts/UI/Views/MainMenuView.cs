using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    /// <summary>
    /// Exposes main-menu commands and renders save-aware labels supplied by its controller.
    /// Предоставляет команды главного меню и отображает зависящие от сохранения подписи контроллера.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private Button playButton;
        [SerializeField] private Text playLabel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        public Button PlayButton => playButton;
        public Button SettingsButton => settingsButton;
        public Button QuitButton => quitButton;

        public void Configure(
            GameObject root,
            Button play,
            Text playText,
            Button settings,
            Button quit)
        {
            menuRoot = root;
            playButton = play;
            playLabel = playText;
            settingsButton = settings;
            quitButton = quit;
        }

        public void SetVisible(bool visible)
        {
            menuRoot.SetActive(visible);
        }

        public void SetHasSave(bool hasSave)
        {
            playLabel.text = hasSave ? "CONTINUE" : "PLAY";
        }
    }
}
