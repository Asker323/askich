using System;
using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  // Slots stay stable for legacy inventory/doors. Retired homes have no scene objects or AI.
  static bool IsCuratedHome(int id)=>id==5||id==6||id==StationHome;
  static bool LegacyCuratedEnemyId(int id,int version)=>(id>=0&&id<32&&id!=2&&id!=30)||(version>=3&&id>=100&&id<=108);
  static float PreviousEnemyHealth(int id)=>id%3==1?160:id%3==2?70:110;
  void RecoverRetiredStorage(){
   foreach(var container in inventory.Data.containers){
    if(IsCuratedHome(container.index/3))continue;
    for(int i=0;i<container.items.Length;i++)if(!container.items[i].Empty){inventory.Data.overflow.Add(container.items[i]);container.items[i]=default;}
   }
  }
  void SpawnCuratedInfected(){
   int[] ids={0,1,8,15,100,108};
   Vector2[] positions={new Vector2(-32,28),new Vector2(-42,40),new Vector2(-14,-5),new Vector2(26,-8),new Vector2(90,0),new Vector2(106,33)};
   for(int i=0;i<ids.Length;i++)infected.Add(MakePerson(positions[i],true,ids[i]));
  }
  readonly List<GameObject> interiorBatches=new List<GameObject>();
  void BuildCuratedInterior(Home h,int id){
   var root=new GameObject("Furnished interior "+id);root.transform.position=V(h.p);interiorBatches.Add(root);
   Color timber=new Color(.30f,.24f,.18f),linen=new Color(.55f,.54f,.45f),ceramic=new Color(.58f,.62f,.56f),metal=new Color(.26f,.30f,.29f);
   Color cloth=id==5?new Color(.29f,.34f,.27f):id==6?new Color(.34f,.28f,.25f):new Color(.22f,.29f,.31f);
   GameObject P(string n,float x,float y,float z,float sx,float sy,float sz,Color c)=>Cube(n,new Vector3(x,y,z),new Vector3(sx,sy,sz),c,root.transform);
   // Private rooms on the west, central hallway and an open living/kitchen area on the east.
   var plaster=id==6?new Color(.46f,.43f,.36f):new Color(.39f,.43f,.37f);
   Partition(h,"Bedroom partition",new Vector2(-1.05f,-2.8f),new Vector2(.14f,2.4f),plaster);
   Partition(h,"Bathroom partition south",new Vector2(-1.05f,.6f),new Vector2(.14f,.6f),plaster);
   Partition(h,"Bathroom partition north",new Vector2(-1.05f,3.1f),new Vector2(.14f,1.8f),plaster);
   Partition(h,"Bathroom dividing wall",new Vector2(-2.5f,.3f),new Vector2(3,.14f),plaster);
   // Doorway jambs mark open 1.3m passages, without adding another interaction prompt.
   foreach(float z in new[]{.92f,2.17f})P("Bathroom timber jamb",-1.05f,1.15f,z,.18f,2.2f,.10f,timber);
   P("Bathroom tiled floor",-2.5f,.18f,2.1f,2.8f,.025f,3.55f,new Color(.27f,.34f,.32f));
   for(int n=0;n<5;n++)P("Bathroom tile grout",-3.7f+n*.56f,.197f,2.1f,.015f,.008f,3.5f,new Color(.44f,.47f,.41f));
   for(int n=0;n<6;n++)P("Bathroom tile grout",-2.5f,.197f,.5f+n*.58f,2.8f,.008f,.015f,new Color(.44f,.47f,.41f));
   // Hollow bathtub: separate rim and dark interior, rather than a solid white block.
   P("Bathtub enamel base",-3.15f,.37f,2.65f,.95f,.35f,1.9f,ceramic);
   P("Bathtub stained interior",-3.15f,.56f,2.65f,.73f,.03f,1.65f,new Color(.22f,.29f,.27f));
   for(int side=-1;side<=1;side+=2){P("Bathtub enamel rim",-3.15f+side*.47f,.66f,2.65f,.10f,.24f,2,ceramic);P("Bathtub enamel rim",-3.15f,.66f,2.65f+side*.95f,.95f,.24f,.10f,ceramic);}
   P("Bath tap pipe",-3.15f,.87f,3.48f,.05f,.23f,.05f,metal);P("Bath tap spout",-3.15f,.96f,3.38f,.05f,.04f,.25f,metal);
   P("Toilet pedestal",-1.65f,.35f,3.25f,.34f,.34f,.42f,ceramic);P("Toilet bowl",-1.65f,.55f,3.16f,.52f,.19f,.65f,ceramic);
   P("Toilet seat opening",-1.65f,.65f,3.14f,.30f,.015f,.40f,new Color(.14f,.20f,.18f));P("Toilet cistern",-1.65f,.83f,3.55f,.52f,.56f,.25f,ceramic);
   P("Bathroom sink pedestal",-3.4f,.57f,.9f,.3f,.72f,.3f,ceramic);P("Bathroom sink rim",-3.4f,.96f,.9f,.70f,.14f,.58f,ceramic);P("Basin dark hollow",-3.4f,1.035f,.9f,.5f,.012f,.37f,new Color(.27f,.34f,.31f));
   P("Sink faucet",-3.4f,1.14f,1.11f,.06f,.24f,.08f,metal);
   P("Towel rail",-3.81f,1.3f,1.55f,.08f,.045f,.65f,metal);P("Hanging worn towel",-3.76f,1.08f,1.55f,.035f,.48f,.46f,linen);
   P("Bedroom bed frame",-2.55f,.36f,-2.65f,1.4f,.38f,2.15f,timber);P("Bedroom mattress",-2.55f,.59f,-2.65f,1.34f,.19f,2.08f,linen);
   P("Creased bed cover",-2.55f,.72f,-2.92f,1.35f,.09f,1.5f,cloth);P("Bed pillow",-2.55f,.76f,-1.94f,.88f,.16f,.38f,linen);
   for(int n=0;n<4;n++){var fold=P("Blanket fold",-3.06f+n*.32f,.78f,-2.92f,.1f,.025f,1.4f,cloth*.86f);fold.transform.localRotation=Quaternion.Euler(0,n%2==0?3:-3,0);}
   P("Living room rug",1.65f,.185f,-.15f,2.1f,.02f,1.8f,cloth*.8f);
   // Sofa faces the television across a clear central space.
   P("Living couch base",.9f,.4f,-.1f,.65f,.42f,1.5f,timber);P("Living couch back",.62f,.85f,-.1f,.18f,.6f,1.55f,cloth);
   for(int n=0;n<2;n++)P("Living couch cushion",.98f,.65f,-.48f+n*.75f,.57f,.17f,.69f,cloth);
   for(int side=-1;side<=1;side+=2)P("Living couch armrest",.93f,.77f,-.1f+side*.78f,.72f,.36f,.12f,cloth);
   P("Television timber stand",3.3f,.53f,-.3f,.65f,.7f,1.3f,timber);
   BuildTelevision(h,new Vector2(3.3f,-.3f));
   // Cabinets remain the real searchable objects; dressing never replaces loot interactions.
   P("Kitchen enamel stove",3.3f,.66f,2.7f,.83f,.95f,.95f,ceramic);P("Cold hob",3.3f,1.15f,2.7f,.7f,.03f,.78f,metal);
   for(int n=0;n<2;n++)P("Stove burner",3.3f,1.175f,2.47f+n*.42f,.45f,.02f,.29f,new Color(.09f,.11f,.10f));
   P("Kitchen shelf",1.3f,1.85f,3.7f,2.9f,.07f,.36f,timber);
   for(int n=0;n<4;n++)P("Dusty kitchen jars",.4f+n*.48f,2,3.7f,.18f,.25f,.17f,Color.Lerp(ceramic,cloth,n*.15f));
   P("Discarded coat",.15f,.21f,-2.55f,.6f,.035f,.45f,cloth);
   for(int n=0;n<4;n++)P("Abandoned letter",-.6f+n*.2f,.21f,-.5f+(n%2)*.17f,.16f,.012f,.23f,linen);
   if(id==6){P("Patient records",-2,.82f,0,.55f,.06f,.36f,cloth);P("Used gauze",-2.35f,.82f,0,.16f,.03f,.2f,new Color(.43f,.22f,.17f));}
   else if(id==StationHome){P("Staff shift log",-2,.82f,0,.55f,.04f,.35f,linen);P("Cold staff mug",-2.45f,.9f,0,.15f,.22f,.15f,ceramic);}
  }
  void BatchCuratedInteriors(){foreach(var root in interiorBatches)StaticBatchingUtility.Combine(root);}
  sealed class Television {public Vector2 p;public Home home;public Renderer screen;public AudioSource sound;public bool on;public float pulse;public Light glow;}
  readonly List<Television> televisions=new List<Television>();
  Texture2D televisionNoise;AudioClip televisionHum;Material televisionScreen;MaterialPropertyBlock tvTint;
  void BuildTelevision(Home h,Vector2 local){
   if(!televisionNoise){
    var random=new System.Random(1707);televisionNoise=new Texture2D(64,64,TextureFormat.RGB24,false);var pixels=new Color[4096];for(int i=0;i<pixels.Length;i++){float v=(float)random.NextDouble();pixels[i]=new Color(v*.54f,v*.65f,v*.60f);}televisionNoise.SetPixels(pixels);televisionNoise.Apply();
    televisionScreen=new Material(Shader.Find("Standard"));televisionScreen.mainTexture=televisionNoise;televisionScreen.EnableKeyword("_EMISSION");televisionScreen.SetTexture("_EmissionMap",televisionNoise);
    var samples=new float[22050];for(int i=0;i<samples.Length;i++)samples[i]=((float)random.NextDouble()*2-1)*.13f+Mathf.Sin(i*2*Mathf.PI*50/22050)*.035f;
    televisionHum=AudioClip.Create("Authored television interference",samples.Length,1,22050,false);televisionHum.SetData(samples,0);tvTint=new MaterialPropertyBlock();
   }
   var root=new GameObject("Interactive television");root.transform.position=V(h.p+local,1.25f);root.transform.rotation=Quaternion.Euler(0,90,0);
   Cube("Dusty television casing",Vector3.zero,new Vector3(.95f,.68f,.51f),new Color(.17f,.16f,.12f),root.transform);
   var screen=Cube("Curved television glass",new Vector3(-.06f,0,-.265f),new Vector3(.68f,.48f,.025f),Color.black,root.transform).GetComponent<Renderer>();screen.sharedMaterial=televisionScreen;
   for(int n=0;n<4;n++)Cube("Television speaker grille",new Vector3(.37f,-.14f+n*.075f,-.27f),new Vector3(.1f,.022f,.025f),new Color(.07f,.08f,.07f),root.transform);
   Cube("Television switch",new Vector3(.36f,.23f,-.29f),new Vector3(.09f,.08f,.06f),new Color(.47f,.43f,.33f),root.transform);
   var audio=root.AddComponent<AudioSource>();audio.clip=televisionHum;audio.loop=true;audio.playOnAwake=false;audio.spatialBlend=1;audio.rolloffMode=AudioRolloffMode.Linear;audio.minDistance=1;audio.maxDistance=12;audio.volume=.18f;audio.dopplerLevel=0;
   var glow=DistrictAtmosphere.Point("Television glow",root.transform.position+Vector3.left*.4f,new Color(.45f,.65f,.57f),.35f,2.8f);glow.enabled=false;
   var tv=new Television{p=h.p+local,home=h,screen=screen,sound=audio,glow=glow};televisions.Add(tv);PaintTelevision(tv);
  }
  Television NearbyTelevision(){
   var tv=televisions.Find(t=>Vector2.Distance(hero.p,t.p)<1.8f&&Sight(hero.p,t.p));if(tv==null)return null;
   float distance=Vector2.Distance(hero.p,tv.p);var window=NearbyWindow();if(window!=null&&Vector2.Distance(hero.p,window.p)<=distance+.2f)return null;
   if(current!=null){int slot=NearbyContainer(current);if(slot>=0&&Vector2.Distance(hero.p,current.p+current.containers[slot])<=distance)return null;}
   return tv;
  }
  bool UseTelevision(){var tv=NearbyTelevision();if(tv==null)return false;tv.on=!tv.on;tv.pulse=0;if(tv.on)tv.sound.Play();else tv.sound.Stop();PaintTelevision(tv);Save();return true;}
  void PaintTelevision(Television tv){tvTint.Clear();tvTint.SetColor("_Color",tv.on?Color.white:new Color(.035f,.045f,.04f));tvTint.SetColor("_EmissionColor",tv.on?new Color(.38f,.56f,.46f)*(1+.12f*Mathf.Sin(Time.time*23)):Color.black);tvTint.SetVector("_MainTex_ST",new Vector4(1,1,0,tv.on?Time.time*7:0));tv.screen.SetPropertyBlock(tvTint);tv.glow.enabled=tv.on;}
  void TickTelevisions(float dt){foreach(var tv in televisions){if(!tv.on)continue;PaintTelevision(tv);tv.pulse-=dt;if(tv.pulse<=0){tv.pulse=3;Alert(tv.p,18);}}}
  int CaptureTelevisions(){int flags=0;for(int i=0;i<televisions.Count;i++)if(televisions[i].on)flags|=1<<i;return flags;}
  void RestoreTelevisions(int flags){for(int i=0;i<televisions.Count;i++){var tv=televisions[i];tv.on=(flags&(1<<i))!=0;tv.pulse=0;if(tv.on)tv.sound.Play();else tv.sound.Stop();PaintTelevision(tv);}}
  void ResetTelevisions(){foreach(var tv in televisions){tv.on=false;tv.sound.Stop();PaintTelevision(tv);}}
  void ReleaseTelevisions(){if(televisionNoise)Destroy(televisionNoise);if(televisionHum)Destroy(televisionHum);if(televisionScreen)Destroy(televisionScreen);}
 }
}
