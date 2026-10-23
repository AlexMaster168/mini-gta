using System;
using System.Collections;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        IEnumerator CheckPhysicsRecovery(Action<bool> result)
        {
            SelectRole(3);Panel="";Player.Teleport(new Vector3(3300,2,-1300));
            GameObject wall=null;VehicleMotor truck=null;GameObject walker=null;
            try
            {
                foreach(var vehicle in Cars)if(vehicle && !vehicle.OnWater)Require(!vehicle.GetComponent<VehicleRecovery>().Embedded(),"Initial fleet must not intersect buildings: "+vehicle.Title);
                foreach(var npc in FindObjectsByType<BaseNPC>(FindObjectsSortMode.None))Require(SpawnSafety.ClearPlayer(npc.transform.position,npc.transform) && SpawnSafety.Ground(npc.transform.position,.2f,.3f,out _,npc.transform),"Army personnel must stand on clear ground: "+npc.name);
                wall=WorldBuilder.Box("Recovery test building",new Vector3(3320,2,-1300),new Vector3(14,4,14),Color.gray);Physics.SyncTransforms();
                truck=WorldBuilder.SpawnCar(this,new Vector3(3320,2,-1300),"Truck placement regression",Color.gray,false,0,VehicleKind.Truck);
                Require(!truck.GetComponent<VehicleRecovery>().Embedded() && Vector2.Distance(new Vector2(truck.transform.position.x,truck.transform.position.z),new Vector2(3320,-1300))>7,"Truck must spawn beside building, not inside or on roof");
                truck.transform.position=new Vector3(3320,1,-1300);truck.Body.position=truck.transform.position;Physics.SyncTransforms();Require(truck.GetComponent<VehicleRecovery>().Embedded(),"Recovery fixture must intersect solid building");
                Require(truck.GetComponent<VehicleRecovery>().Recover() && !truck.GetComponent<VehicleRecovery>().Embedded(),"Vehicle recovery must remove deep penetration");
                walker=new GameObject("Grounded pedestrian regression");walker.transform.position=new Vector3(3340,3,-1300);walker.AddComponent<NpcGroundMotor>().Initialize();
                Require(walker.transform.position.y<.5f,"NPC spawn must snap to floor instead of floating at requested height");
            }
            catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
            if(walker)
            {
                walker.transform.position=new Vector3(3320,1,-1300);Physics.SyncTransforms();yield return new WaitForSeconds(.8f);
                try{Require(SpawnSafety.ClearPlayer(walker.transform.position,walker.transform),"Walking NPC must recover from embedding");}catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
            }
            if(walker)Destroy(walker);if(wall)Destroy(wall);if(truck){Cars.Remove(truck);Destroy(truck.gameObject);}
            foreach(var kind in new[]{VehicleKind.Motorcycle,VehicleKind.Bicycle})
            {
                var bike=WorldBuilder.SpawnCar(this,new Vector3(3350,2,-1300),"Seat check",Color.red,false,0,kind);Player.Enter(bike);bike.TestControls=new Vector2(0,1);
                yield return new WaitForSeconds(.5f);
                try
                {
                    var visual=bike.GetComponentInChildren<RiderVisual>();Require(visual && visual.Rider,"Two-wheel vehicle needs a visible rider");Transform hips=null;foreach(var bone in visual.Rider.GetComponentsInChildren<Transform>())if(bone.name=="Hips")hips=bone;
                    Require(hips && Vector3.Distance(hips.position,visual.SeatPosition)<.03f,"Rider hips must stay attached to seat: "+kind);
                }
                catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
                if(kind==VehicleKind.Bicycle)
                {
                    float angle=bike.GetComponent<BicycleCrank>().Angle;yield return new WaitForSeconds(.8f);
                    try{Require(Mathf.Abs(Mathf.DeltaAngle(angle,bike.GetComponent<BicycleCrank>().Angle))>1,"Bicycle crank must turn while pedalling");}catch(Exception exception){Debug.LogError("SUNSET_SMOKE_FAILED "+exception);result(false);}
                }
                Player.Teleport(new Vector3(3300,2,-1300));Cars.Remove(bike);Destroy(bike.gameObject);
            }
            SelectRole(0);Debug.Log("SUNSET_FLEET_GROUND_RECOVERY_RIDER_SEAT_PEDALS_OK");
        }
    }
}
