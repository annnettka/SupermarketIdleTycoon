using System;
using SupermarketTycoon.Audio;
using SupermarketTycoon.Save;
using SupermarketTycoon.SceneFlow;

namespace SupermarketTycoon.Core
{
    /// <summary>
    /// Exposes immutable application-lifetime dependencies to scene entry points without acting as a service locator.
    /// Передает неизменяемые зависимости уровня приложения точкам входа сцен, не являясь локатором сервисов.
    /// </summary>
    public sealed class ApplicationContext
    {
        public ApplicationContext(
            ISaveRepository saveRepository,
            SettingsService settings,
            AudioService audio,
            SceneFlowService sceneFlow)
        {
            SaveRepository = saveRepository ?? throw new ArgumentNullException(nameof(saveRepository));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            SceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
        }

        public ISaveRepository SaveRepository { get; }
        public SettingsService Settings { get; }
        public AudioService Audio { get; }
        public SceneFlowService SceneFlow { get; }
    }
}
