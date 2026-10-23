using System.Collections.Generic;
using UnityEngine;
namespace SunsetState
{
    public static class RoadNavigation
    {
        static readonly List<Vector3> nodes=new List<Vector3>();static readonly List<List<int>> edges=new List<List<int>>();
        static int Node(Vector3 p){p.y=0;for(int i=0;i<nodes.Count;i++)if((nodes[i]-p).sqrMagnitude<1)return i;nodes.Add(p);edges.Add(new List<int>());return nodes.Count-1;}
        static void Link(int a,int b){if(a==b)return;if(!edges[a].Contains(b))edges[a].Add(b);if(!edges[b].Contains(a))edges[b].Add(a);}
        static void Grid(Vector3 center,int radius,float spacing)
        {for(int x=-radius;x<=radius;x++)for(int z=-radius;z<=radius;z++){int a=Node(center+new Vector3(x*spacing,0,z*spacing));if(x<radius)Link(a,Node(center+new Vector3((x+1)*spacing,0,z*spacing)));if(z<radius)Link(a,Node(center+new Vector3(x*spacing,0,(z+1)*spacing)));}}
        static int Nearest(Vector3 p,int except=-1){p.y=0;int best=-1;float distance=float.MaxValue;for(int i=0;i<nodes.Count;i++){if(i==except)continue;float d=(nodes[i]-p).sqrMagnitude;if(d<distance){distance=d;best=i;}}return best;}
        static void Build()
        {
            if(nodes.Count>0)return;Grid(ViceCityIsland.Center,4,160);Grid(Vector3.zero,4,200);Grid(StateBuilder.SanFierro,3,160);Grid(StateBuilder.LasVenturas,3,160);foreach(var center in StateBuilder.ExpansionCenters)Grid(center,1,160);
            var routes=new List<Vector3[]>(StateBuilder.Routes);routes.AddRange(StateBuilder.ExpansionRoutes);
            foreach(var route in routes)
            {int first=Node(route[0]),last=Node(route[route.Length-1]);Link(first,Nearest(route[0],first));Link(last,Nearest(route[route.Length-1],last));for(int i=1;i<route.Length;i++)Link(Node(route[i-1]),Node(route[i]));}
        }
        public static List<Vector3> Route(Vector3 start,Vector3 goal)
        {
            Build();Vector3 originalStart=start,originalGoal=goal;bool leaving=FactionDistricts.InArmy(start)&&!FactionDistricts.InArmy(goal),entering=!FactionDistricts.InArmy(start)&&FactionDistricts.InArmy(goal);if(leaving)start=FactionDistricts.ArmyRoad;if(entering)goal=FactionDistricts.ArmyRoad;int from=Nearest(start),to=Nearest(goal),count=nodes.Count;var distance=new float[count];var previous=new int[count];var visited=new bool[count];
            for(int i=0;i<count;i++){distance[i]=float.MaxValue;previous[i]=-1;}distance[from]=0;
            for(int pass=0;pass<count;pass++)
            {int best=-1;float value=float.MaxValue;for(int i=0;i<count;i++)if(!visited[i] && distance[i]<value){best=i;value=distance[i];}if(best<0||best==to)break;visited[best]=true;foreach(int next in edges[best]){float candidate=value+Vector3.Distance(nodes[best],nodes[next]);if(candidate<distance[next]){distance[next]=candidate;previous[next]=best;}}}
            var path=new List<Vector3>();for(int i=to;i>=0;i=previous[i]){path.Add(nodes[i]);if(i==from)break;}path.Reverse();path.Insert(0,start);path.Add(goal);if(leaving){path.InsertRange(0,new[]{originalStart,FactionDistricts.ArmyGate+Vector3.forward*14,FactionDistricts.ArmyGate+Vector3.back*14});}if(entering)path.AddRange(new[]{FactionDistricts.ArmyGate+Vector3.back*14,FactionDistricts.ArmyGate+Vector3.forward*14,originalGoal});return path;
        }
    }
    public sealed partial class GameRoot
    {
        bool mapFocused;Vector3 mapCenter;float mapSpan=400,nextNavigation;List<Vector3> navigation=new List<Vector3>();Vector3 navigationGoal;WorldDestructible[] mapStructures;
        Vector3? MapGoal=>gpsDestination.HasValue?gpsDestination:ActiveMission!=null?ActiveMission.Points[Step]:Call!=null?Call.Point:(Vector3?)null;
        void MapLine(Rect clip,Vector2 a,Vector2 b,Color color,float thickness)
        {
            Vector2 delta=b-a;float low=0,high=1;
            bool Cut(float p,float q){if(Mathf.Abs(p)<.00001f)return q>=0;float r=q/p;if(p<0){if(r>high)return false;low=Mathf.Max(low,r);}else{if(r<low)return false;high=Mathf.Min(high,r);}return true;}
            if(!Cut(-delta.x,a.x-clip.xMin)||!Cut(delta.x,clip.xMax-a.x)||!Cut(-delta.y,a.y-clip.yMin)||!Cut(delta.y,clip.yMax-a.y))return;
            Vector2 end=a+delta*high;a+=delta*low;delta=end-a;if(delta.sqrMagnitude<.01f)return;
            var matrix=GUI.matrix;Color old=GUI.color;GUI.color=color;GUI.matrix=matrix*Matrix4x4.TRS(a,Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg),Vector3.one);GUI.DrawTexture(new Rect(0,-thickness*.5f,delta.magnitude,thickness),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=old;
        }
        void DrawMapDetails(Rect rect,bool full)
        {
            if(mapStructures==null)mapStructures=FindObjectsByType<WorldDestructible>(FindObjectsSortMode.None);
            if(!full||mapFocused)
            {
                foreach(var feature in FactionDistricts.Features)
                {Vector2 a=MapPoint(rect,feature.Position+new Vector3(-feature.Size.x*.5f,0,feature.Size.y*.5f),full),b=MapPoint(rect,feature.Position+new Vector3(feature.Size.x*.5f,0,-feature.Size.y*.5f),full);if(!rect.Contains((a+b)*.5f))continue;Rect box=Rect.MinMaxRect(Mathf.Max(a.x,rect.x),Mathf.Max(a.y,rect.y),Mathf.Min(b.x,rect.xMax),Mathf.Min(b.y,rect.yMax));if(box.width>0 && box.height>0)RectBox(box,Color.Lerp(feature.Color,Color.white,.22f));}
                foreach(var entry in InteriorSystem.Entrances)if(entry.Shell&&entry.Shell.activeInHierarchy)
                {var shape=entry.Shell.GetComponent<BoxCollider>();if(!shape||!shape.enabled)continue;var bounds=shape.bounds;Vector2 a=MapPoint(rect,new Vector3(bounds.min.x,0,bounds.max.z),full),b=MapPoint(rect,new Vector3(bounds.max.x,0,bounds.min.z),full);Rect box=Rect.MinMaxRect(Mathf.Max(a.x,rect.x),Mathf.Max(a.y,rect.y),Mathf.Min(b.x,rect.xMax),Mathf.Min(b.y,rect.yMax));if(box.width>1 && box.height>1){RectBox(box,new Color(.11f,.17f,.20f));if(box.width>4&&box.height>4)RectBox(new Rect(box.x+1,box.y+1,box.width-2,box.height-2),entry.Role>=0?Color.Lerp(Roles[entry.Role].Color,new Color(.23f,.29f,.3f),.5f):new Color(.33f,.39f,.4f));}}

            }
            foreach(var shop in ConvenienceStores.Locations){Vector2 p=MapPoint(rect,shop,full);if(rect.Contains(p)){RectBox(new Rect(p.x-5,p.y-5,10,10),new Color(.1f,.95f,.48f));if(full && mapFocused)MapCaption(rect,p+new Vector2(8,-10),"24/7");}}
            var goal=MapGoal;if(goal.HasValue)
            {
                if(Time.unscaledTime>nextNavigation || (navigationGoal-goal.Value).sqrMagnitude>1){navigationGoal=goal.Value;navigation=RoadNavigation.Route(SavePosition,goal.Value);nextNavigation=Time.unscaledTime+1;}
                if(navigation.Count>1)navigation[0]=SavePosition;
                for(int i=1;i<navigation.Count;i++)MapLine(rect,MapPoint(rect,navigation[i-1],full),MapPoint(rect,navigation[i],full),new Color(1,.79f,.12f),full?3:2);
            }
        }
        void DrawPlayerMapArrow(Rect rect,bool full)
        {
            Vector2 p=MapPoint(rect,Player.Position,full);p.x=Mathf.Clamp(p.x,rect.x+12,rect.xMax-12);p.y=Mathf.Clamp(p.y,rect.y+12,rect.yMax-12);
            Transform facing=Player.Vehicle?Player.Vehicle.transform:Player.transform;float yaw=facing.eulerAngles.y*Mathf.Deg2Rad;Vector2 direction=new Vector2(Mathf.Sin(yaw),-Mathf.Cos(yaw))*11,side=new Vector2(-direction.y,direction.x)*.55f;
            MapLine(rect,p+direction,p-direction*.65f+side,Color.black,5);MapLine(rect,p+direction,p-direction*.65f-side,Color.black,5);MapLine(rect,p+direction,p-direction*.65f+side,Color.white,3);MapLine(rect,p+direction,p-direction*.65f-side,Color.white,3);
            if(full)MapCaption(rect,p+new Vector2(14,-10),"ТЫ");
        }
        Vector2 mapPlacesScroll;readonly List<Rect> mapCaptions=new List<Rect>();GUIStyle mapCaptionStyle,mapRowStyle;
        bool MapCaption(Rect clip,Vector2 at,string text)
        {
            if(mapCaptionStyle==null)mapCaptionStyle=new GUIStyle(small){wordWrap=false,clipping=TextClipping.Clip,fontSize=12,padding=new RectOffset(6,6,3,3)};
            Vector2 measured=mapCaptionStyle.CalcSize(new GUIContent(text));float width=Mathf.Min(measured.x+4,clip.width-8),height=24;
            foreach(var offset in new[]{Vector2.zero,new Vector2(0,-30),new Vector2(0,30),new Vector2(-width,-30),new Vector2(-width,30),new Vector2(0,-60),new Vector2(0,60)})
            {Rect box=new Rect(Mathf.Clamp(at.x+offset.x,clip.x+4,clip.xMax-width-4),Mathf.Clamp(at.y+offset.y,clip.y+4,clip.yMax-height-4),width,height);if(mapCaptions.Exists(other=>other.Overlaps(new Rect(box.x-3,box.y-3,box.width+6,box.height+6))))continue;mapCaptions.Add(box);RectBox(box,new Color(.025f,.055f,.07f,.93f));GUI.Label(box,text,mapCaptionStyle);return true;}return false;
        }
        void DrawRegionLabels(Rect rect)
        {
            if(mapFocused)return;
            foreach(var city in new[]{Vector3.zero,StateBuilder.SanFierro,StateBuilder.LasVenturas,ViceCityIsland.Center})MapCaption(rect,MapPoint(rect,city)+new Vector2(-45,-35),StateBuilder.City(city));
            for(int i=0;i<StateBuilder.ExpansionCenters.Length;i++)MapCaption(rect,MapPoint(rect,StateBuilder.ExpansionCenters[i])+new Vector2(-45,-30),StateBuilder.ExpansionNames[i]);
        }
        void DrawNavigationMap(float left,float h)
        {
            Label(left,85,700,40,"КАРТА / НАВИГАЦИЯ",heading);float size=Mathf.Min(h-230,670);Rect rect=new Rect(left+20,145,size,size);float info=rect.xMax+25,width=left+1080-info;
            if(mapRowStyle==null)mapRowStyle=new GUIStyle(button){fontSize=14,wordWrap=false,clipping=TextClipping.Clip,padding=new RectOffset(14,10,8,8)};
            DrawMap(rect,true);
            if(Button(new Rect(info,145,(width-10)/2,42),"ВЕСЬ ШТАТ"))mapFocused=false;
            if(Button(new Rect(info+(width+10)/2,145,(width-10)/2,42),"КО МНЕ")){mapFocused=true;mapCenter=SavePosition;mapSpan=450;}
            Label(info,203,width,65,"Белая стрелка — ты · жёлтая — маршрут\nЗелёный значок — 24/7\nВыбери отделение для подробного плана",small);
            var goal=MapGoal;float distance=goal.HasValue?Vector3.Distance(SavePosition,goal.Value):0;
            if(goal.HasValue && Button(new Rect(info,275,width,42),"К ЦЕЛИ · "+Mathf.RoundToInt(distance)+" М")){mapFocused=true;mapCenter=goal.Value;mapSpan=500;}
            Rect list=new Rect(info,333,width,h-410);RectBox(list,new Color(.055f,.085f,.085f));
            mapPlacesScroll=GUI.BeginScrollView(list,mapPlacesScroll,new Rect(0,0,width-22,WorldBuilder.Places.Count*48));
            for(int i=0;i<WorldBuilder.Places.Count;i++)
            {var place=WorldBuilder.Places[i];Rect row=new Rect(3,i*48+3,width-29,42);GUI.color=Color.white;GUI.backgroundColor=new Color(.26f,.34f,.33f);if(GUI.Button(row,place.Name,mapRowStyle)){mapFocused=true;mapCenter=RoleHome(i)+Vector3.forward*20;mapSpan=i==3||i==13?330:180;}GUI.backgroundColor=Color.white;RectBox(new Rect(row.x+3,row.y+8,4,row.height-16),place.Color);}
            GUI.EndScrollView();
            if(mapFocused && Event.current.type==EventType.ScrollWheel && rect.Contains(Event.current.mousePosition)){mapSpan=Mathf.Clamp(mapSpan*Mathf.Pow(1.15f,Event.current.delta.y),100,3000);Event.current.Use();}
            Label(left+20,h-62,1040,38,"Колесо над картой — масштаб · список справа прокручивается · здания показаны цветными контурами",small);
        }
    }
}
