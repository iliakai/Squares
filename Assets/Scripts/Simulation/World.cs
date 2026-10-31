using System;
using System.Collections.Generic;

namespace Squares.Simulation
{
    public sealed class World
    {
        public const float Dt = 1f / 60f, HalfWidth = 20f, HalfHeight = 9.5f;
        public const int EnemyCap = 200, BulletCap = 650, PickupCap = 250;
        public readonly uint Seed;
        public readonly int Core;
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Bullet> Bullets = new List<Bullet>();
        public readonly List<Pickup> Pickups = new List<Pickup>();
        public readonly List<WorldEvent> Events = new List<WorldEvent>();
        public readonly int[] Ranks = new int[(int)Module.Count];
        public readonly Module[] Choices = new Module[3];
        public RunPhase Phase { get; private set; }
        public V2 Position { get; private set; }
        public V2 Facing { get; private set; } = new V2(1, 0);
        public int Ticks { get; private set; }
        public int Kills { get; private set; }
        public int Level { get; private set; } = 1;
        public int Bosses { get; private set; }
        public int Combo { get; private set; }
        public int DamageTaken { get; private set; }
        public int ConstellationsFound { get; private set; }
        public long CelestialExperienceCreated { get; private set; }
        public long CelestialExperienceCollected { get; private set; }
        public int CelestialPickupsCollected { get; private set; }
        public int ConstellationMask { get; private set; }
        public ConstellationDiscovery LastConstellation { get; private set; }
        public bool HoldingExperience { get; private set; }
        public Specialization Specialization { get; private set; }
        public bool IsSpecializationDraft => Phase == RunPhase.Draft && Level >= 5 && Specialization == Specialization.None;
        public int RerollsRemaining { get; private set; } = 1;
        public CombatStats Stats => CombatStats.Calculate(Ranks, Specialization, researchSpeed);
        public CombatStats Preview(Module module)
        {
            int[] ranks = (int[])Ranks.Clone();
            if (module != Module.Repair) ranks[(int)module]++;
            return CombatStats.Calculate(ranks, Specialization, researchSpeed);
        }
        public CombatStats PreviewSpecialization(int index)
        {
            int[] ranks = (int[])Ranks.Clone();
            if (index == 1) ranks[(int)Module.Arc] = Math.Max(1, ranks[(int)Module.Arc]);
            return CombatStats.Calculate(ranks, (Specialization)(index + 1), researchSpeed);
        }
        public float Hull { get; private set; }
        public float MaxHull { get; private set; }
        public float XP { get; private set; }
        public float Charge { get; private set; }
        public float DashCooldown { get; private set; }
        public float DashTime { get; private set; }
        public float Invincible { get; private set; }
        public float OverloadTime { get; private set; }
        public float Time => Ticks * Dt;
        public int Sector => 1 + Ticks / 1800;
        public float NextLevelXP => 7 + Level * 4 + Level * Level * .3f;
        public float DashPeriod => Stats.DashPeriod;
        public int EarnedFlux => Kills / 12 + Bosses * 25 + (int)(Time / 30);
        public float OrbitAngle => Time * Stats.OrbitSpeed;
        public float OrbitRadius => Stats.OrbitRadius;
        public float SatelliteSize => Stats.SatelliteSize;
        public bool HasStorm => Rank(Module.Arc) >= 2 && Rank(Module.Satellite) >= 2;
        public bool HasShatter => Rank(Module.Pierce) >= 1 && Rank(Module.Multishot) >= 2;
        public bool HasIcebreak => Rank(Module.Blast) >= 2 && Rank(Module.Frost) >= 2;
        private SeededRandom random;
        private int nextId, nextElite = 3600, nextBoss = 10800;
        private float spawnTimer = .5f, shotTimer, arcTimer, comboTimer;
        private readonly float researchXP, researchSpeed;
        private V2 dashHeading;
        private readonly HashSet<int> ramHits = new HashSet<int>();
        private readonly ConstellationMatcher constellationMatcher = new ConstellationMatcher();
        private int constellationCursor;

        public World(uint seed, int core = 0, int vitality = 0, int learning = 0, int tempo = 0)
        {
            Seed = seed; Core = Math.Max(0, Math.Min(2, core)); random = new SeededRandom(seed);
            MaxHull = Hull = 100 + Math.Max(0, Math.Min(5, vitality)) * 10;
            researchXP = Math.Max(0, Math.Min(5, learning)) * .08f;
            researchSpeed = Math.Max(0, Math.Min(5, tempo)) * .03f;
            Ranks[(int)Module.Kinetic] = 1;
            if (Core == 0) Ranks[(int)Module.Rate] = 1;
            if (Core == 1) Ranks[(int)Module.Satellite] = 1;
            if (Core == 2) Ranks[(int)Module.Arc] = 1;
        }

