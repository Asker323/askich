using System.Collections.Generic;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  const int StationHome=21;
  readonly Vector2 enginePosition=new Vector2(101,5);
  GameObject engineModel,canopyPanel,canopyStripe;
  sealed class LifeFind {public Vector2 p;public DistrictItem[] items;public GameObject root;public string title;}
  readonly List<Mesh> houseRoofMeshes=new List<Mesh>();
  readonly List<Light> worldNightLights=new List<Light>();
  readonly List<LifeFind> lifeFinds=new List<LifeFind>();
  static readonly Color[] HouseColours={new Color(.53f,.47f,.36f),new Color(.37f,.43f,.40f),new Color(.49f,.39f,.32f),new Color(.42f,.46f,.48f),new Color(.55f,.52f,.44f),new Color(.35f,.42f,.46f)};
  static readonly string[] HomeRoles={"Дом садовода","Дом железнодорожника","Дом столяра","Дом с остывшим ужином","Семейный дом","Дом учительницы"};
  void ConfigureResidence(Home h,int id){if(id==5||id==6)return;h.title=id==StationHome?"АЗС «Восточная»":HomeRoles[id%6]+" · №"+(id+1);for(int i=0;i<3;i++)h.containers[i].y=2.1f+(id%4)*.17f;}
  void BuildResidenceRoof(Home h,int id){
   h.roof=new GameObject(id==StationHome?"Station flat roof":"Individual house roof");h.roof.transform.position=V(h.p);
   var tint=id%3==0?new Color(.34f,.22f,.18f):id%3==1?new Color(.24f,.29f,.30f):new Color(.31f,.34f,.27f);
   if(id==StationHome){Cube("Station roof",new Vector3(0,3,0),new Vector3(8.6f,.3f,8.6f),new Color(.28f,.31f,.30f),h.roof.transform);Cube("Station green fascia",new Vector3(0,2.9f,-4.3f),new Vector3(8.6f,.38f,.12f),new Color(.16f,.34f,.27f),h.roof.transform);
    void Stroke(float x,float y,float width,float height,float tilt=0){var mark=Cube("АЗС lettering",new Vector3(x,2.9f+y,-4.39f),new Vector3(width,height,.035f),new Color(.85f,.79f,.59f),h.roof.transform);mark.transform.localRotation=Quaternion.Euler(0,0,tilt);}
    Stroke(-.66f,0,.055f,.29f,-17);Stroke(-.57f,0,.055f,.29f,17);Stroke(-.615f,-.015f,.16f,.045f);
    for(int line=-1;line<=1;line++)Stroke(0,line*.12f,.22f,.045f);Stroke(.10f,0,.045f,.25f);
    Stroke(.61f,.12f,.22f,.045f);Stroke(.61f,-.12f,.22f,.045f);Stroke(.51f,0,.045f,.25f);return;}
   if(id%4==1){
    var mesh=new Mesh{name="Hipped roof"};mesh.vertices=new[]{new Vector3(-4.3f,2.9f,-4.3f),new Vector3(4.3f,2.9f,-4.3f),new Vector3(4.3f,2.9f,4.3f),new Vector3(-4.3f,2.9f,4.3f),new Vector3(0,4.5f,0)};mesh.triangles=new[]{0,4,1,1,4,2,2,4,3,3,4,0};var points=mesh.vertices;var indices=mesh.triangles;var facets=new Vector3[indices.Length];for(int j=0;j<indices.Length;j++){facets[j]=points[indices[j]];indices[j]=j;}mesh.vertices=facets;mesh.triangles=indices;mesh.RecalculateNormals();houseRoofMeshes.Add(mesh);var roof=new GameObject("Hipped roof panels",typeof(MeshFilter),typeof(MeshRenderer));roof.transform.SetParent(h.roof.transform,false);roof.GetComponent<MeshFilter>().sharedMesh=mesh;roof.GetComponent<Renderer>().sharedMaterial=SurfaceFor("Hipped roof panels",tint);
   }else if(id%4==2){var sheet=Cube("Low pitched roof",new Vector3(0,3.35f,0),new Vector3(8.8f,.28f,8.6f),tint,h.roof.transform);sheet.transform.localRotation=Quaternion.Euler(0,0,7);}
   else for(int side=-1;side<=1;side+=2){var slope=Cube("Roof slope",new Vector3(side*2.1f,3.6f,0),new Vector3(4.6f,.22f,8.6f),tint,h.roof.transform);slope.transform.localRotation=Quaternion.Euler(0,0,-side*20);}
   Cube("House chimney",new Vector3(id%2==0?2:-2,4.05f,1+id%3*.45f),new Vector3(.6f,1.45f,.6f),new Color(.39f,.28f,.23f),h.roof.transform);
  }
  void BuildOrdinaryInterior(Home h,int id){
   if(id==5||id==6||id==StationHome)return;
   if(BuildPlannedInterior(h,id))return;
   Vector2 p=h.p;int style=id%6;var cloth=Color.Lerp(HouseColours[(id+2)%6],new Color(.32f,.36f,.31f),.4f);
   var bed=Cube("Bed with worn blanket",V(p+new Vector2(2,-1.1f),.43f),new Vector3(1.45f,.55f,2.1f),cloth);if(style%2==1)bed.transform.rotation=Quaternion.Euler(0,90,0);
   Cube("Creased pillow",V(p+new Vector2(style%2==1?2.65f:2,style%2==1?-1.1f:-.4f),.76f),style%2==1?new Vector3(.45f,.15f,.9f):new Vector3(.9f,.15f,.45f),new Color(.64f,.63f,.54f));
   if(style!=2&&style!=4)DistrictAtmosphere.Spawn("Couch",V(p+new Vector2(-2,-2.5f)),style%2==0?90:0);
   else Cube(style==2?"Carpenter workbench":"Child cot",V(p+new Vector2(-2.3f,-2.6f),.55f),new Vector3(1.6f,.7f,1),new Color(.33f,.26f,.19f));
   for(int i=0;i<2+id%3;i++)Cube(style==0?"Seed packets":style==1?"Work gloves":style==2?"Timber offcuts":style==3?"Abandoned dinner":style==4?"Child toys":"School exercise books",V(p+new Vector2(-2.5f+i*.27f,-.1f+(i%2)*.22f),.81f),new Vector3(.20f,.05f,.23f),HouseColours[(i+id)%6]);
   if(style==5)for(int row=0;row<3;row++){Cube("Book shelf",V(p+new Vector2(-3.6f,1),.55f+row*.5f),new Vector3(.38f,.06f,1.4f),new Color(.29f,.23f,.18f));for(int n=0;n<6;n++)Cube("Spines of old books",V(p+new Vector2(-3.6f,.45f+n*.19f),.75f+row*.5f),new Vector3(.25f,.34f,.12f),HouseColours[(n+row)%6]);}
   if(style==0)for(int n=0;n<3;n++){Cube("Kitchen growing pot",V(p+new Vector2(3.55f,-2+n*.4f),.4f),new Vector3(.28f,.35f,.28f),new Color(.35f,.23f,.17f));Cube("Dry indoor leaves",V(p+new Vector2(3.55f,-2+n*.4f),.66f),new Vector3(.37f,.20f,.31f),new Color(.27f,.32f,.19f));}
   Cube("Individual worn rug",V(p+new Vector2(.1f,-.4f),.18f),new Vector3(1.7f,.015f,2.1f+(id%3)*.3f),HouseColours[(id+3)%6]*.65f);
  }
  void DressResidence(Home h,int id){
   if(id==StationHome)return;
   var frame=new Color(.51f,.48f,.38f);
   for(int side=-1;side<=1;side+=2){
    if(id%3==0){var shutter=Cube("Faded window shutter",V(h.p+new Vector2(side*3.38f,-4.32f),1.65f),new Vector3(.42f,1.13f,.09f),HouseColours[(id+1)%6]);h.cut.Add(shutter);}
    if(id%4==1){var board=Cube("Board over abandoned window",V(h.p+new Vector2(side*2.5f,-4.34f),1.55f),new Vector3(1.55f,.18f,.07f),frame);board.transform.rotation=Quaternion.Euler(0,0,side*11);h.cut.Add(board);}
   }
   for(int n=0;n<3;n++)Cube("Distinct chipped plaster",V(h.p+new Vector2(HasHousePlan(id)?(n==2?2.9f:-3.4f+n*.55f):-2.9f+n*1.6f+id%3*.13f,3.81f),.6f+n*.22f),new Vector3(.3f+id%3*.1f,.15f,.02f),new Color(.29f,.30f,.26f));
   if(id%2==0){Cube("Front timber porch",V(h.p+Vector2.down*4.9f,.11f),new Vector3(2.5f,.16f,1.6f),new Color(.31f,.27f,.20f));for(int n=0;n<2;n++)Cube("House flower box",V(h.p+new Vector2(-3.1f+n*6.2f,-5.2f),.28f),new Vector3(1.1f,.4f,.55f),frame);}
   else Cube("Old firewood rack",V(h.p+new Vector2(5.2f,-2.7f),.45f),new Vector3(.9f,.8f,1.8f),new Color(.32f,.25f,.18f));
  }
  void BuildWorldLife(){
   var station=homes[StationHome];station.note="ЗАПИСКА МЕХАНИКА: «Насосы обесточены. Аварийные лампы держит аккумулятор. Бензин увезли. Ключ оставил у прилавка, руководство — на столе. На верстаке лежит двигатель: новичок только испортит резьбу. Сначала прочти хотя бы первую главу. Радио у связиста иногда ловит учебную передачу»";
   Cube("Station forecourt",new Vector3(96,.025f,5),new Vector3(15,.07f,18),new Color(.32f,.33f,.31f));
   canopyPanel=Cube("Fuel island canopy",new Vector3(96,3.5f,3.2f),new Vector3(13,.25f,7),new Color(.23f,.31f,.28f));
   canopyStripe=Cube("Canopy faded yellow stripe",new Vector3(96,3.45f,-.34f),new Vector3(13,.2f,.08f),new Color(.72f,.62f,.32f));
   for(int side=-1;side<=1;side+=2){
    Cube("Canopy steel column",new Vector3(96+side*5.8f,1.7f,3.2f),new Vector3(.24f,3.4f,.24f),new Color(.33f,.37f,.34f));barriers.Add(new Barrier{rect=new Rect(96+side*5.8f-.12f,3.08f,.24f,.24f)});
    var p=new Vector2(96+side*2.2f,2.5f);Cube("Abandoned fuel pump",V(p,.83f),new Vector3(.9f,1.65f,.65f),new Color(.23f,.37f,.29f));Cube("Pump dead display",V(p+Vector2.down*.34f,1.25f),new Vector3(.65f,.34f,.035f),new Color(.07f,.10f,.10f));Cube("Pump nozzle",V(p+Vector2.right*.52f,1.05f),new Vector3(.10f,.38f,.11f),new Color(.14f,.16f,.15f));
    barriers.Add(new Barrier{rect=new Rect(p-new Vector2(.45f,.33f),new Vector2(.9f,.66f))});
   }
   var forecourtLight=DistrictAtmosphere.Point("Station canopy emergency light",new Vector3(96,3,4),new Color(1,.77f,.45f),1.25f,10);forecourtLight.enabled=false;worldNightLights.Add(forecourtLight);
   var shopLight=DistrictAtmosphere.Point("Station shop emergency light",V(station.p,2.3f),new Color(.85f,.83f,.67f),.65f,6);shopLight.enabled=false;worldNightLights.Add(shopLight);
   Cube("Engine stand",V(enginePosition,.65f),new Vector3(1.15f,.1f,.9f),new Color(.25f,.29f,.27f));for(int side=-1;side<=1;side+=2)Cube("Engine stand leg",V(enginePosition+new Vector2(side*.45f,0),.33f),new Vector3(.08f,.66f,.7f),new Color(.25f,.29f,.27f));
   engineModel=new GameObject("Salvageable engine");engineModel.transform.position=V(enginePosition,.85f);Cube("Rusty engine block",Vector3.zero,new Vector3(.8f,.38f,.5f),new Color(.32f,.29f,.23f),engineModel.transform);for(int i=0;i<4;i++)Cube("Cylinder head",new Vector3(-.28f+i*.18f,.23f,0),new Vector3(.12f,.16f,.3f),new Color(.39f,.41f,.36f),engineModel.transform);
   barriers.Add(new Barrier{rect=new Rect(enginePosition-new Vector2(.5f,.4f),new Vector2(1,.8f))});
   AddLifeFind(station.p+new Vector2(-2,0),.83f,"Руководство по механике",new[]{Ingredient(DistrictItemKind.MechanicsBook,1)});
   AddLifeFind(homes[6].p+new Vector2(-2,0),.83f,"Книга «Домашняя мастерская»",new[]{Ingredient(DistrictItemKind.CraftBook,1)});
   AddLifeFind(station.p+new Vector2(2,-1.9f),1.14f,"Гаечный ключ",new[]{Ingredient(DistrictItemKind.Wrench,1)});
   AddLifeFind(new Vector2(103,7),.13f,"Связка досок",new[]{Ingredient(DistrictItemKind.Wood,4)});
   Cube("Station offcut crate",new Vector3(103,.2f,10),new Vector3(.8f,.35f,.7f),new Color(.34f,.28f,.19f));
   AddLifeFind(new Vector2(103,10),.4f,"Ящик обрезков",new[]{Ingredient(DistrictItemKind.Scrap,3),Ingredient(DistrictItemKind.Wire,2),Ingredient(DistrictItemKind.Cloth,2)});
   RefreshLifeFinds();
  }
  void AddLifeFind(Vector2 p,float height,string title,DistrictItem[] items){var root=new GameObject(title);root.transform.position=V(p,height);MakeProgressionIcon(root.transform,items[0].kind);lifeFinds.Add(new LifeFind{p=p,root=root,title=title,items=items});}
  void ReleaseWorldLife(){foreach(var mesh in houseRoofMeshes)if(mesh)Destroy(mesh);houseRoofMeshes.Clear();}
  void RefreshLifeFinds(){for(int i=0;i<lifeFinds.Count;i++)lifeFinds[i].root.SetActive((progression.Data.finds&(1<<i))==0);if(engineModel)engineModel.SetActive(!progression.Data.engineDismantled);}
  int NearbyLifeFind(){int found=-1;float range=1.25f;for(int i=0;i<lifeFinds.Count;i++){var f=lifeFinds[i];float distance=Vector2.Distance(hero.p,f.p);if((progression.Data.finds&(1<<i))==0&&distance<range&&Sight(hero.p,f.p)){range=distance;found=i;}}return found;}
  bool UseLifeFind(){int i=NearbyLifeFind();if(i<0)return false;var find=lifeFinds[i];if(!TryPack(find.items,true)){Say("Освободи место для находки. Она останется здесь.");return true;}progression.Data.finds|=1<<i;RefreshLifeFinds();Save();Say("Подобрано: "+find.title+". I — предметы, J — навыки и ремесло.");return true;}
  bool UseEngine(){if(!NearEngine)return false;BeginMedical(MedicalAction.Salvage);return true;}
  void TickWorldLife(float dt){progression.Advance(dt);atmosphere.SetTime(progression.Data.minute/60);bool underCanopy=new Rect(89.5f,-.3f,13,8).Contains(hero.p);canopyPanel.SetActive(!underCanopy);canopyStripe.SetActive(!underCanopy);foreach(var light in worldNightLights)light.enabled=atmosphere.Daylight<.65f;}
  string ClockText{get{int minute=Mathf.FloorToInt(progression.Data.minute);return (minute/60).ToString("D2")+":"+(minute%60).ToString("D2");}}
  void DrawWorldLife(){
   int find=NearbyLifeFind();if(find>=0)GUI.Label(new Rect(35,235,720,30),lifeFinds[find].title+" · подобрать / E",small);
   else if(NearEngine)GUI.Label(new Rect(35,235,820,40),progression.Data.engineDismantled?"Двигатель разобран":(ProgressionReason(MedicalAction.Salvage)??"Разобрать двигатель · E · шумно"),small);
  }
 }
}
