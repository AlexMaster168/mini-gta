using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        GameObject conversationNpc,companion;float nextNpcGift;
        bool TryNpcConversation()
        {
            GameObject best=null;float range=2.8f;foreach(var person in FindObjectsByType<Pedestrian>(FindObjectsSortMode.None))
            {if(!NpcEffects.Alive(person.gameObject))continue;float d=Vector3.Distance(Player.Position,person.transform.position);if(d<range){range=d;best=person.gameObject;}}
            if(!best)return false;conversationNpc=best;Toggle("conversation");return true;
        }
        void DrawConversation(float left,float h)
        {
            if(!conversationNpc){Panel="";Time.timeScale=1;SetCursor(true);return;}
            var identity=conversationNpc.GetComponentInChildren<CharacterAppearance>();bool female=identity&&identity.Female;
            Label(left,90,1040,50,female?"ГОРОЖАНКА · РАЗГОВОР":"ГОРОЖАНИН · РАЗГОВОР",heading);
            Label(left,160,1040,100,"«Привет! В центре есть магазины, коллегия адвокатов и мэрия. На карте M видно, где ты и куда идти по заданию. Могу рассказать о районе или прогуляться с тобой». Здоровье: "+Mathf.RoundToInt(conversationNpc.GetComponent<Pedestrian>().Health),body);
            if(Button(new Rect(left,290,1040,60),"СПРОСИТЬ ДОРОГУ К БЛИЖАЙШЕМУ 24/7")){Vector3 nearest=ConvenienceStores.Locations[0];foreach(var place in ConvenienceStores.Locations)if(Vector3.Distance(Player.Position,place)<Vector3.Distance(Player.Position,nearest))nearest=place;ActivateMission(new Mission("Совет горожанина","Доберись до ближайшего магазина по карте.",100,false,new[]{nearest+Vector3.back*14,WorldBuilder.Places[0].Position},"Найди магазин 24/7","Зайди в центр занятости"));}
            if(Button(new Rect(left,370,1040,60),"ПОМОЧЬ · ДОСТАВИТЬ ЗАБЫТЫЕ ДОКУМЕНТЫ"))ActivateMission(new Mission("Просьба прохожего","Доставь документы в мэрию и вернись к месту встречи.",450,false,new[]{WorldBuilder.Places[10].Position,conversationNpc.transform.position},"Передай документы в мэрию","Вернись к месту встречи"));
            if(Button(new Rect(left,450,1040,60),companion==conversationNpc?"ЗАВЕРШИТЬ ПРОГУЛКУ":"ПРИГЛАСИТЬ НА ПРОГУЛКУ")){if(companion){var previous=companion.GetComponent<Pedestrian>();if(previous)previous.enabled=true;}companion=companion==conversationNpc?null:conversationNpc;if(companion)companion.GetComponent<Pedestrian>().enabled=false;Panel="";Time.timeScale=1;SetCursor(true);}
            if(Button(new Rect(left,530,1040,60),"ОКАЗАТЬ ПЕРВУЮ ПОМОЩЬ")){var person=conversationNpc.GetComponent<Pedestrian>();person.Health=100;Notify("Первая помощь оказана.");}
        }
        void UpdateCompanion(){if(!companion||!NpcEffects.Alive(companion)){companion=null;return;}if(Player.Vehicle)return;var delta=Player.Position+Player.transform.right*1.7f-companion.transform.position;delta.y=0;var motor=companion.GetComponent<NpcGroundMotor>();if(motor)motor.Move(Vector3.ClampMagnitude(delta,1)*2.6f);if(delta.sqrMagnitude>.2f)companion.transform.rotation=Quaternion.LookRotation(delta);}
    }
}
