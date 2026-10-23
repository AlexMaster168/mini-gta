using UnityEngine;
namespace SunsetState
{
    public sealed class EmergencyLights:MonoBehaviour
    {
        VehicleMotor car;Renderer[] lamps;Light[] beams;Material[] materials;
        public int LampCount=>lamps==null?0:lamps.Length;
        public void Initialize(VehicleMotor vehicle,int faction)
        {
            car=vehicle;bool bike=car.Kind==VehicleKind.Motorcycle||car.Kind==VehicleKind.Bicycle;bool air=car.Flying;
            lamps=new Renderer[4];beams=new Light[4];materials=new Material[4];
            for(int i=0;i<4;i++)
            {
                bool blue=faction==4?true:faction==5?false:i%2==0;
                Color color=blue?new Color(.06f,.32f,1):new Color(1,.04f,.025f);
                var go=WorldBuilder.Box("Emergency strobe",Vector3.zero,bike?new Vector3(.12f,.1f,.12f):new Vector3(.43f,.15f,.22f),color,null,false);go.transform.SetParent(transform,false);
                float side=i%2==0?-1:1;bool front=i<2;
                go.transform.localPosition=air?new Vector3(side*1.4f,1.5f,front?1.5f:-2):bike?new Vector3(side*.25f,front?1.05f:.85f,front?.65f:-.65f):new Vector3(side*.45f,front?car.Dimensions.y+.09f:.85f,front?0:car.Dimensions.z*.49f);
                lamps[i]=go.GetComponent<Renderer>();materials[i]=new Material(Resources.Load<Material>("ShotBase"));materials[i].color=color;lamps[i].sharedMaterial=materials[i];
                var light=go.AddComponent<Light>();light.color=color;light.type=LightType.Point;light.range=bike?8:15;light.intensity=3;light.shadows=LightShadows.None;beams[i]=light;
            }
            car.Beacons=lamps;UpdatePattern(0);
        }
        public void UpdatePattern(float time)
        {
            if(!car || lamps==null)return;int phase=Mathf.FloorToInt(time*12)%8;
            for(int i=0;i<lamps.Length;i++){bool on=car.Durability>0 && car.EmergencyEnabled && (i%2==0?phase==0||phase==2:phase==4||phase==6);lamps[i].enabled=on;beams[i].enabled=on;}
        }
        void LateUpdate(){UpdatePattern(Time.time);}
        void OnDestroy(){if(materials!=null)foreach(var material in materials)if(material)Destroy(material);}
    }
}
