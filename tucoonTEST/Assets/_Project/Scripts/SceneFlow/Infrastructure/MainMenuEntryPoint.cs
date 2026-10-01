using SupermarketTycoon.Core;
using SupermarketTycoon.UI;
using UnityEngine;

namespace SupermarketTycoon.SceneFlow
{
    /// <summary>
    /// Wires the main-menu view to application services for the lifetime of the menu scene.
    /// Связывает представление главного меню с сервисами приложения на время жизни сцены меню.
    /// </summary>
    public sealed class MainMenuEntryPoint : SceneEntryPoint
    {
        [SerializeField] private MainMenuView mainMenuView;
        [SerializeField] private SettingsView settingsView;

        private SettingsController settingsController;
        private MainMenuController mainMenuController;
        private ApplicationContext applicationContext;

        public void Configure(MainMenuView menu, SettingsView settings)
        {
            mainMenuView = menu;
            settingsView = settings;
        }

        public override void Initialize(ApplicationContext context)
        {
            applicationContext = context;
            context.Audio.PlayMenuMusic();
            settingsController = new SettingsController(settingsView, context, ResetProgress);
            mainMenuController = new MainMenuController(mainMenuView, context, settingsController);
        }

        private void OnDestroy()
        {
            mainMenuController?.Dispose();
            settingsController?.Dispose();
        }

        private void ResetProgress()
        {
            applicationContext.SaveRepository.Delete();
            mainMenuController?.Refresh();
            mainMenuView.SetVisible(true);
        }
    }
}
