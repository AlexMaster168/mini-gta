using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SunsetState.Editor
{
    [InitializeOnLoad]
    public static class EditorSmoke
    {
        static EditorSmoke(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-sunset-editor-check")>=0)Application.logMessageReceived+=OnLog;}
        static void OnLog(string message,string stack,LogType type)
        {
            try{System.IO.File.AppendAllText("Library/sunset-smoke-errors.txt",type+": "+message+"\n"+stack+"\n\n");}catch{}
            if(message.StartsWith("SUNSET_SMOKE_FAILED"))EditorApplication.Exit(1);else if(message.StartsWith("SUNSET_SMOKE_OK"))EditorApplication.Exit(0);
        }
        public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
    }
}
