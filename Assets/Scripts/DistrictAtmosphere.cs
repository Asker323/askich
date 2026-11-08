using System.Collections.Generic;
using UnityEngine;

namespace Askich {
 public sealed class DistrictAtmosphere : MonoBehaviour {
  Material grade;
  Light flashlight,sun;
  float lastHour=-100;
  public float Daylight {get;private set;}=1;
  readonly List<Light> lamps=new List<Light>();
  bool lowQuality;
  public bool FlashlightOn=>flashlight&&flashlight.enabled;
  public bool LowQuality=>lowQuality;
  public bool ShaderReady=>grade&&grade.shader.isSupported;
  public void Initialize(Transform hero){
   QualitySettings.antiAliasing=4;
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
   RenderSettings.ambientSkyColor=new Color(.34f,.36f,.39f);
   RenderSettings.ambientEquatorColor=new Color(.24f,.25f,.27f);
   RenderSettings.ambientGroundColor=new Color(.12f,.13f,.14f);
   RenderSettings.fogColor=new Color(.18f,.24f,.28f);RenderSettings.fogStartDistance=37;RenderSettings.fogEndDistance=92;
   GetComponent<Camera>().backgroundColor=RenderSettings.fogColor;
   foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){sun=light;light.color=new Color(.88f,.91f,.97f);light.intensity=.72f;light.transform.rotation=Quaternion.Euler(24,-28,0);}
   var shader=Resources.Load<Shader>("Survival/Dusk");if(shader&&shader.isSupported)grade=new Material(shader);
   var torch=new GameObject("Survivor flashlight");torch.transform.SetParent(hero,false);torch.transform.localPosition=new Vector3(0,1.35f,.22f);torch.transform.localRotation=Quaternion.Euler(12,0,0);
   flashlight=torch.AddComponent<Light>();flashlight.type=LightType.Spot;flashlight.range=19;flashlight.spotAngle=62;flashlight.innerSpotAngle=30;flashlight.intensity=3.2f;flashlight.color=new Color(1,.91f,.72f);flashlight.shadows=LightShadows.Soft;flashlight.shadowBias=.035f;
   // Layer 30 is reserved for the survivor's visual meshes. They retain moonlight
   // shadows, but cannot obscure their own chest-mounted flashlight.
   foreach(var renderer in hero.GetComponentsInChildren<Renderer>(true))renderer.gameObject.layer=30;
   flashlight.cullingMask=~(1<<30);
   for(int i=0;i<8;i++){
    Vector3 p=new Vector3(i%2==0?-20:4,0,-43+(i/2)*27);
    Spawn("StreetLights",p,90);var lamp=Point("Street sodium light",p+new Vector3(-.7f,4.7f,0),new Color(1,.58f,.23f),1.7f,11);lamps.Add(lamp);
    Spawn("TrashBag_1",p+new Vector3(1,0,1),i*47);
    if(i%2==0){Spawn("Barrel",p+new Vector3(1,0,-1.4f),0);Spawn("Pallet_Broken",p+new Vector3(1.2f,0,-2.2f),i*31);}
   }
   for(int i=0;i<6;i++)Spawn("TrafficCone_1",new Vector3(-3+i*1.3f,0,30),i*13);
   SetLowQuality(Application.isMobilePlatform);SetTime(16.5f);
  }
  public void SetTime(float hour){
   if(Mathf.Abs(hour-lastHour)<.002f)return;lastHour=hour;
   Daylight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.5f,8,hour))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(17,20,hour)));
   float day=Daylight;RenderSettings.ambientSkyColor=Color.Lerp(new Color(.13f,.15f,.20f),new Color(.51f,.54f,.58f),day);
   RenderSettings.ambientEquatorColor=Color.Lerp(new Color(.09f,.105f,.13f),new Color(.34f,.35f,.37f),day);RenderSettings.ambientGroundColor=Color.Lerp(new Color(.055f,.06f,.07f),new Color(.17f,.18f,.18f),day);
   RenderSettings.fogColor=Color.Lerp(new Color(.075f,.095f,.12f),new Color(.29f,.34f,.36f),day);GetComponent<Camera>().backgroundColor=RenderSettings.fogColor;
   if(sun){sun.intensity=Mathf.Lerp(.16f,1.02f,day);sun.color=Color.Lerp(new Color(.55f,.62f,.78f),new Color(1,.94f,.83f),day);sun.transform.rotation=Quaternion.Euler(Mathf.Lerp(18,58,day),-55+hour/24*110,0);}
   foreach(var lamp in lamps)lamp.enabled=day<.65f;
  }
  public static GameObject Spawn(string name,Vector3 position,float yaw=0){var prefab=Resources.Load<GameObject>("Survival/Art/"+name);return prefab?Instantiate(prefab,position,Quaternion.Euler(0,yaw,0)):null;}
  public static void Tree(Vector3 position,int seed)=>DistrictTreeArt.Build(position,seed);
  public static Light Point(string name,Vector3 position,Color color,float intensity,float range){var o=new GameObject(name);o.transform.position=position;var l=o.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;return l;}
  public void Aim(Vector2 direction){if(flashlight&&direction.sqrMagnitude>.01f)flashlight.transform.rotation=Quaternion.LookRotation(new Vector3(direction.x,-.20f,direction.y));}
  public void ToggleFlashlight(){flashlight.enabled=!flashlight.enabled;}
  public void SetLowQuality(bool value){lowQuality=value;QualitySettings.antiAliasing=value?0:4;QualitySettings.shadowDistance=value?28:55;QualitySettings.shadows=value?ShadowQuality.HardOnly:ShadowQuality.All;flashlight.shadows=value?LightShadows.None:LightShadows.Soft;}
  void Update(){for(int i=0;i<lamps.Count;i++)lamps[i].intensity=i==2?1.4f+Mathf.PerlinNoise(Time.time*3,0)*.4f:1.7f;}
  void OnRenderImage(RenderTexture source,RenderTexture destination){if(grade&&!lowQuality)Graphics.Blit(source,destination,grade);else Graphics.Blit(source,destination);}
  void OnDestroy(){if(grade)Destroy(grade);}
 }
 public sealed class DistrictTreeSway:MonoBehaviour {
  public float Phase;
  void Update(){transform.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*.7f+Phase)*.55f,0,Mathf.Sin(Time.time*.5f+Phase)*.7f);}
 }
}
