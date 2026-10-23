using System.Collections;
using System.IO;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        RenderTexture previewTarget;int previewRepaints;
        void PreviewRepaint()
        {
            if(!previewTarget||Event.current.type!=EventType.Repaint)return;
            var camera=Camera.main;camera.targetTexture=previewTarget;camera.Render();Graphics.SetRenderTarget(previewTarget);previewRepaints++;
        }
        IEnumerator Snapshot(string name)
        {
            var camera=Camera.main;previewTarget=new RenderTexture(1600,900,24);previewTarget.Create();previewRepaints=0;
            camera.targetTexture=previewTarget;camera.Render();
            yield return null;yield return new WaitForEndOfFrame();
            RenderTexture.active=previewTarget;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","Previews"));Directory.CreateDirectory(folder);
            var pixels=image.GetPixels32();int bright=0;for(int i=0;i<pixels.Length;i+=257)if(pixels[i].r+pixels[i].g+pixels[i].b>30)bright++;
            if(bright<10){Debug.LogError("SUNSET_PREVIEW_FAILED: render target was empty");Application.Quit(1);yield break;}
            File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());Destroy(image);Debug.Log("SUNSET_SCREENSHOT "+name+" GUI_REPAINTS="+previewRepaints);
            camera.targetTexture=null;RenderTexture.active=null;previewTarget.Release();Destroy(previewTarget);previewTarget=null;
        }
        IEnumerator CaptureGamePreviews()
        {
            StartGame(false);Panel="";Time.timeScale=1;var camera=Camera.main;var follow=camera.GetComponent<FollowCamera>();follow.enabled=false;camera.farClipPlane=6000;RenderSettings.fog=false;
            foreach(int role in new[]{3,1,10,13,9})
            {
                SelectRole(role);Panel="";Time.timeScale=1;Wanted=0;
                Vector3 center=WorldBuilder.Places[role].Position+Vector3.forward*(role==13?-30:30);
                camera.transform.position=center+new Vector3(role==13?150:role==3?145:92,role==13?115:role==3?100:70,role==13?-180:role==3?-170:-115);camera.transform.LookAt(center);camera.fieldOfView=55;
                yield return new WaitForSecondsRealtime(.4f);yield return Snapshot(role==3?"ArmyBase":role==1?"PoliceStation":role==10?"CityHall":role==13?"PrisonGrounds":"GroveBase");
            }
            SelectRole(3);Panel="";Time.timeScale=1;camera.transform.position=FactionDistricts.ArmyGate+new Vector3(0,7,-42);camera.transform.LookAt(FactionDistricts.ArmyGate+Vector3.up*2);camera.fieldOfView=62;yield return new WaitForSecondsRealtime(.3f);yield return Snapshot("ArmyCheckpoint");
            SelectRole(3);AcceptMission(0);Panel="map";Time.timeScale=0;mapFocused=true;mapCenter=WorldBuilder.Places[3].Position+Vector3.forward*35;mapSpan=330;
            yield return null;
            mapFocused=false;yield return null;
            SelectRole(2);FemaleChoices[2]=true;SkinChoices[2]=5;Player.SetColor(Roles[2].Color);Panel="wardrobe";Time.timeScale=0;
            yield return null;
            Panel="";Time.timeScale=1;Player.Teleport(new Vector3(3300,2,-1200));
            var motorcycle=WorldBuilder.SpawnCar(this,new Vector3(3300,2,-1200),"Roadster · посадка",new Color(.65f,.16f,.13f),false,0,VehicleKind.Motorcycle);Player.Enter(motorcycle);
            yield return new WaitForSecondsRealtime(.7f);camera.transform.position=motorcycle.transform.position+new Vector3(4,2.3f,-3.7f);camera.transform.LookAt(motorcycle.transform.position+Vector3.up*1.2f);camera.fieldOfView=43;
            yield return Snapshot("MotorcycleRider");Player.Teleport(new Vector3(3350,2,-1200));
            var bicycle=WorldBuilder.SpawnCar(this,new Vector3(3350,2,-1200),"City Bike · педали",new Color(.15f,.6f,.42f),false,0,VehicleKind.Bicycle);Player.Enter(bicycle);bicycle.TestControls=new Vector2(0,1);
            yield return new WaitForSecondsRealtime(1);bicycle.TestControls=Vector2.zero;camera.transform.position=bicycle.transform.position+new Vector3(4,2.3f,-3.7f);camera.transform.LookAt(bicycle.transform.position+Vector3.up*1.25f);
            yield return Snapshot("BicycleRider");Player.Teleport(new Vector3(0,2,0));SelectRole(0);
            foreach(var entry in InteriorSystem.Entrances)if(entry.ShopId==0){EnterBuilding(entry);break;}Panel="shop";Time.timeScale=0;yield return null;
            ExitBuilding();SelectRole(2);SkinChoices[2]=2;FemaleChoices[2]=true;Player.SetColor(Roles[2].Color);Panel="";Time.timeScale=1;Player.Teleport(new Vector3(3300,2,-1400));
            camera.transform.position=Player.Position+new Vector3(2,1.7f,3.2f);camera.transform.LookAt(Player.Position+Vector3.up*.95f);camera.fieldOfView=40;ServiceSelected=true;yield return new WaitForSecondsRealtime(1);yield return Snapshot("FBI-SWAT");
            BuildingEntrance headquarters=null;foreach(var entry in InteriorSystem.Entrances)if(entry.Role==2&&!entry.Name.Contains("Vice")){headquarters=entry;break;}EnterBuilding(headquarters);Panel="";Time.timeScale=1;
            camera.transform.position=interiorOrigin+new Vector3(14,3.6f,-8);camera.transform.LookAt(interiorOrigin+new Vector3(22,1.2f,3));camera.fieldOfView=70;yield return null;yield return Snapshot("HoldingCells");ExitBuilding();
            SelectRole(15);BuildingEntrance court=null;foreach(var entry in InteriorSystem.Entrances)if(entry.Role==15&&!entry.Name.Contains("Vice")){court=entry;break;}EnterBuilding(court);Panel="";Time.timeScale=1;camera.transform.position=interiorOrigin+new Vector3(-7,3,-8);camera.transform.LookAt(interiorOrigin+new Vector3(0,1,4));camera.fieldOfView=65;yield return null;yield return Snapshot("CourtRoom");ExitBuilding();
            SelectRole(6);phoneTab=4;Panel="phone";Time.timeScale=0;yield return null;
            SelectRole(17);Panel="laws";Time.timeScale=0;yield return null;
            SelectRole(2);Panel="operations";Time.timeScale=0;yield return null;Panel="";Time.timeScale=1;
            visaUntil=Time.realtimeSinceStartup+1200;FlyVice(true);camera.transform.position=ViceCityIsland.Center+new Vector3(650,450,-850);camera.transform.LookAt(ViceCityIsland.Center);camera.fieldOfView=60;yield return new WaitForSecondsRealtime(.3f);yield return Snapshot("ViceCity");FlyVice(false);
            SelectRole(0);Panel="";Time.timeScale=1;Player.Teleport(new Vector3(3300,2,-1700));VehicleMotor featureCar=null;
            for(int i=0;i<3;i++){var car=WorldBuilder.SpawnCar(this,new Vector3(3300+i*5,2,-1700),i==0?"Ferrari · стилизация":i==1?"Tesla · стилизация":"BMW · стилизация",new Color(.7f,.2f,.15f),false,0,i==0?VehicleKind.Supercar:i==1?VehicleKind.Electric:VehicleKind.Sport);if(i==1)featureCar=car;}
            yield return new WaitForSecondsRealtime(.7f);camera.transform.position=featureCar.transform.position+new Vector3(9,5,9);camera.transform.LookAt(featureCar.transform.position+Vector3.up*.8f);camera.fieldOfView=55;yield return Snapshot("ModernCars");
            Debug.Log("SUNSET_PREVIEWS_OK");
        }
    }
}
