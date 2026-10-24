using UnityEngine;

namespace SunsetState
{
    public sealed partial class GameRoot
    {
        void ToggleFullscreen()
        {
            RuntimeSettings.Data.Fullscreen=!Screen.fullScreen;RuntimeSettings.Commit();
            Notify("F11 — переключение полного экрана и окна");
        }
    }
}
