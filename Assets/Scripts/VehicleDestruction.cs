using UnityEngine;
namespace SunsetState
{
    public sealed class VehicleDestruction:MonoBehaviour
    {
        VehicleMotor car;GameObject damageSmoke,engineFire;public bool Smoking=>damageSmoke;public bool Burning=>engineFire || Destroyed;public bool Destroyed {get;private set;}
        void Awake(){car=GetComponent<VehicleMotor>();}
        void Update()
        {
            var game=GameRoot.Instance;if(!game || !game.Playing || game.Panel!="" || Destroyed)return;
            if(car.Durability<=0){Explode();return;}
            if(car.Durability<50 && !damageSmoke)damageSmoke=EngineEffect(false);
            if(car.Durability>=50 && damageSmoke){Destroy(damageSmoke);damageSmoke=null;}
            if(car.Durability<=15){if(!engineFire)engineFire=EngineEffect(true);car.Durability=Mathf.Max(0,car.Durability-Time.deltaTime*1.4f);}
            else if(engineFire){Destroy(engineFire);engineFire=null;}
        }
        GameObject EngineEffect(bool fire)
        {
            var go=new GameObject(fire?"Engine fire":"Engine smoke");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(0,.9f,car.Dimensions.z*.3f);
            var system=go.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=system.main;main.loop=true;main.startLifetime=fire?1:2.5f;main.startSpeed=fire?1.3f:1;main.startSize=fire?.65f:1.2f;main.startColor=fire?new Color(1,.35f,.04f,.8f):new Color(.22f,.22f,.22f,.5f);main.gravityModifier=-.1f;main.maxParticles=40;var rate=system.emission;rate.rateOverTime=fire?14:8;var shape=system.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.4f;system.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("ShotBase");system.Play();return go;
        }
        public void BulletHit(float damage,Vector3 point)
        {
            if(Destroyed)return;car.Durability=Mathf.Max(0,car.Durability-damage*(car.Kind==VehicleKind.Tank?.16f:.35f));
            var spark=GameObject.CreatePrimitive(PrimitiveType.Sphere);spark.name="Bullet impact";spark.GetComponent<Collider>().enabled=false;Destroy(spark.GetComponent<Collider>());spark.transform.position=point;spark.transform.localScale=Vector3.one*.12f;spark.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(new Color(1,.65f,.1f));Destroy(spark,.09f);
        }
        public void Explode()
        {
            if(Destroyed)return;Destroyed=true;if(damageSmoke)Destroy(damageSmoke);if(engineFire)Destroy(engineFire);car.Durability=0;car.Traffic=false;car.AIControls=null;car.SetEmergency(false);car.Rotor=null;
            var game=GameRoot.Instance;var driver=car.Driver;
            if(driver){car.Driver=null;driver.Teleport(new Vector3(transform.position.x,2,transform.position.z)+transform.right*(car.Dimensions.x+3));}
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())if(!renderer.GetComponent<TextMesh>())renderer.sharedMaterial=WorldBuilder.Mat(new Color(.12f,.115f,.1f));
            if(car.Wheels!=null)foreach(var wheel in car.Wheels){wheel.motorTorque=0;wheel.brakeTorque=10000;}
            ExplosionDamage(transform.position+Vector3.up,car,150,7,driver);
            ExplosionEffect(transform.position+Vector3.up,true);
            if(driver)game.Notify("Транспорт уничтожен. H / G — получить другой.");
        }
        public static void ExplosionDamage(Vector3 center,VehicleMotor owner,float damage,float radius,bool protectPlayer=false)
        {
            var game=GameRoot.Instance;if(!game)return;
            foreach(var enemy in game.Enemies)if(enemy && !enemy.Neutralized && (!owner || enemy.gameObject!=owner.gameObject)){float distance=Vector3.Distance(center,enemy.transform.position+Vector3.up*.6f);if(distance<radius)enemy.TakeDamage(damage*(1-distance/(radius+1)));}
            foreach(var vehicle in game.Cars)if(vehicle && vehicle!=owner && vehicle.Durability>0)
            {float distance=Vector3.Distance(center,vehicle.transform.position+Vector3.up);if(distance<radius){vehicle.Durability=Mathf.Max(0,vehicle.Durability-damage*(1-distance/(radius+1)));vehicle.Body.AddExplosionForce(30000,center,radius,1.5f);}}
            foreach(var person in Object.FindObjectsByType<Pedestrian>(FindObjectsSortMode.None)){float d=Vector3.Distance(person.transform.position,center);if(d<radius)person.TakeVehicleDamage(damage*(1-d/(radius+1)));}
            foreach(var staff in Object.FindObjectsByType<BaseNPC>(FindObjectsSortMode.None)){float d=Vector3.Distance(staff.transform.position,center);if(d<radius)staff.TakeVehicleDamage(damage*(1-d/(radius+1)));}
            foreach(var road in Object.FindObjectsByType<RoadServiceObject>(FindObjectsSortMode.None))if(Vector3.Distance(road.transform.position,center)<radius+3)road.SetBroken(true);
            foreach(var structure in Object.FindObjectsByType<WorldDestructible>(FindObjectsSortMode.None))if(!structure.Destroyed)
            {float d=Vector3.Distance(structure.Shape.ClosestPoint(center),center);if(d<radius)structure.Damage(damage*(1-d/(radius+1)));}
            if(!protectPlayer && (!owner || game.Player.Vehicle!=owner) && Vector3.Distance(game.Player.Position,center)<radius*.75f)game.DamagePlayer(damage*.4f,center);
        }
        public static void ExplosionEffect(Vector3 point,bool burning)
        {
            var root=new GameObject(burning?"Burning vehicle wreck":"Cannon explosion");root.transform.position=point;
            var particles=root.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=particles.main;main.loop=false;main.duration=.6f;main.startLifetime=1.1f;main.startSpeed=7;main.startSize=1.6f;main.startColor=new Color(1,.45f,.06f,.85f);main.maxParticles=80;var emission=particles.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,35)});var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.8f;particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("ShotBase");particles.Play();
            var light=root.AddComponent<Light>();light.color=new Color(1,.4f,.08f);light.range=18;light.intensity=5;Object.Destroy(light,.35f);
            var clip=Resources.Load<AudioClip>("Audio/Weapons/Shotgun");if(clip)AudioSource.PlayClipAtPoint(clip,point,.8f);
            if(burning)
            {
                var flames=new GameObject("Wreck flames");flames.transform.SetParent(root.transform,false);var fire=flames.AddComponent<ParticleSystem>();fire.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var flame=fire.main;flame.duration=18;flame.loop=false;flame.startLifetime=1.1f;flame.startSpeed=1.4f;flame.startSize=.9f;flame.startColor=new Color(1,.3f,.035f,.8f);flame.gravityModifier=-.1f;flame.maxParticles=50;var flameRate=fire.emission;flameRate.rateOverTime=24;fire.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("ShotBase");fire.Play();
                var smoke=new GameObject("Wreck smoke");smoke.transform.SetParent(root.transform,false);var system=smoke.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var settings=system.main;settings.duration=18;settings.loop=false;settings.startLifetime=3;settings.startSpeed=1.7f;settings.startSize=1.8f;settings.startColor=new Color(.12f,.12f,.12f,.6f);settings.gravityModifier=-.08f;settings.maxParticles=80;var rate=system.emission;rate.rateOverTime=15;system.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("ShotBase");system.Play();
            }
            Object.Destroy(root,burning?23:2);
        }
    }
}
