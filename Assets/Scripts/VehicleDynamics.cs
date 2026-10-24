using UnityEngine;

namespace SunsetState
{
    public sealed partial class VehicleMotor
    {
        public WheelCollider[] Wheels;
        public Transform[] WheelVisuals;
        public int Gear {get;private set;}=1;
        public float RPM {get;private set;}=900;
        internal Vector2? TestControls;
        internal bool TestHandbrake;
        public bool Handbraking {get;private set;}
        public float Steering=>smoothSteer;
        public float Throttle=>smoothThrottle;
        float smoothSteer,smoothThrottle,skid;
        public float Skid=>skid;
        void InitializeWheels()
        {
            if(Dimensions==Vector3.zero)Dimensions=(Kind==VehicleKind.Motorcycle || Kind==VehicleKind.Bicycle)?new Vector3(.8f,1.4f,2.2f):Kind==VehicleKind.Bus?new Vector3(2.6f,3.2f,11):Kind==VehicleKind.Tank?new Vector3(3.4f,2.1f,7f):Kind==VehicleKind.Truck?new Vector3(2.5f,3,7.2f):Kind==VehicleKind.SUV?new Vector3(2.1f,1.9f,4.8f):new Vector3(1.95f,1.5f,4.65f);
            bool bike=(Kind==VehicleKind.Motorcycle || Kind==VehicleKind.Bicycle),heavy=Kind==VehicleKind.Truck||Kind==VehicleKind.Bus,tank=Kind==VehicleKind.Tank;
            Body.mass=Kind==VehicleKind.Bicycle?110:bike?240:tank?8000:heavy?5000:Kind==VehicleKind.SUV?1850:1400;
            Body.centerOfMass=new Vector3(0,bike?.25f:.28f,0);Body.linearDamping=.035f;Body.angularDamping=1.4f;Body.maxAngularVelocity=2.5f;
            Body.constraints=bike?RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ:RigidbodyConstraints.None;
            Wheels=new WheelCollider[bike?2:4];float radius=bike?.34f:tank?.62f:heavy?.46f:.35f;
            var model=GetComponentInChildren<VehicleModelData>();WheelVisuals=new Transform[Wheels.Length];
            for(int i=0;i<Wheels.Length;i++)
            {
                var wheel=new GameObject("Suspension "+i);wheel.transform.SetParent(transform,false);
                int side=i%2==0?-1:1;bool front=bike?i==0:i<2;
                wheel.transform.localPosition=new Vector3((bike?0:side*Dimensions.x*.41f),radius+.12f,(front?1:-1)*Dimensions.z*(tank?.28f:heavy?.34f:.31f));
                float wheelRadius=radius;
                if(model && model.WheelCenters!=null && model.WheelCenters.Length==Wheels.Length){wheel.transform.localPosition=model.WheelCenters[i]+Vector3.up*.12f;wheelRadius=Mathf.Clamp(model.WheelRadii[i],.2f,.7f);WheelVisuals[i]=model.transform.Find("Wheel"+i);}
                var collider=wheel.AddComponent<WheelCollider>();Wheels[i]=collider;collider.radius=wheelRadius;collider.mass=tank?110:heavy?60:25;collider.suspensionDistance=tank?.42f:heavy?.32f:.24f;collider.wheelDampingRate=1.1f;
                collider.forceAppPointDistance=.12f;collider.ConfigureVehicleSubsteps(5,12,15);
                var spring=collider.suspensionSpring;spring.spring=tank?140000:heavy?85000:Kind==VehicleKind.Bicycle?6500:bike?15000:35000;spring.damper=tank?11000:heavy?9000:Kind==VehicleKind.Bicycle?650:bike?1800:4500;spring.targetPosition=.5f;collider.suspensionSpring=spring;
                var forward=collider.forwardFriction;forward.extremumSlip=.4f;forward.extremumValue=1;forward.asymptoteSlip=.9f;forward.asymptoteValue=.75f;forward.stiffness=1.7f;collider.forwardFriction=forward;
                var sideways=collider.sidewaysFriction;sideways.extremumSlip=.22f;sideways.extremumValue=1;sideways.asymptoteSlip=.65f;sideways.asymptoteValue=.7f;sideways.stiffness=2.05f;collider.sidewaysFriction=sideways;
            }
            Body.solverIterations=10;Body.solverVelocityIterations=6;
            gameObject.AddComponent<VehicleEffects>().Initialize(this);
        }
        void StepWheels(float throttle,float steer,bool handbrake)
        {
            if(TestControls.HasValue){throttle=TestControls.Value.y;steer=TestControls.Value.x;handbrake=TestHandbrake;}
            Handbraking=handbrake;
            float dt=Time.fixedDeltaTime;
            smoothThrottle=Mathf.MoveTowards(smoothThrottle,throttle,dt*2.8f);smoothSteer=Mathf.MoveTowards(smoothSteer,steer,dt*3.5f);
            bool bike=(Kind==VehicleKind.Motorcycle || Kind==VehicleKind.Bicycle),heavy=Kind==VehicleKind.Truck||Kind==VehicleKind.Bus,tank=Kind==VehicleKind.Tank;
            float speed=Vector3.Dot(Body.linearVelocity,transform.forward),absolute=Mathf.Abs(speed);
            float max=Kind==VehicleKind.Bicycle?12:tank?28:Kind==VehicleKind.Supercar?78:Kind==VehicleKind.Electric?62:Kind==VehicleKind.Sport?66:bike?55:heavy?30:Kind==VehicleKind.SUV?43:48;
            if(HealthFraction<.15f)max*=.5f;
            bool reversing=throttle*speed < -1;
            float torque=(Kind==VehicleKind.Bicycle?160:tank?9000:Kind==VehicleKind.Supercar?2300:Kind==VehicleKind.Electric?2400:Kind==VehicleKind.Sport?1800:bike?550:heavy?4300:Kind==VehicleKind.SUV?1400:1100)*smoothThrottle;
            if(absolute>max || reversing || handbrake)torque=0;
            if(throttle<0 && speed<0 && absolute>12)torque=0;
            torque*=Mathf.Lerp(1,.25f,Mathf.InverseLerp(max*.65f,max,absolute));
            float wheelbase=Mathf.Abs(Wheels[0].transform.localPosition.z-Wheels[Wheels.Length-1].transform.localPosition.z);
            float steeringLimit=Mathf.Lerp(tank?26:heavy?32:36,tank?8:heavy?7:10,Mathf.InverseLerp(3,55,absolute));
            float angle=smoothSteer*steeringLimit;
            skid=0;
            for(int i=0;i<Wheels.Length;i++)
            {
                bool front=bike?i==0:i<2;var wheel=Wheels[i];wheel.steerAngle=front?angle:0;
                bool drive=bike?!front:Kind==VehicleKind.SUV||heavy||!front;
                wheel.motorTorque=drive?torque:0;
                bool parked=!Driver && !Traffic && !TestControls.HasValue && !AIControls.HasValue;
                wheel.brakeTorque=parked?5000:reversing?(tank?16000:heavy?12000:4500):handbrake&&!front?(tank?20000:heavy?16000:8000):Mathf.Abs(throttle)<.01f?75:0;
                var friction=wheel.sidewaysFriction;friction.stiffness=handbrake&&!front?.55f:2.05f;wheel.sidewaysFriction=friction;
                if(wheel.GetGroundHit(out WheelHit hit))skid=Mathf.Max(skid,Mathf.Abs(hit.sidewaysSlip));
                if(WheelVisuals!=null && i<WheelVisuals.Length && WheelVisuals[i]){wheel.GetWorldPose(out Vector3 position,out Quaternion rotation);WheelVisuals[i].position=position;WheelVisuals[i].rotation=rotation;}
            }
            if(!bike){AntiRoll(Wheels[0],Wheels[1],tank?18000:heavy?14000:7500);AntiRoll(Wheels[2],Wheels[3],tank?16000:heavy?13000:6500);}
            int grounded=0;foreach(var wheel in Wheels)if(wheel.isGrounded)grounded++;
            if(grounded>=2)Body.AddForce(Vector3.down*absolute*absolute*3,ForceMode.Force);
            if(grounded>=2 && absolute>8 && !handbrake)
            {
                float desiredYaw=Mathf.Clamp(speed/Mathf.Max(1.2f,wheelbase)*Mathf.Tan(angle*Mathf.Deg2Rad),-Mathf.Min(.7f,12/absolute),Mathf.Min(.7f,12/absolute));
                Vector3 angular=Body.angularVelocity;angular.y=Mathf.MoveTowards(angular.y,desiredYaw,dt*2);Body.angularVelocity=angular;
                Vector3 lateral=transform.right*Vector3.Dot(Body.linearVelocity,transform.right);Body.AddForce(-lateral*Mathf.Lerp(1.5f,3,Mathf.InverseLerp(10,50,absolute)),ForceMode.Acceleration);
            }
            if(absolute>1)Body.AddForce(-Body.linearVelocity*absolute*(heavy?1.8f:.42f),ForceMode.Force);
            Gear=speed<-.4f?-1:Mathf.Clamp(1+Mathf.FloorToInt(Speed/(heavy?23:38)),1,6);
            float targetRpm=900+(Speed%(heavy?23:38))/(heavy?23:38)*5500+Mathf.Abs(throttle)*700;
            RPM=Mathf.Lerp(RPM,targetRpm,dt*5);
            if(Driver && Input.GetKey(KeyCode.V))
            {
                if(Speed>8)return;GetComponent<VehicleRecovery>().Recover();
            }
        }
        void AntiRoll(WheelCollider left,WheelCollider right,float stiffness)
        {
            float lt=1,rt=1;bool lg=left.GetGroundHit(out WheelHit lh),rg=right.GetGroundHit(out WheelHit rh);
            if(lg)lt=Mathf.Clamp01((-left.transform.InverseTransformPoint(lh.point).y-left.radius)/left.suspensionDistance);
            if(rg)rt=Mathf.Clamp01((-right.transform.InverseTransformPoint(rh.point).y-right.radius)/right.suspensionDistance);
            float force=(lt-rt)*stiffness;
            if(lg)Body.AddForceAtPosition(left.transform.up*-force,left.transform.position);
            if(rg)Body.AddForceAtPosition(right.transform.up*force,right.transform.position);
        }
    }
}
