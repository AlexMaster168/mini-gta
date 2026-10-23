using UnityEngine;
namespace SunsetState
{
    public enum PersonalBoard { None, Skateboard, Snowboard }
    public sealed partial class GameRoot
    {
        public void OpenAccessories(){Toggle("accessories");}
        public PersonalBoard SelectedBoard=PersonalBoard.Skateboard;
        void DrawAccessories(float left,float h)
        {
            Label(left,85,1000,50,"ЛИЧНЫЕ АКСЕССУАРЫ",heading);
            if(Button(new Rect(left+700,85,370,45),"АКСЕССУАРЫ / СКИНЫ →"))Toggle("wardrobe");
            Label(left,150,1000,65,"Выбери доску. U — вызвать / убрать · WASD — разгон и поворот · Пробел — тормоз.\nДоски личные: они не входят в служебный автопарк.",body);
            for(int i=1;i<=2;i++)
            {
                var board=(PersonalBoard)i;string name=i==1?"СКЕЙТ":"СНОУБОРД";
                if(Button(new Rect(left+(i-1)*370,245,350,70),name+(SelectedBoard==board?" · ВЫБРАН":" · ВЫБРАТЬ"))){Player.StopBoard();SelectedBoard=board;Save();}
            }
            if(Button(new Rect(left,350,350,50),Player.BoardRiding?"УБРАТЬ ДОСКУ · U":"ВЫЗВАТЬ ДОСКУ · U")){Panel="";Time.timeScale=1;SetCursor(true);Player.ToggleBoard();}
            if(Button(new Rect(left+370,350,350,50),"СНЯТЬ АКСЕССУАР")){Player.StopBoard();SelectedBoard=PersonalBoard.None;Save();}
        }
    }
    public sealed partial class PlayerMotor
    {
        GameObject boardRoot;Vector3 boardVelocity;float nextBoardImpact;
        public bool BoardRiding=>boardRoot;
        public void ToggleBoard()
        {
            var game=GameRoot.Instance;
            if(BoardRiding){StopBoard();game.Notify("Доска убрана · U — вызвать снова");return;}
            if(Vehicle){game.Notify("Сначала выйди из транспорта: E.");return;}
            if(game.InInterior || game.RoleIndex==13&&!game.Escaped){game.Notify("Доска доступна на улице после освобождения.");return;}
            if(game.SelectedBoard==PersonalBoard.None){game.OpenAccessories();return;}
            bool snow=game.SelectedBoard==PersonalBoard.Snowboard;
            boardRoot=new GameObject(snow?"Personal snowboard":"Personal skateboard");boardRoot.transform.SetParent(transform,false);
            boardVelocity=Vector3.zero;
            var deck=WorldBuilder.Box("Deck",Vector3.zero,new Vector3(snow?.3f:.26f,.07f,snow?1.6f:.85f),snow?new Color(.13f,.65f,.85f):new Color(.7f,.25f,.12f),null,false);
            deck.transform.SetParent(boardRoot.transform,false);deck.transform.localPosition=Vector3.up*.09f;
            foreach(int end in new[]{-1,1})
            {
                var tip=WorldBuilder.Box("Rounded tip",Vector3.zero,new Vector3(snow?.27f:.24f,.06f,.18f),new Color(.15f,.2f,.25f),null,false);tip.transform.SetParent(boardRoot.transform,false);tip.transform.localPosition=new Vector3(0,.13f,end*(snow?.78f:.4f));tip.transform.localRotation=Quaternion.Euler(end*-15,0,0);
                if(snow)continue;
                foreach(int side in new[]{-1,1})
                {
                    var wheel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);wheel.name="Skate wheel";wheel.transform.SetParent(boardRoot.transform,false);wheel.transform.localPosition=new Vector3(side*.14f,.055f,end*.28f);wheel.transform.localRotation=Quaternion.Euler(0,0,90);wheel.transform.localScale=new Vector3(.09f,.03f,.09f);Destroy(wheel.GetComponent<Collider>());wheel.GetComponent<Renderer>().sharedMaterial=WorldBuilder.Mat(Color.white);
                }
            }
            CharacterModels.FreezePose(Visual);Visual.localRotation=Quaternion.Euler(0,65,0);Visual.localPosition=Vector3.up*.15f;
            game.Notify((snow?"Сноуборд":"Скейт")+" · WASD — движение · Пробел — тормоз · U — убрать");
        }
        public void StopBoard()
        {
            if(!boardRoot)return;Destroy(boardRoot);boardRoot=null;boardVelocity=Vector3.zero;
            SetColor(GameRoot.Instance.Roles[GameRoot.Instance.RoleIndex].Color);
        }
        Vector3 BoardMotion(Vector3 input)
        {
            bool snow=GameRoot.Instance.SelectedBoard==PersonalBoard.Snowboard;
            float max=snow?13:10;
            if(Input.GetKey(KeyCode.Space))boardVelocity=Vector3.MoveTowards(boardVelocity,Vector3.zero,Time.deltaTime*16);
            else if(input.sqrMagnitude>.01f)boardVelocity=Vector3.MoveTowards(boardVelocity,input*max,Time.deltaTime*(snow?4:5));
            else boardVelocity=Vector3.MoveTowards(boardVelocity,Vector3.zero,Time.deltaTime*(snow?.35f:1.2f));
            if((Controller.collisionFlags&CollisionFlags.Sides)!=0)boardVelocity*=.85f;
            if(boardVelocity.magnitude>2.5f && Time.time>nextBoardImpact)
            {
                bool hitPerson=false;Vector3 center=transform.position+Vector3.up*.8f+boardVelocity.normalized*.65f;
                foreach(var hit in Physics.OverlapBox(center,new Vector3(.55f,.8f,.8f),transform.rotation,~(1<<2),QueryTriggerInteraction.Ignore))
                {var person=NpcEffects.Actor(hit);if(person && VehicleImpact.ApplyPersonHit(person,boardVelocity.magnitude,90,true))hitPerson=true;}
                if(hitPerson)nextBoardImpact=Time.time+1;
            }
            return boardVelocity;
        }
    }
}
