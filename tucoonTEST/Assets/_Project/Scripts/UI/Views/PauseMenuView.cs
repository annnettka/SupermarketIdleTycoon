using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;

        public Button ResumeButton => resumeButton;
        public Button StatsButton => statsButton;
        public Button SettingsButton => settingsButton;
        public Button MainMenuButton => mainMenuButton;

        public void Configure(GameObject menuRoot, Button resume, Button stats, Button settings, Button mainMenu)
        {
            root = menuRoot;
            resumeButton = resume;
            statsButton = stats;
            settingsButton = settings;
            mainMenuButton = mainMenu;
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }
    }
}
