using System.Collections;
using UnityEngine;

namespace SupermarketTycoon.Buildings
{
    public sealed class BuildingUpgradeFeedback : MonoBehaviour
    {
        private Coroutine routine;

        public void PlayReveal()
        {
            PlayParticles();
            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(Reveal());
        }

        public void Play()
        {
            PlayParticles();

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(Pulse());
        }

        private void PlayParticles()
        {
            var particles = GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particles.Length; i++)
            {
                particles[i].Play(true);
            }
        }

        private IEnumerator Reveal()
        {
            var targetScale = transform.localScale;
            var startScale = targetScale * 0.68f;
            transform.localScale = startScale;
            var elapsed = 0f;
            const float duration = 0.32f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, progress);
                yield return null;
            }

            transform.localScale = targetScale;
            routine = null;
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
