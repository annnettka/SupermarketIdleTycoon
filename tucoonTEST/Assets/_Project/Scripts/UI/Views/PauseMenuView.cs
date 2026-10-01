using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    /// <summary>
    /// Exposes pause-menu commands and renders controller-selected pause, settings, and statistics panels.
    /// Предоставляет команды меню паузы и отображает выбранные контроллером панели паузы, настроек и статистики.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private PanelTransition transition;

        public Button ResumeButton => resumeButton;
        public Button StatsButton => statsButton;
        public Button SettingsButton => settingsButton;
        public Button MainMenuButton => mainMenuButton;

        public void Configure(
            GameObject menuRoot,
            Button resume,
            Button stats,
            Button settings,
            Button mainMenu,
            PanelTransition panelTransition)
        {
            root = menuRoot;
            resumeButton = resume;
            statsButton = stats;
            settingsButton = settings;
            mainMenuButton = mainMenu;
            transition = panelTransition;
        }

        public void SetVisible(bool visible)
        {
            if (transition != null)
            {
                transition.SetVisible(visible);
            }
            else
            {
                root.SetActive(visible);
            }
        }
    }
}
