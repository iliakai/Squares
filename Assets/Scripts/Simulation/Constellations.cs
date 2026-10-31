using System;
using System.Collections.Generic;

namespace Squares.Simulation
{
    public sealed class ConstellationPattern
    {
        public readonly string Name, Subtitle;
        public readonly V2[] Points, AnchorSpace;
        public readonly int[] Edges, MatchOrder;
        public readonly int AnchorA, AnchorB;
        public ConstellationPattern(string name, string subtitle, V2[] points, params int[] edges)
        {
            if (points.Length < 5 || points.Length > 20) throw new ArgumentException("Constellation requires 5–20 main stars");
            Name = name; Subtitle = subtitle; Edges = edges;
            float spanSquared = 0;
            for (int i = 0; i < points.Length; i++)
                for (int j = i + 1; j < points.Length; j++)
                {
                    float d = V2.DistanceSquared(points[i], points[j]);
                    if (d > spanSquared) { spanSquared = d; AnchorA = i; AnchorB = j; }
                }
            V2 center = default; foreach (V2 p in points) center += p;
            center /= points.Length;
            float span = (float)Math.Sqrt(spanSquared);
            Points = new V2[points.Length]; AnchorSpace = new V2[points.Length];
            V2 axis = points[AnchorB] - points[AnchorA];
            var order = new List<int>();
            for (int i = 0; i < points.Length; i++)
            {
                Points[i] = (points[i] - center) / span;
                V2 v = points[i] - points[AnchorA];
                AnchorSpace[i] = new V2((v.X * axis.X + v.Y * axis.Y) / spanSquared,
                    (axis.X * v.Y - axis.Y * v.X) / spanSquared);
                if (i != AnchorA && i != AnchorB) order.Add(i);
            }
            // Check distinctive off-axis stars early to reject bad anchors cheaply.
            order.Sort((a, b) => Math.Abs(AnchorSpace[b].Y).CompareTo(Math.Abs(AnchorSpace[a].Y)));
            MatchOrder = order.ToArray();
        }
    }

    public static class Constellations
    {
        // Original simplified diagrams of the prominent stars, not full sky-region catalogs.
        // See specification 003 for IAU/NASA references and the distinction for the Dipper.
        public static readonly ConstellationPattern[] All = {
            new ConstellationPattern("Кассиопея", "Небесная буква W", new[] {
                new V2(-2,1), new V2(-1,-.6f), new V2(0,.7f), new V2(1,-.8f), new V2(2,1.4f)
            }, 0,1, 1,2, 2,3, 3,4),
            new ConstellationPattern("Большая Медведица", "Семь звёзд ковша", new[] {
                new V2(0,0), new V2(1.1f,1), new V2(2.3f,.85f), new V2(3.2f,1.1f),
                new V2(5.1f,1.5f), new V2(5,0), new V2(3.3f,-.4f)
            }, 0,1, 1,2, 2,3, 3,4, 4,5, 5,6, 6,3),
            new ConstellationPattern("Орион", "Плечи, пояс и ноги охотника", new[] {
                new V2(-1.3f,2), new V2(1.15f,2.25f), new V2(-.55f,.15f), new V2(0,.3f),
                new V2(.6f,.45f), new V2(-1,-2), new V2(1.4f,-1.7f)
            }, 0,1, 0,2, 1,4, 2,3, 3,4, 2,5, 4,6),
            new ConstellationPattern("Лебедь", "Северный крест", new[] {
                new V2(0,2.7f), new V2(0,.8f), new V2(0,-2.7f), new V2(-2.1f,0),
                new V2(2,.9f), new V2(-3,-.9f)
            }, 0,1, 1,2, 1,3, 1,4, 3,5),
            new ConstellationPattern("Лира", "Вега и небесная арфа", new[] {
                new V2(-1,1.5f), new V2(-.7f,.45f), new V2(-1.8f,.9f), new V2(.8f,0),
                new V2(1.3f,-1.5f), new V2(-.35f,-1)
            }, 0,1, 0,2, 2,1, 1,3, 3,4, 4,5, 5,1),
            new ConstellationPattern("Лев", "Серп и хвост льва", new[] {
                new V2(-2,-1), new V2(-2.25f,0), new V2(-1.7f,.8f), new V2(-2,1.7f),
                new V2(-1.3f,2.3f), new V2(-2.7f,2.3f), new V2(1,-.2f), new V2(2.6f,-.7f), new V2(.9f,1.1f)
            }, 5,4, 4,3, 3,2, 2,1, 1,0, 2,8, 8,7, 7,6, 6,0, 6,8)
        };
        public const float MinimumSpan = 3, MaximumSpan = 45;
        public const float StarTolerance = .075f, RmsTolerance = .045f;
        public const float MaximumStarError = .65f, MaximumRmsError = .38f;
        public const int SearchInterval = 30, AnchorBudget = 1200;
    }

    public sealed class ConstellationDiscovery
    {
        public int PatternIndex, Tick;
        public int[] PickupIds;
        public V2[] Stars;
        public V2 Center;
        public long OriginalValue, Reward;
        public float Error;
    }

    public sealed class ConstellationMatcher
    {
        private const float CellSize = .8f;
        private readonly List<Pickup> candidates = new List<Pickup>();
        private readonly Dictionary<long, List<int>> cells = new Dictionary<long, List<int>>();
        private readonly Stack<List<int>> freeCells = new Stack<List<int>>();
        private readonly int[] chosen = new int[20], best = new int[20];
        private static readonly int[] PatternOrder = { 5, 1, 2, 3, 4, 0 };
        public int LastAnchorTests { get; private set; }

