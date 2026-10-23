using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        void DrawGoalGuide(float w,float h)
        {
            var goal=MapGoal;if(!goal.HasValue)return;Vector3 target=goal.Value;bool viaGate=FactionDistricts.InArmy(Player.Position)&&!FactionDistricts.InArmy(target);if(viaGate)target=FactionDistricts.ArmyGate+Vector3.back*12;
            Vector3 view=Camera.main.WorldToViewportPoint(target+Vector3.up*2);
            Vector2 center=new Vector2(w*.5f,h*.5f);bool inside=view.z>0&&view.x>.08f&&view.x<.92f&&view.y>.23f&&view.y<.74f;
            Vector2 tip;
            if(inside)tip=new Vector2(view.x*w,(1-view.y)*h);
            else{Vector3 local=Camera.main.transform.InverseTransformPoint(target);Vector2 direction=new Vector2(local.x,-local.y);if(local.z<0)direction.y=Mathf.Max(1,Mathf.Abs(direction.y));if(direction.sqrMagnitude<.01f)direction=Vector2.up;direction.Normalize();float radius=Mathf.Min((w*.5f-70)/Mathf.Max(.01f,Mathf.Abs(direction.x)),(h*.5f-170)/Mathf.Max(.01f,Mathf.Abs(direction.y)));tip=center+direction*radius;}
            Vector2 arrow=inside?Vector2.down:(tip-center).normalized,side=new Vector2(-arrow.y,arrow.x);
            Rect clip=new Rect(0,0,w,h);MapLine(clip,tip,tip-arrow*18+side*9,new Color(1,.82f,.15f),4);MapLine(clip,tip,tip-arrow*18-side*9,new Color(1,.82f,.15f),4);
            string action=ActiveMission!=null?ActiveMission.Steps[Step]:Call!=null?Call.Title:"GPS · выбранная точка";
            if(viaGate)action="Через КПП → "+action;
            string text=action+"\n"+Mathf.RoundToInt(Vector3.Distance(Player.Position,target))+" м";
            float x=Mathf.Clamp(tip.x-125,30,w-280),y=Mathf.Clamp(tip.y+15,150,h-185);
            RectBox(new Rect(x,y,250,65),new Color(.025f,.06f,.07f,.94f));Label(x+9,y+5,232,19,Mathf.RoundToInt(Vector3.Distance(Player.Position,target))+" м · К ЦЕЛИ",small);Label(x+9,y+25,232,35,action,small);
        }
    }
}
