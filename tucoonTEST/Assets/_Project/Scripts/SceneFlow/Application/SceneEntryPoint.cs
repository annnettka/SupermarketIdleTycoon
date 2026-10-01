using SupermarketTycoon.Core;
using UnityEngine;

namespace SupermarketTycoon.SceneFlow
{
    /// <summary>
    /// Defines the injection boundary through which a loaded scene receives application dependencies.
    /// Определяет границу внедрения, через которую загруженная сцена получает зависимости приложения.
    /// </summary>
    public abstract class SceneEntryPoint : MonoBehaviour
    {
        /// <summary>
        /// Receives application-lifetime dependencies after asynchronous scene activation.
        /// Получает зависимости уровня приложения после активации асинхронно загруженной сцены.
        /// </summary>
        public abstract void Initialize(ApplicationContext context);
    }
}