        public ConstellationDiscovery Find(IReadOnlyList<Pickup> pickups, V2 player, float magnet,
            int tick, int cursor = 0, bool holdingExperience = false)
        {
            LastAnchorTests = 0; candidates.Clear();
            foreach (Pickup p in pickups)
            {
                if (p.Celestial || p.Value <= 0 || p.Value > long.MaxValue / 200 || p.AvailableTick > tick) continue;
                if (!holdingExperience && V2.DistanceSquared(player, p.Position) <= (magnet + .3f) * (magnet + .3f)) continue;
                candidates.Add(p);
            }
            if (candidates.Count < 5) return null;
            candidates.Sort((a, b) => a.Id.CompareTo(b.Id));
            foreach (List<int> list in cells.Values) { list.Clear(); freeCells.Push(list); }
            cells.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                V2 p = candidates[i].Position;
                long key = Key(Cell(p.X), Cell(p.Y));
                if (!cells.TryGetValue(key, out List<int> list))
                { list = freeCells.Count > 0 ? freeCells.Pop() : new List<int>(); cells.Add(key, list); }
                list.Add(i);
            }
            int count = candidates.Count, pairs = count * (count - 1), budget = Math.Min(pairs, Constellations.AnchorBudget);
            foreach (int patternIndex in PatternOrder)
            {
                ConstellationPattern pattern = Constellations.All[patternIndex];
                if (count < pattern.Points.Length) continue;
                float bestScore = float.MaxValue;
                for (int scan = 0; scan < budget; scan++)
                {
                    int pair = (int)(((long)cursor + scan) % pairs);
                    int ia = pair / (count - 1), ib = pair % (count - 1); if (ib >= ia) ib++;
                    V2 a = candidates[ia].Position, axis = candidates[ib].Position - a;
                    float lengthSquared = axis.LengthSquared;
                    if (lengthSquared < Constellations.MinimumSpan * Constellations.MinimumSpan ||
                        lengthSquared > Constellations.MaximumSpan * Constellations.MaximumSpan) continue;
                    LastAnchorTests++;
                    float toleranceSquared = Math.Min(lengthSquared * Constellations.StarTolerance * Constellations.StarTolerance,
                        Constellations.MaximumStarError * Constellations.MaximumStarError);
                    float allowedError = Math.Min(lengthSquared * Constellations.RmsTolerance * Constellations.RmsTolerance,
                        Constellations.MaximumRmsError * Constellations.MaximumRmsError) * pattern.Points.Length;
                    for (int mirror = -1; mirror <= 1; mirror += 2)
                    {
                        for (int i = 0; i < pattern.Points.Length; i++) chosen[i] = -1;
                        chosen[pattern.AnchorA] = ia; chosen[pattern.AnchorB] = ib;
                        float error = 0; bool complete = true;
                        foreach (int star in pattern.MatchOrder)
                        {
                            V2 v = pattern.AnchorSpace[star];
                            V2 expected = a + axis * v.X + new V2(-axis.Y, axis.X) * (v.Y * mirror);
                            int nearest = Nearest(expected, toleranceSquared, pattern.Points.Length, out float residual);
                            if (nearest < 0) { complete = false; break; }
                            chosen[star] = nearest; error += residual;
                            if (error > allowedError) { complete = false; break; }
                        }
                        float score = error / lengthSquared;
                        if (complete && score < bestScore)
                        { bestScore = score; Array.Copy(chosen, best, pattern.Points.Length); }
                    }
                }
                if (bestScore == float.MaxValue) continue;
                var discovery = new ConstellationDiscovery {
                    PatternIndex = patternIndex, Tick = tick, Stars = new V2[pattern.Points.Length],
                    PickupIds = new int[pattern.Points.Length], Error = (float)Math.Sqrt(bestScore / pattern.Points.Length)
                };
                for (int i = 0; i < pattern.Points.Length; i++)
                {
                    Pickup p = candidates[best[i]];
                    discovery.Stars[i] = p.Position; discovery.PickupIds[i] = p.Id;
                    discovery.Center += p.Position; discovery.OriginalValue += p.Value;
                }
                discovery.Center /= pattern.Points.Length;
                discovery.Reward = discovery.OriginalValue * 10;
                return discovery;
            }
            return null;
        }

        private int Nearest(V2 p, float toleranceSquared, int starCount, out float residual)
        {
            float radius = (float)Math.Sqrt(toleranceSquared); residual = toleranceSquared; int nearest = -1;
            int minX = Cell(p.X - radius), maxX = Cell(p.X + radius), minY = Cell(p.Y - radius), maxY = Cell(p.Y + radius);
            for (int x = minX; x <= maxX; x++) for (int y = minY; y <= maxY; y++)
            {
                if (!cells.TryGetValue(Key(x, y), out List<int> list)) continue;
                foreach (int i in list)
                {
                    bool used = false;
                    for (int j = 0; j < starCount; j++) if (chosen[j] == i) { used = true; break; }
                    if (used) continue;
                    float d = V2.DistanceSquared(candidates[i].Position, p);
                    if (d < residual) { nearest = i; residual = d; }
                }
            }
            return nearest;
        }
        private static int Cell(float p) => (int)Math.Floor(p / CellSize);
        private static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;
    }
}
