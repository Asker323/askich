using System;
using System.IO;
using UnityEngine;

namespace Askich
{
    /// <summary>Local progression, kept separate from temporary combat stats.</summary>
    [Serializable]
    public sealed class AskichProfile
    {
        public int version = 1;
        public int credits;
        public int unlockedSkins = 1;
        public int selectedSkin;
        public int bestScore;

        // Development tests redirect storage before scene Awake; normal players use their own profile.
        public static string SavePathOverride;
        public static string StoragePath => string.IsNullOrEmpty(SavePathOverride)
            ? Path.Combine(Application.persistentDataPath, "profile.json") : SavePathOverride;

        public void Validate()
        {
            version = 1;
            credits = Mathf.Clamp(credits, 0, 10000000);
            bestScore = Mathf.Clamp(bestScore, 0, 100000000);
            unlockedSkins = (unlockedSkins & 7) | 1;
            if (selectedSkin < 0 || selectedSkin > 2 || (unlockedSkins & (1 << selectedSkin)) == 0)
                selectedSkin = 0;
        }

        public static AskichProfile Load()
        {
            try
            {
                if (!File.Exists(StoragePath)) return new AskichProfile();
                var profile = JsonUtility.FromJson<AskichProfile>(File.ReadAllText(StoragePath));
                if (profile == null) return new AskichProfile();
                profile.Validate();
                return profile;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Debug.LogWarning("Askich profile could not be read: " + error.GetType().Name);
                return new AskichProfile();
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
