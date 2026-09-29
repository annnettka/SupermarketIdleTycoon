using UnityEngine;
using UnityEngine.UI;

namespace SupermarketTycoon.UI
{
    public sealed class SettingsView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Slider masterVolume;
        [SerializeField] private Slider musicVolume;
        [SerializeField] private Slider sfxVolume;
        [SerializeField] private Toggle fullscreen;
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private GameObject resetConfirmation;
        [SerializeField] private Button confirmResetButton;
        [SerializeField] private Button cancelResetButton;

        public Slider MasterVolume => masterVolume;
        public Slider MusicVolume => musicVolume;
        public Slider SfxVolume => sfxVolume;
        public Toggle Fullscreen => fullscreen;
        public Button BackButton => backButton;
        public Button ResetButton => resetButton;
        public Button ConfirmResetButton => confirmResetButton;
        public Button CancelResetButton => cancelResetButton;

        public void Configure(
            GameObject settingsRoot,
            Slider master,
            Slider music,
            Slider sfx,
            Toggle fullscreenToggle,
            Button back,
            Button reset,
            GameObject confirmation,
            Button confirm,
            Button cancel)
        {
            root = settingsRoot;
            masterVolume = master;
            musicVolume = music;
            sfxVolume = sfx;
            fullscreen = fullscreenToggle;
            backButton = back;
            resetButton = reset;
            resetConfirmation = confirmation;
            confirmResetButton = confirm;
            cancelResetButton = cancel;
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
            if (!visible)
            {
                ShowResetConfirmation(false);
            }
        }

        public void ShowResetConfirmation(bool visible)
        {
            resetConfirmation.SetActive(visible);
        }

        public void SetValues(float master, float music, float sfx, bool isFullscreen)
        {
            masterVolume.SetValueWithoutNotify(master);
            musicVolume.SetValueWithoutNotify(music);
            sfxVolume.SetValueWithoutNotify(sfx);
            fullscreen.SetIsOnWithoutNotify(isFullscreen);
        }
    }
}
