using System;

namespace Squares.Simulation
{
    // Test input generator. It uses only public observations and normal player commands.
    public static class AutoPilot
    {
        public static Command Input(World w)
        {
            V2 target = new V2((float)Math.Cos(w.Time * .2f) * 12, (float)Math.Sin(w.Time * .2f) * 5.8f);
            float nearest = 10000;
            foreach (Pickup p in w.Pickups)
            {
                float d = V2.DistanceSquared(w.Position, p.Position);
                bool safe = true;
                foreach (Enemy e in w.Enemies)
                    if (V2.DistanceSquared(e.Position, p.Position) < 5) { safe = false; break; }
                if (safe && d < nearest) { nearest = d; target = p.Position; }
            }
            V2 movement = (target - w.Position).Normalized;
            float danger = 100;
            foreach (Enemy e in w.Enemies)
            {
                V2 away = w.Position - e.Position;
                float d = away.Length;
                danger = Math.Min(danger, d);
                if (d < 3.6f) movement += away.Normalized * (3.6f - d) * 1.4f;
            }
            foreach (Bullet b in w.Bullets)
            {
                if (!b.Hostile) continue;
                V2 away = w.Position - b.Position;
                if (away.Length < 2) movement += away.Normalized * (2 - away.Length) * 1.4f;
            }
            if (Math.Abs(w.Position.X) > 17.5f) movement.X -= Math.Sign(w.Position.X) * 2;
            if (Math.Abs(w.Position.Y) > 7.5f) movement.Y -= Math.Sign(w.Position.Y) * 2;
            return new Command(movement.Normalized, danger < 1.6f, w.Charge >= 100);
        }
        public static int Pick(World w, int style = 0)
        {
            if (w.IsSpecializationDraft) return style == 1 ? 0 : style == 3 ? 2 : 1;
            int best = 0; float score = -10000;
            for (int i = 0; i < 3; i++)
            {
                Module id = w.Choices[i];
                float s = 10 - w.Rank(id);
                if (id == Module.Repair) s = w.Hull < w.MaxHull * .6f ? 95 : -100;
                if (id == Module.Regen) s += w.Rank(id) < 2 ? 25 : 5;
                if (id == Module.Hull) s += w.Hull < w.MaxHull * .7f ? 40 : 3;
                if (id == Module.Satellite) s += 25;
                if (id == Module.Arc) s += 20;
                if (id == Module.Multishot || id == Module.Power || id == Module.Rate) s += 16;
                if (id == Module.Magnet && w.Rank(id) < 2) s += 24;
                if (id == Module.OrbitPower && w.Rank(Module.Satellite) >= 2) s += 20;
                if (id == Module.Thrusters && w.Rank(id) < 2) s += 20;
                if (id == Module.Blast || id == Module.Frost) s += 22;
                if (id == Module.Ram) s += 12;
                if (style == 1 && (id == Module.Multishot || id == Module.Blast || id == Module.Pierce || id == Module.Kinetic)) s += 35;
                if (style == 2 && (id == Module.Satellite || id == Module.OrbitPower || id == Module.Arc || id == Module.Chain)) s += 35;
                if (style == 3 && (id == Module.Ram || id == Module.Dash || id == Module.Frost || id == Module.Thrusters)) s += 35;
                if (s > score) { score = s; best = i; }
            }
            return best;
        }
    }
}
