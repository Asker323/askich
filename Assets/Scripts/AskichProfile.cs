using System;
using System.IO;
using UnityEngine;

namespace Askich
{
    /// <summary>Local progression, kept separate from temporary combat stats.</summary>
    [Serializable]
    public sealed class AskichProfile
    {
        public int version = 4;
        public int tutorialFlags;
        public float effectsVolume = .7f, ambienceVolume = .35f;
        public bool lowEffects;
        public int credits;
        public int unlockedSkins = 1;
        public int selectedSkin;
        public int bestScore;
        public int selectedFaction;
        public int completedFactions;
        public int ownedItems;
        public int[] equippedItems = new int[9];
        public Settlement[] settlements = new Settlement[3];

        // Development tests redirect storage before scene Awake; normal players use their own profile.
        public static string SavePathOverride;
        public static string StoragePath => string.IsNullOrEmpty(SavePathOverride)
            ? Path.Combine(Application.persistentDataPath, "profile.json") : SavePathOverride;

        public void Validate()
        {
            if(version<4){effectsVolume=.7f;ambienceVolume=.35f;}
            version = 4;
            effectsVolume = float.IsNaN(effectsVolume)? .7f : Mathf.Clamp01(effectsVolume);
            ambienceVolume = float.IsNaN(ambienceVolume)? .35f : Mathf.Clamp01(ambienceVolume);
            selectedFaction = Mathf.Clamp(selectedFaction, 0, 2);
            completedFactions &= 7;
            ownedItems &= 511;
            if(equippedItems==null||equippedItems.Length!=9) Array.Resize(ref equippedItems,9);
            for(int i=0;i<9;i++) if(equippedItems[i]!=i+1||(ownedItems&(1<<i))==0) equippedItems[i]=0;
            if(settlements==null||settlements.Length!=3) Array.Resize(ref settlements,3);
            for(int i=0;i<3;i++) {if(settlements[i]==null)settlements[i]=new Settlement();settlements[i].Validate();}
            credits = Mathf.Clamp(credits, 0, 10000000);
            bestScore = Mathf.Clamp(bestScore, 0, 100000000);
            unlockedSkins = (unlockedSkins & 7) | 1;
            if (selectedSkin < 0 || selectedSkin > 2 || (unlockedSkins & (1 << selectedSkin)) == 0)
                selectedSkin = 0;
        }

        public static bool ImportLegacy(string source, string destination)
        {
            if(File.Exists(destination)||!File.Exists(source))return false;
            var old=JsonUtility.FromJson<AskichProfile>(File.ReadAllText(source));
            if(old==null)return false;
            old.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            // Copy only after parsing. Never modify or remove the original Askich save.
            File.WriteAllText(destination+".migration",JsonUtility.ToJson(old,true));
            File.Move(destination+".migration",destination);return true;
        }

        public static AskichProfile Load()
        {
            try
            {
                if(string.IsNullOrEmpty(SavePathOverride) && Application.productName=="GOIDA")
                {
                    string parent=Path.GetDirectoryName(Application.persistentDataPath);
                    ImportLegacy(Path.Combine(parent,"Askich","profile.json"),StoragePath);
                }
                if (!File.Exists(StoragePath)) {var fresh = new AskichProfile();fresh.Validate();return fresh;}
                var profile = JsonUtility.FromJson<AskichProfile>(File.ReadAllText(StoragePath));
                if (profile == null) {var fresh = new AskichProfile();fresh.Validate();return fresh;}
                profile.Validate();
                return profile;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Debug.LogWarning("Askich profile could not be read: " + error.GetType().Name);
                var fresh = new AskichProfile(); fresh.Validate(); return fresh;
            }
        }

        public bool Save()
        {
            Validate();
            string temporary = StoragePath + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StoragePath));
                File.WriteAllText(temporary, JsonUtility.ToJson(this, true));
                if (File.Exists(StoragePath)) File.Replace(temporary, StoragePath, null);
                else File.Move(temporary, StoragePath);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Debug.LogWarning("Askich progress could not be saved: " + error.GetType().Name);
                return false;
            }
        }
    }
}
