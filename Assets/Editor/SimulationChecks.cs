using System;
using System.IO;
using Squares.Simulation;
using UnityEngine;

namespace Squares.Editor
{
    public static class SimulationChecks
    {
        private static int assertions;
        private static void Check(bool value, string message)
        { assertions++; if (!value) throw new Exception("SQUARES_CHECK_FAILED: " + message); }

        public static void Run()
        {
            assertions = 0;
            var a = new World(12035);
            var b = new World(12035);
            for (int i = 0; i < 9000; i++)
            {
                if (a.Phase == RunPhase.Draft)
                {
                    int pick = AutoPilot.Pick(a);
                    Check(a.Choices[0] != a.Choices[1] && a.Choices[1] != a.Choices[2] && a.Choices[0] != a.Choices[2], "Unique draft choices");
                    int tick = a.Ticks; float hull = a.Hull;
                    a.Step(new Command(new V2(1, 1), true, true));
                    Check(a.Ticks == tick && a.Hull == hull, "Draft freezes simulation");
                    Check(!a.Choose(-1) && !a.Choose(3), "Invalid draft indices rejected");
                    Module selected = a.Choices[pick]; int rank = a.Rank(selected);
                    Check(a.Choose(pick) && b.Choose(pick), "Valid choice applies");
                    Check(a.Rank(selected) == rank + (selected == Module.Repair ? 0 : 1), "One rank per choice");
                }
                Command input = AutoPilot.Input(a);
                a.Step(input); b.Step(input);
                Check(a.Fingerprint() == b.Fingerprint(), "Seeded replay at input " + i);
                Check(Math.Abs(a.Position.X) <= World.HalfWidth && Math.Abs(a.Position.Y) <= World.HalfHeight, "Arena bounds");
                Check(a.Enemies.Count <= World.EnemyCap && a.Bullets.Count <= World.BulletCap && a.Pickups.Count <= World.PickupCap, "Entity caps");
                Check(a.Hull >= 0 && a.Hull <= a.MaxHull && a.Charge >= 0 && a.Charge <= 100, "Resource bounds");
            }
            Check(a.Kills > 20 && a.Level > 3, "Combat and progression occur");
            var dash = new World(1);
            dash.Step(new Command(new V2(1, 0), true, true));
            Check(dash.DashTime > 0 && dash.DashCooldown > 0 && dash.Invincible > 0, "Dash activates");
            Check(dash.OverloadTime == 0 && dash.Charge == 0, "Uncharged overload rejected");
            float cooldown = dash.DashCooldown;
            dash.Step(new Command(new V2(1, 0), true));
            Check(dash.DashCooldown < cooldown, "Holding dash does not bypass cooldown");
            var diagonal = new World(2); var cardinal = new World(2);
            diagonal.Step(new Command(new V2(1, 1))); cardinal.Step(new Command(new V2(1, 0)));
            Check(Math.Abs(diagonal.Position.Length - cardinal.Position.Length) < .0001f, "Normalized diagonal");
            for (int i = 0; i < (int)Module.Count; i++)
            {
                var ranks = new int[(int)Module.Count];
                for (int j = 0; j < ranks.Length; j++) ranks[j] = 1;
                int cap = Upgrades.All[i].MaxRank;
                if (cap > 0) { ranks[i] = cap; Check(!Upgrades.Available((Module)i, ranks), "Rank cap " + (Module)i); }
            }
            var fallbackRanks = new int[(int)Module.Count];
            foreach (Upgrade u in Upgrades.All) fallbackRanks[(int)u.Id] = u.MaxRank;
            int fallbacks = 0;
            foreach (Upgrade u in Upgrades.All) if (Upgrades.Available(u.Id, fallbackRanks)) fallbacks++;
            // A maxed-out build still needs three distinct options.
            Check(fallbacks >= 3, "Three choices remain for a capped build");
            CheckBossCombat();
            CheckPersistence();
            Debug.Log("SQUARES_CHECKS_PASS assertions=" + assertions);
        }

        private static void CheckBossCombat()
        {
            bool sawBoss = false, usedOverload = false, died = false;
            int bossKills = 0, totalKills = 0;
            // Repeatable normal input only; research settings are legitimate fully purchased ranks.
            foreach (uint seed in new uint[] { 735193, 12035, 89411 })
            {
                var w = new World(seed, 1, 5, 5, 5);
                for (int i = 0; i < 24000 && w.Phase != RunPhase.Dead; i++)
                {
                    if (w.Phase == RunPhase.Draft) w.Choose(AutoPilot.Pick(w));
                    w.Step(AutoPilot.Input(w));
                    if (w.OverloadTime > 0) usedOverload = true;
                    foreach (Enemy e in w.Enemies) if (e.Kind == EnemyKind.Boss) sawBoss = true;
                    Check(w.Enemies.Count <= World.EnemyCap && w.Bullets.Count <= World.BulletCap && w.Pickups.Count <= World.PickupCap, "Long combat caps");
                }
                bossKills += w.Bosses; totalKills += w.Kills;
                Debug.Log($"SQUARES_BOT seed={seed} seconds={w.Time:F1} kills={w.Kills} level={w.Level} bosses={w.Bosses} hull={w.Hull:F1}");
            }
            // A stationary reactor must eventually die, and death must stop all future ticks.
            var stationary = new World(311);
            for (int i = 0; i < 18000 && stationary.Phase != RunPhase.Dead; i++)
            {
                if (stationary.Phase == RunPhase.Draft) stationary.Choose(0);
                stationary.Step(default);
            }
            died = stationary.Phase == RunPhase.Dead;
            int finalTick = stationary.Ticks; stationary.Step(new Command(new V2(1, 1), true, true));
            Check(died && stationary.Ticks == finalTick, "Death and halted simulation");
            Check(sawBoss && totalKills > 200, "Automated combat reaches a boss");
            Check(usedOverload, "Kills charge and trigger overload");
            Check(bossKills > 0, "A legal build can defeat the boss");
        }

        private static void CheckPersistence()
        {
            string directory = Path.GetFullPath("Temp/ProfileChecks");
            var store = new SaveStore(directory);
            var profile = new Profile { flux = 100, bestKills = 123 };
            Check(profile.BuyCore(1) && profile.flux == 65, "Unlock purchase");
            Check(!profile.BuyCore(1) && profile.flux == 65, "Duplicate unlock rejected");
            Check(profile.BuyResearch(0) && profile.vitality == 1 && profile.flux == 50, "Research purchase");
            Check(!profile.BuyCore(2) && profile.flux == 50, "Overspend rejected");
            Check(!profile.BuyResearch(-1) && !profile.BuyResearch(3), "Invalid research rejected");
            Check(store.Save(profile), "First save");
            Check(store.Load().bestKills == 123 && store.Load().satelliteUnlocked, "Save roundtrip");
            profile.bestKills = 456;
            Check(store.Save(profile) && store.Load().bestKills == 456, "Atomic replacement");
            File.WriteAllText(Path.Combine(directory, "profile.json"), "broken");
            Check(store.Load().bestKills == 123, "Corrupt primary recovers backup");
            profile.flux = -5; profile.selectedCore = 15; profile.vitality = 100; profile.Validate();
            Check(profile.flux == 0 && profile.selectedCore == 0 && profile.vitality == 5, "Validate save ranges");
        }
    }
}
