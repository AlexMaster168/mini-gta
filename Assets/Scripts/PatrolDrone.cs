using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed class PatrolDrone:MonoBehaviour
    {
        public static readonly List<PatrolDrone> Active=new List<PatrolDrone>();
        public int Faction;public float Health;public bool Destroyed=>Health<=0;Vector3 home;float phase,nextReport;Transform[] rotors;Light searchlight;AudioSource audio;Rigidbody body;
        public static PatrolDrone Spawn(Vector3 position,int faction)
        {
            var root=new GameObject(faction==2?"Патрульный дрон ФБР":"Полицейский патрульный дрон");root.transform.position=position;
            var drone=root.AddComponent<PatrolDrone>();drone.Faction=faction;drone.Health=faction==2?100:70;drone.home=position;drone.phase=Active.Count*2;
            var collider=root.AddComponent<SphereCollider>();collider.radius=.55f;drone.body=root.AddComponent<Rigidbody>();drone.body.isKinematic=true;drone.body.useGravity=false;
            var color=faction==2?new Color(.1f,.13f,.18f):new Color(.16f,.3f,.5f);
            System.Func<string,Vector3,Vector3,Color,Transform> part=(name,p,s,c)=>{var item=WorldBuilder.Box(name,Vector3.zero,s,c,null,false);item.transform.SetParent(root.transform,false);item.transform.localPosition=p;return item.transform;};
            part("Drone body",Vector3.zero,new Vector3(.65f,.22f,.8f),color);drone.rotors=new Transform[4];
            for(int i=0;i<4;i++){Vector3 p=new Vector3(i%2==0?-.65f:.65f,.05f,i<2?.65f:-.65f);part("Rotor arm",p*.5f,new Vector3(.12f,.08f,1),color).localRotation=Quaternion.Euler(0,i%2==0?45:-45,0);drone.rotors[i]=part("Spinning propeller",p,new Vector3(.65f,.035f,.08f),Color.black);}
            part("Camera pod",new Vector3(0,-.25f,.2f),new Vector3(.2f,.2f,.25f),Color.black);
            var lamp=new GameObject("Searchlight");lamp.transform.SetParent(root.transform,false);lamp.transform.localPosition=Vector3.down*.2f;drone.searchlight=lamp.AddComponent<Light>();drone.searchlight.type=LightType.Spot;drone.searchlight.spotAngle=42;drone.searchlight.range=55;drone.searchlight.intensity=3;drone.searchlight.color=new Color(.85f,.94f,1);
            drone.audio=root.AddComponent<AudioSource>();drone.audio.clip=Resources.Load<AudioClip>("Audio/Rotor");drone.audio.loop=true;drone.audio.playOnAwake=false;drone.audio.spatialBlend=1;drone.audio.minDistance=3;drone.audio.maxDistance=70;drone.audio.volume=.06f;drone.audio.pitch=1.7f;
            InteriorSystem.Label(position+Vector3.up,"ДРОН · "+(faction==2?"ФБР":"ПОЛИЦИЯ"),root.transform,.035f);Active.Add(drone);return drone;
        }
        void Update()
        {
            var game=GameRoot.Instance;if(!game || !game.Playing || game.Panel!="" || Destroyed){if(audio && audio.isPlaying)audio.Pause();return;}
            bool track=game.Wanted>0 && !game.InInterior && Vector3.Distance(transform.position,game.Player.Position)<(Faction==2?180:140);
            Vector3 target=track?game.Player.Position+new Vector3(8,18,5):home+new Vector3(Mathf.Sin(Time.time*.11f+phase)*55,Mathf.Sin(Time.time*.5f)*2,Mathf.Cos(Time.time*.11f+phase)*55);
            Vector3 motion=target-transform.position;transform.position=Vector3.MoveTowards(transform.position,target,(Faction==2?14:10)*Time.deltaTime);
            if(Vector3.ProjectOnPlane(motion,Vector3.up).sqrMagnitude>.1f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(Vector3.ProjectOnPlane(motion,Vector3.up)),Time.deltaTime*3);
            foreach(var rotor in rotors)rotor.Rotate(0,2200*Time.deltaTime,0);searchlight.transform.LookAt(track?game.Player.Position:transform.position+Vector3.down*30);searchlight.enabled=track || game.Hour<7 || game.Hour>19;
            if(Vector3.Distance(transform.position,game.Player.Position)<70){if(audio.clip && !audio.isPlaying)audio.Play();}else audio.Pause();
            if(track && Time.time>nextReport && Visible(game.Player.Position+Vector3.up)){nextReport=Time.time+20;game.DroneSpottedPlayer(Faction);}
        }
        bool Visible(Vector3 target){foreach(var hit in Physics.RaycastAll(transform.position,(target-transform.position).normalized,Vector3.Distance(target,transform.position),~(1<<2))){if(hit.transform.IsChildOf(transform))continue;var player=GameRoot.Instance.Player;if(player.Vehicle && hit.transform.IsChildOf(player.Vehicle.transform))continue;return false;}return true;}
        public void TakeDamage(float damage)
        {if(Destroyed)return;Health=Mathf.Max(0,Health-damage);if(GameRoot.Instance && !GameRoot.Instance.LawEnforcement)GameRoot.Instance.Wanted=Mathf.Max(3,GameRoot.Instance.Wanted);if(!Destroyed)return;searchlight.enabled=false;audio.Stop();body.isKinematic=false;body.useGravity=true;Destroy(gameObject,4);}
        void OnDestroy(){Active.Remove(this);}
    }
    public sealed partial class GameRoot
    {
        public void DroneSpottedPlayer(int faction){if(Wanted<=0 || InInterior)return;wantedTimer=0;Notify((faction==2?"Дрон ФБР":"Полицейский дрон")+" передал твоё местоположение. Уйди из зоны наблюдения.");}
    }
}