        public int Rank(Module id) => Ranks[(int)id];
        public V2 SatellitePosition(int index)
        {
            int count = Rank(Module.Satellite);
            return Position + V2.Direction(OrbitAngle + index * 6.2831853f / Math.Max(1, count)) * OrbitRadius;
        }

        public void Step(Command command)
        {
            Events.Clear();
            if (Phase != RunPhase.Running) return;
            Ticks++;
            HoldingExperience = command.HoldExperience;
            Invincible = Math.Max(0, Invincible - Dt);
            DashCooldown = Math.Max(0, DashCooldown - Dt);
            DashTime = Math.Max(0, DashTime - Dt);
            OverloadTime = Math.Max(0, OverloadTime - Dt);
            comboTimer -= Dt;
            if (comboTimer <= 0) Combo = 0;
            V2 move = command.Move;
            if (move.LengthSquared > 1) move = move.Normalized;
            if (move.LengthSquared > .001f) Facing = move.Normalized;
            if (command.Dash && DashCooldown <= 0)
            {
                DashCooldown = DashPeriod; DashTime = .19f; Invincible = Math.Max(Invincible, .29f);
                ramHits.Clear();
                dashHeading = Facing; Emit(EffectKind.Dash, Position);
            }
            float speed = Stats.Speed;
            Position += (DashTime > 0 ? dashHeading * 23 : move * speed) * Dt;
            Position = Clamp(Position, .4f);
            if (command.Overload && Charge >= 100)
            {
                Charge = 0; OverloadTime = 5; Invincible = Math.Max(Invincible, .8f);
                Hull = Math.Min(MaxHull, Hull + 12); Emit(EffectKind.Overload, Position);
                foreach (Enemy e in Enemies)
                    if (V2.DistanceSquared(e.Position, Position) < 64) Hit(e, 65 * DamageScale, false);
                for (int i = Bullets.Count - 1; i >= 0; i--)
                    if (Bullets[i].Hostile) Bullets.RemoveAt(i);
            }
            Hull = Math.Min(MaxHull, Hull + Stats.Regen * Dt);
            SpawnDirector();
            Weapons();
            UpdateEnemies();
            UpdateBullets();
            RemoveDead();
            if (Ticks % Constellations.SearchInterval == 0) DiscoverConstellation();
            UpdatePickups();
            if (Hull <= 0) { Hull = 0; Phase = RunPhase.Dead; }
            else CheckLevel();
        }

        private float DamageScale => Stats.AllDamage * (OverloadTime > 0 ? 1.45f : 1);
        private void Weapons()
        {
            CombatStats stats = Stats;
            float haste = OverloadTime > 0 ? 1.8f : 1;
            shotTimer -= Dt * haste; arcTimer -= Dt * haste;
            Enemy target = Nearest(Position, 19);
            if (target != null && shotTimer <= 0 && Rank(Module.Kinetic) > 0)
            {
                shotTimer = stats.ShotPeriod;
                V2 direction = (target.Position - Position).Normalized;
                int count = stats.Shots;
                for (int i = 0; i < count; i++)
                    AddBullet(Position, V2.Rotate(direction, (i - (count - 1) * .5f) * .14f) * 20,
                        stats.BulletDamage * (OverloadTime > 0 ? 1.45f : 1), false, Rank(Module.Pierce));
                Emit(EffectKind.Shot, Position);
            }
            if (target != null && arcTimer <= 0 && Rank(Module.Arc) > 0 &&
                V2.DistanceSquared(target.Position, Position) <= 81)
            {
                arcTimer = stats.ArcPeriod;
                V2 origin = Position;
                var hit = new HashSet<int>();
                int count = stats.ArcTargets;
                for (int i = 0; i < count; i++)
                {
                    Enemy e = Nearest(origin, i == 0 ? 9 : 4.5f, hit);
                    if (e == null) break;
                    hit.Add(e.Id);
                    Emit(EffectKind.Arc, origin, e.Position);
                    Hit(e, stats.ArcDamage * (OverloadTime > 0 ? 1.45f : 1), true);
                    origin = e.Position;
                }
            }
            for (int i = 0; i < Rank(Module.Satellite); i++)
            {
                V2 satellite = SatellitePosition(i);
                foreach (Enemy e in Enemies)
                {
                    if (e.HP <= 0 || e.OrbitHit > 0) continue;
                    float radius = e.Size + SatelliteSize;
                    if (V2.DistanceSquared(satellite, e.Position) > radius * radius) continue;
                    e.OrbitHit = .35f;
                    Hit(e, stats.OrbitDamage * (OverloadTime > 0 ? 1.45f : 1), true);
                }
            }
        }

