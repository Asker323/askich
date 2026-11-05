using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  Texture2D[] inventoryIcons;
  Texture2D bodyPicture;
  Camera inventoryCamera;GameObject inventoryStage,bodyModel,portraitBackpack;
  int previewWeapon=-1;bool previewBag;
  float vitalTipTime;int vitalTip=-1;
  Texture2D ItemIcon(int kind){EnsureInventoryArt();return inventoryIcons[Mathf.Clamp(kind,0,8)];}
  void EnsureInventoryArt(){
   if(inventoryIcons!=null)return;
   inventoryIcons=new Texture2D[9];
   inventoryStage=new GameObject("Inventory portrait studio");inventoryStage.transform.position=Vector3.down*200;
   inventoryCamera=new GameObject("Inventory portrait camera",typeof(Camera)).GetComponent<Camera>();
   inventoryCamera.enabled=false;inventoryCamera.cullingMask=1<<31;inventoryCamera.clearFlags=CameraClearFlags.SolidColor;inventoryCamera.backgroundColor=Color.clear;inventoryCamera.orthographic=true;
   var key=new GameObject("Inventory studio key",typeof(Light));key.transform.SetParent(inventoryStage.transform,false);key.transform.rotation=Quaternion.Euler(35,-25,0);var light=key.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.cullingMask=1<<31;
   for(int kind=1;kind<=8;kind++){
    var root=new GameObject(ItemNames[kind]);root.transform.SetParent(inventoryStage.transform,false);
    void Part(string name,Vector3 pos,Vector3 scale,Color color){Cube(name,pos,scale,color,root.transform);}
    if(kind==8){var bag=MakeBackpack("School bag portrait",Vector3.zero,root.transform);bag.transform.localRotation=Quaternion.Euler(0,180,0);}
    else if(kind==1||kind==2){
     Part("Wooden shaft",Vector3.zero,new Vector3(.055f,kind==1?.7f:1.1f,.055f),new Color(.35f,.25f,.15f));
     Part("Blade",new Vector3(kind==1?.12f:0,kind==1?.23f:.55f,0),new Vector3(kind==1?.3f:.12f,.22f,.05f),new Color(.67f,.69f,.66f));
    }else if(kind==6){
     Part("Amber glass bottle",new Vector3(0,-.05f,0),new Vector3(.3f,.5f,.23f),new Color(.36f,.22f,.1f));
     Part("Bottle neck",new Vector3(0,.26f,0),new Vector3(.13f,.16f,.14f),new Color(.42f,.3f,.18f));
     Part("Bottle paper label",new Vector3(0,-.03f,-.125f),new Vector3(.25f,.21f,.025f),new Color(.83f,.78f,.61f));
    }else if(kind==4||kind==5){
     Part("Cloth bandage",Vector3.zero,new Vector3(.48f,.26f,.3f),new Color(.77f,.76f,.64f));
     Part("Paper wrap",new Vector3(0,0,-.16f),new Vector3(.17f,.28f,.025f),kind==5?new Color(.31f,.5f,.41f):new Color(.48f,.45f,.35f));
    }else if(kind==3){
     Part("Food tin",Vector3.zero,new Vector3(.4f,.5f,.33f),new Color(.45f,.46f,.39f));
     Part("Faded food label",new Vector3(0,0,-.18f),new Vector3(.36f,.26f,.025f),new Color(.53f,.31f,.18f));
    }else{
     for(int i=0;i<3;i++)Part("Pebble",new Vector3((i-1)*.15f,i%2*.1f,0),new Vector3(.22f,.16f,.18f),new Color(.44f,.45f,.4f));
    }
    SetPortraitLayer(root);
    inventoryIcons[kind]=CapturePortrait(root,128,128,.05f);
    root.SetActive(false);Destroy(root);
   }
   bodyModel=Instantiate(Resources.Load<GameObject>("Survival/Art/Characters_Matt"),inventoryStage.transform,false);
   var anim=bodyModel.GetComponentInChildren<Animation>();
   foreach(AnimationState state in anim)if(state.name.EndsWith("|Idle")){state.clip.SampleAnimation(anim.gameObject,0);break;}
   anim.enabled=false;
   foreach(var bone in bodyModel.GetComponentsInChildren<Transform>(true))if(System.Array.IndexOf(new[]{"knife","woodenbat_barbed","woodenbat_saw","pistol","shotgun","guitar","smg","spear","rifle","axe"},bone.name.ToLowerInvariant())>=0)bone.gameObject.SetActive(false);
   SetPortraitLayer(bodyModel);
  }
  static void SetPortraitLayer(GameObject obj){foreach(var t in obj.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;}
  Texture2D CapturePortrait(GameObject target,int width,int height,float size){
   var renderers=target.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
   inventoryCamera.transform.position=bounds.center+new Vector3(2,1.2f,target==bodyModel?4:-4);
   inventoryCamera.transform.LookAt(bounds.center);inventoryCamera.orthographicSize=Mathf.Max(size,Mathf.Max(bounds.extents.y,Mathf.Max(bounds.extents.x,bounds.extents.z))*1.3f);
   var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);bool fog=RenderSettings.fog;
   var image=new Texture2D(width,height,TextureFormat.RGBA32,false);
   try{RenderSettings.fog=false;inventoryCamera.targetTexture=rt;inventoryCamera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();}
   finally{RenderSettings.fog=fog;inventoryCamera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
   return image;
  }
  void DrawBodyPreview(Rect area){
   if(!bodyPicture||previewWeapon!=weapon||previewBag!=inventory.Data.backpack){
    if(bodyPicture)Destroy(bodyPicture);
    bodyModel.SetActive(true);
    foreach(var t in bodyModel.GetComponentsInChildren<Transform>(true)){
     string name=t.name.ToLowerInvariant();if(name=="axe")t.gameObject.SetActive(weapon==1);if(name=="spear")t.gameObject.SetActive(weapon==2);
    }
    if(portraitBackpack){portraitBackpack.SetActive(false);Destroy(portraitBackpack);portraitBackpack=null;}
    if(inventory.Data.backpack){portraitBackpack=MakeBackpack("Portrait school backpack",new Vector3(0,1.15f,-.32f),bodyModel.transform);SetPortraitLayer(portraitBackpack);}
    bodyPicture=CapturePortrait(bodyModel,256,384,1.15f);bodyModel.SetActive(false);previewWeapon=weapon;previewBag=inventory.Data.backpack;
   }
   GUI.DrawTexture(area,bodyPicture,ScaleMode.ScaleToFit,true);
  }
  Texture2D[] vitalIcons;
  void EnsureVitalIcons(){
   if(vitalIcons!=null)return;vitalIcons=new Texture2D[5];
   for(int k=0;k<5;k++){
    var texture=new Texture2D(48,48,TextureFormat.RGBA32,false);var pixels=new Color[48*48];
    for(int y=0;y<48;y++)for(int x=0;x<48;x++){
     float u=(x-24)/20f,v=(y-24)/20f;bool inside=false;
     if(k==0){float q=u*u+v*v-.55f;inside=q*q*q-u*u*v*v*v<0;}
     if(k==1)inside=v>-.9f&&v<.9f&&Mathf.Abs(u-(-v*.35f+(v>0?-.12f:.12f)))<.16f;
     if(k==2)inside=(v<.05f&&u*u+(v+.24f)*(v+.24f)<.28f)||(v>=0&&v<.85f&&Mathf.Abs(u)<(.85f-v)*.62f);
     if(k==3)inside=Mathf.Abs(u)<.16f&&Mathf.Abs(v)<.7f||Mathf.Abs(v)<.16f&&Mathf.Abs(u)<.7f;
     if(k==4)inside=Mathf.Abs(u)<(v>.35f?.18f:.35f)&&v>-.7f&&v<.7f;
     pixels[y*48+x]=inside?Color.white:Color.clear;
    }
    texture.SetPixels(pixels);texture.Apply();vitalIcons[k]=texture;
   }
  }
  void DrawCompactVitals(){
   EnsureVitalIcons();var names=new[]{"Здоровье","Выносливость","Кровотечение","Заражение раны","Опьянение"};
   for(int i=0;i<5;i++){
    if(i==2&&!bleeding||i==3&&infection<=0||i==4&&drunkSeconds<=0)continue;
    var area=new Rect(28+i*66,24,46,46);
    float value=i==0?health/100:i==1?stamina/100:i==2?1:i==3?infection/100:drunkSeconds/90;
    GUI.color=i==0||i==2?new Color(.92f,.5f,.43f):i==1?new Color(.83f,.76f,.46f):MedicalAccent;
    GUI.DrawTexture(area,vitalIcons[i]);GUI.color=Color.white;
    FillRect(new Rect(area.x,76,46,5),new Color(.17f,.22f,.20f));FillRect(new Rect(area.x,76,46*Mathf.Clamp01(value),5),MedicalAccent);
    if(area.Contains(pointer.Position)&&(!TouchControls.Enabled||pointer.Held)){vitalTip=i;vitalTipTime=Time.unscaledTime+1.5f;}
   }
   if(vitalTip>=0&&Time.unscaledTime<vitalTipTime){
    Panel(new Rect(360,30,300,48));GUI.Label(new Rect(370,37,280,36),names[vitalTip]+" · "+(vitalTip==0?Mathf.CeilToInt(health):vitalTip==1?Mathf.CeilToInt(stamina):vitalTip==2?1:vitalTip==3?Mathf.CeilToInt(infection):Mathf.CeilToInt(drunkSeconds)),small);
   }
  }
  void ReleaseInventoryArt(){
   if(inventoryIcons!=null)foreach(var icon in inventoryIcons)if(icon)Destroy(icon);
   if(vitalIcons!=null)foreach(var icon in vitalIcons)if(icon)Destroy(icon);
   if(bodyPicture)Destroy(bodyPicture);if(inventoryCamera)Destroy(inventoryCamera.gameObject);if(inventoryStage)Destroy(inventoryStage);
  }
 }
}
