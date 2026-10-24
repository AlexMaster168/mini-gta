using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SunsetState.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("Sunset State/Prepare project")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/SanOro.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/SanOro.unity",true)};
            PlayerSettings.companyName="Sunset State";PlayerSettings.productName="Sunset State";
            PlayerSettings.bundleVersion="0.3.3";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Gamma;
            QualitySettings.vSyncCount=0;QualitySettings.shadows=ShadowQuality.HardOnly;
            QualitySettings.shadowDistance=90;QualitySettings.antiAliasing=2;
            Directory.CreateDirectory("Assets/Resources");
            EnsureMaterial("CityBase","Sunset/City");EnsureMaterial("ShotBase","Sprites/Default");
            EnsureMaterial("Sky","Skybox/Procedural");EnsureMaterial("WorldTextBase","Sunset/WorldText");
            VehicleModelBaker.BakeAll();
            AssetDatabase.SaveAssets();Debug.Log("SUNSET_SETUP_OK");
        }
        static void EnsureMaterial(string name,string shaderName)
        {
            var shader=Shader.Find(shaderName);if(!shader)throw new System.Exception("Missing shader: "+shaderName);
            string path="Assets/Resources/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}else{material.shader=shader;EditorUtility.SetDirty(material);}
        }
        [MenuItem("Sunset State/Build Windows")]
        public static void BuildWindows()
        {
            Prepare();Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/SanOro.unity"},locationPathName="Builds/Windows/SunsetState.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Windows build failed: "+report.summary.result);
            File.WriteAllText("Builds/Windows/build-info.json",JsonUtility.ToJson(new BuildInfo{version=PlayerSettings.bundleVersion,unity=Application.unityVersion,builtUtc=System.DateTime.UtcNow.ToString("O"),channel="preview"},true));
            Debug.Log("SUNSET_BUILD_OK");
        }
        [System.Serializable]sealed class BuildInfo{public string version,unity,builtUtc,channel;}
    }
}
