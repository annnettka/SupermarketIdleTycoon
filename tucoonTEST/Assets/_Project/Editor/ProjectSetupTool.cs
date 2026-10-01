using UnityEditor;

namespace SupermarketTycoon.Editor
{
    /// <summary>
    /// Keeps the legacy setup menu as a thin alias for the canonical build-and-repair pipeline.
    /// Сохраняет прежнее меню настройки как тонкий псевдоним канонического конвейера сборки и восстановления.
    /// </summary>
    public static class ProjectSetupTool
    {
        /// <summary>
        /// Delegates project setup to the idempotent playable-demo builder.
        /// Передает настройку проекта идемпотентному конструктору игрового демо.
        /// </summary>
        [MenuItem("Tools/Supermarket Tycoon/Setup Project")]
        public static void SetupProject()
        {
            SupermarketTycoonDemoBuilder.BuildPlayableDemoFromMenu();
        }
    }
}
