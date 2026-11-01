using System;
using System.Collections.Generic;

namespace Squares.Simulation
{
    public sealed partial class World
    {
        private enum ImpactKind { Shield, Projectile, Hull, Enemy }
        private struct Impact
        {
            public float Time;
            public ImpactKind Kind;
            public Bullet Bullet, Other;
            public Enemy Target;
            public int Shield;
        }
        private readonly List<Impact> projectileImpacts = new List<Impact>();
        private readonly List<V2> shieldStarts = new List<V2>(), shieldEnds = new List<V2>();

        // Swept square against a moving square in relative coordinates: no tunneling.
        private static bool Sweep(V2 start, V2 end, float extent, out float time)
        {
            float enter = 0, exit = 1; V2 delta = end - start;
            if (!Slab(start.X, delta.X, extent, ref enter, ref exit) ||
                !Slab(start.Y, delta.Y, extent, ref enter, ref exit)) { time = 0; return false; }
            time = enter; return true;
        }
        private static bool Slab(float start, float delta, float extent, ref float enter, ref float exit)
        {
            if (Math.Abs(delta) < .000001f) return Math.Abs(start) <= extent;
            float a = (-extent - start) / delta, b = (extent - start) / delta;
            if (a > b) { float swap = a; a = b; b = swap; }
            enter = Math.Max(enter, a); exit = Math.Min(exit, b);
            return enter <= exit;
        }
        private static V2 At(Bullet b, float time) => b.PreviousPosition + (b.Position - b.PreviousPosition) * time;
        private static int CompareImpacts(Impact a, Impact b)
        {
            int order = a.Time.CompareTo(b.Time); if (order != 0) return order;
            order = a.Kind.CompareTo(b.Kind); if (order != 0) return order;
            order = a.Bullet.Id.CompareTo(b.Bullet.Id); if (order != 0) return order;
            int ta = a.Other != null ? a.Other.Id : a.Target != null ? a.Target.Id : a.Shield;
            int tb = b.Other != null ? b.Other.Id : b.Target != null ? b.Target.Id : b.Shield;
            return ta.CompareTo(tb);
        }

        private void UpdateBullets()
        {
            projectileImpacts.Clear(); shieldStarts.Clear(); shieldEnds.Clear();
            foreach (Bullet b in Bullets)
            {
                b.PreviousPosition = b.Position;
                b.Position += b.Velocity * Dt; b.Life -= Dt;
            }
            CombatStats stats = Stats;
            float angle = Time * 1.57079633f;
            float bulletProjection = (float)(Math.Abs(Math.Cos(angle)) + Math.Abs(Math.Sin(angle)));
            for (int s = 0; s < stats.Satellites; s++)
            {
                float phase = s * 6.2831853f / stats.Satellites;
                V2 before = previousPlayerPosition + V2.Direction((Time - Dt) * stats.OrbitSpeed + phase) * stats.OrbitRadius;
                shieldStarts.Add(before); shieldEnds.Add(SatellitePosition(s));
            }
            foreach (Bullet b in Bullets)
            {
                if (b.Life <= 0) continue;
                if (b.Hostile)
                {
                    if (Sweep(b.PreviousPosition - previousPlayerPosition, b.Position - Position, .34f + b.Size, out float t))
                        projectileImpacts.Add(new Impact { Time = t, Kind = ImpactKind.Hull, Bullet = b });
                    for (int s = 0; s < stats.Satellites; s++)
                    {
                        V2 start = V2.Rotate(b.PreviousPosition - shieldStarts[s], -angle);
                        V2 end = V2.Rotate(b.Position - shieldEnds[s], -angle);
                        if (Sweep(start, end, stats.SatelliteSize + b.Size * bulletProjection, out float shieldTime))
                            projectileImpacts.Add(new Impact { Time = shieldTime, Kind = ImpactKind.Shield, Bullet = b, Shield = s });
                    }
                    foreach (Bullet friendly in Bullets)
                    {
                        if (friendly.Hostile || friendly.Life <= 0) continue;
                        if (Sweep(b.PreviousPosition - friendly.PreviousPosition, b.Position - friendly.Position,
                            b.Size + friendly.Size, out float interceptTime))
                            projectileImpacts.Add(new Impact { Time = interceptTime, Kind = ImpactKind.Projectile, Bullet = b, Other = friendly });
                    }
                }
                else foreach (Enemy e in Enemies)
                {
                    if (e.HP <= 0 || b.HitIds.Contains(e.Id)) continue;
                    if (Sweep(b.PreviousPosition - e.PreviousPosition, b.Position - e.Position, b.Size + e.Size, out float t))
                        projectileImpacts.Add(new Impact { Time = t, Kind = ImpactKind.Enemy, Bullet = b, Target = e });
                }
            }
            projectileImpacts.Sort(CompareImpacts);
            foreach (Impact hit in projectileImpacts)
            {
                Bullet b = hit.Bullet; if (b.Life <= 0) continue;
                switch (hit.Kind)
                {
                    case ImpactKind.Shield:
                        b.Life = 0; ShieldBlocks++; ProjectilesIntercepted++;
                        Emit(EffectKind.Parry, At(b, hit.Time)); break;
                    case ImpactKind.Projectile:
                        if (hit.Other.Life <= 0) break;
                        b.Life = 0; hit.Other.Life = 0; ProjectilesIntercepted++;
                        Emit(EffectKind.Parry, (At(b, hit.Time) + At(hit.Other, hit.Time)) * .5f); break;
                    case ImpactKind.Hull:
                        Hurt(b.Damage); b.Life = 0; break;
                    case ImpactKind.Enemy:
                        if (hit.Target.HP > 0 && !b.HitIds.Contains(hit.Target.Id)) ApplyBulletHit(b, hit.Target);
                        break;
                }
            }
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                Bullet b = Bullets[i];
                if (b.Life <= 0 || Math.Abs(b.Position.X) > 22 || Math.Abs(b.Position.Y) > 11) Bullets.RemoveAt(i);
            }
        }
    }
}
