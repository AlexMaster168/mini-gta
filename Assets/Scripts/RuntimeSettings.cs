using System;
using System.IO;
using UnityEngine;
namespace SunsetState
{
    [Serializable]public sealed class UserSettingsData
    {
        public int Version=2,Quality=1,FrameLimit=60,Width=1280,Height=720;
        public float Master=.8f,Music=.24f,Engine=.12f,MouseSensitivity=1;
        public bool InvertY,VSync,ShowFPS,Fullscreen=true,LimitAmbient=true;
        static float Safe(float value,float fallback,float min,float max)=>float.IsNaN(value)||float.IsInfinity(value)?fallback:Mathf.Clamp(value,min,max);
        public void Normalize()
        {Version=2;Quality=Mathf.Clamp(Quality,0,2);FrameLimit=FrameLimit==-1?-1:FrameLimit<=30?30:FrameLimit<=60?60:120;Width=Mathf.Clamp(Width,960,3840);Height=Mathf.Clamp(Height,540,2160);Master=Safe(Master,.8f,0,1);Music=Safe(Music,.24f,0,.6f);Engine=Safe(Engine,.12f,0,.3f);MouseSensitivity=Safe(MouseSensitivity,1,.2f,3);}
    }
    public static class RuntimeSettings
    {
        public static UserSettingsData Data=new UserSettingsData();static string path,lastSaved;static bool testing;
        public static UserSettingsData Read(string file)
        {
            foreach(string candidate in new[]{file,file+".bak"})
            {try{if(!File.Exists(candidate)||new FileInfo(candidate).Length>65536)continue;string json=File.ReadAllText(candidate);if(!json.Contains("\"Version\""))continue;var data=JsonUtility.FromJson<UserSettingsData>(json);if(data==null||data.Version<1||data.Version>2)continue;if(data.Version==1)data.LimitAmbient=true;data.Normalize();return data;}catch(IOException){}catch(ArgumentException){}}
            return new UserSettingsData();
        }
        public static void Initialize(bool test)
        {
            testing=test;path=Path.Combine(test?Application.temporaryCachePath:Application.persistentDataPath,test?"sunset-test-settings.json":"settings.json");Data=test?new UserSettingsData():Read(path);lastSaved=null;
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-screen-fullscreen");if(index>=0&&index+1<args.Length)Data.Fullscreen=args[index+1]!="0";
            index=Array.IndexOf(args,"-screen-width");if(index>=0&&index+1<args.Length&&int.TryParse(args[index+1],out int width))Data.Width=width;index=Array.IndexOf(args,"-screen-height");if(index>=0&&index+1<args.Length&&int.TryParse(args[index+1],out int height))Data.Height=height;
        }
        public static void Apply(bool display=false)
        {
            Data.Normalize();AudioListener.volume=Data.Master;StateRadio.Volume=Data.Music;VehicleAudio.EngineVolume=Data.Engine;
            QualitySettings.vSyncCount=Data.VSync?1:0;Application.targetFrameRate=Data.FrameLimit;QualitySettings.antiAliasing=Data.Quality==0?0:Data.Quality==1?2:4;QualitySettings.shadows=Data.Quality==0?ShadowQuality.Disable:Data.Quality==1?ShadowQuality.HardOnly:ShadowQuality.All;QualitySettings.shadowDistance=Data.Quality==0?0:Data.Quality==1?90:150;
            if(Camera.main)Camera.main.farClipPlane=Data.Quality==0?1800:Data.Quality==1?3500:5000;
            if(display&&!testing){int width=Display.main.systemWidth,height=Display.main.systemHeight;if(width>0&&height>0)Screen.SetResolution(Data.Fullscreen?width:Mathf.Min(width,Data.Width),Data.Fullscreen?height:Mathf.Min(height,Data.Height),Data.Fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);}
        }
        public static void Save()
        {
            if(path==null)return;Data.Master=AudioListener.volume;Data.Music=StateRadio.Volume;Data.Engine=VehicleAudio.EngineVolume;Data.Normalize();string json=JsonUtility.ToJson(Data,true);if(json==lastSaved)return;
            try{SaveStore.AtomicWrite(path,json,true);lastSaved=json;}catch(IOException e){Debug.LogWarning("Settings write failed: "+e.Message);}catch(UnauthorizedAccessException e){Debug.LogWarning("Settings write failed: "+e.Message);}
        }
        public static void Commit(){Data.Master=AudioListener.volume;Data.Music=StateRadio.Volume;Data.Engine=VehicleAudio.EngineVolume;Apply(true);Save();}
    }
    public sealed partial class GameRoot
    {
        Vector2 settingsScroll;
        void DrawSettings(float left,float h)
        {
            Label(left,90,1040,45,"НАСТРОЙКИ",heading);var data=RuntimeSettings.Data;
            settingsScroll=GUI.BeginScrollView(new Rect(left,155,1080,h-250),settingsScroll,new Rect(0,0,1040,760));
            Label(0,0,1040,35,"Графика",body);string[] quality={"НИЗКАЯ","СРЕДНЯЯ","ВЫСОКАЯ"};for(int i=0;i<3;i++)if(Button(new Rect(i*350,42,335,44),(data.Quality==i?"✓ ":"")+quality[i]))data.Quality=i;
            Label(0,100,1040,30,"Ограничение кадров (при выключенной вертикальной синхронизации)",small);int[] limits={30,60,120,-1};for(int i=0;i<4;i++)if(Button(new Rect(i*262,137,250,42),(data.FrameLimit==limits[i]?"✓ ":"")+(limits[i]<0?"БЕЗ ЛИМИТА":limits[i]+" FPS")))data.FrameLimit=limits[i];
            data.VSync=GUI.Toggle(new Rect(0,195,510,36),data.VSync,(data.VSync?"ВКЛ · ":"ВЫКЛ · ")+"Вертикальная синхронизация",body);data.ShowFPS=GUI.Toggle(new Rect(530,195,510,36),data.ShowFPS,(data.ShowFPS?"ВКЛ · ":"ВЫКЛ · ")+"Показывать FPS",body);
            data.Fullscreen=GUI.Toggle(new Rect(0,245,1040,36),data.Fullscreen,(data.Fullscreen?"ВКЛ · ":"ВЫКЛ · ")+"Полный экран · F11",body);
            Label(0,292,1040,30,"Размер окна: "+data.Width+" × "+data.Height,body);int[] widths={1280,1600,1920};int[] heights={720,900,1080};for(int i=0;i<3;i++)if(Button(new Rect(i*350,332,335,42),widths[i]+" × "+heights[i])){data.Width=widths[i];data.Height=heights[i];}
            Label(0,398,1040,30,"Чувствительность мыши: "+data.MouseSensitivity.ToString("0.0"),body);data.MouseSensitivity=GUI.HorizontalSlider(new Rect(0,440,1030,25),data.MouseSensitivity,.2f,3);data.InvertY=GUI.Toggle(new Rect(0,480,1040,35),data.InvertY,(data.InvertY?"ВКЛ · ":"ВЫКЛ · ")+"Инверсия вертикальной оси камеры",body);
            Label(0,535,1040,30,"Общий звук / музыка / двигатель",body);AudioListener.volume=GUI.HorizontalSlider(new Rect(0,580,330,25),AudioListener.volume,0,1);StateRadio.Volume=GUI.HorizontalSlider(new Rect(350,580,330,25),StateRadio.Volume,0,.6f);VehicleAudio.EngineVolume=GUI.HorizontalSlider(new Rect(700,580,330,25),VehicleAudio.EngineVolume,0,.3f);
            data.LimitAmbient=GUI.Toggle(new Rect(0,635,1040,36),data.LimitAmbient,(data.LimitAmbient?"ВКЛ · ":"ВЫКЛ · ")+"Ограничивать фоновую симуляцию вдали от игрока",body);
            if(Button(new Rect(0,700,510,44),"ПОКАЗАТЬ ДИАГНОСТИКУ"))Toggle("diagnostics");
            GUI.EndScrollView();
            if(Button(new Rect(left,h-75,510,48),"ПРИМЕНИТЬ И СОХРАНИТЬ")){data.Master=AudioListener.volume;data.Music=StateRadio.Volume;data.Engine=VehicleAudio.EngineVolume;RuntimeSettings.Apply(true);RuntimeSettings.Save();Notify("Настройки сохранены");}
            if(Button(new Rect(left+530,h-75,510,48),"ВЕРНУТЬСЯ")){RuntimeSettings.Commit();if(Playing)Toggle("pause");else Panel="";}
        }
    }
}
