using UnityEngine;
namespace SunsetState
{
    public static class WorldSimulation
    {
        public static int DormantVehicles,DormantActors;
        public static float WakeRadius=>RuntimeSettings.Data.Quality==0?180:RuntimeSettings.Data.Quality==1?280:420;
        public static bool ShouldSleep(Vector3 position,bool dormant)
        {
            var game=GameRoot.Instance;if(!RuntimeSettings.Data.LimitAmbient||!game||!game.Player)return false;
            float radius=WakeRadius+(dormant?0:100);return (position-game.Player.Position).sqrMagnitude>radius*radius;
        }
    }
    public sealed partial class GameRoot
    {
        public bool SimulationPinned(GameObject actor)
        {
            if(actor==companion||actor==conversationNpc||actor==transportSuspect||actor==serviceSubject||actor==hostage)return true;
            if(pursuitTarget&&actor==pursuitTarget.gameObject||pursuer&&actor==pursuer.gameObject||Call!=null&&Call.Runner&&actor==Call.Runner.gameObject)return true;
            return ActiveMission!=null&&Step<ActiveMission.Points.Length&&(actor.transform.position-ActiveMission.Points[Step]).sqrMagnitude<6400;
        }
    }
    public sealed partial class VehicleMotor
    {
        public bool SimulationDormant{get;private set;}
        float nextSimulationCheck;Vector3 sleepingVelocity,sleepingAngular;CollisionDetectionMode wakingCollision;
        public void RefreshSimulationBudget(bool force=false)
        {
            var game=GameRoot.Instance;if(!game||!game.Playing||!Body)return;
            bool important=Driver||AIControls.HasValue||TestControls.HasValue||!Traffic||Flying||OnWater||HealthFraction<.99f||GetComponent<HostileAgent>()||game.SimulationPinned(gameObject);
            if(important||!RuntimeSettings.Data.LimitAmbient){WakeSimulation();return;}
            if(!force&&Time.unscaledTime<nextSimulationCheck)return;nextSimulationCheck=Time.unscaledTime+.25f;
            if(WaterBody.At(transform.position)!=null){WakeSimulation();return;}
            bool sleep=WorldSimulation.ShouldSleep(transform.position,SimulationDormant);
            if(sleep&&!SimulationDormant&&!Body.isKinematic)
            {
                sleepingVelocity=Body.linearVelocity;sleepingAngular=Body.angularVelocity;wakingCollision=Body.collisionDetectionMode;
                if(Wheels!=null)foreach(var wheel in Wheels){wheel.motorTorque=0;wheel.brakeTorque=0;wheel.enabled=false;}
                Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;Body.collisionDetectionMode=CollisionDetectionMode.Discrete;Body.isKinematic=true;SimulationDormant=true;WorldSimulation.DormantVehicles++;
            }
            else if(!sleep)WakeSimulation();
        }
        public void WakeSimulation()
        {
            if(!SimulationDormant)return;SimulationDormant=false;WorldSimulation.DormantVehicles=Mathf.Max(0,WorldSimulation.DormantVehicles-1);
            if(!Body)return;Body.isKinematic=false;Body.collisionDetectionMode=wakingCollision;if(Wheels!=null)foreach(var wheel in Wheels)wheel.enabled=true;Body.linearVelocity=sleepingVelocity;Body.angularVelocity=sleepingAngular;Body.WakeUp();
        }
        void OnDestroy(){if(SimulationDormant)WorldSimulation.DormantVehicles=Mathf.Max(0,WorldSimulation.DormantVehicles-1);}
    }
    public sealed class AmbientActorBudget:MonoBehaviour
    {
        public bool Dormant{get;private set;}
        Pedestrian pedestrian;NpcGroundMotor motor;CharacterMotion[] motions;Animation[] animations;bool[] motionStates,animationStates;bool pedestrianState,motorState;float nextCheck;
        void Start(){Capture();}
        void Capture(){if(pedestrian&&motor)return;pedestrian=GetComponent<Pedestrian>();motor=GetComponent<NpcGroundMotor>();motions=GetComponentsInChildren<CharacterMotion>();animations=GetComponentsInChildren<Animation>();nextCheck=Time.unscaledTime+(GetInstanceID()&31)*.01f;}
        void Update(){var game=GameRoot.Instance;if(game&&game.Playing&&game.Panel==""&&Time.unscaledTime>=nextCheck){nextCheck=Time.unscaledTime+.25f;Evaluate();}}
        public void Evaluate()
        {
            Capture();var game=GameRoot.Instance;if(!pedestrian||!motor||!game||!game.Playing)return;
            if(pedestrian.Neutralized||GetComponent<CivilianKnockdown>()||game.SimulationPinned(gameObject)||!WorldSimulation.ShouldSleep(transform.position,Dormant)){Wake();return;}
            if(Dormant)return;pedestrianState=pedestrian.enabled;motorState=motor.enabled;motionStates=new bool[motions.Length];animationStates=new bool[animations.Length];
            for(int i=0;i<motions.Length;i++){motionStates[i]=motions[i].enabled;motions[i].enabled=false;}for(int i=0;i<animations.Length;i++){animationStates[i]=animations[i].enabled;animations[i].enabled=false;}
            pedestrian.enabled=false;motor.enabled=false;Dormant=true;WorldSimulation.DormantActors++;
        }
        public void Wake()
        {
            if(!Dormant)return;Dormant=false;WorldSimulation.DormantActors=Mathf.Max(0,WorldSimulation.DormantActors-1);if(pedestrian)pedestrian.enabled=pedestrianState;if(motor)motor.enabled=motorState;
            for(int i=0;i<motions.Length;i++)if(motions[i])motions[i].enabled=motionStates[i];for(int i=0;i<animations.Length;i++)if(animations[i])animations[i].enabled=animationStates[i];
        }
        void OnDisable(){Wake();}
        void OnDestroy(){Wake();}
    }
}
