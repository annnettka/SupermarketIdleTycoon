using System.Collections;
using UnityEngine;

namespace SupermarketTycoon.Buildings
{
    public sealed class BuildingUpgradeFeedback : MonoBehaviour
    {
        private Coroutine routine;

        public void Play()
        {
            var particles = GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particles.Length; i++)
            {
                particles[i].Play(true);
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(Pulse());
        }

        private IEnumerator Pulse()
        {
            var baseScale = transform.localScale;
            var peakScale = baseScale * 1.08f;
            var elapsed = 0f;
            while (elapsed < 0.14f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(baseScale, peakScale, Mathf.Clamp01(elapsed / 0.14f));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(peakScale, baseScale, Mathf.Clamp01(elapsed / 0.18f));
                yield return null;
            }

            transform.localScale = baseScale;
            routine = null;
        }
    }
}
