using UnityEngine;
namespace Askich
{
    public sealed class WarriorMotion : MonoBehaviour
    {
        public float baseScale=1;
        public bool moving,attacking;
        void LateUpdate()
        {
            float phase=Time.time*(moving?11:2.5f),stretch=attacking?.025f:Mathf.Sin(phase)*(moving?.012f:.004f);
            transform.localScale=new Vector3(baseScale*(1-stretch),baseScale*(1+stretch),baseScale);
            transform.localRotation=Quaternion.Euler(0,0,moving&&!attacking?Mathf.Sin(phase)*1.2f:0);
        }
    }
}
