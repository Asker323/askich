using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  float searchNoiseClock,findUntil;int findKind;string findText;
  void ConfigureStorage(Home h,int id){
   if(id!=5&&id!=6){h.labels=new[]{"Ящик в кладовке","Кухонный ящик","Домашняя аптечка"};h.loot=new[]{id==2?"Копьё":id==19?"Топор":id%4==0?"Доски":id%4==1?"Лоскуты ткани":"Пусто",id%3==1?"Пусто":ItemNames[14+(id/3)%3],id%2==0?"Бинт":"Пусто"};h.seconds=new[]{2.8f,2.2f,1.3f};h.noise=new[]{10f,6f,3f};}
   if(id==StationHome){h.labels=new[]{"Полка магазина","Продуктовый ящик","Ящик электрика"};h.loot=new[]{"Сухари","Фасоль в банке","Проволока"};h.seconds=new[]{1.8f,2.2f,3.2f};h.noise=new[]{4f,5f,11f};}
  }
  void BuildStorageModel(Home h,int id,int i){
   var art=new System.Collections.Generic.List<Renderer>();
   GameObject Part(string name,Vector3 position,Vector3 size,Color tint,Transform parent=null){var obj=Cube(name,position,size,tint,parent);art.Add(obj.GetComponent<Renderer>());return obj;}
   bool med=id!=StationHome&&(id==6||i==2);bool tool=id==StationHome?i==2:!med&&i==0;
   Vector2 p=h.p+h.containers[i];float height=med?.42f:tool?.6f:.9f;float width=med?.78f:1.2f;float depth=med?.52f:.7f;
   Color color=med?new Color(.68f,.69f,.6f):tool?new Color(.22f,.28f,.29f):new Color(.39f,.28f,.18f);
   Part(h.labels[i],V(p,.18f+height/2),new Vector3(width,height,depth),color);
   Part("Storage dark opening",V(p,.18f+height),new Vector3(width-.12f,.025f,depth-.12f),new Color(.07f,.09f,.08f));
   var lid=new GameObject("Storage lid").transform;lid.position=V(p+Vector2.up*depth/2,.21f+height);h.lids[i]=lid;
   Part("Container lid",new Vector3(0,0,-depth/2),new Vector3(width+.04f,.07f,depth+.04f),color,lid);
   Part("Lid handle",new Vector3(0,.06f,-depth*.8f),new Vector3(.25f,.06f,.07f),new Color(.12f,.14f,.13f),lid);
   if(med){var red=new Color(.51f,.2f,.17f);Part("First aid cross",new Vector3(0,.045f,-depth/2),new Vector3(.28f,.012f,.075f),red,lid);Part("First aid cross",new Vector3(0,.046f,-depth/2),new Vector3(.075f,.012f,.28f),red,lid);}
   if(tool)for(int side=-1;side<=1;side+=2)Part("Metal corner",V(p+new Vector2(side*(width/2-.06f),-depth/2-.01f),.18f+height/2),new Vector3(.075f,height,.03f),new Color(.48f,.45f,.34f));
   if(!med&&!tool)for(int n=0;n<2;n++){Part("Kitchen drawer seam",V(p+Vector2.down*(depth/2+.01f),.35f+n*.35f),new Vector3(width-.08f,.025f,.025f),new Color(.17f,.14f,.1f));}
   h.storageRest[i]=lid.position;
   if(!med&&!tool){
    h.drawers[i]=true;
    // Replace the moving top with a shallow drawer; the cabinet top stays fixed.
    foreach(var renderer in art)if(renderer.transform.IsChildOf(lid))renderer.enabled=false;
    lid.position=V(p,.69f);h.storageRest[i]=lid.position;
    Part("Fixed kitchen worktop",V(p,1.1f),new Vector3(width+.06f,.08f,depth+.06f),color);
    Part("Sliding drawer front",new Vector3(0,0,-depth/2-.025f),new Vector3(width,.27f,.07f),color,lid);
    Part("Drawer pull",new Vector3(0,0,-depth/2-.09f),new Vector3(.28f,.045f,.05f),new Color(.38f,.38f,.32f),lid);
    Part("Drawer bottom",new Vector3(0,-.1f,0),new Vector3(width-.12f,.035f,depth-.08f),new Color(.2f,.15f,.1f),lid);
    for(int side=-1;side<=1;side+=2)Part("Drawer side",new Vector3(side*(width/2-.08f),0,0),new Vector3(.055f,.23f,depth-.08f),color,lid);
    Part("Forgotten wrapper",new Vector3(.23f,-.07f,-.1f),new Vector3(.18f,.025f,.14f),new Color(.5f,.46f,.34f),lid);
   }
   h.storageArt[i]=art.FindAll(v=>v.enabled).ToArray();
  }
  DistrictItemKind StorageKind(Home h,int i)=>h.loot[i]=="Топор"?DistrictItemKind.Axe:h.loot[i]=="Копьё"?DistrictItemKind.Spear:h.loot[i]=="Еда"?DistrictItemKind.Food:h.loot[i]=="Бинт"?DistrictItemKind.Bandage:NamedStorageKind(h.loot[i]);
  DistrictItemKind NamedStorageKind(string name){for(int kind=14;kind<ItemNames.Length;kind++)if(ItemNames[kind]==name)return (DistrictItemKind)kind;return DistrictItemKind.Empty;}
  void RevealStorage(Home h,int i){
   if(h.taken[i])return;var kind=StorageKind(h,i);var items=inventory.Container(homes.IndexOf(h)*3+i);
   if(kind!=DistrictItemKind.Empty){items[0]=new DistrictItem(kind,1);if(TryCarry(kind,1))items[0]=default;}
   h.taken[i]=true;progression.Gain(DistrictSkill.Agility,1.5f);findKind=(int)kind;findText=kind==DistrictItemKind.Empty?"Пусто":items[0].Empty?ItemNames[(int)kind]:"Нет места · оставлено: "+ItemNames[(int)kind];findUntil=Time.unscaledTime+3;
   if(containerAudio)containerAudio.Play(V(h.p+h.containers[i],1),h.noise[i],true);Save();
  }
  void CancelSearch(){searchSlot=-1;searchTime=0;searchNoiseClock=0;if(containerAudio)containerAudio.SetSearching(false);}
  void SearchBeat(){if(searchSlot<0)return;Alert(searchHome.p+searchHome.containers[searchSlot],searchHome.noise[searchSlot]);if(containerAudio)containerAudio.Pulse();}
  void TickSearchNoise(float dt){if(searchSlot<0)return;searchNoiseClock+=dt;if(searchNoiseClock>=.75f){searchNoiseClock%=.75f;SearchBeat();}}
  bool TakeStoredItem(int store,int slot){
   if(store<0||slot<0||slot>=6||store!=NearbyStore()||medicalAction!=MedicalAction.None)return false;
   var items=inventory.Container(store);var item=items[slot];if(item.Empty)return false;
   if(!TryCarry((DistrictItemKind)item.kind,item.amount)){InventoryFeedback("Нет свободной ячейки.");return false;}
   items[slot]=default;if(containerAudio)containerAudio.Play(V(hero.p,1),0,true);Save();return true;
  }
  void DrawStorageActions(int store){
   if(store<0)return;
   if(Button(new Rect(905,535,245,45),"Забрать всё")){for(int i=0;i<6;i++)TakeStoredItem(store,i);}
   if(inventorySelection<=-10&&inventorySelection>=-15){int slot=-inventorySelection-10;var item=inventory.Container(store)[slot];if(item.Empty)return;GUI.Label(new Rect(905,190,245,64),ItemNames[item.kind],text);if(Button(new Rect(905,334,245,56),"Забрать"))if(TakeStoredItem(store,slot))inventorySelection=-1;}
  }
  string StorageTitle(int store)=>homes[store/3].labels[store%3];
  void DrawFind(){if(Time.unscaledTime>findUntil||findUntil==0||suppliesOpen)return;Panel(new Rect(440,480,410,62));if(findKind>0)GUI.DrawTexture(new Rect(448,484,54,54),ItemIcon(findKind),ScaleMode.ScaleToFit,true);GUI.Label(new Rect(findKind>0?515:455,493,325,42),findText,small);}
  void DressScavenging(Home h,int id){
   if(id!=5&&id!=6)return;
   void P(string name,Vector3 p,Vector3 size,Color c){Cube(name,V(h.p)+p,size,c);}
   var paper=new Color(.68f,.65f,.51f);var rust=new Color(.34f,.17f,.12f);
   if(id==5){
    P("Meal table",new Vector3(1,.66f,.3f),new Vector3(1.45f,.1f,.85f),new Color(.34f,.25f,.18f));
    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)P("Meal table leg",new Vector3(1+x*.6f,.4f,.3f+z*.3f),new Vector3(.08f,.5f,.08f),new Color(.29f,.22f,.16f));
    for(int side=-1;side<=1;side+=2){P("Dinner plate",new Vector3(1+side*.4f,.73f,.3f),new Vector3(.3f,.03f,.3f),paper);P("Leftover bread",new Vector3(1+side*.4f,.77f,.3f),new Vector3(.18f,.06f,.12f),new Color(.31f,.23f,.12f));}
    P("Spilled dinner",new Vector3(1,.72f,.3f),new Vector3(.24f,.015f,.18f),rust);
    P("Overturned chair seat",new Vector3(1.4f,.27f,-.65f),new Vector3(.5f,.12f,.5f),new Color(.31f,.24f,.17f));
    for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)P("Overturned chair leg",new Vector3(1.4f+x*.2f,.5f,-.65f+z*.2f),new Vector3(.06f,.42f,.06f),new Color(.31f,.24f,.17f));
    P("Overturned chair back",new Vector3(1.4f,.27f,-1.12f),new Vector3(.5f,.08f,.45f),new Color(.31f,.24f,.17f));
    for(int i=0;i<4;i++)P("Discarded clothing",new Vector3(-.7f+i*.35f,.21f,-2.8f+(i%2)*.35f),new Vector3(.34f,.035f,.28f),i%2==0?new Color(.22f,.3f,.28f):new Color(.35f,.3f,.23f));
   }else{
    P("Used dressing tray",new Vector3(-2,.84f,.3f),new Vector3(.4f,.03f,.27f),new Color(.4f,.45f,.43f));
    for(int i=0;i<3;i++)P("Used gauze",new Vector3(-2.1f+i*.1f,.87f,.3f),new Vector3(.08f,.02f,.14f),i==1?rust:paper);
    P("Patient coat left behind",new Vector3(.6f,.2f,-2.5f),new Vector3(.7f,.04f,.55f),new Color(.24f,.3f,.32f));
    for(int i=0;i<5;i++)P("Hasty footprints",new Vector3(.2f+(i%2)*.3f,.195f,-.3f-i*.55f),new Vector3(.12f,.008f,.25f),new Color(.24f,.24f,.19f));
   }
  }
 }
}
