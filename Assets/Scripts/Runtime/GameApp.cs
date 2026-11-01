using System;
using System.IO;
using Squares.Simulation;
using UnityEngine;

namespace Squares
{
    public sealed partial class GameApp : MonoBehaviour
    {
        private enum ScreenMode { Menu, Run, Results }
        private ScreenMode mode;
        private World world;
        private Profile profile;
        private SaveStore saves;
        private SquareView view;
        private SynthAudio audioBus;
        private bool paused, banked, endedVoluntarily, buildOpen;
        private string skillToast;
        private float skillToastUntil;
        private float accumulator, uiWidth, uiHeight, uiScale;
        private GUIStyle label, title, body;
        private Font font;
        private int menuFrames, errors, picks;
        private int screenshotsCount;
        private bool smoke, smokeFinished, screenshotTaken, smokePaused, pausePassed, draftCaptured, realtimeReview;
        private float reviewDraftTime;
        private int reviewMilestone = 30;
        private bool buildChecked, buildPausePassed, specializationCaptured;
        private int buildFrames, buildTick;
        private int pauseAtTick, pauseFrames, draftFrames;
        private float smokeSeconds = 210;
        private string smokeResult, capturePath;
        private static readonly Color Text = new Color(.88f, .94f, .97f);
        private static readonly Color Muted = new Color(.44f, .56f, .65f);
        private static readonly Color Panel = new Color(.043f, .063f, .095f, .98f);
        private static readonly Color LineColor = new Color(.13f, .21f, .27f);
        private static readonly string[] CoreNames = { "СТРЕЛОК", "СПУТНИК", "ПРОВОДНИК" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<GameApp>() == null) new GameObject("SQUARES // OVERLOAD").AddComponent<GameApp>();
        }
        private void Awake()
        {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
            Application.runInBackground = true;
            Application.logMessageReceived += OnLog;
            string[] args = Environment.GetCommandLineArgs();
            smoke = Array.IndexOf(args, "-squares-smoke") >= 0;
            constellationDemo = smoke && Array.IndexOf(args, "-constellation-demo") >= 0;
            reviewStargazer = smoke && Array.IndexOf(args, "-review-stargazer") >= 0;
            if (uint.TryParse(Argument(args, "-review-seed"), out uint parsedSeed)) reviewSeed = parsedSeed;
            if (int.TryParse(Argument(args, "-review-core"), out int parsedCore)) reviewCore = Mathf.Clamp(parsedCore, 0, 2);
            if (int.TryParse(Argument(args, "-review-pattern"), out int parsedPattern)) reviewPattern = Mathf.Clamp(parsedPattern, 0, Constellations.All.Length - 1);
            realtimeReview = Array.IndexOf(args, "-realtime-review") >= 0;
            smokeResult = Argument(args, "-smoke-result"); capturePath = Argument(args, "-capture");
            if (float.TryParse(Argument(args, "-smoke-seconds"), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float duration)) smokeSeconds = Mathf.Clamp(duration, 20, 1200);
            string root = Path.GetDirectoryName(Application.dataPath);
            saves = new SaveStore(Path.Combine(root, smoke ? "Temp/SmokeSave" : "Saves"));
            profile = smoke ? new Profile() : saves.Load();
            if (smoke) profile.selectedCore = reviewCore;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            view = new SquareView(transform); audioBus = new SynthAudio(gameObject);
            Debug.Log("SQUARES_BOOT_OK version=0.3.1 smoke=" + smoke + " fixture=" + constellationDemo);
        }
        private static string Argument(string[] args, string key)
        { int i = Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        private void OnLog(string condition, string trace, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }
        private void OnDestroy() { Application.logMessageReceived -= OnLog; }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11)) UnityEngine.Screen.fullScreen = !UnityEngine.Screen.fullScreen;
            if (smoke) { SmokeUpdate(); Render(); return; }
            if (Input.GetKeyDown(KeyCode.C)) { atlasOpen = !atlasOpen; buildOpen = false; accumulator = 0; }
            if (atlasOpen)
            { if (Input.GetKeyDown(KeyCode.Escape)) atlasOpen = false; Render(); return; }
            if (mode == ScreenMode.Menu)
            {
                if (Input.GetKeyDown(KeyCode.Return)) StartRun();
            }
            else if (mode == ScreenMode.Results)
            {
                if (Input.GetKeyDown(KeyCode.Return)) { mode = ScreenMode.Menu; world = null; view.ClearEffects(); }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Tab)) { buildOpen = !buildOpen; accumulator = 0; }
                if (Input.GetKeyDown(KeyCode.Escape) && buildOpen) { buildOpen = false; Render(); return; }
                if (Input.GetKeyDown(KeyCode.Escape)) { paused = !paused; accumulator = 0; }
                if (buildOpen) { accumulator = 0; }
                else if (!paused && world.Phase == RunPhase.Draft)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Pick(0);
                    else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Pick(1);
                    else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) Pick(2);
                    if (Input.GetKeyDown(KeyCode.R)) world.Reroll();
                    accumulator = 0;
                }
                else if (!paused && world.Phase == RunPhase.Running)
                {
                    float x = (Held(KeyCode.D, KeyCode.RightArrow) ? 1 : 0) - (Held(KeyCode.A, KeyCode.LeftArrow) ? 1 : 0);
                    float y = (Held(KeyCode.W, KeyCode.UpArrow) ? 1 : 0) - (Held(KeyCode.S, KeyCode.DownArrow) ? 1 : 0);
                    var command = new Command(new V2(x, y), Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift), Input.GetKey(KeyCode.Q),
                        Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
                    accumulator = Mathf.Min(accumulator + Time.unscaledDeltaTime, World.Dt * 8);
                    while (accumulator >= World.Dt && world.Phase == RunPhase.Running)
                    { Tick(command); accumulator -= World.Dt; }
                    if (world.Phase == RunPhase.Dead) EndRun(false);
                }
            }
            Render();
        }
        private static bool Held(KeyCode a, KeyCode b) => Input.GetKey(a) || Input.GetKey(b);
        private void Render() => view.Render(world, Mathf.Min(.05f, Time.unscaledDeltaTime),
            mode != ScreenMode.Run || (!paused && !buildOpen && !atlasOpen && world.Phase == RunPhase.Running), profile.flashes);
        private void Tick(Command command)
        {
            world.Step(command);
            foreach (WorldEvent e in world.Events)
            {
                view.Event(e, profile.flashes); audioBus.Event(e, profile.sound && !smoke);
                if (smoke && e.Kind == EffectKind.Constellation)
                    Debug.Log($"SQUARES_STAR name={Constellations.All[e.Discovery.PatternIndex].Name} stars={e.Discovery.Stars.Length} sum={e.Discovery.OriginalValue} reward={e.Discovery.Reward} rms={e.Discovery.Error:F4} fixture={constellationDemo}");
            }
        }
        private void StartRun()
        {
            uint seed = smoke ? reviewSeed : unchecked((uint)DateTime.UtcNow.Ticks);
            world = new World(seed, profile.selectedCore, profile.vitality, profile.learning, profile.tempo);
            if (constellationDemo) AddConstellationReviewFixture();
            mode = ScreenMode.Run; paused = banked = buildOpen = atlasOpen = false; accumulator = 0; picks = 0; view.ClearEffects();
        }
        private void Pick(int index)
        {
            bool special = world.IsSpecializationDraft;
            string name = special ? SkillText.Specialty((Specialization)(index + 1)) : Upgrades.All[(int)world.Choices[index]].Name;
            int oldSynergies = (world.HasStorm ? 1 : 0) | (world.HasShatter ? 2 : 0) | (world.HasIcebreak ? 4 : 0);
            if (world.Choose(index))
            {
                if (!special) picks++;
                accumulator = 0; skillToast = "Установлено: " + name;
                int newSynergies = (world.HasStorm ? 1 : 0) | (world.HasShatter ? 2 : 0) | (world.HasIcebreak ? 4 : 0);
                if (oldSynergies != newSynergies) skillToast = "СОЧЕТАНИЕ ОТКРЫТО!  " + name + "  /  TAB: подробности";
                skillToastUntil = Time.unscaledTime + 3;
            }
        }
        private void EndRun(bool voluntary)
        {
            if (banked) return;
            banked = true; endedVoluntarily = voluntary; mode = ScreenMode.Results; paused = false;
            profile.flux += world.EarnedFlux; profile.totalRuns++;
            profile.bestKills = Math.Max(profile.bestKills, world.Kills);
            profile.bestSeconds = Math.Max(profile.bestSeconds, (int)world.Time);
            profile.constellationMask |= world.ConstellationMask; profile.totalConstellations += world.ConstellationsFound;
            if (!smoke) saves.Save(profile);
        }

        private void OnGUI()
        {
            uiScale = Mathf.Min(UnityEngine.Screen.width / 1600f, UnityEngine.Screen.height / 900f);
            uiWidth = UnityEngine.Screen.width / uiScale; uiHeight = UnityEngine.Screen.height / uiScale;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * uiScale);
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, richText = false,
                    clipping = TextClipping.Clip, padding = new RectOffset(0, 0, 0, 0) };
                title = new GUIStyle(label) { fontStyle = FontStyle.Bold };
                body = new GUIStyle(label) { wordWrap = true };
            }
            if (atlasOpen) AtlasScreen();
            else if (mode == ScreenMode.Menu) Menu();
            else
            {
                Hud();
                if (mode == ScreenMode.Results) Results();
                else if (buildOpen) BuildScreen();
                else if (paused) PauseMenu();
                else if (world.Phase == RunPhase.Draft) Draft();
            }
            if (!string.IsNullOrEmpty(saves.Error))
                TextAt(new Rect(28, uiHeight - 30, uiWidth - 56, 25), saves.Error, 14, SquareView.Gold);
            GUI.matrix = Matrix4x4.identity;
        }

        private void Menu()
        {
            Fill(new Rect(0, 0, uiWidth, uiHeight), new Color(.025f, .037f, .065f, .96f));
            float left = (uiWidth - 1360) / 2;
            TextAt(new Rect(left, 48, 750, 30), "ОДИН РЕАКТОР. ОДНА АРЕНА. ТЫСЯЧИ СОЧЕТАНИЙ.", 15, SquareView.Mint);
            TextAt(new Rect(left - 4, 94, 760, 100), "SQUARES", 90, Text, true);
            TextAt(new Rect(left, 202, 750, 55), "// OVERLOAD", 42, SquareView.Mint, true);
            TextAt(new Rect(left + 890, 61, 470, 30), "ВЕРСИЯ 0.3.1 / ПЕРЕХВАТ СНАРЯДОВ", 15, Muted, false, TextAnchor.UpperRight);
            TextAt(new Rect(left + 940, 124, 420, 60), profile.flux + "  ФЛЮКСА", 44, SquareView.Gold, true, TextAnchor.UpperRight);
            TextAt(new Rect(left + 960, 198, 400, 40), "РЕКОРД  " + Clock(profile.bestSeconds) + "   /   " + profile.bestKills + " УБИЙСТВ", 17, Muted, false, TextAnchor.UpperRight);
            Fill(new Rect(left, 284, 1360, 1), LineColor);
            TextAt(new Rect(left, 308, 800, 28), "01  /  ВЫБЕРИ СТАРТОВЫЙ РЕАКТОР", 16, Muted);
            string[] descriptions = {
                "СТРЕЛЬБА / ТЕМП\nАвтопушка + скорострельность.\nСтреляй издалека и собирай врагов в ряд.",
                "БЛИЖНИЙ БОЙ / ЗАЩИТА\nАвтопушка + спутник-щит.\nСпутник бьёт врагов и сбивает их пули.",
                "ЦЕПИ / ТОЛПА\nАвтопушка + цепная молния.\nЧем теснее враги, тем сильнее разряд."
            };
            for (int i = 0; i < 3; i++)
            {
                float x = left + i * 462;
                bool unlocked = profile.Unlocked(i), selected = profile.selectedCore == i;
                Color accent = i == 0 ? SquareView.Mint : i == 1 ? SquareView.Blue : SquareView.Gold;
                PanelAt(new Rect(x, 352, 436, 218), selected ? accent : LineColor);
                Fill(new Rect(x + 22, 374, 28, 28), accent);
                Fill(new Rect(x + 29, 381, 14, 14), Panel);
                TextAt(new Rect(x + 64, 372, 345, 35), CoreNames[i], 25, Text, true);
                TextAt(new Rect(x + 22, 423, 392, 82), descriptions[i], 18, Muted, wrap: true);
                string action = unlocked ? selected ? "ВЫБРАН" : "ВЫБРАТЬ" : "ОТКРЫТЬ / " + (i == 1 ? 35 : 60) + " ФЛЮКСА";
                if (ButtonAt(new Rect(x + 22, 520, 392, 32), action, accent,
                    unlocked || profile.flux >= (i == 1 ? 35 : 60)))
                {
                    if (unlocked || profile.BuyCore(i)) { profile.selectedCore = i; saves.Save(profile); }
                }
            }
            TextAt(new Rect(left, 598, 1360, 28), "02  /  УЛУЧШЕНИЯ МЕЖДУ ЗАБЕГАМИ", 16, Muted);
            string[] researchNames = { "ЗДОРОВЬЕ", "ОПЫТ", "СКОРОСТЬ" };
            string[] researchDesc = { "+10 к стартовому здоровью", "+8% получаемого опыта", "+3% скорости движения" };
            for (int i = 0; i < 3; i++)
            {
                float x = left + i * 462;
                PanelAt(new Rect(x, 636, 436, 86), LineColor);
                int rank = profile.ResearchRank(i);
                TextAt(new Rect(x + 16, 648, 230, 24), researchNames[i] + "  " + rank + "/5", 18, Text, true);
                TextAt(new Rect(x + 16, 678, 230, 24), researchDesc[i], 16, Muted);
                if (ButtonAt(new Rect(x + 260, 654, 160, 48), rank >= 5 ? "МАКС." : profile.ResearchCost(i) + " ФЛЮКСА",
                    SquareView.Gold, rank < 5 && profile.flux >= profile.ResearchCost(i)))
                    if (profile.BuyResearch(i)) saves.Save(profile);
            }
            TextAt(new Rect(left, uiHeight - 134, 880, 34), "WASD  ДВИЖЕНИЕ    SPACE  РЫВОК    Q  ПЕРЕГРУЗКА", 17, Text);
            TextAt(new Rect(left, uiHeight - 95, 900, 54), "Созвездия из точек опыта дают награду ×10. C — звёздный атлас.\nCTRL — придержать опыт. TAB — сборка. Босс в 03:00.", 16, Muted, wrap: true);
            if (ButtonAt(new Rect(left + 940, uiHeight - 136, 420, 70), "НА АРЕНУ   [ ENTER ]", SquareView.Mint)) StartRun();
            TextAt(new Rect(left + 940, uiHeight - 49, 420, 24), "F11  ПОЛНЫЙ ЭКРАН  /  ESC  ПАУЗА", 14, Muted, false, TextAnchor.UpperRight);
        }

        private void Hud()
        {
            Fill(new Rect(0, 0, uiWidth, 82), new Color(.025f, .037f, .065f, .97f));
            Fill(new Rect(0, 81, uiWidth, 1), LineColor);
            TextAt(new Rect(28, 16, 270, 24), "SQUARES  //  " + CoreNames[world.Core], 16, SquareView.Mint, true);
            TextAt(new Rect(28, 48, 300, 24), "СЕКТОР " + world.Sector.ToString("00") + " / УРОВЕНЬ " + world.Level.ToString("00"), 15, Muted);
            TextAt(new Rect(340, 13, 340, 25), "ЗДОРОВЬЕ  " + Mathf.CeilToInt(world.Hull) + " / " + (int)world.MaxHull, 16, Text);
            Bar(new Rect(340, 47, 300, 7), world.Hull / world.MaxHull, world.Hull < 30 ? SquareView.Red : SquareView.Mint);
            TextAt(new Rect(uiWidth / 2 - 80, 14, 160, 46), Clock(world.Time), 34, Text, true, TextAnchor.UpperCenter);
            TextAt(new Rect(uiWidth - 560, 17, 285, 24), world.Kills + " УБИТО / " + world.EarnedFlux + " ФЛЮКСА", 15, Text);
            TextAt(new Rect(uiWidth - 560, 48, 285, 24), world.Bosses > 0 ? "БОССОВ: " + world.Bosses + " / БЕСКОНЕЧНОСТЬ" : "ДО БОССА " + Clock(Mathf.Max(0, 180 - world.Time)), 14, Muted);
            if (ButtonAt(new Rect(uiWidth - 230, 20, 200, 40), "ПАУЗА [ ESC ]", Muted)) paused = !paused;
            Bar(new Rect(0, 82, uiWidth, 3), world.XP / world.NextLevelXP, SquareView.Blue);

            float bottom = uiHeight - 84;
            Fill(new Rect(0, bottom, uiWidth, 84), new Color(.025f, .037f, .065f, .97f));
            Fill(new Rect(0, bottom, uiWidth, 1), LineColor);
            TextAt(new Rect(28, bottom + 15, 230, 24), world.DashCooldown <= 0 ? "SPACE / РЫВОК ГОТОВ" : "РЫВОК / " + world.DashCooldown.ToString("F1") + " с", 15, Text);
            Bar(new Rect(28, bottom + 50, 200, 4), 1 - world.DashCooldown / world.DashPeriod, SquareView.Mint);
            TextAt(new Rect(270, bottom + 15, 300, 24), world.Charge >= 100 ? "Q / ПЕРЕГРУЗКА ГОТОВА" : "ПЕРЕГРУЗКА / " + (int)world.Charge + "%", 15, world.Charge >= 100 ? SquareView.Gold : Text);
            Bar(new Rect(270, bottom + 50, 250, 4), world.Charge / 100, SquareView.Gold);
            string modules = "ПУЛИ " + world.Stats.Shots + " / СПУТНИКИ " + world.Rank(Module.Satellite) + " / МОЛНИЯ " + world.Rank(Module.Arc);
            TextAt(new Rect(590, bottom + 15, 570, 24), modules, 16, Text);
            string synergy = "TAB: НАВЫКИ И СОЧЕТАНИЯ  /  " + SkillText.Specialty(world.Specialization);
            TextAt(new Rect(590, bottom + 48, 670, 24), synergy, 13, world.HasStorm || world.HasShatter ? SquareView.Gold : Muted);
            if (world.Combo > 1) TextAt(new Rect(uiWidth - 260, bottom + 17, 230, 43), "x" + world.Combo + " КОМБО", 25, SquareView.Mint, true, TextAnchor.UpperRight);
            else TextAt(new Rect(uiWidth - 260, bottom + 24, 230, 30), "WASD: ДВИЖЕНИЕ", 13, Muted, false, TextAnchor.UpperRight);
            foreach (Enemy e in world.Enemies)
                if (e.Kind == EnemyKind.Boss)
                {
                    TextAt(new Rect(uiWidth / 2 - 270, 100, 540, 24), "ДЕЛИТЕЛЬ // СТРАЖ СЕКТОРА", 15, SquareView.Red, true, TextAnchor.UpperCenter);
                    Bar(new Rect(uiWidth / 2 - 270, 131, 540, 6), e.HP / e.MaxHP, SquareView.Red); break;
                }
            if (world.Time < 10 && world.Phase == RunPhase.Running && !paused && mode == ScreenMode.Run)
            {
                TextAt(new Rect(uiWidth / 2 - 480, uiHeight - 155, 960, 26), "СОБИРАЙ БЕЛЫЕ КВАДРАТЫ ОПЫТА. СТРЕЛЬБА АВТОМАТИЧЕСКАЯ. TAB — СБОРКА.", 16, Muted, false, TextAnchor.UpperCenter);
            }
            if (world.OverloadTime > 0)
                TextAt(new Rect(uiWidth / 2 - 300, uiHeight - 128, 600, 30), "// ПЕРЕГРУЗКА АКТИВНА //", 19, SquareView.Gold, true, TextAnchor.UpperCenter);
            if (Time.unscaledTime < skillToastUntil && world.Phase == RunPhase.Running && !buildOpen)
                TextAt(new Rect(uiWidth / 2 - 550, 150, 1100, 32), skillToast, 21, SquareView.Mint, true, TextAnchor.UpperCenter);
            ConstellationHud();
        }

        private void Draft()
        {
            SkillDraft();
        }
        private void PauseMenu()
        {
            Shade(); float x = (uiWidth - 580) / 2, y = (uiHeight - 470) / 2;
            PanelAt(new Rect(x, y, 580, 470), SquareView.Mint);
            TextAt(new Rect(x + 36, y + 32, 508, 50), "РЕАКТОР НА ПАУЗЕ", 32, Text, true);
            TextAt(new Rect(x + 36, y + 95, 508, 60), "WASD: бег / SPACE: рывок / Q: перегрузка\nTAB: сборка и рецепты. Оружие стреляет само.", 18, Muted, wrap: true);
            if (ButtonAt(new Rect(x + 36, y + 178, 508, 52), "ПРОДОЛЖИТЬ [ ESC ]", SquareView.Mint)) { paused = false; accumulator = 0; }
            if (ButtonAt(new Rect(x + 36, y + 251, 245, 43), "ЗВУК: " + (profile.sound ? "ВКЛ" : "ВЫКЛ"), Muted))
            { profile.sound = !profile.sound; saves.Save(profile); }
            if (ButtonAt(new Rect(x + 299, y + 251, 245, 43), "ТРЯСКА: " + (profile.flashes ? "ВКЛ" : "ВЫКЛ"), Muted))
            { profile.flashes = !profile.flashes; saves.Save(profile); }
            TextAt(new Rect(x + 36, y + 317, 508, 28), "ЗАВЕРШИТЬ ЗАБЕГ И СОХРАНИТЬ " + world.EarnedFlux + " ФЛЮКСА", 15, Muted);
            if (ButtonAt(new Rect(x + 36, y + 362, 508, 52), "ЗАВЕРШИТЬ ЗАБЕГ", SquareView.Gold)) EndRun(true);
        }
        private void Results()
        {
            Shade(); float x = (uiWidth - 900) / 2, y = (uiHeight - 550) / 2;
            PanelAt(new Rect(x, y, 900, 550), SquareView.Mint);
            TextAt(new Rect(x + 42, y + 35, 816, 28), "ИТОГИ ЗАБЕГА / SEED " + world.Seed, 15, Muted);
            TextAt(new Rect(x + 42, y + 90, 816, 62), endedVoluntarily ? "РЕАКТОР ЭВАКУИРОВАН" : "РЕАКТОР РАЗРУШЕН", 41, Text, true);
            TextAt(new Rect(x + 42, y + 166, 816, 40), SkillText.Specialty(world.Specialization) + " / СЛЕДУЮЩАЯ СБОРКА БУДЕТ ДРУГОЙ.", 17, SquareView.Mint);
            TextAt(new Rect(x + 42, y + 207, 816, 30), "СОЗВЕЗДИЙ: " + world.ConstellationsFound + " / ПОДОБРАНО ЗВЁЗДНОГО ОПЫТА: " + world.CelestialExperienceCollected, 16, SquareView.Gold);
            string[] names = { "ВРЕМЯ", "УБИЙСТВА", "УРОВЕНЬ", "БОССЫ" };
            string[] values = { Clock(world.Time), world.Kills.ToString(), world.Level.ToString(), world.Bosses.ToString() };
            for (int i = 0; i < 4; i++)
            {
                TextAt(new Rect(x + 42 + i * 210, y + 244, 190, 25), names[i], 15, Muted);
                TextAt(new Rect(x + 42 + i * 210, y + 281, 190, 60), values[i], 40, Text, true);
            }
            TextAt(new Rect(x + 42, y + 372, 816, 45), "+ " + world.EarnedFlux + " ФЛЮКСА СОХРАНЕНО", 29, SquareView.Gold, true);
            if (ButtonAt(new Rect(x + 42, y + 446, 816, 62), "В АНГАР [ ENTER ]", SquareView.Mint))
            { mode = ScreenMode.Menu; world = null; view.ClearEffects(); }
        }

        private void Shade() => Fill(new Rect(0, 0, uiWidth, uiHeight), new Color(.012f, .02f, .04f, .86f));
        private void PanelAt(Rect r, Color border)
        { Fill(r, border); Fill(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), Panel); }
        private static void Fill(Rect r, Color c)
        { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
        private void Bar(Rect r, float value, Color c)
        { Fill(r, LineColor); Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(value), r.height), c); }
        private void TextAt(Rect rect, string text, int size, Color color, bool bold = false,
            TextAnchor anchor = TextAnchor.UpperLeft, bool wrap = false)
        {
            GUIStyle style = wrap ? body : bold ? title : label;
            style.fontSize = size; style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            style.alignment = anchor; style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }
        private bool ButtonAt(Rect rect, string text, Color accent, bool enabled = true)
        {
            bool hover = enabled && rect.Contains(Event.current.mousePosition);
            Fill(rect, enabled ? hover ? new Color(accent.r * .3f, accent.g * .3f, accent.b * .3f) : new Color(accent.r * .13f, accent.g * .13f, accent.b * .13f) : new Color(.055f, .075f, .10f));
            Fill(new Rect(rect.x, rect.yMax - 2, rect.width, 2), enabled ? accent : LineColor);
            TextAt(new Rect(rect.x + 8, rect.y + (rect.height - 20) / 2, rect.width - 16, 24), text, 16,
                enabled ? hover ? Color.white : accent : Muted, true, TextAnchor.UpperCenter);
            if (enabled && Event.current.type == EventType.MouseDown && Event.current.button == 0 && hover)
            { Event.current.Use(); return true; }
            return false;
        }
        private static string Clock(float seconds)
        { int s = Mathf.Max(0, (int)seconds); return (s / 60).ToString("00") + ":" + (s % 60).ToString("00"); }

        private void SmokeUpdate()
        {
            if (smokeFinished) return;
            if (mode == ScreenMode.Menu)
            {
                menuFrames++;
                if (menuFrames == 3) CaptureState("menu");
                if (menuFrames >= 6) StartRun();
                return;
            }
            if (!smokePaused && world.Ticks >= 720)
            { smokePaused = true; paused = true; pauseAtTick = world.Ticks; }
            if (paused)
            {
                if (pauseFrames == 1) CaptureState("pause");
                if (++pauseFrames < 4) return;
                pausePassed = world.Ticks == pauseAtTick; paused = false;
            }
            if (!buildChecked && world.Time >= 60)
            { buildChecked = true; buildOpen = true; buildTick = world.Ticks; }
            if (buildOpen)
            {
                if (++buildFrames == 2) CaptureState("build");
                if (buildFrames < 5) return;
                buildPausePassed = world.Ticks == buildTick; buildOpen = false;
            }
            if (SmokeAtlas()) return;
            if (world.Phase == RunPhase.Draft)
            {
                if (world.IsSpecializationDraft && !specializationCaptured && draftFrames == 1)
                { CaptureState("specialization"); specializationCaptured = true; }
                if (!draftCaptured && draftFrames == 1) { CaptureState("draft"); draftCaptured = true; }
                reviewDraftTime += Time.unscaledDeltaTime;
                if (++draftFrames > 2 && (!realtimeReview || reviewDraftTime >= 1.4f))
                { Pick(AutoPilot.Pick(world)); draftFrames = 0; reviewDraftTime = 0; }
                return;
            }
            int steps = 24;
            if (realtimeReview)
            {
                accumulator = Mathf.Min(accumulator + Time.unscaledDeltaTime, World.Dt * 8);
                steps = (int)(accumulator / World.Dt); accumulator -= steps * World.Dt;
            }
            for (int i = 0; i < steps && world.Phase == RunPhase.Running && world.Time < smokeSeconds; i++)
            {
                Command input = AutoPilot.Input(world);
                if (constellationDemo && world.Ticks < 90) input.HoldExperience = true;
                if (reviewStargazer && world.Time > 15 && world.Time % 45 < 12) input.HoldExperience = true;
                Tick(input);
            }
            CaptureConstellationStages();
            if (world.Time >= reviewMilestone)
            {
                CaptureState("combat-" + reviewMilestone);
                Debug.Log($"SQUARES_REVIEW t={world.Time:F1} kills={world.Kills} level={world.Level} hull={world.Hull:F1} bosses={world.Bosses} constellations={world.ConstellationsFound} star_xp={world.CelestialExperienceCollected}");
                reviewMilestone += 30;
            }
            if (!screenshotTaken && world.Time >= 45 && world.Phase == RunPhase.Running && !string.IsNullOrEmpty(capturePath))
            {
                string dir = Path.GetDirectoryName(capturePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(capturePath); screenshotTaken = true; screenshotsCount++;
            }
            if (world.Phase == RunPhase.Dead || world.Time >= smokeSeconds)
            {
                EndRun(world.Phase != RunPhase.Dead);
                CaptureState("results");
                smokeFinished = true; Invoke(nameof(WriteSmokeResult), .5f);
            }
        }
        private void CaptureState(string name)
        {
            if (string.IsNullOrEmpty(capturePath)) return;
            string directory = Path.GetDirectoryName(capturePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, name + ".png"));
            screenshotsCount++;
        }
        [Serializable]
        private sealed class SmokeReport
        {
            public bool passed, pausePassed, buildPausePassed, atlasPausePassed, screenshotRequested, fixture;
            public int errors, ticks, kills, level, bosses, picks, flux, constellations, starPickups, screenshots, intercepts, shieldBlocks;
            public long starXP, starXPCreated;
            public float seconds, hull;
            public string fingerprint;
        }
        private void WriteSmokeResult()
        {
            bool combatPassed = constellationDemo ? world.ConstellationsFound > 0 && world.CelestialPickupsCollected > 0 : world.Kills >= 20 && world.Time >= 30;
            var report = new SmokeReport { passed = errors == 0 && pausePassed && (!buildChecked || buildPausePassed) && (!atlasChecked || atlasPausePassed) && picks >= 3 && combatPassed,
                pausePassed = pausePassed, buildPausePassed = buildPausePassed, atlasPausePassed = atlasPausePassed, fixture = constellationDemo,
                constellations = world.ConstellationsFound, starPickups = world.CelestialPickupsCollected, starXP = world.CelestialExperienceCollected,
                intercepts = world.ProjectilesIntercepted, shieldBlocks = world.ShieldBlocks,
                starXPCreated = world.CelestialExperienceCreated, screenshotRequested = screenshotsCount > 0,
                screenshots = screenshotsCount, errors = errors, ticks = world.Ticks,
                kills = world.Kills, level = world.Level, bosses = world.Bosses, picks = picks, flux = world.EarnedFlux,
                seconds = world.Time, hull = world.Hull, fingerprint = world.Fingerprint().ToString() };
            string json = JsonUtility.ToJson(report, true);
            if (!string.IsNullOrEmpty(smokeResult))
            { Directory.CreateDirectory(Path.GetDirectoryName(smokeResult)); File.WriteAllText(smokeResult, json); }
            Debug.Log("SQUARES_SMOKE_" + (report.passed ? "PASS " : "FAIL ") + json);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
