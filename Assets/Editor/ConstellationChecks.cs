using System;
using System.Collections.Generic;
using System.Diagnostics;
using Squares.Simulation;
using UnityEngine;

namespace Squares.Editor
{
    public static class ConstellationChecks
    {
        private static int assertions;
        private static void Check(bool condition, string message)
        { assertions++; if (!condition) throw new Exception("CONSTELLATION_CHECK_FAILED: " + message); }

        private static List<Pickup> Figure(int patternIndex, float span, float rotation, V2 center, bool mirror, float jitter = 0)
        {
            var result = new List<Pickup>();
            var random = new SeededRandom((uint)(patternIndex + 31));
            V2[] points = Constellations.All[patternIndex].Points;
            for (int i = 0; i < points.Length; i++)
            {
                V2 p = points[i]; if (mirror) p.X *= -1;
                p = center + V2.Rotate(p * span, rotation);
                p += new V2(random.Value() - .5f, random.Value() - .5f) * (span * jitter);
                result.Add(new Pickup { Id = 10000 + i, Position = p, Value = 3L * (i + 1) });
            }
            return result;
        }
        public static void Run()
        {
            assertions = 0; var matcher = new ConstellationMatcher();
            for (int index = 0; index < Constellations.All.Length; index++)
            {
                ConstellationPattern pattern = Constellations.All[index];
                Check(pattern.Points.Length >= 5 && pattern.Points.Length <= 20, "Main star count: " + pattern.Name);
                foreach (int endpoint in pattern.Edges) Check(endpoint >= 0 && endpoint < pattern.Points.Length, "Valid diagram edges");
                for (int transform = 0; transform < 3; transform++)
                {
                    List<Pickup> pickups = Figure(index, 4 + transform * 3, .4f + transform * .8f, new V2(-5, 2), transform % 2 == 1, .018f);
                    // Unrelated stars are outside the local figure and must not be consumed.
                    for (int n = 0; n < 8; n++) pickups.Add(new Pickup { Id = 20000 + n, Position = new V2(14 + n * .3f, -8), Value = 3 });
                    ConstellationDiscovery match = matcher.Find(pickups, new V2(30, 30), 0, 0);
                    Check(match != null && match.PatternIndex == index, "Noisy rotated/mirrored figure: " + pattern.Name);
                    Check(match.PickupIds.Length == pattern.Points.Length, "One pickup per star");
                    var ids = new HashSet<int>(match.PickupIds);
                    Check(ids.Count == pattern.Points.Length, "No repeated pickup correspondence");
                    long sum = 0; foreach (Pickup p in pickups) if (ids.Contains(p.Id)) sum += p.Value;
                    Check(match.Reward == sum * 10 && match.OriginalValue == sum, "Exact sum times ten");
                }
            }
            List<Pickup> missing = Figure(0, 6, .1f, default, false); missing.RemoveAt(2);
            Check(matcher.Find(Figure(0, 30, .04f, default, false, .004f), new V2(30, 30), 0, 0)?.PatternIndex == 0,
                "Large constellation spanning most of the arena is recognized");
            Check(matcher.Find(missing, default, 0, 0, holdingExperience: true) == null, "Incomplete W rejected");
            List<Pickup> stretched = Figure(0, 4, 0, default, false);
            foreach (Pickup p in stretched) p.Position = new V2(p.Position.X * 2.5f, p.Position.Y * .3f);
            Check(matcher.Find(stretched, default, 0, 0, holdingExperience: true) == null, "Nonuniform stretched figure rejected");
            var line = new List<Pickup>();
            for (int i = 0; i < 12; i++) line.Add(new Pickup { Id = i, Position = new V2(i * .7f, 0), Value = 3 });
            Check(matcher.Find(line, default, 0, 0, holdingExperience: true) == null, "A line is not a constellation");
            List<Pickup> bigValues = Figure(0, 6, .5f, new V2(8, 3), false);
            foreach (Pickup p in bigValues) p.Value = 3000000000L;
            Check(matcher.Find(bigValues, default, 0, 0).Reward == 150000000000L, "Reward arithmetic exceeds 32-bit safely");
            foreach (Pickup p in bigValues) p.Celestial = true;
            Check(matcher.Find(bigValues, default, 0, 0) == null, "Giant rewards cannot be re-multiplied");
            CheckWorldIntegration();
            Stress(matcher);
            UnityEngine.Debug.Log("SQUARES_CONSTELLATION_CHECKS_PASS assertions=" + assertions);
        }

