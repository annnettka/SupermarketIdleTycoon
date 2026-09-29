using UnityEditor;

namespace SupermarketTycoon.Editor
{
    public static class ProjectSetupTool
    {
        [MenuItem("Tools/Supermarket Tycoon/Setup Project")]
        public static void SetupProject()
        {
            SupermarketTycoonDemoBuilder.BuildPlayableDemoFromMenu();
        }
    }
}
