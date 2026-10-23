using System;
using System.Collections;
using UnityEngine;
namespace SunsetState
{
    public sealed partial class GameRoot
    {
        IEnumerator CheckReadability(Action<bool> result)
        {
            SelectRole(2);SkinChoices[2]=2;Player.SetColor(Roles[2].Color);yield return null;
            try
            {
                foreach(float yaw in new[]{0f,45f,89.9f,90f,135f,180f,270f})
                {
                    Player.transform.rotation=Quaternion.Euler(0,yaw,0);
                    foreach(var fit in Player.Visual.GetComponentsInChildren<WorldLabelFit>())
                    {fit.Fit();var surface=fit.GetComponent<Renderer>();Vector3 size=surface.localBounds.size,scale=surface.transform.lossyScale;Require(size.x*Mathf.Abs(scale.x)<=fit.Width+.015f && size.y*Mathf.Abs(scale.y)<=fit.Height+.015f,"Uniform label must fit patch independently of player rotation: "+yaw);}
                }
                var route=RoadNavigation.Route(new Vector3(630,1,805),Vector3.zero);Require(route.Exists(point=>Vector3.Distance(point,FactionDistricts.ArmyGate+Vector3.forward*14)<1)&&route.Exists(point=>Vector3.Distance(point,FactionDistricts.ArmyGate+Vector3.back*14)<1),"Route out of army compound must pass checkpoint gap");
                Require(map.width>=1024&&Camera.main.farClipPlane>=3000&&RenderSettings.fogDensity<.0006f,"Map resolution and world visibility must improve");
                Require(FindObjectsByType<WorldTextFacing>(FindObjectsSortMode.None).Length==0,"Surface signs must not rotate out of walls");
                SelectRole(3);Player.Teleport(FactionDistricts.ArmyGate+Vector3.forward*12);
            }
            catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            yield return new WaitForSeconds(1.6f);
            try
            {
                var from=FactionDistricts.ArmyGate+Vector3.up*2+Vector3.forward*10;
                foreach(var hit in Physics.BoxCastAll(from,new Vector3(1.6f,1.3f,.5f),Vector3.back,Quaternion.identity,24,~(1<<2),QueryTriggerInteraction.Ignore))
                {if(hit.collider.attachedRigidbody||hit.collider is CharacterController)continue;Require(false,"Checkpoint must physically pass a vehicle after opening: "+hit.collider.name);}
                Debug.Log("SUNSET_LABEL_ROTATIONS_FIXED_SIGNS_GATE_ROUTE_CLEARANCE_MAP_VISIBILITY_OK");
            }
            catch(Exception e){Debug.LogError("SUNSET_SMOKE_FAILED "+e);result(false);}
            SelectRole(0);
        }
    }
}
