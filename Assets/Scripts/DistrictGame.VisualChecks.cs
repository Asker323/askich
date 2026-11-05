#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace Askich {
 public sealed partial class DistrictGame {
  IEnumerator CheckVisualPreview(){
   string folder=Environment.GetEnvironmentVariable("GOIDA_VISUAL_CHECK");Directory.CreateDirectory(folder);testSave=Path.Combine(Application.temporaryCachePath,"berezki-visual-"+Guid.NewGuid()+".json");Application.runInBackground=true;playing=true;paused=false;
   var results=new System.Collections.Generic.List<string>();
   void Require(bool ok,string message){results.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(folder,"visual-checks.txt"),results);if(!ok){Application.Quit(1);throw new Exception(message);}}
   yield return new WaitForSeconds(.5f);yield return CheckVisualPolish(Require,folder);Application.Quit();
  }
  IEnumerator CheckVisualPolish(Action<bool,string> check,string folder){
   check(surfaceShader&&surfaceShader.isSupported&&surfaceMaterials.Count>3,"world materials compile for walls, floors, roofs and terrain");
   var canopy=GameObject.Find("Individual wind leaves");
   check(canopy&&canopy.GetComponent<MeshFilter>().sharedMesh.vertexCount>1000&&canopy.GetComponent<Renderer>().sharedMaterial.shader.isSupported,"branching trees have supported individual-leaf canopy meshes");
   var block=new MaterialPropertyBlock();infected[0].root.GetComponentInChildren<SkinnedMeshRenderer>().GetPropertyBlock(block);
   yield return new WaitForEndOfFrame();check(infected[0].visual.UsesAdultModel&&Mathf.Abs(infected[0].visual.HeadScaleRatio-1)<.01f,"new anatomically proportioned infected are active at end of frame");
   check(block.GetFloat("_Decay")>.8f,"infected carry individual decayed material treatment");
   check(Resources.Load<AudioClip>("Survival/radio")==null&&Resources.Load<AudioClip>("Survival/Radio/message0")==null,"synthetic radio speech is absent from packaged resources");
   CancelMedical();hero.p=new Vector2(0,-23);heroVelocity=Vector2.zero;
   foreach(var z in infected)z.hp=0;yield return new WaitForSeconds(1);
   yield return CaptureGameCamera(Path.Combine(folder,"polish-street.png"));
   hero.p=new Vector2(6,-24);yield return new WaitForSeconds(1);int index=0;
   foreach(var z in infected){if(index>=3)break;z.visual.Restore(false);z.root.position=V(hero.p+new Vector2((index-1)*1.8f,1.8f));z.root.rotation=Quaternion.Euler(0,225,0);index++;}
   yield return new WaitForSeconds(.2f);
   yield return CaptureGameCamera(Path.Combine(folder,"polish-infected.png"),3.5f);
   var tree=FindObjectsByType<DistrictTreeSway>(FindObjectsSortMode.None)[0];hero.p=new Vector2(tree.transform.position.x,tree.transform.position.z)-Vector2.up*2;yield return new WaitForSeconds(1);
   yield return CaptureGameCamera(Path.Combine(folder,"polish-trees.png"),6);
  }
 }
}
#endif
