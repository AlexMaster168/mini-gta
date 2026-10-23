using UnityEngine;

namespace SunsetState
{
    public sealed partial class GameRoot
    {
        void ToggleFullscreen()
        {
            if(Screen.fullScreen)
                Screen.SetResolution(Mathf.Min(1280,Display.main.systemWidth),Mathf.Min(720,Display.main.systemHeight),FullScreenMode.Windowed);
            else
                Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);
            Notify("F11 — переключение полного экрана и окна");
        }
    }
}