        private void SpawnDirector()
        {
            spawnTimer -= Dt;
            if (spawnTimer <= 0)
            {
                spawnTimer = Math.Max(.16f, .82f - Sector * .042f);
                int count = 1 + (Sector > 6 ? 1 : 0) + (Sector > 14 ? 1 : 0);
                for (int i = 0; i < count; i++)
                {
                    int roll = random.Range(100);
                    EnemyKind kind = EnemyKind.Chaser;
                    if (Sector >= 2 && roll < 22) kind = EnemyKind.Charger;
                    if (Sector >= 3 && roll >= 22 && roll < 40) kind = EnemyKind.Sniper;
                    if (Sector >= 4 && roll >= 40 && roll < 53) kind = EnemyKind.Splitter;
                    Spawn(kind, EdgePosition());
                }
            }
            if (Ticks >= nextElite)
            { nextElite += 3600; Spawn(EnemyKind.Elite, EdgePosition()); }
            if (Ticks >= nextBoss)
            {
                nextBoss += 10800;
                Spawn(EnemyKind.Boss, new V2(Position.X > 0 ? -17 : 17, 0));
                Emit(EffectKind.Boss, Position);
            }
        }

        private V2 EdgePosition()
        {
            V2 p;
            switch (random.Range(4))
            {
                case 0: p = new V2(-19.5f, (random.Value() * 2 - 1) * 9); break;
                case 1: p = new V2(19.5f, (random.Value() * 2 - 1) * 9); break;
                case 2: p = new V2((random.Value() * 2 - 1) * 19, -9); break;
                default: p = new V2((random.Value() * 2 - 1) * 19, 9); break;
            }
            if (V2.DistanceSquared(p, Position) < 25) p = new V2(-p.X, -p.Y);
            return p;
        }

        private void Spawn(EnemyKind kind, V2 p, bool small = false)
        {
            if (Enemies.Count >= EnemyCap) return;
            float hp = 18 + Sector * 3.2f, speed = 1.45f + Math.Min(1.1f, Sector * .045f), size = .38f;
            switch (kind)
            {
                case EnemyKind.Charger: hp *= 1.3f; speed *= .85f; size = .45f; break;
                case EnemyKind.Sniper: hp *= .85f; speed *= .65f; size = .42f; break;
                case EnemyKind.Splitter: hp *= 1.6f; speed *= .8f; size = .56f; break;
                case EnemyKind.Elite: hp *= 9; speed *= .8f; size = .85f; break;
                case EnemyKind.Boss: hp = 1550 * (1 + Bosses * .65f); speed = 1.2f; size = 1.3f; break;
            }
            if (small) { hp *= .45f; speed *= 1.35f; size = .24f; }
            Enemies.Add(new Enemy { Id = ++nextId, Kind = kind, Position = p, HP = hp, MaxHP = hp,
                Size = size, Speed = speed, Timer = 1.2f + random.Value() * 1.3f });
        }

