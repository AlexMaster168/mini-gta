using UnityEngine;
namespace SunsetState
{
    public sealed class TankWeapon:MonoBehaviour
    {
        public Transform Turret,Barrel,Muzzle;public float Elevation {get;private set;}public float NextShot {get;private set;}
        VehicleMotor car;
        public void Initialize(VehicleMotor motor)
        {
            car=motor;Turret=new GameObject("Turret pivot").transform;Turret.SetParent(transform,false);Turret.localPosition=new Vector3(0,1.85f,-.3f);
            string[] turretParts={"Turret","Commander hatch","Loader hatch","Antenna","Smoke launcher","Gun barrel","Muzzle brake"};
            foreach(string name in turretParts){var part=transform.Find(name);if(part)part.SetParent(Turret,true);}
            Barrel=new GameObject("Gun elevation pivot").transform;Barrel.SetParent(Turret,false);Barrel.localPosition=new Vector3(0,.15f,1.55f);
            foreach(string name in new[]{"Gun barrel","Muzzle brake"}){var part=Turret.Find(name);if(part)part.SetParent(Barrel,true);}
            Muzzle=new GameObject("Cannon muzzle").transform;Muzzle.SetParent(Barrel,false);Muzzle.localPosition=new Vector3(0,0,2.7f);
        }
        public void Aim(float localYaw,float elevation)
        {Elevation=Mathf.Clamp(elevation,-6,30);Turret.localRotation=Quaternion.Euler(0,localYaw,0);Barrel.localRotation=Quaternion.Euler(-Elevation,0,0);}
        void Update()
        {
            var game=GameRoot.Instance;if(!car || !car.Driver || !game.Playing || game.Panel!="")return;
            float yaw=Turret.localEulerAngles.y,elevation=Elevation;
            if(Input.GetKey(KeyCode.Z))yaw-=55*Time.deltaTime;if(Input.GetKey(KeyCode.X))yaw+=55*Time.deltaTime;
            if(Input.GetKey(KeyCode.PageUp))elevation+=22*Time.deltaTime;if(Input.GetKey(KeyCode.PageDown))elevation-=22*Time.deltaTime;
            if(Input.GetMouseButton(1) && Camera.main)
            {
                Vector3 aim=Camera.main.transform.position+Camera.main.transform.forward*400;
                float nearest=400;foreach(var hit in Physics.RaycastAll(Camera.main.transform.position,Camera.main.transform.forward,400,~(1<<2)))if(!hit.transform.IsChildOf(transform) && hit.distance<nearest){nearest=hit.distance;aim=hit.point;}
                Vector3 local=transform.InverseTransformDirection(aim-Muzzle.position);float desired=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
                yaw=Mathf.MoveTowardsAngle(yaw,desired,70*Time.deltaTime);elevation=Mathf.MoveTowards(elevation,Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg,30*Time.deltaTime);
            }
            Aim(yaw,elevation);if(Input.GetMouseButton(0))Fire();
        }
        public bool Fire()
        {
            var game=GameRoot.Instance;if(Time.time<NextShot || !game || !game.Playing)return false;NextShot=Time.time+3.2f;
            var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="Tank shell";shell.transform.position=Muzzle.position;shell.transform.localScale=Vector3.one*.18f;Destroy(shell.GetComponent<Collider>());shell.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(new Color(1,.75f,.15f));shell.AddComponent<TankShell>().Initialize(car,Muzzle.forward*160);
            var sound=Resources.Load<AudioClip>("Audio/Weapons/Shotgun");if(sound)AudioSource.PlayClipAtPoint(sound,Muzzle.position,.7f);
            car.Body.AddForceAtPosition(-Muzzle.forward*5000,Muzzle.position,ForceMode.Impulse);
            if(!game.LawEnforcement)game.Wanted=Mathf.Min(5,game.Wanted+2);return true;
        }
        public static void Blast(Vector3 position,VehicleMotor owner)
        {
            VehicleDestruction.ExplosionDamage(position,owner,650,11);
            VehicleDestruction.ExplosionEffect(position,false);
        }
    }
    public sealed class TankShell:MonoBehaviour
    {
        VehicleMotor owner;Vector3 velocity;float age;
        public void Initialize(VehicleMotor source,Vector3 speed){owner=source;velocity=speed;}
        void FixedUpdate()
        {
            var game=GameRoot.Instance;if(!game || !game.Playing || game.Panel!="")return;
            float dt=Time.fixedDeltaTime;Vector3 delta=velocity*dt;RaycastHit? closest=null;
            foreach(var hit in Physics.SphereCastAll(transform.position,.12f,delta.normalized,delta.magnitude,~(1<<2),QueryTriggerInteraction.Ignore))if((!owner || !hit.transform.IsChildOf(owner.transform)) && hit.transform!=transform && (!closest.HasValue || hit.distance<closest.Value.distance))closest=hit;
            if(closest.HasValue){TankWeapon.Blast(closest.Value.point,owner);Destroy(gameObject);return;}
            transform.position+=delta;velocity+=Physics.gravity*dt;age+=dt;if(age>6 || !WorldLimits.Contains(transform.position))Destroy(gameObject);
        }
    }
}
