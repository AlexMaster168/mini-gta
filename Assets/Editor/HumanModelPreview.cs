using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SunsetState.Editor
{
    public static class HumanModelPreview
    {
        public static void RenderAndBuild(){Render();RenderInterior();ProjectSetup.BuildWindows();}
        public static void RenderInterior()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.5f,.52f,.55f);RenderSettings.fog=false;
            var camera=new GameObject("Interior preview camera").AddComponent<Camera>();camera.tag="MainCamera";camera.cullingMask=~(1<<2);camera.fieldOfView=75;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var game=new GameObject("Preview game").AddComponent<GameRoot>();GameRoot.Instance=game;game.RoleIndex=2;
            game.Player=new GameObject("Preview player").AddComponent<PlayerMotor>();game.Player.Initialize();
            game.EnterBuilding(new BuildingEntrance{Name="Федеральное бюро",Role=2,Door=new Vector3(0,1,0),Color=new Color(.25f,.45f,.7f)});
            camera.transform.position=new Vector3(0,603,-8.8f);camera.transform.LookAt(new Vector3(0,601.4f,4));
            foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))text.transform.rotation=WorldTextFacing.ReadableRotation(text.transform.position,camera.transform.position);
            var target=new RenderTexture(1500,900,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(1500,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1500,900),0,0);image.Apply();Directory.CreateDirectory("Previews");File.WriteAllBytes("Previews/FBI-Interior.png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Debug.Log("INTERIOR_PREVIEW_OK");
        }
        public static void Render()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.65f,.65f,.65f);RenderSettings.fog=false;
            var sun=new GameObject("Preview light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.transform.rotation=Quaternion.Euler(35,150,0);
            int[] roles={0,1,3,4,5,10,13};
            var diagnostic=Object.Instantiate(Resources.Load<GameObject>("Characters/Casual_Male"));
            var animation=diagnostic.GetComponentInChildren<Animation>();if(animation)foreach(AnimationState state in animation){Debug.Log("HUMAN_CLIP "+state.name);if(state.name.ToLowerInvariant().Contains("idle")){state.clip.SampleAnimation(diagnostic,0);}}
            foreach(var t in diagnostic.GetComponentsInChildren<Transform>())if(t.parent==diagnostic.transform || t==diagnostic.transform)Debug.Log("HUMAN_RAW_TRANSFORM "+t.name+" scale="+t.localScale+" lossy="+t.lossyScale);
            foreach(var skin in diagnostic.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();skin.BakeMesh(mesh);Debug.Log("HUMAN_RAW_MESH "+skin.name+" local="+skin.transform.localScale+" lossy="+skin.transform.lossyScale+" bounds="+skin.bounds+" baked="+mesh.bounds+" root="+skin.rootBone.lossyScale);mesh.RecalculateBounds();Debug.Log("HUMAN_RECALCULATED "+mesh.bounds);Object.DestroyImmediate(mesh);}
            Object.DestroyImmediate(diagnostic);
            for(int i=0;i<roles.Length;i++)
            {
                var actor=new GameObject("Role "+roles[i]);actor.transform.position=new Vector3((i-3)*1.4f,0,0);
                var model=CharacterModels.Create(actor.transform,roles[i],Color.white);
                Debug.Log("HUMAN_PREVIEW role="+roles[i]+" height="+CharacterModels.GeometryBounds(model.gameObject).size.y+" bones="+model.GetComponentInChildren<SkinnedMeshRenderer>().bones.Length);
            }
            var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,2.7f,12);camera.transform.LookAt(new Vector3(0,.9f,0));camera.fieldOfView=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.19f,.23f);
            var target=new RenderTexture(1600,650,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1600,650,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,650),0,0);image.Apply();Directory.CreateDirectory("Previews");File.WriteAllBytes("Previews/HumanRoles.png",image.EncodeToPNG());
            RenderTexture.active=null;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
            Debug.Log("HUMAN_PREVIEW_OK");
        }
    }
}
