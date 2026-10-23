using UnityEngine;
namespace SunsetState
{
    public static class UniformDetails
    {
        public static void Swat(Transform model,Transform head,Transform chest)
        {
            foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {var materials=renderer.materials;foreach(var material in materials)if(!material.name.Contains("Skin"))material.color=new Color(.055f,.075f,.095f);renderer.materials=materials;}
            foreach(var item in model.GetComponentsInChildren<Transform>())if(item.name=="Uniform Head"||item.name=="Uniform Spine")item.gameObject.SetActive(false);
            Color armor=new Color(.06f,.08f,.105f);
            Shape(head,"SWAT ballistic helmet",new Vector3(0,.28f,0),new Vector3(.76f,.38f,.69f),armor,false,true);
            Shape(head,"SWAT face cover",new Vector3(0,-.02f,.25f),new Vector3(.55f,.21f,.18f),armor);
            Shape(chest,"SWAT plate carrier",new Vector3(0,.06f,.08f),new Vector3(.5f,.46f,.32f),armor);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<2;i++)Shape(chest,"Magazine pouch",new Vector3(side*.11f,-.03f+i*.13f,.27f),new Vector3(.16f,.1f,.08f),armor);
            Mark(chest,"FBI · SWAT",new Vector3(0,.21f,.25f),Quaternion.Euler(0,180,0),.38f,Color.white);
            Mark(chest,"FBI · SWAT",new Vector3(0,.13f,-.1f),Quaternion.identity,.4f,Color.white);
        }
        public static readonly string[] Codes={"CIVIL","POLICE","FBI","ARMY","EMS","FIRE","TAXI","LOGISTICS","SERVICE","GROVE","CITY HALL","LICENSE","CORRECTIONS","INMATE","INDEPENDENT","LAWYER","MAFIA","GOVERNOR"};
        static Transform Anchor(Transform model,string boneName,float height)
        {
            Transform bone=model;foreach(var child in model.GetComponentsInChildren<Transform>())if(child.name==boneName){bone=child;break;}
            var anchor=new GameObject("Uniform "+boneName).transform;anchor.position=model.parent.position+Vector3.up*height;anchor.rotation=model.parent.rotation;anchor.SetParent(bone,true);return anchor;
        }
        static GameObject Shape(Transform parent,string name,Vector3 offset,Vector3 scale,Color color,bool round=false,bool sphere=false)
        {
            var obj=GameObject.CreatePrimitive(sphere?PrimitiveType.Sphere:round?PrimitiveType.Cylinder:PrimitiveType.Cube);obj.name=name;var collider=obj.GetComponent<Collider>();collider.enabled=false;if(Application.isPlaying)Object.Destroy(collider);else Object.DestroyImmediate(collider);
            obj.transform.position=parent.position+parent.rotation*offset;obj.transform.rotation=parent.rotation;obj.transform.localScale=scale;obj.transform.SetParent(parent,true);obj.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(color);obj.layer=parent.gameObject.layer;return obj;
        }
        public static void Mark(Transform parent,string text,Vector3 offset,Quaternion rotation,float width,Color color)
        {
            var label=new GameObject("Faction marking · "+text);label.transform.SetParent(parent,false);label.transform.localPosition=offset;label.transform.localRotation=rotation;
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");var mesh=label.AddComponent<TextMesh>();mesh.font=font;mesh.text=text;mesh.fontSize=64;mesh.characterSize=.1f;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.fontStyle=FontStyle.Bold;mesh.color=color;var renderer=label.GetComponent<Renderer>();renderer.sharedMaterial=WorldText.ForFont(font);
            WorldLabelFit.Attach(mesh,width,width*.32f);label.layer=parent.gameObject.layer;
        }
        public static void Apply(Transform model,int role)
        {
            if(role==0||role==9||role==13||role==14||role==16)return;
            Transform head=Anchor(model,"Head",1.43f),body=Anchor(model,"Spine",.9f);Color uniform=role==1?new Color(.08f,.18f,.34f):role==2?new Color(.08f,.1f,.14f):role==3?new Color(.24f,.31f,.18f):role==4?Color.white:role==5?new Color(.85f,.34f,.04f):role==6?new Color(1,.78f,.1f):role==11?new Color(.2f,.35f,.6f):new Color(.18f,.23f,.28f);
            bool cap=role==1||role==2||role==6||role==11||role==12;
            if(cap)
            {
                Shape(head,"Service cap",new Vector3(0,.3f,0),new Vector3(.73f,.055f,.63f),uniform,true);
                Shape(head,"Cap visor",new Vector3(0,.25f,.25f),new Vector3(.38f,.035f,.23f),Color.black);
                Mark(head,Codes[role],new Vector3(0,.31f,.325f),Quaternion.Euler(0,180,0),role==12?.31f:.27f,role==6?Color.black:new Color(1,.87f,.35f));
            }
            else if(role==4)Shape(head,"Medical cap",new Vector3(0,.3f,0),new Vector3(.48f,.06f,.42f),Color.white,true);
            else if(role==5)Shape(head,"Fire helmet",new Vector3(0,.3f,0),new Vector3(.54f,.1f,.49f),uniform,true);
            if(role==1||role==2||role==12||role==5)
            {
                Shape(body,"Identified uniform vest",new Vector3(0,.08f,.08f),new Vector3(.46f,.4f,.3f),uniform);
                Mark(body,Codes[role],new Vector3(0,.12f,.237f),Quaternion.Euler(0,180,0),.35f,role==2?new Color(1,.85f,.25f):Color.white);
                Mark(body,Codes[role],new Vector3(0,.12f,-.105f),Quaternion.identity,.35f,Color.white);
            }
            else
            {
                Shape(body,"Service badge",new Vector3(.12f,.1f,.2f),new Vector3(.12f,.08f,.02f),role==4?Color.red:new Color(.95f,.8f,.25f));
                Mark(body,Codes[role],new Vector3(0,.07f,.23f),Quaternion.Euler(0,180,0),.32f,role==4?Color.red:Color.white);
            }
        }
        public static void Vehicle(VehicleMotor car,int role)
        {
            if(car.transform.Find("Vehicle livery"))return;var root=new GameObject("Vehicle livery").transform;root.SetParent(car.transform,false);bool bike=car.Kind==VehicleKind.Motorcycle||car.Kind==VehicleKind.Bicycle;
            float y=car.Flying?1.5f:bike?.8f:car.Dimensions.y*.52f;float x=car.Flying?1.26f:Mathf.Max(.3f,car.Dimensions.x*.51f);float width=bike?.55f:Mathf.Clamp(car.Dimensions.z*.55f,1.2f,3.3f);
            Color color=role==6?Color.black:Color.white;
            Mark(root,Codes[role],new Vector3(-x,y,0),Quaternion.Euler(0,90,0),width,color);Mark(root,Codes[role],new Vector3(x,y,0),Quaternion.Euler(0,-90,0),width,color);
            if(role==6 && !bike && !car.Flying)
            {Shape(root,"Taxi roof sign",new Vector3(0,car.Dimensions.y+.16f,.1f),new Vector3(.85f,.27f,.25f),new Color(1,.8f,.1f));Mark(root,"TAXI",new Vector3(0,car.Dimensions.y+.17f,.23f),Quaternion.Euler(0,180,0),.68f,Color.black);}
        }
    }
}
