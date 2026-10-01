using System;
using UnityEngine;

namespace SupermarketTycoon.Core
{
    /// <summary>
    /// Provides authoritative session pause state and keeps time-scale changes behind a focused boundary.
    /// Предоставляет авторитетное состояние паузы сессии и скрывает изменение масштаба времени за узкой границей.
    /// </summary>
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
