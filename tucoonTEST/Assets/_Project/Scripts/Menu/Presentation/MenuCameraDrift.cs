using System.Collections;
using UnityEngine;

namespace SupermarketTycoon.Menu
{
    /// <summary>
    /// Adds an unscaled, reversible camera drift to the menu without affecting scene-flow responsibilities.
    /// Добавляет в меню обратимое движение камеры в независимом времени, не затрагивая обязанности переходов сцен.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuCameraDrift : MonoBehaviour
    {
        [SerializeField] private Vector3 localOffset = new Vector3(0.55f, 0.12f, 0.25f);
        [SerializeField, Min(4f)] private float duration = 14f;

        private Vector3 origin;
        private Coroutine routine;

        public void Configure(Vector3 offset, float cycleDuration)
        {
            localOffset = offset;
            duration = Mathf.Max(4f, cycleDuration);
        }

        private void OnEnable()
        {
            origin = transform.position;
            routine = StartCoroutine(Drift());
        }

        private void OnDisable()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            transform.position = origin;
        }

        private IEnumerator Drift()
        {
            var elapsed = 0f;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                var wave = Mathf.Sin(elapsed / duration * Mathf.PI * 2f);
                transform.position = origin + localOffset * wave;
                yield return null;
            }
        }
    }
}