        private void UpdateEnemies()
        {
            int count = Enemies.Count;
            for (int i = 0; i < count; i++)
            {
                Enemy e = Enemies[i];
                if (e.HP <= 0) continue;
                e.Timer -= Dt; e.OrbitHit = Math.Max(0, e.OrbitHit - Dt);
                e.SlowTime = Math.Max(0, e.SlowTime - Dt);
                V2 delta = Position - e.Position;
                V2 direction = delta.Normalized;
                float movement = e.Speed;
                if (e.Kind == EnemyKind.Charger)
                {
                    if (e.Rush > 0) { e.Rush -= Dt; direction = e.Heading; movement = 11; }
                    else if (e.Windup > 0)
                    {
                        e.Windup -= Dt; movement = 0;
                        if (e.Windup <= 0) { e.Rush = .6f; e.Timer = 3; }
                    }
                    else if (e.Timer <= 0) { e.Windup = .7f; e.Heading = direction; movement = 0; }
                }
                if (e.Kind == EnemyKind.Sniper)
                {
                    if (delta.LengthSquared < 30) direction = direction * -1;
                    else if (delta.LengthSquared < 72) movement = 0;
                    if (e.Timer <= 0)
                    { e.Timer = 2.4f; AddBullet(e.Position, delta.Normalized * 5.7f, 12 + Sector, true); }
                }
                if (e.Kind == EnemyKind.Boss && e.Timer <= 0)
                {
                    e.Timer = 1.7f;
                    for (int fan = -2; fan <= 2; fan++)
                        AddBullet(e.Position, V2.Rotate(delta.Normalized, fan * .24f) * 5, 20, true);
                    Spawn(EnemyKind.Chaser, Clamp(e.Position + new V2(1.5f, 0), .5f));
                }
                // Soft separation costs O(n), using only adjacent spawn order neighbors.
                if (i > 0)
                {
                    V2 gap = e.Position - Enemies[i - 1].Position;
                    if (gap.LengthSquared > .001f && gap.LengthSquared < .7f)
                        e.Position += gap.Normalized * .7f * Dt;
                }
                if (e.SlowTime > 0) movement *= 1 - e.SlowFactor;
                e.Position = Clamp(e.Position + direction * movement * Dt, e.Size);
                float radius = e.Size + .34f;
                if (DashTime > 0 && Stats.RamDamage > 0 && !ramHits.Contains(e.Id) &&
                    V2.DistanceSquared(e.Position, Position) < (radius + .3f) * (radius + .3f))
                {
                    ramHits.Add(e.Id); Hit(e, Stats.RamDamage * (OverloadTime > 0 ? 1.45f : 1), false);
                    Emit(EffectKind.Ram, e.Position); e.Position = Clamp(e.Position + dashHeading * .4f, e.Size);
                }
                if (V2.DistanceSquared(e.Position, Position) < radius * radius)
                {
                    Hurt(e.Kind == EnemyKind.Boss ? 25 : 10 + Sector * .8f);
                    if (e.Kind != EnemyKind.Boss) e.Position = Clamp(e.Position - direction * .55f, e.Size);
                }
            }
        }

        private void AddBullet(V2 p, V2 velocity, float damage, bool hostile, int pierce = 0)
        {
            if (Bullets.Count >= BulletCap) return;
            Bullets.Add(new Bullet { Id = ++nextId, Position = p, Velocity = velocity, Damage = damage,
                Hostile = hostile, Pierce = pierce, Life = hostile ? 7 : 2, Size = hostile ? .16f : .1f });
        }

        private void UpdateBullets()
        {
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                Bullet b = Bullets[i];
                b.Life -= Dt;
                b.Position += b.Velocity * Dt;
                if (b.Hostile)
                {
                    if (V2.DistanceSquared(b.Position, Position) < .25f)
                    { Hurt(b.Damage); b.Life = 0; }
                }
                else
                {
                    foreach (Enemy e in Enemies)
                    {
                        if (e.HP <= 0 || b.HitIds.Contains(e.Id)) continue;
                        float r = e.Size + b.Size;
                        if (V2.DistanceSquared(b.Position, e.Position) > r * r) continue;
                        b.HitIds.Add(e.Id);
                        if (Rank(Module.Frost) > 0)
                        {
                            e.SlowTime = Stats.SlowDuration;
                            e.SlowFactor = Stats.SlowFraction * (e.Kind == EnemyKind.Boss ? .5f : 1);
                            Emit(EffectKind.Chill, e.Position);
                        }
                        Hit(e, b.Damage, true);
                        if (Rank(Module.Blast) > 0)
                        {
                            CombatStats stats = Stats;
                            Emit(EffectKind.Blast, e.Position, size: stats.BlastRadius);
                            foreach (Enemy neighbor in Enemies)
                            {
                                if (neighbor.Id == e.Id || neighbor.HP <= 0) continue;
                                V2 gap = neighbor.Position - e.Position;
                                if (Math.Abs(gap.X) > stats.BlastRadius || Math.Abs(gap.Y) > stats.BlastRadius) continue;
                                Hit(neighbor, b.Damage * stats.BlastFraction * (HasIcebreak && neighbor.SlowTime > 0 ? 1.5f : 1), false);
                            }
                        }
                        if (HasShatter) e.Position = Clamp(e.Position + b.Velocity.Normalized * .22f, e.Size);
                        if (b.Pierce-- <= 0) { b.Life = 0; break; }
                    }
                }
                if (b.Life <= 0 || Math.Abs(b.Position.X) > 22 || Math.Abs(b.Position.Y) > 11)
                    Bullets.RemoveAt(i);
            }
        }