        private static void CheckWorldIntegration()
        {
            var world = new World(903);
            world.Pickups.AddRange(Figure(0, 6, .3f, default, false, .01f));
            var unrelated = new Pickup { Id = 30000, Position = new V2(16, -8), Value = 27 };
            world.Pickups.Add(unrelated);
            for (int i = 0; i < 30; i++) world.Step(new Command(default, holdExperience: true));
            Check(world.ConstellationsFound == 1 && world.LastConstellation != null, "World discovers a real pickup layout");
            Check(world.LastConstellation.OriginalValue == 45 && world.LastConstellation.Reward == 450, "World reward is exactly 45 × 10");
            Check(world.Pickups.Count == 2 && world.Pickups.Contains(unrelated), "Only matched stars are removed");
            Pickup reward = world.Pickups.Find(p => p.Celestial);
            Check(reward != null && reward.Value == 450 && reward.AvailableTick > world.Ticks, "Single large pickup with appearance grace");
            for (int i = 0; i < 60; i++) world.Step(default);
            Check(world.XP == 0 && world.Pickups.Contains(reward), "Grace prevents instantly hiding the reward");
            while (world.Ticks < 190 && world.Phase == RunPhase.Running) world.Step(default);
            Check(!world.Pickups.Contains(reward) && world.Phase == RunPhase.Draft, "Reward can be collected and levels up normally");
            Check(world.CelestialPickupsCollected == 1 && world.CelestialExperienceCollected == 450, "Collection telemetry counts once");
            Check(Math.Abs(world.XP + (7 + 4 + .3f) - 450) < .01f, "Reward credited exactly once before level choice");
            float xp = world.XP; world.Step(default);
            Check(world.XP == xp && world.ConstellationsFound == 1, "Paused draft cannot credit it again");
            int levels = 0;
            while (world.Phase == RunPhase.Draft && levels++ < 50) world.Choose(AutoPilot.Pick(world));
            Check(world.Phase == RunPhase.Running && world.Level >= 8, "Large XP preserves queued level-ups and specialization");
            var learning = new World(904); learning.Ranks[(int)Module.Learning] = 2;
            learning.Pickups.AddRange(Figure(0, 6, .3f, default, false));
            for (int i = 0; i < 30; i++) learning.Step(new Command(default, holdExperience: true));
            while (learning.Ticks < 190 && learning.Phase == RunPhase.Running) learning.Step(default);
            Check(learning.CelestialExperienceCreated == 450 && Math.Abs(learning.XP + 11.3f - 585) < .01f,
                "Learning bonus applied once on collection, after exact base tenfold reward");
        }

        private static void Stress(ConstellationMatcher matcher)
        {
            var random = new SeededRandom(184711);
            var pickups = new List<Pickup>();
            for (int i = 0; i < World.PickupCap; i++) pickups.Add(new Pickup { Id = i + 1,
                Position = new V2((random.Value() * 2 - 1) * 19, (random.Value() * 2 - 1) * 9), Value = 3 });
            var watch = Stopwatch.StartNew(); int matches = 0;
            for (int i = 0; i < 100; i++)
            {
                if (matcher.Find(pickups, default, 2.2f, i, i * Constellations.AnchorBudget) != null) matches++;
                Check(matcher.LastAnchorTests <= Constellations.All.Length * Constellations.AnchorBudget, "Bounded anchor work");
            }
            watch.Stop();
            UnityEngine.Debug.Log($"SQUARES_CONSTELLATION_STRESS scans=100 pickups={pickups.Count} total_ms={watch.Elapsed.TotalMilliseconds:F1} matches={matches}");
        }
    }
}
