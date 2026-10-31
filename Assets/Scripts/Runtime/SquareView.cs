using System.Collections.Generic;
using Squares.Simulation;
using UnityEngine;

namespace Squares
{
    public sealed class SquareView
    {
        private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        private readonly List<Particle> particles = new List<Particle>();
        private readonly List<Pulse> pulses = new List<Pulse>();
        private readonly Transform root;
        private readonly Sprite sprite;
        private int used;
        private float shake;
        public readonly Camera Camera;
        public static readonly Color Mint = new Color(.4f, 1, .79f);
        public static readonly Color Blue = new Color(.3f, .65f, 1);
        public static readonly Color Gold = new Color(1, .75f, .32f);
        public static readonly Color Red = new Color(1, .29f, .4f);
        private struct Particle { public Vector2 P, V; public float Age, Life, Size; public Color Color; }
        private struct Pulse { public Vector2 P; public float Age, Radius; }

        public SquareView(Transform parent)
        {
            root = new GameObject("Square renderer pool").transform; root.SetParent(parent);
            Texture2D texture = new Texture2D(1, 1) { filterMode = FilterMode.Point, name = "Square pixel" };
            texture.SetPixel(0, 0, Color.white); texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            Camera = new GameObject("Arena Camera").AddComponent<Camera>();
            Camera.transform.SetParent(parent); Camera.transform.position = new Vector3(0, 0, -10);
            Camera.orthographic = true; Camera.orthographicSize = 13;
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = new Color(.025f, .037f, .065f);
            Camera.gameObject.AddComponent<AudioListener>();
        }

