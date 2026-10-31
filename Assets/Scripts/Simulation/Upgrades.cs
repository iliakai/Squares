using System;

namespace Squares.Simulation
{
    public enum Module
    {
        Kinetic, Power, Rate, Multishot, Pierce, Satellite, OrbitPower, OrbitRadius,
        Arc, Chain, Hull, Repair, Magnet, Thrusters, Dash, Critical, Regen, Learning, Overdrive, Capacitor, Count
    }

    public sealed class Upgrade
    {
        public readonly Module Id;
        public readonly string Name, Category, Description;
        public readonly int MaxRank;
        public Upgrade(Module id, string name, string category, string description, int maxRank)
        { Id = id; Name = name; Category = category; Description = description; MaxRank = maxRank; }
    }

    public static class Upgrades
    {
        public static readonly Upgrade[] All = {
            new Upgrade(Module.Kinetic, "KINETIC ARRAY", "WEAPON", "Unlock / +30% projectile damage.", 5),
            new Upgrade(Module.Power, "DENSE AMMO", "KINETIC", "+25% damage to every weapon.", 5),
            new Upgrade(Module.Rate, "RAPID CYCLE", "KINETIC", "+18% kinetic and arc fire rate.", 5),
            new Upgrade(Module.Multishot, "FRACTAL SHOT", "KINETIC", "+1 projectile per burst. Wider fan.", 4),
            new Upgrade(Module.Pierce, "PHASE ROUNDS", "KINETIC", "Projectiles pierce one more enemy.", 3),
            new Upgrade(Module.Satellite, "SATELLITE", "WEAPON", "+1 orbiting square that deals contact damage.", 5),
            new Upgrade(Module.OrbitPower, "HEAVY ORBIT", "SATELLITE", "+45% satellite damage and +10% speed.", 5),
            new Upgrade(Module.OrbitRadius, "WIDE ORBIT", "SATELLITE", "+0.4 orbit radius and larger satellites.", 3),
            new Upgrade(Module.Arc, "ARC RELAY", "WEAPON", "Unlock / +35% lightning damage. Ignores armor.", 5),
            new Upgrade(Module.Chain, "CHAIN REACTION", "ARC", "Lightning jumps to one additional enemy.", 5),
            new Upgrade(Module.Hull, "REINFORCED HULL", "SURVIVAL", "+20 max hull and restore 30 hull.", 5),
            new Upgrade(Module.Repair, "FIELD REPAIR", "SURVIVAL", "Restore 40% of maximum hull immediately.", 0),
            new Upgrade(Module.Magnet, "DATA VACUUM", "UTILITY", "+1.0 pickup radius. Collect from safety.", 4),
            new Upgrade(Module.Thrusters, "VECTOR DRIVE", "UTILITY", "+10% movement speed.", 4),
            new Upgrade(Module.Dash, "PHASE DRIVE", "UTILITY", "+24% dash recharge rate per rank.", 4),
            new Upgrade(Module.Critical, "FAULT FINDER", "DAMAGE", "+10% chance of a double-damage hit.", 5),
            new Upgrade(Module.Regen, "SELF REPAIR", "SURVIVAL", "Regenerate 0.6 hull per second.", 4),
            new Upgrade(Module.Learning, "DATA MINING", "UTILITY", "+15% experience from data squares.", 4),
            new Upgrade(Module.Overdrive, "LIMIT BREAK", "ENDLESS", "+8% all damage. No rank limit.", 0),
            new Upgrade(Module.Capacitor, "FLUX CAPACITOR", "ENDLESS", "+8% overload charge per kill. No rank limit.", 0)
        };

        public static bool Available(Module id, int[] ranks)
        {
            Upgrade u = All[(int)id];
            if (u.MaxRank > 0 && ranks[(int)id] >= u.MaxRank) return false;
            if ((id == Module.Multishot || id == Module.Pierce) && ranks[(int)Module.Kinetic] == 0) return false;
            if ((id == Module.OrbitPower || id == Module.OrbitRadius) && ranks[(int)Module.Satellite] == 0) return false;
            if (id == Module.Chain && ranks[(int)Module.Arc] == 0) return false;
            return true;
        }
    }
}
