using System.Collections;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        bool bootComplete;string bootStage="Подготовка";float bootPhase=.05f;
        IEnumerator InitializeGame()
        {
            WorldSimulation.DormantVehicles=0;WorldSimulation.DormantActors=0;
            bootStage="Создаём города и транспорт";yield return null;
            WorldBuilder.Build(this);
            var player=new GameObject("Player");Player=player.AddComponent<PlayerMotor>();Player.Initialize();player.transform.position=new Vector3(0,2,0);
            Camera.main.gameObject.AddComponent<FollowCamera>().Target=Player;
            bootPhase=.6f;bootStage="Готовим персонажа и игровые системы";yield return null;
            InitializeCombat();gameObject.AddComponent<StateRadio>();
            bootPhase=.75f;bootStage="Готовим карту и навигацию";yield return null;CreateMap();
            bootPhase=.95f;bootStage="Применяем настройки";yield return null;RuntimeSettings.Apply(true);SetCursor(false);
            bootComplete=true;Debug.Log("SUNSET_STARTUP_READY "+Application.version);
        }
        void DrawLoading()
        {
            GUI.color=new Color(.035f,.075f,.055f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
            var style=new GUIStyle(GUI.skin.label){fontSize=26,alignment=TextAnchor.MiddleCenter};style.normal.textColor=Color.white;
            GUI.Label(new Rect(20,Screen.height*.38f,Screen.width-40,70),"SUNSET STATE",style);style.fontSize=18;GUI.Label(new Rect(20,Screen.height*.5f,Screen.width-40,50),bootStage,style);
            float width=Mathf.Min(560,Screen.width-80),left=(Screen.width-width)*.5f;GUI.color=new Color(.15f,.22f,.18f);GUI.DrawTexture(new Rect(left,Screen.height*.62f,width,6),Texture2D.whiteTexture);GUI.color=new Color(.83f,.98f,.46f);GUI.DrawTexture(new Rect(left,Screen.height*.62f,width*bootPhase,6),Texture2D.whiteTexture);GUI.color=Color.white;
        }
    }
}