        private static Vector2 Vec(V2 v) => new Vector2(v.X, v.Y);
        public void ClearEffects() { particles.Clear(); pulses.Clear(); shake = 0; }
        public void Event(WorldEvent e, bool flashes)
        {
            if (e.Kind == EffectKind.Pickup || e.Kind == EffectKind.Shot) return;
            if (e.Kind == EffectKind.Blast && pulses.Count < 100) pulses.Add(new Pulse { P = Vec(e.A), Radius = e.Size });
            Color color = e.Kind == EffectKind.Hurt ? Red : e.Kind == EffectKind.Arc || e.Kind == EffectKind.Chill ? Blue :
                e.Kind == EffectKind.Blast || e.Kind == EffectKind.Ram ? Gold : Mint;
            int count = e.Kind == EffectKind.Kill ? 8 : e.Kind == EffectKind.Overload ? 70 : 4;
            if (e.Kind == EffectKind.Arc)
            {
                Vector2 a = Vec(e.A), b = Vec(e.B);
                int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / .22f);
                for (int i = 0; i < steps; i++)
                    AddParticle(Vector2.Lerp(a, b, i / (float)steps), Vector2.zero, .15f, .11f, Blue);
                return;
            }
            for (int i = 0; i < count; i++)
                AddParticle(Vec(e.A), new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) *
                    (e.Kind == EffectKind.Overload ? 17 : 4), Random.Range(.16f, .52f), Random.Range(.05f, .17f), color);
            if (flashes && (e.Kind == EffectKind.Hurt || e.Kind == EffectKind.Overload)) shake = .2f;
        }
        private void AddParticle(Vector2 p, Vector2 v, float life, float size, Color color)
        {
            if (particles.Count < 900) particles.Add(new Particle { P = p, V = v, Life = life, Size = size, Color = color });
        }

        public void Render(World world, float dt, bool animate, bool flashes)
        {
            used = 0;
            Camera.orthographicSize = Mathf.Max(13, 22 / Mathf.Max(.5f, Camera.aspect));
            if (!flashes) shake = 0;
            shake = Mathf.Max(0, shake - dt);
            Camera.transform.position = new Vector3(shake > 0 ? Random.Range(-shake, shake) * .25f : 0,
                shake > 0 ? Random.Range(-shake, shake) * .25f : 0, -10);
            Color grid = new Color(.1f, .16f, .22f, .38f);
            for (int x = -20; x <= 20; x += 2) Box(new Vector2(x, 0), new Vector2(.018f, 19), grid);
            for (int y = -8; y <= 8; y += 2) Box(new Vector2(0, y), new Vector2(40, .018f), grid);
            Color edge = new Color(.19f, .34f, .41f);
            Box(new Vector2(0, -9.7f), new Vector2(40.4f, .055f), edge);
            Box(new Vector2(0, 9.7f), new Vector2(40.4f, .055f), edge);
            Box(new Vector2(-20.2f, 0), new Vector2(.055f, 19.4f), edge);
            Box(new Vector2(20.2f, 0), new Vector2(.055f, 19.4f), edge);
            for (int x = -20; x <= 20; x += 2)
            {
                Box(new Vector2(x, -9.7f), new Vector2(.12f, .23f), edge);
                Box(new Vector2(x, 9.7f), new Vector2(.12f, .23f), edge);
            }
            if (world != null) DrawWorld(world);
            for (int i = pulses.Count - 1; i >= 0; i--)
            {
                Pulse p = pulses[i]; if (animate) p.Age += dt;
                if (p.Age > .22f) { pulses.RemoveAt(i); continue; }
                Color c = Gold; c.a = (1 - p.Age / .22f) * .55f;
                Frame(p.P, p.Radius * 2, .055f, c); pulses[i] = p;
            }
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];
                if (animate) { p.Age += dt; p.P += p.V * dt; p.V *= Mathf.Max(0, 1 - dt * 3); }
                if (p.Age >= p.Life) { particles.RemoveAt(i); continue; }
                Color c = p.Color; c.a = 1 - p.Age / p.Life;
                Box(p.P, Vector2.one * p.Size, c); particles[i] = p;
            }
            for (int i = used; i < pool.Count; i++) pool[i].enabled = false;
        }

        private void DrawWorld(World w)
        {
            foreach (Pickup p in w.Pickups)
            {
                Vector2 pos = Vec(p.Position);
                Box(pos, Vector2.one * .3f, new Color(.4f, .9f, 1, .07f));
                Box(pos, Vector2.one * (p.Value > 3 ? .2f : .105f), new Color(.7f, .93f, 1));
            }
            foreach (Enemy e in w.Enemies)
            {
                Color c = EnemyColor(e.Kind); Vector2 pos = Vec(e.Position);
                if (e.SlowTime > 0)
                {
                    c = Color.Lerp(c, Blue, .75f);
                    Frame(pos, e.Size * 2 + .16f, .035f, new Color(.4f, .8f, 1, .65f));
                }
                if (e.Windup > 0)
                {
                    Line(pos, pos + Vec(e.Heading) * 6.5f, .08f, new Color(1, .55f, .2f, .3f));
                    Frame(pos, e.Size * 2 + .3f + e.Windup * .25f, .035f, Gold);
                }
                Box(pos, Vector2.one * (e.Size * 2 + .24f), new Color(c.r, c.g, c.b, .065f));
                Box(pos, Vector2.one * e.Size * 2, c);
                Box(pos, Vector2.one * e.Size * 1.6f, new Color(c.r * .17f, c.g * .17f, c.b * .17f));
                Box(pos, Vector2.one * e.Size * .65f, c, e.Kind == EnemyKind.Boss ? w.Time * 30 : 0);
                if (e.Kind == EnemyKind.Sniper)
                {
                    Box(pos, new Vector2(e.Size * 2.5f, .09f), c);
                    Box(pos, new Vector2(.09f, e.Size * 2.5f), c);
                }
                if (e.HP < e.MaxHP)
                {
                    float width = e.Size * 2, ratio = Mathf.Clamp01(e.HP / e.MaxHP);
                    Box(pos + new Vector2(0, e.Size + .18f), new Vector2(width, .045f), new Color(.15f, .15f, .2f));
                    Box(pos + new Vector2((ratio - 1) * width * .5f, e.Size + .18f), new Vector2(width * ratio, .045f), c);
                }
            }
            foreach (Bullet b in w.Bullets)
            {
                Color c = b.Hostile ? Red : w.Rank(Module.Frost) > 0 ? Blue : w.Rank(Module.Blast) > 0 ? Gold : Mint;
                Vector2 p = Vec(b.Position);
                Box(p - Vec(b.Velocity).normalized * .12f, Vector2.one * b.Size * 3.3f, new Color(c.r, c.g, c.b, .12f));
                Box(p, Vector2.one * b.Size * 2, c);
            }
            Vector2 player = Vec(w.Position);
            if (w.OverloadTime > 0) Frame(player, 1.25f, .055f, Gold);
            if (w.DashTime > 0)
                for (int i = 1; i <= 4; i++)
                    Box(player - Vec(w.Facing) * i * .35f, Vector2.one * (.75f - i * .08f), w.Rank(Module.Ram) > 0 ? new Color(1, .75f, .3f, .5f / i) : new Color(.4f, 1, .8f, .25f / i));
            Color body = w.Invincible > 0 && (int)(w.Time * 30) % 2 == 0 ? Color.white : Mint;
            Frame(player, 1.06f, .055f, new Color(.3f, 1, .8f, .22f));
            Box(player, Vector2.one * .76f, body);
            Box(player, Vector2.one * .54f, new Color(.04f, .14f, .13f));
            Box(player, Vector2.one * .24f, body);
            Box(player + Vec(w.Facing) * .42f, Vector2.one * .16f, Color.white);
            // Visible module sockets grow with the build; all geometry stays square.
            int sockets = Mathf.Min(4, w.Rank(Module.Multishot) + w.Rank(Module.Power));
            for (int i = 0; i < sockets; i++)
                Box(player + new Vector2((i % 2 == 0 ? -1 : 1) * .54f, (i < 2 ? -1 : 1) * .33f), Vector2.one * .19f, Blue);
            for (int i = 0; i < w.Rank(Module.Satellite); i++)
            {
                Vector2 p = Vec(w.SatellitePosition(i));
                Box(p, Vector2.one * w.SatelliteSize * 2.8f, new Color(.3f, .6f, 1, .13f));
                Box(p, Vector2.one * w.SatelliteSize * 2, w.HasStorm ? Gold : Blue, w.Time * 90);
                Box(p, Vector2.one * w.SatelliteSize, new Color(.04f, .08f, .15f), w.Time * 90);
            }
        }

        public static Color EnemyColor(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Charger: return new Color(1, .59f, .25f);
                case EnemyKind.Sniper: return new Color(.76f, .46f, 1);
                case EnemyKind.Splitter: return new Color(.2f, .84f, .91f);
                case EnemyKind.Elite: return Gold;
                case EnemyKind.Boss: return new Color(1, .3f, .55f);
                default: return Red;
            }
        }
        private void Frame(Vector2 p, float size, float thickness, Color c)
        {
            Box(p + Vector2.up * size * .5f, new Vector2(size, thickness), c);
            Box(p - Vector2.up * size * .5f, new Vector2(size, thickness), c);
            Box(p - Vector2.right * size * .5f, new Vector2(thickness, size), c);
            Box(p + Vector2.right * size * .5f, new Vector2(thickness, size), c);
        }
        private void Line(Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 delta = b - a;
            Box((a + b) * .5f, new Vector2(delta.magnitude, width), color, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
        private void Box(Vector2 p, Vector2 scale, Color c, float angle = 0)
        {
            if (used >= 4200) return;
            if (used == pool.Count)
            {
                var obj = new GameObject("Square"); obj.transform.SetParent(root);
                var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
                pool.Add(renderer);
            }
            SpriteRenderer r = pool[used]; r.enabled = true; r.color = c; r.sortingOrder = used++;
            r.transform.position = new Vector3(p.x, p.y, 0);
            r.transform.localScale = new Vector3(Mathf.Max(.0001f, scale.x), Mathf.Max(.0001f, scale.y), 1);
            r.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
