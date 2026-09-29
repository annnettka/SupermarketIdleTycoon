using System;
using UnityEngine;

namespace SupermarketTycoon.Core
{
    public sealed class PauseService
    {
        public bool IsPaused { get; private set; }
        public event Action<bool> PauseChanged;

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused)
            {
                return;
            }

            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            PauseChanged?.Invoke(paused);
        }
    }
}
