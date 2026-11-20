#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckWardrobe(){
   string folder=Environment.GetEnvironmentVariable("GOIDA_WARDROBE_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"wardrobe-"+Guid.NewGuid()+".json");playing=true;paused=false;Application.runInBackground=true;
   var results=new System.Collections.Generic.List<string>();
   void Require(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"wardrobe-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   foreach(var z in infected)z.hp=0;hero.p=new Vector2(-12,-13);inventory.Load(new DistrictInventoryData());UpdateClothing();yield return new WaitForSeconds(1);
   bool Visible(string name){foreach(var mesh in hero.root.GetComponentsInChildren<SkinnedMeshRenderer>(true))if(mesh.name==name)return mesh.gameObject.activeInHierarchy;return false;}
   Vector2 stopped=Move(homes[5].p+Vector2.down*5,Vector2.up*4);Require(stopped.y<homes[5].p.y-4.1f,"large-frame motion cannot tunnel through a closed door");
   Require(hero.visual.UsesAdultModel&&Visible("Body")&&Visible("Shorts")&&!Visible("Shirt")&&!Visible("Trousers")&&!Visible("Shoes"),"fresh hero uses new adult mesh wearing only shorts");
   Require(infected.TrueForAll(z=>z.visual.UsesAdultModel&&z.visual.Ready),"all infected use new adult meshes and skeletal clips");
   var axe=Resources.Load<Mesh>("Survival/Human/Axe");Require(axe&&axe.bounds.size.magnitude<1.2f,"adult axe has human-scale dimensions");
   hero.root.rotation=Quaternion.Euler(0,225,0);yield return CaptureGameCamera(Path.Combine(folder,"wardrobe-start.png"),2.8f);
   for(int i=0;i<3;i++){
    hero.p=clothingPositions[i];Require(UseClothingFind(),"clothing pickup "+i);int slot=Array.FindIndex(inventory.Data.slots,v=>v.kind==11+i);Require(slot>=0&&inventory.Equip(slot),"equip found clothing "+i);UpdateClothing();Require(!UseClothingFind(),"clothing cannot be collected twice "+i);
   }
   hero.p=new Vector2(-12,-13);yield return new WaitForSeconds(1);Require(Visible("Shirt")&&Visible("Trousers")&&Visible("Shoes")&&!Visible("Shorts"),"equipped garments are visible and shorts cannot protrude through trousers");
   yield return CaptureGameCamera(Path.Combine(folder,"wardrobe-equipped.png"),2.8f);
   inventory.Data.trousers=false;UpdateClothing();bool shortsRestored=Visible("Shorts")&&!Visible("Trousers");inventory.Data.trousers=true;UpdateClothing();Require(shortsRestored&&!Visible("Shorts")&&Visible("Trousers"),"removing and re-equipping trousers switches the underlying shorts correctly");
   Save();string snapshot=File.ReadAllText(SavePath);inventory.Data.shirt=false;Require(Load()&&inventory.Data.shirt&&inventory.Data.clothingFound==7,"v8 save restores clothing equipment and world pickups");
   var old=JsonUtility.FromJson<SaveData>(snapshot);old.version=7;PopulateLegacyTestEnemies(old);old.inventory.shirt=old.inventory.trousers=old.inventory.shoes=false;old.inventory.clothingFound=0;File.WriteAllText(SavePath,JsonUtility.ToJson(old));Require(Load()&&inventory.Data.shirt&&inventory.Data.trousers&&inventory.Data.shoes&&inventory.Data.clothingFound==7,"v7 migration keeps formerly dressed survivor clothed");
   var bad=JsonUtility.FromJson<SaveData>(snapshot);bad.inventory.clothingFound=0;File.WriteAllText(SavePath,JsonUtility.ToJson(bad));File.WriteAllText(SavePath+".bak","broken");Require(!Load()&&inventory.Data.shirt,"invalid equipped clothing is rejected before world mutation");File.WriteAllText(SavePath,snapshot);Require(Load(),"valid v8 restores after rejected payload");
   var bag=new DistrictInventory();bag.TryAdd(DistrictItemKind.Shirt,1);bag.Equip(0);for(int i=0;i<6;i++)bag.TryAdd(DistrictItemKind.Water,3);Require(!bag.UnequipClothing(11)&&bag.Data.shirt,"full pockets cannot discard equipped clothing");
   bag.Data.slots[0]=default;Require(bag.UnequipClothing(11)&&!bag.Data.shirt&&bag.Count(DistrictItemKind.Shirt)==1,"removing clothing returns one item to a free pocket");
   Application.Quit();
  }
 }
}
#endif
