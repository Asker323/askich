using UnityEngine;
namespace Askich {
public sealed class LivingScenery : MonoBehaviour {
public int kind;
SpriteRenderer view,ember;Vector3 scale;float seed;
void Start() {
view=GetComponent<SpriteRenderer>();scale=transform.localScale;seed=transform.position.x*3+transform.position.y*7;
if(kind==9)ember=NeonVisual.Make("Warm firelight","glow",new Color(1,.4f,.1f,.45f),new Vector2(0,.35f),Vector2.one*1.2f,view.sortingOrder-1,transform);
}
void Update() {
float t=Time.time+seed;
if(kind==3||kind==4){transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*.8f)*1.15f);transform.localScale=new Vector3(scale.x*(1+Mathf.Sin(t)*.008f),scale.y,scale.z);}
if(kind==9){float f=.88f+Mathf.Sin(t*11)*.08f+Mathf.Sin(t*17)*.04f;view.color=new Color(1,f,.8f*f);if(ember!=null)ember.color=new Color(1,.4f,.1f,.35f+Mathf.Sin(t*9)*.15f);}
if(kind==12)transform.localScale=new Vector3(scale.x*(1+Mathf.Sin(t*1.3f)*.01f),scale.y,scale.z);
}}
public sealed class DriftingLeaf : MonoBehaviour {
public Vector2 origin;public float phase;
void Update(){float t=Time.time*.4f+phase;transform.position=origin+new Vector2(Mathf.Repeat(t,4)-2,Mathf.Sin(t*2)*.25f);transform.rotation=Quaternion.Euler(0,0,t*60);}
}}
