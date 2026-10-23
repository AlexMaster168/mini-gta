using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace SunsetState.Editor
{
    public static class InventoryPreview
    {
        public static void RenderAndBuild(){Render();ProjectSetup.BuildWindows();}
        public static void Render()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientLight=new Color(.65f,.65f,.65f);RenderSettings.fog=false;
            var light=new GameObject("Studio light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(35,150,0);
            var game=new GameObject("Wardrobe preview").AddComponent<GameRoot>();GameRoot.Instance=game;
            int[] roles={1,2,3,4,14};
            for(int i=0;i<roles.Length;i++)
            {
                int role=roles[i];game.RoleIndex=role;var actor=new GameObject("Outfit "+role);actor.transform.position=new Vector3((i-2)*1.7f,0,0);
                game.Player=actor.AddComponent<PlayerMotor>();game.Player.Visual=CharacterModels.Create(actor.transform,role,game.Roles[role].Color);game.SkinChoices[role]=2;
                game.AccessoryChoices[role*3]=role==4?1:7;game.AccessoryChoices[role*3+1]=role==4?4:5;game.AccessoryChoices[role*3+2]=6;game.ShieldChoices[role]=role>=1&&role<=3?1:0;game.ApplyPlayerOutfit();
            }
            var camera=new GameObject("Outfit preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,2.3f,12);camera.transform.LookAt(new Vector3(0,.9f,0));camera.fieldOfView=32;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.19f,.23f);
            var target=new RenderTexture(1600,700,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(1600,700,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,700),0,0);image.Apply();Directory.CreateDirectory("Previews");File.WriteAllBytes("Previews/RoleLoadouts.png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Debug.Log("INVENTORY_PREVIEW_OK");
        }
    }
}
