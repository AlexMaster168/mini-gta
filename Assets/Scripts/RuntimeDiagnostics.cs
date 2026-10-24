using System;
using System.IO;
using System.Text;
using UnityEngine;
namespace SunsetState
{
    public sealed class RuntimeDiagnostics:MonoBehaviour
    {
        public static RuntimeDiagnostics Instance;string logPath;bool writing;float average=.016f,worst,window;public int Errors{get;private set;}
        public float FPS=>1/Mathf.Max(.001f,average);public float WorstMilliseconds{get;private set;}
        public void Initialize(bool test)
        {
            Instance=this;string folder=Path.Combine(test?Application.temporaryCachePath:Application.persistentDataPath,"Diagnostics");Directory.CreateDirectory(folder);logPath=Path.Combine(folder,test?"test-session.log":"session.log");
            try{if(File.Exists(logPath))File.Copy(logPath,logPath+".previous",true);File.WriteAllText(logPath,"Sunset State "+Application.version+" · Unity "+Application.unityVersion+" · "+DateTime.UtcNow.ToString("O")+Environment.NewLine);}catch(IOException){}catch(UnauthorizedAccessException){}
            Application.logMessageReceived+=OnLog;
        }
        void Update(){float dt=Time.unscaledDeltaTime;if(dt<=0)return;average=Mathf.Lerp(average,dt,1-Mathf.Exp(-dt*2));worst=Mathf.Max(worst,dt);window+=dt;if(window>=2){WorstMilliseconds=worst*1000;worst=0;window=0;}}
        void OnLog(string message,string stack,LogType type)
        {
            if(writing||type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;Errors++;writing=true;
            try{if(File.Exists(logPath)&&new FileInfo(logPath).Length>256*1024){File.Copy(logPath,logPath+".previous",true);File.WriteAllText(logPath,"");}string entry=DateTime.UtcNow.ToString("O")+" "+type+" "+message+Environment.NewLine+stack+Environment.NewLine;if(entry.Length>8192)entry=entry.Substring(0,8192);File.AppendAllText(logPath,entry);}catch(IOException){}catch(UnauthorizedAccessException){}finally{writing=false;}
        }
        public string ExportReport()
        {
            var report=new StringBuilder();report.AppendLine("Sunset State support report");report.AppendLine("Version: "+Application.version+"; Unity: "+Application.unityVersion);report.AppendLine("UTC: "+DateTime.UtcNow.ToString("O"));report.AppendLine("OS: "+SystemInfo.operatingSystem);report.AppendLine("CPU: "+SystemInfo.processorType);report.AppendLine("GPU: "+SystemInfo.graphicsDeviceName+"; RAM MB: "+SystemInfo.systemMemorySize);report.AppendLine("Resolution: "+Screen.width+"x"+Screen.height+"; quality: "+RuntimeSettings.Data.Quality);report.AppendLine("FPS estimate: "+FPS.ToString("0")+"; slowest recent frame ms: "+WorstMilliseconds.ToString("0.0"));report.AppendLine("Dormant ambient vehicles: "+WorldSimulation.DormantVehicles+"; NPC: "+WorldSimulation.DormantActors+"; wake radius: "+WorldSimulation.WakeRadius);report.AppendLine("Errors this session: "+Errors);report.AppendLine("Local only. No saves or account credentials included.");if(File.Exists(logPath))report.AppendLine(File.ReadAllText(logPath));string path=Path.Combine(Path.GetDirectoryName(logPath),"support-report.txt");SaveStore.AtomicWrite(path,report.ToString(),false);return path;
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;if(Instance==this)Instance=null;}
    }
    public sealed partial class GameRoot
    {
        void DrawDiagnostics(float left,float h)
        {
            var diagnostics=RuntimeDiagnostics.Instance;Label(left,100,1040,45,"ДИАГНОСТИКА",heading);
            Label(left,180,1040,150,"Версия: "+Application.version+" · Unity "+Application.unityVersion+"\nFPS: "+(diagnostics?diagnostics.FPS.ToString("0"):"—")+" · самый долгий кадр: "+(diagnostics?diagnostics.WorstMilliseconds.ToString("0.0"):"—")+" мс\nОшибок за сеанс: "+(diagnostics?diagnostics.Errors:0)+"\nОтчёт хранится только на этом компьютере и никуда не отправляется.",body);
            Label(left,330,1040,32,"Приостановлено: "+WorldSimulation.DormantVehicles+" машин / "+WorldSimulation.DormantActors+" NPC · радиус пробуждения "+WorldSimulation.WakeRadius+" м",small);
            if(Button(new Rect(left,370,1040,60),"СОХРАНИТЬ ОТЧЁТ ДЛЯ ПОДДЕРЖКИ")){try{string file=diagnostics.ExportReport();Notify("Отчёт: "+file);}catch(Exception e){Notify("Не удалось записать отчёт: "+e.Message);}}
            if(Button(new Rect(left,460,1040,55),"НАЗАД К НАСТРОЙКАМ"))Toggle("settings");
        }
        void DrawFrameCounter(){if(RuntimeSettings.Data.ShowFPS&&RuntimeDiagnostics.Instance)Label(570,4,220,25,RuntimeDiagnostics.Instance.FPS.ToString("0")+" FPS · "+Application.version,small);}
    }
}
