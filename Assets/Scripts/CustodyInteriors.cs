using System;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        void BuildCustodyAnnex(Action<string,Vector3,Vector3,Color> box,Action<Vector3,string> sign)
        {
            Color wall=new Color(.54f,.58f,.59f),steel=new Color(.2f,.25f,.29f);
            box("Custody annex floor",new Vector3(19,-.15f,0),new Vector3(14,.3f,20),wall);
            box("Custody annex ceiling",new Vector3(19,4.5f,0),new Vector3(14,.25f,20),wall);
            box("Annex outer wall",new Vector3(26,2.1f,0),new Vector3(.25f,4.4f,20),wall);
            foreach(int side in new[]{-1,1})box("Annex end wall",new Vector3(19,2.1f,side*10),new Vector3(14,4.4f,.25f),wall);
            box("Interview separation",new Vector3(22,2,0),new Vector3(8,4,.2f),wall);
            box("Interview desk",new Vector3(22,.75f,-5),new Vector3(2.8f,1.5f,1.5f),steel);
            foreach(int side in new[]{-1,1})box("Interview chair",new Vector3(22,.5f,-5+side*1.6f),new Vector3(.7f,1,.7f),steel);
            box("Observation glass",new Vector3(18,2,-5),new Vector3(.1f,1.1f,3),new Color(.18f,.32f,.37f));
            sign(new Vector3(22,3,-5),"ДОПРОСНАЯ · C / E");
            for(int cell=0;cell<2;cell++)
            {
                float z=2.5f+cell*4;box("Cell bed",new Vector3(24,.55f,z),new Vector3(2,.3f,2.6f),steel);
                box("Cell mattress",new Vector3(24,.76f,z),new Vector3(1.9f,.15f,2.5f),new Color(.4f,.48f,.48f));
                for(int i=0;i<8;i++)box("Holding cell bars",new Vector3(20,1.5f,z-1.5f+i*.42f),new Vector3(.07f,3,.07f),steel);
                box("Cell rear partition",new Vector3(23,1.6f,z+1.8f),new Vector3(6,3.2f,.15f),wall);
                sign(new Vector3(20,3.5f,z),"КАМЕРА "+(cell+1)+" · E");
            }
            for(int i=0;i<3;i++)
            {box("CCTV camera",new Vector3(13+i*5,3.7f,-9),new Vector3(.4f,.25f,.65f),steel);box("CCTV recording indicator",new Vector3(13+i*5,3.7f,-8.65f),new Vector3(.08f,.08f,.03f),Color.red);}
            sign(new Vector3(14,3,-1),"ИЗОЛЯТОР / ДОПРОС →");
        }
        void BuildCourtRoom(Action<string,Vector3,Vector3,Color> box,Action<Vector3,string> sign)
        {
            Color wood=new Color(.28f,.15f,.09f);
            box("Judge podium",new Vector3(0,.2f,5),new Vector3(6,.4f,3),wood);box("Judge bench",new Vector3(0,1,5.5f),new Vector3(5,1.6f,1),wood);
            foreach(int side in new[]{-1,1})box(side<0?"Defence table":"Prosecution table",new Vector3(side*4,.7f,1),new Vector3(2.8f,1.4f,1.4f),wood);
            for(int i=0;i<3;i++)box("Public bench",new Vector3(4,.5f,-3-i*1.5f),new Vector3(5,1,.65f),wood);
            sign(new Vector3(0,3.4f,5),"СУД ШТАТА · ПРАВО НА ЗАЩИТУ");
        }
    }
}
