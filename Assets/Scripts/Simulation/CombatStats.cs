using System;

namespace Squares.Simulation
{
    // One source for combat and the before/after numbers in the upgrade UI.
    public struct CombatStats
    {
        public float AllDamage, BulletDamage, ShotPeriod, OrbitDamage, ArcDamage, ArcPeriod;
        public float Speed, MagnetRadius, DashPeriod, Regen, OrbitRadius, OrbitSpeed, SatelliteSize;
        public float BlastRadius, BlastFraction, SlowFraction, SlowDuration, RamDamage, CriticalChance;
        public int Shots, PierceTargets, Satellites, ArcTargets;
        public static CombatStats Calculate(int[] r, Specialization specialization, float researchSpeed = 0)
        {
            float damage = (1 + r[(int)Module.Power] * .25f) * (1 + r[(int)Module.Overdrive] * .08f);
            float rate = 1 + r[(int)Module.Rate] * .18f;
            bool storm = r[(int)Module.Arc] >= 2 && r[(int)Module.Satellite] >= 2;
            int blast = r[(int)Module.Blast], frost = r[(int)Module.Frost], ram = r[(int)Module.Ram];
            return new CombatStats {
                AllDamage = damage,
                BulletDamage = r[(int)Module.Kinetic] > 0 ? (12 + (r[(int)Module.Kinetic] - 1) * 3.6f) * damage *
                    (specialization == Specialization.Siege ? 1.6f : specialization == Specialization.Storm ? .75f : 1) : 0,
                ShotPeriod = .48f / rate, Shots = 1 + r[(int)Module.Multishot], PierceTargets = 1 + r[(int)Module.Pierce],
                Satellites = r[(int)Module.Satellite],
                OrbitDamage = 16 * (1 + r[(int)Module.OrbitPower] * .45f) * damage * (storm ? 1.3f : 1) *
                    (specialization == Specialization.Storm ? 1.6f : 1),
                OrbitRadius = 1.65f + r[(int)Module.OrbitRadius] * .4f,
                OrbitSpeed = 2.2f + r[(int)Module.OrbitPower] * .22f,
                SatelliteSize = .32f + r[(int)Module.OrbitRadius] * .035f,
                ArcDamage = r[(int)Module.Arc] > 0 ? (14 + (r[(int)Module.Arc] - 1) * 4.9f) * damage *
                    (specialization == Specialization.Storm ? 1.4f : 1) : 0,
                ArcPeriod = 1.2f / rate, ArcTargets = 2 + r[(int)Module.Chain] + (storm ? 2 : 0),
                Speed = 5.2f * (1 + r[(int)Module.Thrusters] * .1f + researchSpeed) *
                    (specialization == Specialization.Scout ? 1.25f : specialization == Specialization.Siege ? .85f : 1),
                MagnetRadius = 2.2f + r[(int)Module.Magnet] + (specialization == Specialization.Scout ? 1.5f : 0),
                DashPeriod = 2.6f / (1 + r[(int)Module.Dash] * .24f), Regen = r[(int)Module.Regen] * .6f,
                CriticalChance = r[(int)Module.Critical] * .1f,
                BlastRadius = blast > 0 ? 1.2f + blast * .2f : 0,
                BlastFraction = blast > 0 ? .35f + (blast - 1) * .15f : 0,
                SlowFraction = frost > 0 ? .3f + (frost - 1) * .1f : 0,
                SlowDuration = frost > 0 ? 1.5f + frost * .4f : 0,
                RamDamage = ram > 0 ? (40 + (ram - 1) * 25) * damage : 0
            };
        }
    }
}
