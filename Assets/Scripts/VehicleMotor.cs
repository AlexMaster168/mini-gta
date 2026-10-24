using UnityEngine;

namespace SunsetState
{
    public enum VehicleKind { Sedan, Sport, SUV, Motorcycle, Truck, Bus, Helicopter, Boat, Tank, Bicycle, Supercar, Electric }
    public sealed partial class VehicleMotor : MonoBehaviour
    {
        public PlayerMotor Driver;
        public bool Traffic;
        public string Title;
        public Rigidbody Body;
        public Vector2? AIControls;
        public VehicleKind Kind;
        public float Durability=100;
        public float MaxDurability=>Kind==VehicleKind.Tank?1200:Kind==VehicleKind.Truck||Kind==VehicleKind.Bus?450:Kind==VehicleKind.Motorcycle||Kind==VehicleKind.Bicycle?180:300;
        public float HealthFraction=>Mathf.Clamp01(Durability/MaxDurability);
        public Vector3 Dimensions;
        public Transform Rotor;
        public int Faction = -1;
        public Renderer[] Beacons;
        bool beaconsEnabled;
        public bool EmergencyEnabled=>beaconsEnabled;
        public void SetEmergency(bool enabled){beaconsEnabled=enabled;}
        public bool Flying => Kind == VehicleKind.Helicopter;
        public bool OnWater => Kind == VehicleKind.Boat;
        public float Speed => Body ? Body.linearVelocity.magnitude * 3.6f : 0;
        float trafficTurnTimer;
        Vector3 lastPosition;
        Vector3 trafficCenter;
        float trafficLimit=850;
        public void Initialize()
        {
            Durability=MaxDurability;
            if(Vector3.Distance(transform.position,StateBuilder.SanFierro)<1000){trafficCenter=StateBuilder.SanFierro;trafficLimit=500;}
            else if(Vector3.Distance(transform.position,StateBuilder.LasVenturas)<1000){trafficCenter=StateBuilder.LasVenturas;trafficLimit=500;}
            Body = gameObject.AddComponent<Rigidbody>();
            Body.mass = Kind == VehicleKind.Motorcycle ? 240 : Kind == VehicleKind.Tank ? 8000 : Kind == VehicleKind.Truck || Kind == VehicleKind.Bus ? 5000 : 1300; Body.linearDamping = .5f; Body.angularDamping = 4;
            Body.centerOfMass = new Vector3(0, .1f, 0);
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            if (OnWater) { Body.useGravity = false; Body.constraints |= RigidbodyConstraints.FreezePositionY; }
            if(!Flying && !OnWater)InitializeWheels();
            gameObject.AddComponent<VehicleAudio>().Initialize(this);
            gameObject.AddComponent<VehicleImpact>();
            gameObject.AddComponent<VehicleRecovery>();gameObject.AddComponent<VehicleWaterState>();
            gameObject.AddComponent<VehicleDestruction>();gameObject.AddComponent<VehicleBodyDamage>();
            if(Kind==VehicleKind.Bicycle)gameObject.AddComponent<BicycleCrank>();
            if(Kind==VehicleKind.Tank)gameObject.AddComponent<TankWeapon>().Initialize(this);
        }
        void Update()
        {
            if (!GameRoot.Instance || !GameRoot.Instance.Playing || GameRoot.Instance.Panel != "") return;
            if (Rotor) Rotor.Rotate(0, (Driver ? 1600 : 70) * Time.deltaTime, 0, Space.Self);
            if (Driver && Input.GetKeyDown(KeyCode.O)) beaconsEnabled = !beaconsEnabled;
        }
        void FixedUpdate()
        {
            var game = GameRoot.Instance;
            if (!game || !game.Playing || game.Panel != "") return;
            if(GetComponent<VehicleWaterState>().Flooded){if(Wheels!=null)foreach(var wheel in Wheels){wheel.motorTorque=0;wheel.brakeTorque=0;}Body.useGravity=true;return;}
            if(Durability<=0){if(Wheels!=null)foreach(var wheel in Wheels){wheel.motorTorque=0;wheel.brakeTorque=10000;}return;}
            float throttle = 0, steer = 0;
            if (Driver)
            {
                throttle = Input.GetAxisRaw("Vertical"); steer = Input.GetAxisRaw("Horizontal");
            }
            else if(AIControls.HasValue){steer=AIControls.Value.x;throttle=AIControls.Value.y;}
            else if (Traffic)
            {
                throttle = Speed < 34 ? .55f : 0;
                if (Physics.Raycast(transform.position + Vector3.up * .7f + transform.forward * 2.6f, transform.forward, 9)) throttle = -.2f;
                trafficTurnTimer += Time.fixedDeltaTime;
                if (trafficTurnTimer > 8)
                {
                    if (Vector3.Distance(lastPosition, transform.position) < 3) transform.Rotate(0, 180, 0);
                    lastPosition = transform.position; trafficTurnTimer = 0;
                }
                if (Mathf.Abs(transform.position.x-trafficCenter.x) > trafficLimit || Mathf.Abs(transform.position.z-trafficCenter.z) > trafficLimit)
                    Body.MoveRotation(Quaternion.LookRotation(trafficCenter-transform.position));
            }
            if (Flying)
            {
                Body.useGravity = !Driver;
                if (Driver)
                {
                    float lift = Input.GetKey(KeyCode.Space) ? 1 : Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C) ? -1 : 0;
                    Vector3 velocity = Body.linearVelocity;
                    velocity.y = Mathf.Lerp(velocity.y, lift * 12, Time.fixedDeltaTime * 3);
                    Body.linearVelocity = velocity;
                    if (transform.position.y > 220 && velocity.y > 0) { velocity.y = 0; Body.linearVelocity = velocity; }
                    Body.AddForce(transform.forward * throttle * 13, ForceMode.Acceleration);
                    Body.MoveRotation(Body.rotation * Quaternion.Euler(0, steer * 65 * Time.fixedDeltaTime, 0));
                }
                return;
            }
            if(Wheels!=null){StepWheels(throttle,steer,Driver && Input.GetKey(KeyCode.Space));return;}
            if (OnWater)
            {
                Vector3 position = Body.position;
                position.x = Mathf.Clamp(position.x, -1280, -962);
                position.z = Mathf.Clamp(position.z, -1080, 1080);
                Body.position = position;
            }
            float acceleration = Kind == VehicleKind.Sport ? 22 : Kind == VehicleKind.Motorcycle ? 19 : Kind == VehicleKind.Tank ? 7 : Kind == VehicleKind.Truck || Kind == VehicleKind.Bus ? 8 : 14;
            float maxSpeed = Kind == VehicleKind.Sport ? 65 : Kind == VehicleKind.Motorcycle ? 53 : Kind == VehicleKind.Tank ? 26 : Kind == VehicleKind.Truck || Kind == VehicleKind.Bus ? 26 : OnWater ? 30 : 40;
            if(HealthFraction<.15f){maxSpeed*=.5f;acceleration*=.6f;}
            if (Body.linearVelocity.magnitude < maxSpeed || throttle * Vector3.Dot(Body.linearVelocity, transform.forward) < 0)
                Body.AddForce(transform.forward * throttle * acceleration, ForceMode.Acceleration);
            float forwardSpeed = Vector3.Dot(Body.linearVelocity, transform.forward);
            float handling = Kind == VehicleKind.Motorcycle ? 5 : Kind == VehicleKind.Bus || Kind == VehicleKind.Truck || Kind == VehicleKind.Tank ? 2.1f : 3.5f;
            Body.MoveRotation(Body.rotation * Quaternion.Euler(0, steer * Mathf.Clamp(forwardSpeed, -15, 15) * handling * Time.fixedDeltaTime, 0));
            Vector3 local = transform.InverseTransformDirection(Body.linearVelocity);
            local.x *= .86f; Body.linearVelocity = transform.TransformDirection(local);
            if (!Driver && !Traffic) Body.linearVelocity *= .985f;
        }
        void OnCollisionEnter(Collision collision)
        {
            if(collision.relativeVelocity.magnitude>8)Durability=Mathf.Max(0,Durability-(collision.relativeVelocity.magnitude-5)*.35f);
            var other=collision.collider.GetComponentInParent<VehicleMotor>();if(other)GetComponent<VehicleImpact>().Ram(other,collision.relativeVelocity.magnitude);
        }
    }
}
