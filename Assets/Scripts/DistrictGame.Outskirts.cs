using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  const float West=-58,East=118,South=-58,North=58;
  int legacyEnemyCount;
  bool depotLooted;
  readonly Vector2 depot=new Vector2(102,40);
  static bool InsideWorld(Vector2 p)=>p.x>=West&&p.x<=East&&p.y>=South&&p.y<=North;
  void BuildOutskirts(){
   for(int x=70;x<=110;x+=20)for(int z=-50;z<=50;z+=20)
    Cube("Outskirts ground",new Vector3(x,-.3f,z),new Vector3(20,.5f,20),new Color(.24f,.28f,.20f));
   for(int z=-24;z<=24;z+=24){
    for(int x=70;x<=110;x+=20)Cube("Continuous east road",new Vector3(x,-.02f,z),new Vector3(20,.08f,7),new Color(.17f,.19f,.20f));
    for(int x=64;x<118;x+=8)Cube("Worn road marking",new Vector3(x,.03f,z),new Vector3(2,.02f,.1f),new Color(.46f,.45f,.35f));
   }
   // The northern dirt track gives an alternative to the occupied road junctions.
   Cube("Northern dirt track",new Vector3(82,.005f,51),new Vector3(76,.03f,2.8f),new Color(.31f,.28f,.21f));
   for(int z=-36;z<=12;z+=24)for(int x=72;x<=96;x+=24){
    BuildHome(new Vector2(x,z),homes.Count);var h=homes[homes.Count-1];
    h.loot=x==72?new[]{"Еда","Пусто","Бинт"}:new[]{"Пусто","Копьё","Пусто"};
    h.note="ЗАПИСКА: «На восточной окраине остались припасы дорожников. Ищи груз во дворе севернее последних домов. К башне можно вернуться по грунтовке»";
    Cube("Outskirts note",V(h.p+new Vector2(-2,0),.79f),new Vector3(.4f,.015f,.3f),new Color(.74f,.7f,.56f));
   }
   Cube("Road service yard",new Vector3(101,.02f,39),new Vector3(20,.08f,16),new Color(.3f,.3f,.27f));
   DistrictAtmosphere.Spawn("Vehicle_Pickup",new Vector3(95,0,39),90);
   barriers.Add(new Barrier{rect=new Rect(92.5f,37.9f,5,2.2f)});
   for(int i=0;i<4;i++){DistrictAtmosphere.Spawn("Barrel",new Vector3(106+i%2,0,35+i/2),0);DistrictAtmosphere.Spawn("Pallet_Broken",new Vector3(99+i,0,44),i*17);}
   Cube("Road worker supply crate",V(depot,.6f),new Vector3(1.4f,1.2f,1),new Color(.35f,.3f,.19f));
   barriers.Add(new Barrier{rect=new Rect(depot.x-.7f,depot.y-.5f,1.4f,1)});
   Cube("Supply crate white marking",V(depot+Vector2.down*.51f,.7f),new Vector3(.65f,.13f,.025f),new Color(.73f,.71f,.6f));
   for(int i=0;i<18;i++){var p=new Vector3(63+i*3,0,i%2==0?56:46);DistrictAtmosphere.Tree(p,i+200);}
   // A visible perimeter replaces the old invisible edge; the east roads end here.
   Cube("East perimeter fence",new Vector3(119,1,0),new Vector3(.16f,2,120),new Color(.24f,.26f,.24f));
   for(int z=-56;z<=56;z+=8)Cube("Fence post",new Vector3(118.8f,1.2f,z),new Vector3(.25f,2.4f,.25f),new Color(.16f,.18f,.17f));
  }
  void SpawnOutskirts(){
   legacyEnemyCount=infected.Count;
   Vector2[] positions={new Vector2(67,-24),new Vector2(82,-24),new Vector2(109,-24),new Vector2(65,0),new Vector2(88,0),new Vector2(112,0),new Vector2(74,24),new Vector2(94,24),new Vector2(103,34)};
   for(int i=0;i<positions.Length;i++)if(!Blocked(positions[i]))infected.Add(MakePerson(positions[i],true,100+i));
  }
  bool UseDepot(){
   if(Vector2.Distance(hero.p,depot)>=2)return false;
   if(depotLooted){Say("Груз дорожников уже разобран.");return true;}
   var snapshot=JsonUtility.ToJson(inventory.Data);
   if(!inventory.TryAdd(DistrictItemKind.Food,2)||!inventory.TryAdd(DistrictItemKind.Bandage,1)){inventory.Load(JsonUtility.FromJson<DistrictInventoryData>(snapshot));Say("Освободи ячейки для груза.");return true;}
   depotLooted=true;Alert(depot,12);Save();Say("Груз дорожников: 2 еды и бинт. Шум ящика мог привлечь заражённых.");return true;
  }
 }
}
