using System;
using System.Collections;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        IEnumerator CheckWorldSimulation(Action<bool> result)
        {
            SelectRole(0);Player.Teleport(new Vector3(3300,2,-1800));bool previousLimit=RuntimeSettings.Data.LimitAmbient;int quality=RuntimeSettings.Data.Quality;RuntimeSettings.Data.LimitAmbient=true;RuntimeSettings.Data.Quality=1;
            var car=WorldBuilder.SpawnCar(this,new Vector3(4300,2,-1800),"Ambient budget test",Color.gray,true,0);GameObject person=new GameObject("Ambient actor test");person.transform.position=new Vector3(4400,1,-1800);CharacterModels.Create(person.transform,0,Color.gray);person.AddComponent<Pedestrian>();var budget=person.AddComponent<AmbientActorBudget>();var previousConversation=conversationNpc;
            yield return null;
            car.RefreshSimulationBudget(true);budget.Evaluate();Vector3 parked=car.Body.position,standing=person.transform.position;yield return new WaitForSeconds(.3f);
            try
            {
                car.RefreshSimulationBudget(true);budget.Evaluate();Require(car.SimulationDormant&&car.Body.isKinematic&&budget.Dormant,"Far ambient traffic and actors must pause");foreach(var wheel in car.Wheels)Require(!wheel.enabled,"Dormant wheel physics must stop");
                Require(Vector3.Distance(parked,car.Body.position)<.001f&&Vector3.Distance(standing,person.transform.position)<.001f,"Dormant objects must stay physically stable");foreach(var motion in person.GetComponentsInChildren<CharacterMotion>())Require(!motion.enabled,"Far ambient animations must pause");
                Require(WorldSimulation.ShouldSleep(Player.Position+Vector3.right*330,true)&&!WorldSimulation.ShouldSleep(Player.Position+Vector3.right*330,false),"Hysteresis must prevent threshold oscillation");
                car.TestControls=Vector2.zero;car.RefreshSimulationBudget(true);Require(!car.SimulationDormant&&!car.Body.isKinematic,"Test and mission-style controls must remain awake");car.TestControls=null;car.RefreshSimulationBudget(true);
                car.AIControls=Vector2.up;car.RefreshSimulationBudget(true);Require(!car.SimulationDormant,"Pursuit AI must remain awake regardless of distance");car.AIControls=null;car.RefreshSimulationBudget(true);
                ActiveMission=new Mission("Budget pin","",1,false,new[]{car.transform.position},"Objective");Step=0;car.RefreshSimulationBudget(true);Require(!car.SimulationDormant,"Objects at mission objective must remain active");ActiveMission=null;car.RefreshSimulationBudget(true);
                conversationNpc=person;budget.Evaluate();Require(!budget.Dormant,"Conversation and companion actors must remain active");conversationNpc=previousConversation;budget.Evaluate();
                car.GetComponent<VehicleDestruction>().BulletHit(24,car.transform.position);Require(!car.SimulationDormant,"A remotely damaged vehicle must wake immediately");car.Durability=car.MaxDurability;car.RefreshSimulationBudget(true);
                RuntimeSettings.Data.LimitAmbient=false;car.RefreshSimulationBudget(true);budget.Evaluate();Require(!car.SimulationDormant&&!budget.Dormant,"Disabling optimization must restore ambient simulation");RuntimeSettings.Data.LimitAmbient=true;
                Player.Teleport(new Vector3(4300,2,-1800));car.RefreshSimulationBudget(true);budget.Evaluate();Require(!car.SimulationDormant&&!budget.Dormant&&person.GetComponent<Pedestrian>().enabled&&person.GetComponent<NpcGroundMotor>().enabled,"Approaching must restore actor motion and vehicle physics");
                Player.Teleport(new Vector3(3300,2,-1800));car.RefreshSimulationBudget(true);Player.Enter(car);Require(Player.Vehicle==car&&!car.SimulationDormant&&!car.Body.isKinematic,"Entering a parked distant car must wake it before driving");Player.Teleport(new Vector3(3300,2,-1800));
                var settings=new UserSettingsData{Version=1,LimitAmbient=false};string legacy=JsonUtility.ToJson(settings);string path=System.IO.Path.Combine(Application.temporaryCachePath,"sunset-simulation-settings-check.json");System.IO.File.WriteAllText(path,legacy);Require(RuntimeSettings.Read(path).LimitAmbient,"Old settings must enable ambient optimization on migration");System.IO.File.Delete(path);
                Debug.Log("SUNSET_SIMULATION_HYSTERESIS_SLEEP_WAKE_AI_DAMAGE_ENTRY_SETTINGS_MIGRATION_OK");
            }
            catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            conversationNpc=previousConversation;RuntimeSettings.Data.LimitAmbient=previousLimit;RuntimeSettings.Data.Quality=quality;Cars.Remove(car);Destroy(car.gameObject);Destroy(person);SelectRole(0);
        }
    }
}
