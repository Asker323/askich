#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;
namespace Askich
{
    // A separate profile prepared with normal economy commands for real mouse UI checks.
    // No resource grants. The player's own profile is never opened or overwritten.
    public static class ManualVillageCheck
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Prepare()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-askich-ui-check")<0)return;
            AskichProfile.SavePathOverride=Path.Combine(Path.GetTempPath(),"goida-ui-"+Guid.NewGuid().ToString("N")+".json");
            var profile=new AskichProfile();profile.Validate();var t=profile.settlements[0];
            int[] plots={0,1,2,3,4,5,7,8},types={3,5,2,6,7,8,1,9};
            for(int i=0;i<plots.Length;i++)
            {
                int guard=0;while(t.BuildReason(plots[i],types[i])!=""&&guard++<200)t.AdvanceDay();
                if(!t.Build(plots[i],types[i]))throw new InvalidOperationException("UI check setup cannot progress");
                while(t.pendingPlot>=0)t.AdvanceDay();
            }
            while(t.wood<200)t.AdvanceDay();
            if(!profile.Save())throw new IOException("Cannot isolate UI check profile");
        }
    }
}
#endif
