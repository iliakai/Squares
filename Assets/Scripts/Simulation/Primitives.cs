using System;

namespace Squares.Simulation
{
    public struct V2
    {
        public float X, Y;
        public V2(float x, float y) { X = x; Y = y; }
        public float LengthSquared => X * X + Y * Y;
        public float Length => (float)Math.Sqrt(LengthSquared);
        public V2 Normalized => Length > .0001f ? this / Length : new V2(1, 0);
        public static V2 operator +(V2 a, V2 b) => new V2(a.X + b.X, a.Y + b.Y);
        public static V2 operator -(V2 a, V2 b) => new V2(a.X - b.X, a.Y - b.Y);
        public static V2 operator *(V2 a, float b) => new V2(a.X * b, a.Y * b);
        public static V2 operator /(V2 a, float b) => new V2(a.X / b, a.Y / b);
        public static float DistanceSquared(V2 a, V2 b) => (a - b).LengthSquared;
        public static V2 Direction(float angle) => new V2((float)Math.Cos(angle), (float)Math.Sin(angle));
        public static V2 Rotate(V2 a, float angle)
        {
            float c = (float)Math.Cos(angle), s = (float)Math.Sin(angle);
            return new V2(a.X * c - a.Y * s, a.X * s + a.Y * c);
        }
        public override string ToString() => $"({X:F2}, {Y:F2})";
    }

    public struct SeededRandom
    {
        private uint state;
        public SeededRandom(uint seed) { state = seed == 0 ? 1 : seed; }
        public uint Next() { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return state; }
        public float Value() => (Next() & 0xFFFFFF) / 16777216f;
        public int Range(int count) => (int)(Next() % (uint)Math.Max(1, count));
    }

    public struct Command
    {
        public V2 Move;
        public bool Dash, Overload;
        public Command(V2 move, bool dash = false, bool overload = false)
        { Move = move; Dash = dash; Overload = overload; }
    }

    public enum RunPhase { Running, Draft, Dead }
    public enum EnemyKind { Chaser, Charger, Sniper, Splitter, Elite, Boss }
    public enum EffectKind { Shot, Hit, Kill, Hurt, Dash, Arc, Overload, Level, Boss, Pickup }
    public struct WorldEvent
    {
        public EffectKind Kind;
        public V2 A, B;
        public float Size;
        public WorldEvent(EffectKind kind, V2 a, V2 b = default, float size = 1)
        { Kind = kind; A = a; B = b; Size = size; }
    }
    public sealed class Enemy
    {
        public int Id;
        public EnemyKind Kind;
        public V2 Position, Heading;
        public float HP, MaxHP, Size, Speed, Timer, Windup, Rush, OrbitHit;
    }
    public sealed class Bullet
    {
        public int Id, Pierce;
        public V2 Position, Velocity;
        public float Life, Damage, Size;
        public bool Hostile;
        public readonly System.Collections.Generic.List<int> HitIds = new System.Collections.Generic.List<int>();
    }
    public sealed class Pickup
    {
        public int Id, Value;
        public V2 Position;
    }
}
