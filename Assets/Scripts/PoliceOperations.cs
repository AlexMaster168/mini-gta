using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        readonly List<GameObject> operationObjects=new List<GameObject>();bool operationEvidence;float operationHold;
        static readonly string[] OperationNames={"Ограбление 24/7","Сбытчики у склада","Освобождение заложника","Угроза взрыва","Охрана мирного митинга","Вооружённое нападение","Защита государственного банка"};
        void DrawOperationsPanel(float left,float h)
        {
            Label(left,85,1040,45,"ПОЛИЦИЯ / ФБР · ОПЕРАТИВНЫЕ ВЫЕЗДЫ",heading);
            Label(left,140,1040,55,"L — диспетчер · N — требование остановиться · 8 — тайзер · F — наручники. Мирных участников митинга нужно защищать.",body);
            for(int i=0;i<7;i++){int id=i;if(RoleIndex==1&&(i==2||i==3))continue;if(Button(new Rect(left,200+i*61,1040,55),OperationNames[i]+" →")){StartOperation(id);Panel="";Time.timeScale=1;SetCursor(true);}}
        }
        void StartOperation(int kind)
        {
            if(RoleIndex!=1&&RoleIndex!=2)return;if(RoleIndex==1&&(kind==2||kind==3)){Notify("Штурм и обезвреживание выполняет ФБР. Tab — сменить роль.");return;}if(Call!=null)EndCall(false);ClearOperation();
            Vector3 point=DutyPoint();if(kind==6)point=StateBank.Entrance.Door;
            if(kind==0 && ConvenienceStores.Locations.Length>0)point=ConvenienceStores.Locations[0]+Vector3.back*15;
            Call=new DutyCall{Code=kind==0?"10-32":kind==1?"10-60":kind==2?"10-91":kind==3?"10-82":kind==4?"10-37":"10-90",Title=OperationNames[kind],Description=kind==4?"Охраняй площадь 30 секунд, помоги участникам":kind==3?"Устрани угрозу и удерживай E у подозрительного предмета":kind==2?"Задержи охрану, E — освободить заложника, затем сопроводить":kind==1?"Останови сбытчиков и изыми товар клавишей E":"Останови вооружённых нападающих",Point=point,Expires=Time.unscaledTime+300,Reward=kind==4?900:1500,Wave=1};
            if(kind==0||kind==1||kind==2||kind==3||kind==5||kind==6)
            {
                for(int i=0;i<(kind==5||kind==6?5:3);i++){var enemy=HostileAgent.Spawn(this,point+new Vector3(i*5-6,0,5),kind==1?"Наркодилер / охрана":kind==0?"Грабитель 24/7":"Вооружённый сообщник",false);enemy.Suspect=kind==0&&i==0;Call.Threats.Add(enemy);}
            }
            if(kind==1||kind==3){var item=WorldBuilder.Box(kind==1?"Изъять запрещённый товар · E":"Подозрительный предмет · E",point+new Vector3(0,.5f,0),new Vector3(1,.8f,.8f),kind==1?new Color(.4f,.3f,.18f):new Color(.3f,.32f,.34f));operationObjects.Add(item);}
            if(kind==2)SpawnHostage(point+Vector3.back*4,false);
            if(kind==4)for(int i=0;i<9;i++){var npc=new GameObject("Мирный участник митинга");npc.transform.position=Supported(point+new Vector3(i%3*3-3,0,i/3*3));CharacterModels.Create(npc.transform,0,Color.white,i%2==0);npc.AddComponent<NeutralActor>();var capsule=npc.AddComponent<CapsuleCollider>();capsule.center=Vector3.up*.9f;capsule.height=1.8f;capsule.radius=.3f;operationObjects.Add(npc);}
            var mark=WorldBuilder.Box("Место вызова",point+Vector3.up*4,new Vector3(1,3,1),new Color(.3f,.6f,1),null,false);Call.Marker=mark;
            Notify("Рация: "+Call.Code+" · "+Call.Title+". Метка на карте. L — выбрать другой выезд.");
        }
        void UpdateOperation()
        {
            if(Call==null)return;
            bool near=Vector3.Distance(Player.Position,Call.Point)<9 && !Player.Vehicle;
            if(Call.Code=="10-37")
            {
                foreach(var npc in operationObjects)if(npc){var actor=npc.GetComponent<NeutralActor>();if(actor&&!NpcEffects.Alive(npc)){Notify("Пострадал мирный участник. Задание провалено.");EndCall(false);return;}}
                operationHold=near?operationHold+Time.deltaTime:operationHold;operationEvidence=operationHold>=30;
            }
            if(Call.Code=="10-82"||Call.Code=="10-60")
            {bool secure=true;foreach(var enemy in Call.Threats)if(enemy&&!enemy.Neutralized&&!enemy.Stunned)secure=false;
                if(near&&secure&&Input.GetKey(KeyCode.E))operationHold+=Time.deltaTime;else operationHold=0;
                if(operationHold>=3){operationEvidence=true;foreach(var item in operationObjects)if(item)item.SetActive(false);Notify(Call.Code=="10-60"?"Товар изъят и передан экспертам.":"Опасный предмет обезврежен специалистами.");}}
        }
        void ClearOperation(){foreach(var item in operationObjects)if(item)Destroy(item);operationObjects.Clear();operationEvidence=false;operationHold=0;if(RoleIndex!=16)ClearHostage();}
    }
}
