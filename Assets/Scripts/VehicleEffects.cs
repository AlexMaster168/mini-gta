using UnityEngine;
namespace SunsetState
{
    public sealed class VehicleEffects:MonoBehaviour
    {
        VehicleMotor car;LineRenderer[] lines;Vector3[] previous;bool[] drawing;float timer;int cursor;
        Light[] headlights;bool lightsOn;AudioSource tires;static AudioClip tireClip;
        public void Initialize(VehicleMotor motor)
        {
            car=motor;previous=new Vector3[car.Wheels.Length];drawing=new bool[car.Wheels.Length];
            lines=new LineRenderer[80];
            if(!tireClip){float[] data=new float[22050];var noise=new System.Random(24);float sample=0;for(int i=0;i<data.Length;i++){sample=Mathf.Lerp(sample,(float)noise.NextDouble()*2-1,.55f);data[i]=sample*.18f*(.75f+Mathf.Sin(i*.08f)*.25f);}tireClip=AudioClip.Create("Tire friction",data.Length,1,22050,false);tireClip.SetData(data,0);}
            tires=gameObject.AddComponent<AudioSource>();tires.clip=tireClip;tires.loop=true;tires.playOnAwake=false;tires.spatialBlend=1;tires.minDistance=5;tires.maxDistance=55;
            headlights=new Light[2];for(int i=0;i<2;i++){var lamp=new GameObject("Headlight");lamp.transform.SetParent(transform,false);lamp.transform.localPosition=new Vector3((i==0?-1:1)*car.Dimensions.x*.32f,.72f,car.Dimensions.z*.48f);var light=lamp.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=65;light.range=48;light.intensity=2.5f;light.color=new Color(1,.94f,.8f);light.shadows=LightShadows.None;light.enabled=false;headlights[i]=light;}
        }
        void Update()
        {
            var game=GameRoot.Instance;if(!game || !game.Playing)return;
            if(car.Driver && Input.GetKeyDown(KeyCode.L))lightsOn=!lightsOn;
            foreach(var lamp in headlights)lamp.enabled=lightsOn || (car.Driver || car.Traffic) && (game.Hour<6 || game.Hour>19);
            bool skid=game.Panel=="" && car.Speed>12 && car.Skid>.22f;
            if(skid){if(!tires.isPlaying)tires.Play();tires.volume=Mathf.Clamp01(car.Skid)*.5f;}else tires.Pause();
            timer+=Time.deltaTime;if(timer<.065f)return;timer=0;
            for(int i=0;i<car.Wheels.Length;i++)
            {
                var wheel=car.Wheels[i];bool contact=wheel.GetGroundHit(out WheelHit hit) && car.Speed>10 && (Mathf.Abs(hit.sidewaysSlip)>.22f || car.Handbraking);
                if(contact){Vector3 point=hit.point+Vector3.up*.035f;if(drawing[i] && Vector3.Distance(previous[i],point)<4)DrawMark(previous[i],point);previous[i]=point;}drawing[i]=contact;
            }
        }
        void DrawMark(Vector3 a,Vector3 b)
        {
            // Bounded ring of skid marks; no growing scene allocations during a long drive.
            var line=lines[cursor];if(!line){var go=new GameObject("Tire mark");line=go.AddComponent<LineRenderer>();line.sharedMaterial=WorldBuilder.Mat(new Color(.075f,.08f,.085f));line.positionCount=2;line.startWidth=line.endWidth=.17f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;lines[cursor]=line;}
            line.SetPosition(0,a);line.SetPosition(1,b);cursor=(cursor+1)%lines.Length;
        }
        void OnDestroy(){if(lines!=null)foreach(var line in lines)if(line)Destroy(line.gameObject);}
    }
}
