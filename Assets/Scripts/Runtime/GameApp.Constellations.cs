using Squares.Simulation;
using UnityEngine;

namespace Squares
{
    public sealed partial class GameApp
    {
        private bool atlasOpen, constellationDemo, atlasChecked, atlasPausePassed, reviewStargazer;
        private int atlasFrames, atlasTick, constellationCaptureStage;
        private uint reviewSeed = 735193;
        private int reviewCore, reviewPattern;

        private void AddConstellationReviewFixture()
        {
            // Explicit smoke-only fixture. Never runs in ordinary gameplay.
            ConstellationPattern pattern = Constellations.All[reviewPattern];
            for (int i = 0; i < pattern.Points.Length; i++)
            {
                V2 p = new V2(7, 2.5f) + V2.Rotate(pattern.Points[i] * 6, .35f);
                p += new V2((i % 3 - 1) * .035f, (i % 2 == 0 ? 1 : -1) * .035f);
                world.Pickups.Add(new Pickup { Id = 100000 + i, Position = p, Value = 3 });
            }
        }

        private void AtlasScreen()
        {
            Shade(); float x = (uiWidth - 1380) / 2, y = (uiHeight - 760) / 2;
            PanelAt(new Rect(x, y, 1380, 760), SquareView.Gold);
            int mask = profile.constellationMask | (world == null ? 0 : world.ConstellationMask);
            int count = 0; for (int i = 0; i < Constellations.All.Length; i++) if ((mask & (1 << i)) != 0) count++;
            TextAt(new Rect(x + 28, y + 24, 1040, 45), "ЗВЁЗДНЫЙ АТЛАС / " + count + " ИЗ " + Constellations.All.Length, 31, Text, true);
            if (ButtonAt(new Rect(x + 1110, y + 27, 240, 40), "ЗАКРЫТЬ [ C ]", SquareView.Gold)) atlasOpen = false;
            TextAt(new Rect(x + 28, y + 85, 1324, 45), "Оставляй опыт на арене: похожая фигура из белых точек превращается в один большой сгусток с наградой ×10.", 18, Text, wrap: true);
            for (int i = 0; i < Constellations.All.Length; i++)
            {
                float cx = x + 28 + (i % 3) * 444, cy = y + 146 + (i / 3) * 238;
                ConstellationPattern pattern = Constellations.All[i]; bool known = (mask & (1 << i)) != 0;
                PanelAt(new Rect(cx, cy, 420, 216), known ? SquareView.Gold : LineColor);
                Rect diagram = new Rect(cx + 10, cy + 28, 158, 158);
                AtlasDiagram(pattern, diagram, known ? SquareView.Gold : SquareView.Blue);
                TextAt(new Rect(cx + 182, cy + 25, 222, 55), pattern.Name, 21, Text, true, wrap: true);
                TextAt(new Rect(cx + 182, cy + 87, 222, 24), pattern.Points.Length + " ОСНОВНЫХ ЗВЁЗД", 14, SquareView.Gold);
                string tolerance = (Constellations.ToleranceFor(pattern.Points.Length).StarFraction * 100).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                TextAt(new Rect(cx + 182, cy + 113, 222, 24), "ДОПУСК: " + tolerance + "%", 14, SquareView.Mint);
                TextAt(new Rect(cx + 182, cy + 144, 222, 32), pattern.Subtitle, 14, Muted, wrap: true);
                TextAt(new Rect(cx + 182, cy + 179, 222, 25), known ? "УЖЕ ОТКРЫТО" : "ЕЩЁ НЕ НАЙДЕНО", 14, known ? SquareView.Mint : Muted);
            }
            TextAt(new Rect(x + 28, y + 641, 1324, 30), "CTRL: придержать магнит и не подбирать опыт. Отпусти, когда захочешь собрать награду. Игра на паузе.", 17, SquareView.Mint);
            TextAt(new Rect(x + 28, y + 686, 1324, 53), "Поворот и размер допустимы. Меньше звёзд — строже допуск. 5 точек по 3 опыта → один сгусток на 150.\nБонус навыка опыта применяется при подборе. Золотые сгустки повторно в созвездия не входят.", 16, Muted, wrap: true);
        }
        private void AtlasDiagram(ConstellationPattern pattern, Rect rect, Color color)
        {
            Vector2 center = rect.center;
            for (int i = 0; i < pattern.Edges.Length; i += 2)
            {
                V2 pa = pattern.Points[pattern.Edges[i]], pb = pattern.Points[pattern.Edges[i + 1]];
                Vector2 a = center + new Vector2(pa.X, -pa.Y) * rect.width * .82f;
                Vector2 b = center + new Vector2(pb.X, -pb.Y) * rect.width * .82f;
                Matrix4x4 matrix = GUI.matrix;
                GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(a.x, a.y, 0),
                    Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg), Vector3.one);
                Fill(new Rect(0, -1, Vector2.Distance(a, b), 2), color);
                GUI.matrix = matrix;
            }
            foreach (V2 p in pattern.Points)
            {
                Vector2 at = center + new Vector2(p.X, -p.Y) * rect.width * .82f;
                Fill(new Rect(at.x - 4, at.y - 4, 8, 8), Text);
            }
        }

        private void ConstellationHud()
        {
            TextAt(new Rect(28, 101, 410, 25), "СОЗВЕЗДИЯ: " + world.ConstellationsFound + "  /  C: АТЛАС", 15, SquareView.Gold);
            if (world.HoldingExperience || world.Time < 30)
                TextAt(new Rect(28, 129, 430, 25), world.HoldingExperience ? "CTRL / МАГНИТ ПРИДЕРЖАН" : "CTRL: придержать сбор опыта", 14, world.HoldingExperience ? SquareView.Gold : Muted);
            if (mode != ScreenMode.Run || paused || atlasOpen || buildOpen || world.Phase != RunPhase.Running) return;
            foreach (Pickup p in world.Pickups)
            {
                if (!p.Celestial) continue;
                Vector3 screen = view.Camera.WorldToScreenPoint(new Vector3(p.Position.X, p.Position.Y + .95f, 0));
                float sx = screen.x / uiScale, sy = (UnityEngine.Screen.height - screen.y) / uiScale;
                if (sy < 150 || sy > uiHeight - 115) continue;
                TextAt(new Rect(sx - 175, sy, 350, 28), "+" + p.Value + " ОПЫТА", 20, SquareView.Gold, true, TextAnchor.UpperCenter);
            }
            foreach (ConstellationDiscovery d in view.ConstellationVisuals)
            {
                float age = (world.Ticks - d.Tick) * World.Dt;
                float flight = Mathf.Clamp01((age - .8f) / 1.7f), scale = 1 + flight * flight * 10;
                float top = 0; foreach (V2 p in d.Stars) top = Mathf.Max(top, p.Y - d.Center.Y);
                Vector3 screen = view.Camera.WorldToScreenPoint(new Vector3(d.Center.X, d.Center.Y + top * scale + 1 + flight * flight * 24, 0));
                float sx = screen.x / uiScale, sy = (UnityEngine.Screen.height - screen.y) / uiScale;
                if (flight < .1f) { sx = Mathf.Clamp(sx, 350, uiWidth - 350); sy = Mathf.Max(164, sy); }
                Color gold = SquareView.Gold; gold.a = 1 - flight * .8f;
                TextAt(new Rect(sx - 350, sy - 45, 700, 46), Constellations.All[d.PatternIndex].Name.ToUpperInvariant(), 32, gold, true, TextAnchor.UpperCenter);
                TextAt(new Rect(sx - 350, sy + 4, 700, 28), d.Stars.Length + " ЗВЁЗД / " + d.OriginalValue + " × 10 = " + d.Reward + " ОПЫТА", 17, Text, true, TextAnchor.UpperCenter);
            }
        }
        private bool SmokeAtlas()
        {
            if (!atlasChecked && world.Time >= (constellationDemo ? 10 : 75))
            { atlasChecked = true; atlasOpen = true; atlasTick = world.Ticks; }
            if (!atlasOpen) return false;
            if (++atlasFrames == 2) CaptureState("atlas");
            if (atlasFrames < 5) return true;
            atlasPausePassed = world.Ticks == atlasTick; atlasOpen = false; return false;
        }
        private void CaptureConstellationStages()
        {
            if (world.LastConstellation == null || constellationCaptureStage >= 3) return;
            float age = (world.Ticks - world.LastConstellation.Tick) * World.Dt;
            if (constellationCaptureStage == 0 && age >= .15f) { CaptureState("constellation-appear"); constellationCaptureStage++; }
            else if (constellationCaptureStage == 1 && age >= 1.1f) { CaptureState("constellation-flight"); constellationCaptureStage++; }
            else if (constellationCaptureStage == 2 && age >= 2) { CaptureState("constellation-reward"); constellationCaptureStage++; }
        }
    }
}
