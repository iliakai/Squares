using System;
using System.IO;
using UnityEngine;

namespace Squares
{
    [Serializable]
    public sealed class Profile
    {
        public int version = 1;
        public int flux, totalRuns, bestKills, bestSeconds, selectedCore;
        public int constellationMask, totalConstellations;
        public bool satelliteUnlocked, conductorUnlocked;
        public int vitality, learning, tempo;
        public bool sound = true, flashes = true;

        public bool Unlocked(int core) => core == 0 || (core == 1 && satelliteUnlocked) || (core == 2 && conductorUnlocked);
        public bool BuyCore(int core)
        {
            int cost = core == 1 ? 35 : 60;
            if (core < 1 || core > 2 || Unlocked(core) || flux < cost) return false;
            flux -= cost;
            if (core == 1) satelliteUnlocked = true; else conductorUnlocked = true;
            return true;
        }
        public int ResearchRank(int index) => index == 0 ? vitality : index == 1 ? learning : tempo;
        public int ResearchCost(int index) => 15 + ResearchRank(index) * 15;
        public bool BuyResearch(int index)
        {
            if (index < 0 || index > 2 || ResearchRank(index) >= 5 || flux < ResearchCost(index)) return false;
            flux -= ResearchCost(index);
            if (index == 0) vitality++; else if (index == 1) learning++; else tempo++;
            return true;
        }
        public void Validate()
        {
            version = 1; flux = Math.Max(0, Math.Min(10000000, flux));
            totalRuns = Math.Max(0, totalRuns); bestKills = Math.Max(0, bestKills); bestSeconds = Math.Max(0, bestSeconds);
            constellationMask &= (1 << Squares.Simulation.Constellations.All.Length) - 1;
            totalConstellations = Math.Max(0, totalConstellations);
            vitality = Math.Max(0, Math.Min(5, vitality)); learning = Math.Max(0, Math.Min(5, learning));
            tempo = Math.Max(0, Math.Min(5, tempo));
            if (selectedCore < 0 || selectedCore > 2 || !Unlocked(selectedCore)) selectedCore = 0;
        }
    }

    public sealed class SaveStore
    {
        public readonly string DirectoryPath;
        public string Error { get; private set; }
        public SaveStore(string directory) { DirectoryPath = directory; }
        private string PathName => Path.Combine(DirectoryPath, "profile.json");

        public Profile Load()
        {
            foreach (string path in new[] { PathName, PathName + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    string json = File.ReadAllText(path);
                    if (!json.TrimStart().StartsWith("{")) throw new FormatException("Invalid profile JSON");
                    Profile data = JsonUtility.FromJson<Profile>(json);
                    if (data == null) throw new FormatException("Empty profile");
                    data.Validate(); return data;
                }
                catch (Exception e) { Error = "Save recovery: " + e.Message; }
            }
            return new Profile();
        }

        public bool Save(Profile profile)
        {
            try
            {
                profile.Validate(); Directory.CreateDirectory(DirectoryPath);
                string temp = PathName + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(profile, true));
                if (File.Exists(PathName)) File.Replace(temp, PathName, PathName + ".bak");
                else File.Move(temp, PathName);
                Error = null; return true;
            }
            catch (Exception e) { Error = "Could not save: " + e.Message; return false; }
        }
    }
}
