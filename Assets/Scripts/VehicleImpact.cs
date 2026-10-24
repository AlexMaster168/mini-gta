using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed class VehicleImpact:MonoBehaviour
    {
        VehicleMotor car;readonly Dictionary<int,float> recentlyHit=new Dictionary<int,float>();
        void Awake(){car=GetComponent<VehicleMotor>();}
        void FixedUpdate()
        {
            var game=GameRoot.Instance;if(!game || !game.Playing || game.Panel!="" || car.SimulationDormant || !car.Body || car.Speed<10)return;
            Vector3 delta=car.Body.linearVelocity*Time.fixedDeltaTime;
            Bounds body=new Bounds(car.transform.position+Vector3.up*.8f,new Vector3(car.Dimensions.x,1.8f,car.Dimensions.z));
            bool first=true;foreach(var shape in car.GetComponentsInChildren<BoxCollider>())if(shape.enabled && shape.attachedRigidbody==car.Body){if(first){body=shape.bounds;first=false;}else body.Encapsulate(shape.bounds);}
            Vector3 extent=body.extents+new Vector3(Mathf.Abs(delta.x),Mathf.Abs(delta.y),Mathf.Abs(delta.z))*.5f+Vector3.one*.1f;
            foreach(var hit in Physics.OverlapBox(body.center+delta*.5f,extent,Quaternion.identity,~(1<<2),QueryTriggerInteraction.Collide))
            {var actor=NpcEffects.Actor(hit);if(actor)HitPerson(actor,car.Body.linearVelocity.magnitude);}
        }
        public void HitPerson(GameObject person,float metersPerSecond)
        {
            int key=person.GetInstanceID();if(recentlyHit.TryGetValue(key,out float until) && Time.time<until)return;recentlyHit[key]=Time.time+1;
            ApplyPersonHit(person,metersPerSecond,car.Body.mass,car.Driver);
        }
        public static bool ApplyPersonHit(GameObject person,float speed,float mass,bool responsible)
        {
            if(!person || speed<2.5f)return false;
            var budget=person.GetComponent<AmbientActorBudget>();if(budget)budget.Wake();
            var enemy=person.GetComponent<HostileAgent>();var pedestrian=person.GetComponent<Pedestrian>();var staff=person.GetComponent<BaseNPC>();var contact=person.GetComponent<NeutralActor>();
            float damage=(speed-1)*6*Mathf.Clamp(Mathf.Sqrt(mass/1400f),.45f,2);
            if(enemy){if(enemy.Neutralized || enemy.VehicleUnit)return false;enemy.TakeDamage(damage);enemy.Stun(4);}
            else if(pedestrian){if(pedestrian.Neutralized)return false;pedestrian.TakeVehicleDamage(damage);}
            else if(staff){if(staff.Neutralized)return false;staff.TakeVehicleDamage(damage);}
            else if(contact){if(contact.Health<=0)return false;contact.TakeDamage(damage);}
            else return false;
            var game=GameRoot.Instance;if(responsible && game && !game.LawEnforcement){game.Wanted=Mathf.Min(5,game.Wanted+2);game.Notify("Наезд · полиция объявила розыск");}return true;
        }
        public void Ram(VehicleMotor other,float relativeSpeed)
        {
            if(!other || other==car || relativeSpeed<3)return;
            other.WakeSimulation();
            float massRatio=Mathf.Clamp(car.Body.mass/Mathf.Max(1,other.Body.mass),.4f,4);
            other.Durability=Mathf.Max(0,other.Durability-(relativeSpeed-3)*1.5f*massRatio);
            if(other.Durability<=0){other.Traffic=false;other.AIControls=null;var unit=other.GetComponent<HostileAgent>();if(unit)unit.TakeDamage(1000);}
        }
    }
}