        private void Hit(Enemy e, float damage, bool critical)
        {
            if (e.HP <= 0) return;
            if (critical && random.Value() < Stats.CriticalChance) damage *= 2;
            e.HP -= damage;
            Emit(EffectKind.Hit, e.Position, size: damage);
        }

        private void Hurt(float amount)
        {
            if (Invincible > 0 || Hull <= 0) return;
            Hull -= amount; DamageTaken += (int)amount; Invincible = .65f;
            Combo = 0; Emit(EffectKind.Hurt, Position);
        }

        private void RemoveDead()
        {
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = Enemies[i];
                if (e.HP > 0) continue;
                Enemies.RemoveAt(i); Kills++; Combo++; comboTimer = 2.8f;
                Charge = Math.Min(100, Charge + (2 + Math.Min(8, Combo) * .25f) * (1 + Rank(Module.Capacitor) * .08f));
                int value = e.Kind == EnemyKind.Boss ? 90 : e.Kind == EnemyKind.Elite ? 18 : 3;
                if (e.Kind == EnemyKind.Boss) { Bosses++; Hull = Math.Min(MaxHull, Hull + 30); }
                Emit(EffectKind.Kill, e.Position, size: e.Size);
                if (Pickups.Count < PickupCap)
                    Pickups.Add(new Pickup { Id = ++nextId, Position = e.Position, Value = value });
                else // Merge at the cap so experience is never silently discarded.
                    Pickups[random.Range(Pickups.Count)].Value += value;
                if (e.Kind == EnemyKind.Splitter)
                {
                    Spawn(EnemyKind.Chaser, Clamp(e.Position + new V2(.3f, 0), .3f), true);
                    Spawn(EnemyKind.Chaser, Clamp(e.Position - new V2(.3f, 0), .3f), true);
                }
            }
        }

        private void UpdatePickups()
        {
            if (HoldingExperience) return;
            float magnet = Stats.MagnetRadius;
            for (int i = Pickups.Count - 1; i >= 0; i--)
            {
                Pickup p = Pickups[i];
                if (p.AvailableTick > Ticks) continue;
                float distance = V2.DistanceSquared(Position, p.Position);
                if (distance < magnet * magnet)
                {
                    float step = 12 * Dt;
                    p.Position = distance < step * step ? Position : p.Position + (Position - p.Position).Normalized * step;
                }
                if (V2.DistanceSquared(Position, p.Position) > .3f) continue;
                XP += p.Value * (1 + Rank(Module.Learning) * .15f + researchXP);
                if (p.Celestial) { CelestialPickupsCollected++; CelestialExperienceCollected += p.Value; }
                Emit(p.Celestial ? EffectKind.StarPickup : EffectKind.Pickup, p.Position, size: p.Value); Pickups.RemoveAt(i);
            }
        }

        private void DiscoverConstellation()
        {
            ConstellationDiscovery discovery = constellationMatcher.Find(Pickups, Position, Stats.MagnetRadius,
                Ticks, constellationCursor, HoldingExperience);
            constellationCursor = (constellationCursor + Constellations.AnchorBudget) % 1000000000;
            if (discovery == null) return;
            var consumed = new HashSet<int>(discovery.PickupIds);
            Pickups.RemoveAll(p => consumed.Contains(p.Id));
            Pickups.Add(new Pickup { Id = ++nextId, Position = discovery.Center, Value = discovery.Reward,
                Celestial = true, AvailableTick = Ticks + 150 });
            ConstellationsFound++; ConstellationMask |= 1 << discovery.PatternIndex;
            CelestialExperienceCreated += discovery.Reward; LastConstellation = discovery;
            // A discovery is player-facing feedback and must survive a very busy combat tick.
            if (Events.Count >= 256) Events.RemoveAt(Events.Count - 1);
            Events.Add(new WorldEvent(EffectKind.Constellation, discovery.Center) { Discovery = discovery });
        }

        private void CheckLevel()
        {
            if (XP < NextLevelXP) return;
            XP -= NextLevelXP; Level++; Phase = RunPhase.Draft;
            RollChoices();
            Emit(EffectKind.Level, Position);
        }

        private void RollChoices()
        {
            var pool = new List<Module>();
            foreach (Upgrade u in Upgrades.All)
                if (Upgrades.Available(u.Id, Ranks)) pool.Add(u.Id);
            for (int i = 0; i < 3; i++)
            {
                int pick = random.Range(pool.Count); Choices[i] = pool[pick]; pool.RemoveAt(pick);
            }
        }

        public bool Choose(int index)
        {
            if (Phase != RunPhase.Draft || index < 0 || index >= 3) return false;
            if (IsSpecializationDraft)
            {
                Specialization = (Specialization)(index + 1);
                if (Specialization == Specialization.Storm) Ranks[(int)Module.Arc] = Math.Max(1, Rank(Module.Arc));
                if (Specialization == Specialization.Scout) { MaxHull *= .8f; Hull = Math.Min(Hull, MaxHull); }
                RollChoices(); // The ordinary level-up remains queued after specialization.
                return true;
            }
            Module id = Choices[index];
            if (!Upgrades.Available(id, Ranks)) return false;
            if (id != Module.Repair) Ranks[(int)id]++;
            if (id == Module.Hull) { MaxHull += 20; Hull = Math.Min(MaxHull, Hull + 30); }
            if (id == Module.Repair) Hull = Math.Min(MaxHull, Hull + MaxHull * .4f);
            Phase = RunPhase.Running; CheckLevel(); return true;
        }

        public bool Reroll()
        {
            if (Phase != RunPhase.Draft || IsSpecializationDraft || RerollsRemaining <= 0) return false;
            var fresh = new List<Module>();
            foreach (Upgrade u in Upgrades.All)
                if (Upgrades.Available(u.Id, Ranks) && Array.IndexOf(Choices, u.Id) < 0) fresh.Add(u.Id);
            if (fresh.Count == 0) return false;
            Module guaranteed = fresh[random.Range(fresh.Count)];
            RollChoices();
            if (Array.IndexOf(Choices, guaranteed) < 0) Choices[0] = guaranteed;
            RerollsRemaining--; return true;
        }

        private Enemy Nearest(V2 origin, float range, HashSet<int> excluded = null)
        {
            Enemy found = null; float best = range * range;
            foreach (Enemy e in Enemies)
            {
                if (e.HP <= 0 || (excluded != null && excluded.Contains(e.Id))) continue;
                float d = V2.DistanceSquared(e.Position, origin);
                if (d < best) { best = d; found = e; }
            }
            return found;
        }
        private static V2 Clamp(V2 p, float inset) => new V2(
            Math.Max(-HalfWidth + inset, Math.Min(HalfWidth - inset, p.X)),
            Math.Max(-HalfHeight + inset, Math.Min(HalfHeight - inset, p.Y)));
        private void Emit(EffectKind kind, V2 a, V2 b = default, float size = 1)
        { if (Events.Count < 256) Events.Add(new WorldEvent(kind, a, b, size)); }

        public ulong Fingerprint()
        {
            ulong hash = 1469598103934665603UL;
            Action<int> add = n => { unchecked { hash = (hash ^ (uint)n) * 1099511628211UL; } };
            Action<float> number = n => add(BitConverter.ToInt32(BitConverter.GetBytes(n), 0));
            add(Ticks); add(Kills); add(Level); add(Bosses); add((int)Phase); add((int)Specialization); add(RerollsRemaining);
            add(ConstellationsFound); add(constellationCursor); add(HoldingExperience ? 1 : 0);
            add(ConstellationMask); add(CelestialPickupsCollected);
            add((int)CelestialExperienceCreated); add((int)(CelestialExperienceCreated >> 32));
            add((int)CelestialExperienceCollected); add((int)(CelestialExperienceCollected >> 32));
            foreach (Module choice in Choices) add((int)choice);
            number(Position.X); number(Position.Y); number(Hull); number(XP); number(Charge);
            foreach (int rank in Ranks) add(rank);
            foreach (Enemy e in Enemies)
            { add(e.Id); add((int)e.Kind); number(e.Position.X); number(e.Position.Y); number(e.HP); number(e.SlowTime); }
            foreach (Bullet b in Bullets) { add(b.Id); number(b.Position.X); number(b.Position.Y); number(b.Life); }
            foreach (Pickup p in Pickups) { add(p.Id); number(p.Position.X); number(p.Position.Y); add((int)p.Value); add((int)(p.Value >> 32)); add(p.AvailableTick); add(p.Celestial ? 1 : 0); }
            return hash;
        }
    }
}
