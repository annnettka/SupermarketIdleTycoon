using SupermarketTycoon.Core;
using UnityEngine;

namespace SupermarketTycoon.SceneFlow
{
    public abstract class SceneEntryPoint : MonoBehaviour
    {
        public abstract void Initialize(ApplicationContext context);
    }
}
