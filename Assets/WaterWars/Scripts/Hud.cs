// Hud.cs
// ---------------------------------------------------------------------------
// Everything drawn flat on the screen: score and timer, your Soak meter,
// ammo, allowance, the buy menu, the hero picker, your hero's ability and
// ultimate, status effects, messages, and markers for sites A/B, the Big One
// and revealed enemies.
//
// It uses Unity's simplest drawing tool ("OnGUI", also called IMGUI), which
// needs no art files at all. That's perfect for a prototype. The design doc's
// real UI (sticker and sidewalk-chalk style, the Soak meter as a filling glass
// of water, the buy menu as a lemonade stand) will replace this later.
// ---------------------------------------------------------------------------

using UnityEngine;

namespace WaterWars
{
    public class Hud : MonoBehaviour
    {
        static float hitMarkerUntil;
        static bool hitMarkerHead;
        static bool hitMarkerSoaked;

        // Floating numbers that pop up where your water landed.
        struct Popup { public Vector3 Where; public string Text; public float Born; public Color Color; }
        static readonly System.Collections.Generic.List<Popup> popups = new System.Collections.Generic.List<Popup>();

        static AudioSource sfx;
        static AudioClip hitClip, soakedClip;

        /// Water calls this when YOU hit someone: an X on the crosshair, a "plip"
        /// sound, and a number showing how much soak you added.
        public static void ShowHitMarker(bool head, float amount, Vector3 where, bool soakedThem)
        {
            hitMarkerUntil = Time.time + (soakedThem ? 0.5f : 0.25f);
            hitMarkerHead = head;
            hitMarkerSoaked = soakedThem;
            popups.Add(new Popup
            {
                Where = where + Vector3.up * 0.4f,
                Text = soakedThem ? "SOAKED!" : Mathf.RoundToInt(amount).ToString(),
                Born = Time.time,
                Color = soakedThem ? new Color(1f, 0.85f, 0.2f) : head ? new Color(1f, 0.85f, 0.2f) : Color.white,
            });
            if (popups.Count > 20) popups.RemoveAt(0);
            if (sfx != null)
            {
                sfx.pitch = head ? 1.35f : 1f;
                sfx.PlayOneShot(soakedThem ? soakedClip : hitClip, soakedThem ? 0.9f : 0.6f);
            }
        }

        void Awake()
        {
            // Make the hit sounds in code (no audio files needed): a quick falling "plip",
            // and a longer two-note "bloop" when you soak someone out.
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.spatialBlend = 0f;
            hitClip = MakeTone("Hit", 0.09f, 1400f, 700f);
            soakedClip = MakeTone("Soaked", 0.3f, 600f, 1200f);
        }

        /// Make a little sound in code: a tone sliding from one pitch to another.
        /// `noise` (0..1) mixes in hiss (barks, farts); `wobbleHz` makes it warble (phones ringing).
        public static AudioClip MakeTone(string name, float seconds, float fromHz, float toHz, float noise = 0f, float wobbleHz = 0f)
        {
            int rate = 44100, n = (int)(rate * seconds);
            var data = new float[n];
            float phase = 0f;
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float hz = Mathf.Lerp(fromHz, toHz, t);
                if (wobbleHz > 0f) hz *= 1f + 0.08f * Mathf.Sin(2f * Mathf.PI * wobbleHz * i / rate);
                phase += 2f * Mathf.PI * hz / rate;
                float v = Mathf.Sin(phase) * (1f - noise) + ((float)rng.NextDouble() * 2f - 1f) * noise;
                data[i] = v * (1f - t) * 0.5f;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // Short messages in the middle of the screen ("Nozzle: MIST", "Ring ring!").
        static string toast;
        static float toastUntil;
        public static void Toast(string text) { toast = text; toastUntil = Time.time + 2f; }

        GUIStyle big, mid, small, smallRight, center, button, box;
        float s; // scale so the HUD looks the same size on any screen
        int lastStyleHeight;

        static readonly Color Water = new Color(0.35f, 0.7f, 1f);
        static readonly Color Panel = new Color(0f, 0f, 0f, 0.55f);

        void MakeStyles()
        {
            s = Mathf.Max(0.6f, Screen.height / 1080f);
            if (big != null && lastStyleHeight == Screen.height) return;
            lastStyleHeight = Screen.height;
            big = new GUIStyle(GUI.skin.label) { fontSize = (int)(54 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            mid = new GUIStyle(GUI.skin.label) { fontSize = (int)(30 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(GUI.skin.label) { fontSize = (int)(20 * s), alignment = TextAnchor.UpperLeft, wordWrap = true };
            smallRight = new GUIStyle(small) { alignment = TextAnchor.UpperRight };
            center = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = (int)(22 * s), wordWrap = true };
            box = new GUIStyle(GUI.skin.box);
            foreach (var st in new[] { big, mid, small, smallRight, center }) st.normal.textColor = Color.white;
        }

        void OnGUI()
        {
            var rm = RoundManager.Instance;
            if (rm == null || rm.Player == null) return;
            MakeStyles();
            var player = rm.Player;
            var pc = player.GetComponent<PlayerController>();

            if (rm.Phase == Phase.HeroSelect) { DrawHeroSelect(rm); return; }

            DrawWorldMarkers(rm, pc);
            DrawPopups(pc != null ? pc.Cam : null);
            DrawWetOverlay(player);
            DrawBlind(player);
            DrawTopBar(rm);
            DrawFeed(rm);
            if (player.Alive)
            {
                if (player.Zoomed) DrawScope();
                DrawCrosshair();
                DrawSoak(player);
                DrawToys(player);
                DrawMoneyAndAbility(player, rm);
                DrawStatus(player);
                DrawHints(rm, player);
                DrawProgress(rm, player);
                if (player.Hero == HeroDef.TattleTale) DrawEnemyUlts(rm);
            }
            if (Time.time < toastUntil) Label(new Rect(0, Screen.height * 0.67f, Screen.width, 40 * s), toast, center);
            else if (pc != null)
            {
                string who = pc.Spectating != null ? "Watching " + pc.Spectating.DisplayName + " (click to switch)" : "Your whole crew is soaked";
                Label(new Rect(0, Screen.height - 140 * s, Screen.width, 40 * s), "You're SOAKED! Sitting on the curb until next round.", mid);
                Label(new Rect(0, Screen.height - 95 * s, Screen.width, 30 * s), who, center);
            }

            if (Time.time < rm.BannerUntil)
                Shadowed(new Rect(0, Screen.height * 0.25f, Screen.width, 160 * s), rm.Banner, big);

            if (WWInput.Held(WWKey.Tab) || rm.Phase == Phase.MatchOver) DrawScoreboard(rm);
            if (pc != null && pc.BuyMenuOpen) DrawBuyMenu(rm, player);
            if (pc != null && pc.CursorFree) DrawPauseMenu(pc);
            if (rm.Phase == Phase.MatchOver) DrawMatchOver();
        }

        // ---- Small drawing helpers ------------------------------------------
        static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Label(Rect r, string text, GUIStyle st) { Shadowed(r, text, st); }

        static void Shadowed(Rect r, string text, GUIStyle st)
        {
            var old = st.normal.textColor;
            st.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, st);
            st.normal.textColor = old;
            GUI.Label(r, text, st);
        }

        static string Clock(float seconds)
        {
            int t = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (t / 60) + ":" + (t % 60).ToString("00");
        }

        // ---- Hero picker: 12 heroes, hover to read the kit, click to play ------------
        HeroDef hoveredHero;

        void DrawHeroSelect(RoundManager rm)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.05f, 0.1f, 0.2f, 0.8f));
            Shadowed(new Rect(0, 20 * s, Screen.width, 70 * s), "WATER WARS", big);
            Label(new Rect(0, 90 * s, Screen.width, 30 * s), "Grey-box prototype: Big Splash, 5v5 against bots. Pick a hero (hover to read, click to play).", center);

            int cols = 4;
            float w = 300 * s, h = 82 * s, gap = 14 * s;
            float gridW = cols * w + (cols - 1) * gap;
            if (gridW > Screen.width - 20) { float f = (Screen.width - 20) / gridW; w *= f; gap *= f; gridW = Screen.width - 20; }
            float x0 = (Screen.width - gridW) * 0.5f, y0 = 135 * s;
            var btn = new GUIStyle(button) { fontSize = (int)(19 * s) };
            var all = HeroDef.All;
            HeroDef hover = null;
            for (int i = 0; i < all.Length; i++)
            {
                var hero = all[i];
                var r = new Rect(x0 + (i % cols) * (w + gap), y0 + (i / cols) * (h + gap), w, h);
                Fill(new Rect(r.x, r.y, 10 * s, r.height), hero.Color);
                if (GUI.Button(new Rect(r.x + 10 * s, r.y, r.width - 10 * s, r.height), hero.Name + "\n" + hero.Class, btn)) rm.StartMatch(hero);
                if (r.Contains(Event.current.mousePosition)) hover = hero;
            }
            if (hover != null) hoveredHero = hover;
            var show = hoveredHero ?? all[0];

            float py = y0 + 3 * (h + gap) + 6 * s;
            var panel = new Rect(x0, py, gridW, 300 * s);
            Fill(panel, Panel);
            var title = new GUIStyle(mid) { alignment = TextAnchor.UpperLeft };
            title.normal.textColor = show.Color;
            Shadowed(new Rect(panel.x + 16 * s, panel.y + 8 * s, gridW, 40 * s), show.Name + "  (" + show.Class + ")", title);
            Label(new Rect(panel.x + 16 * s, panel.y + 48 * s, gridW - 32 * s, 260 * s), HeroDescription(show), small);

            string controls =
                "Mouse: look    Left click: squirt    Right click: scope, or your hero sidearm's extra    WASD: move    Space: jump    Ctrl/C: crouch    Shift: walk\n" +
                "R: reload    1 / 2: main toy / sidearm    Q: ability    X: ultimate    F: water balloon    V: hero action    E (hold): plant or disarm\n" +
                "G: drop the Big One    B: Lemonade Stand (allowance phase)    Enter: skip the allowance phase    Tab: scoreboard    Esc: settings";
            Label(new Rect(x0, panel.yMax + 10 * s, gridW, 100 * s), controls, center);
        }

        static string HeroDescription(HeroDef h)
        {
            string side = h.Sidearm != null ? h.Sidearm.Name : "Squirt Pistol";
            string t =
                "Class rule: " + h.ClassRule + "\n" +
                "Passive - " + h.PassiveName + ": " + h.PassiveHelp + "\n" +
                "[Q] " + h.AbilityName + " ($" + h.AbilityCost + ", up to " + h.AbilityMaxCharges + "): " + h.AbilityHelp + "\n" +
                "[X] Ultimate - " + h.UltName + " (" + h.UltCost + " points): " + h.UltHelp + "\n" +
                "Sidearm: " + side;
            if (!string.IsNullOrEmpty(h.ActionHelp)) t += "\nExtra: " + h.ActionHelp;
            return t;
        }

        // ---- Top bar: score, round, clock -------------------------------------
        void DrawTopBar(RoundManager rm)
        {
            float w = 520 * s;
            var r = new Rect((Screen.width - w) * 0.5f, 10 * s, w, 70 * s);
            Fill(r, Panel);
            var orange = new GUIStyle(mid); orange.normal.textColor = TeamInfo.Color(Team.Orange);
            var teal = new GUIStyle(mid); teal.normal.textColor = TeamInfo.Color(Team.Teal);
            GUI.Label(new Rect(r.x, r.y, 150 * s, r.height), rm.Score[0].ToString(), orange);
            GUI.Label(new Rect(r.xMax - 150 * s, r.y, 150 * s, r.height), rm.Score[1].ToString(), teal);

            string clock;
            if (rm.Phase == Phase.Buy) clock = "BUY " + Clock(rm.PhaseTimeLeft);
            else if (rm.Big.State == BigOneState.Planted && rm.Phase == Phase.Play) clock = "FUSE " + Clock(rm.Big.FuseLeft);
            else if (rm.Phase == Phase.Play) clock = Clock(rm.PhaseTimeLeft);
            else clock = "--";
            GUI.Label(new Rect(r.x, r.y - 6 * s, r.width, r.height), clock, mid);
            string sub = (rm.SuddenDeath ? "Sudden death" : "Round " + rm.Round) + "  |  You " + (rm.Player.Team == rm.Attackers ? "attack" : "defend");
            GUI.Label(new Rect(r.x, r.y + 38 * s, r.width, 30 * s), sub, center);

            // Who's still in: one dot per kid.
            DrawDots(rm, Team.Orange, r.x - 10 * s, true);
            DrawDots(rm, Team.Teal, r.xMax + 10 * s, false);
        }

        void DrawDots(RoundManager rm, Team t, float edge, bool leftSide)
        {
            float d = 18 * s;
            int i = 0;
            foreach (var k in rm.Kids)
            {
                if (k.Team != t) continue;
                float x = leftSide ? edge - (i + 1) * (d + 4 * s) : edge + i * (d + 4 * s);
                Fill(new Rect(x, 36 * s, d, d), k.Alive ? TeamInfo.Color(t) : new Color(0.2f, 0.2f, 0.2f, 0.8f));
                i++;
            }
        }

        // ---- Kill feed ("Mo soaked Ravi") ----------------------------------------
        void DrawFeed(RoundManager rm)
        {
            float y = 10 * s;
            for (int i = 0; i < rm.Feed.Count; i++)
            {
                if (Time.time - rm.FeedTimes[i] > 7f) continue;
                Label(new Rect(Screen.width - 520 * s, y, 500 * s, 28 * s), rm.Feed[i], smallRight);
                y += 28 * s;
            }
        }

        // ---- Crosshair ---------------------------------------------------------
        void DrawCrosshair()
        {
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            float len = 8 * s, thick = Mathf.Max(2f, 2 * s), gap = 4 * s;
            Fill(new Rect(cx - thick / 2, cy - gap - len, thick, len), Color.white);
            Fill(new Rect(cx - thick / 2, cy + gap, thick, len), Color.white);
            Fill(new Rect(cx - gap - len, cy - thick / 2, len, thick), Color.white);
            Fill(new Rect(cx + gap, cy - thick / 2, len, thick), Color.white);

            // Hit marker: a big X around the crosshair. Yellow for face hits and soak-outs.
            if (Time.time < hitMarkerUntil)
            {
                Color c = hitMarkerHead || hitMarkerSoaked ? new Color(1f, 0.85f, 0.2f) : new Color(0.55f, 0.85f, 1f);
                float size = (hitMarkerSoaked ? 30 : 20) * s, w = Mathf.Max(4f, 4 * s);
                var old = GUI.matrix;
                for (int i = 0; i < 4; i++)
                {
                    GUIUtility.RotateAroundPivot(45f + 90f * i, new Vector2(cx, cy));
                    Fill(new Rect(cx + 9 * s, cy - w / 2 + 1, size, w), new Color(0f, 0f, 0f, 0.6f));
                    Fill(new Rect(cx + 9 * s, cy - w / 2, size, w), c);
                    GUI.matrix = old;
                }
            }
        }

        // Looking through a scope: dark edges and long thin crosshair lines.
        void DrawScope()
        {
            float r = Screen.height * 0.45f, cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            var dark = new Color(0f, 0f, 0f, 0.85f);
            Fill(new Rect(0, 0, cx - r, Screen.height), dark);
            Fill(new Rect(cx + r, 0, Screen.width - cx - r, Screen.height), dark);
            Fill(new Rect(cx - r, 0, r * 2, cy - r), dark);
            Fill(new Rect(cx - r, cy + r, r * 2, Screen.height - cy - r), dark);
            Fill(new Rect(cx - r, cy - 1, r * 2, 2), new Color(0f, 0f, 0f, 0.8f));
            Fill(new Rect(cx - 1, cy - r, 2, r * 2), new Color(0f, 0f, 0f, 0.8f));
        }

        void DrawPopups(Camera cam)
        {
            if (cam == null) return;
            var st = new GUIStyle(mid);
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                float age = Time.time - popups[i].Born;
                if (age > 0.9f) { popups.RemoveAt(i); continue; }
                Vector3 sp = cam.WorldToScreenPoint(popups[i].Where + Vector3.up * age * 0.8f);
                if (sp.z <= 0f) continue;
                var c = popups[i].Color;
                c.a = 1f - age / 0.9f;
                st.normal.textColor = c;
                GUI.Label(new Rect(sp.x - 100 * s, Screen.height - sp.y - 20 * s, 200 * s, 40 * s), popups[i].Text, st);
            }
        }

        // ---- Soak meter (a glass that fills with water) --------------------------
        void DrawSoak(Kid k)
        {
            float w = 70 * s, h = 200 * s;
            var glass = new Rect(30 * s, Screen.height - h - 40 * s, w, h);
            Fill(new Rect(glass.x - 4 * s, glass.y - 4 * s, glass.width + 8 * s, glass.height + 8 * s), new Color(1f, 1f, 1f, 0.5f));
            Fill(glass, Panel);
            float fill = k.Soak / k.MaxSoak;
            Fill(new Rect(glass.x, glass.yMax - h * fill, w, h * fill), Water);
            Label(new Rect(glass.x - 20 * s, glass.y + h * 0.35f, w + 40 * s, 40 * s), Mathf.RoundToInt(k.Soak).ToString(), mid);
            Label(new Rect(glass.x - 10 * s, glass.yMax + 4 * s, 200 * s, 30 * s), "SOAK /" + Mathf.RoundToInt(k.MaxSoak), small);
            // Shields from teammates' heroes, stacked above the glass.
            string extra = "";
            if (k.Shield > 0f) extra += "Laundry shield " + Mathf.RoundToInt(k.Shield) + "\n";
            if (k.Sunscreen > 0f) extra += "Sunscreen " + Mathf.RoundToInt(k.Sunscreen) + "\n";
            if (extra != "") Label(new Rect(glass.x - 10 * s, glass.y - 60 * s, 300 * s, 56 * s), extra, small);

            // Towel bar next to the glass.
            if (k.Towel > 0f)
            {
                float th = h * (k.Towel / Tuning.BeachTowelAbsorb);
                var towel = new Rect(glass.xMax + 14 * s, glass.yMax - th, 16 * s, th);
                Fill(towel, new Color(1f, 1f, 1f, 0.9f));
                Label(new Rect(glass.xMax + 36 * s, glass.yMax - 30 * s, 200 * s, 30 * s), "Towel " + Mathf.RoundToInt(k.Towel), small);
            }
        }

        // ---- Toys and ammo ---------------------------------------------------------
        void DrawToys(Kid k)
        {
            var toy = k.Current;
            float w = 360 * s;
            var r = new Rect(Screen.width - w - 30 * s, Screen.height - 150 * s, w, 120 * s);
            Fill(r, Panel);
            Label(new Rect(r.x + 14 * s, r.y + 8 * s, w, 30 * s), toy.Def.Name, small);
            string ammo = k.Reloading ? "Reloading..." : toy.Ammo + " / " + toy.Def.TankShots;
            var ammoStyle = new GUIStyle(mid) { alignment = TextAnchor.MiddleLeft };
            Label(new Rect(r.x + 14 * s, r.y + 36 * s, w, 40 * s), ammo, ammoStyle);
            if (k.Reloading) Fill(new Rect(r.x + 14 * s, r.y + 78 * s, (w - 28 * s) * k.ReloadProgress, 6 * s), Water);
            string slots = (k.Primary != null ? "[1] " + k.Primary.Def.Name + "   " : "") + "[2] " + k.Sidearm.Def.Name;
            Label(new Rect(r.x + 14 * s, r.y + 88 * s, w, 30 * s), slots, small);
        }

        void DrawMoneyAndAbility(Kid k, RoundManager rm)
        {
            float w = 760 * s;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height - 96 * s, w, 76 * s);
            Fill(r, Panel);
            if (k.Hero == null) return;
            var h = k.Hero;
            string ult = k.UltReady ? "READY!" : k.UltPoints + "/" + h.UltCost;
            string line1 = "$" + k.Money + "     [Q] " + h.AbilityName + " x" + k.AbilityCharges +
                           "     [X] " + h.UltName + " " + ult + "     [F] Balloons x" + k.Balloons;
            Label(new Rect(r.x + 14 * s, r.y + 6 * s, w - 28 * s, 30 * s), line1, small);
            // Ult charge bar.
            Fill(new Rect(r.x + 14 * s, r.y + 36 * s, w - 28 * s, 5 * s), new Color(1f, 1f, 1f, 0.15f));
            Fill(new Rect(r.x + 14 * s, r.y + 36 * s, (w - 28 * s) * Mathf.Clamp01((float)k.UltPoints / h.UltCost), 5 * s),
                k.UltReady ? new Color(1f, 0.85f, 0.2f) : h.Color);
            string line2 = h.Name + ": " + h.PassiveName;
            if (k.Kit != null && !string.IsNullOrEmpty(k.Kit.Status)) line2 += "     " + k.Kit.Status;
            Label(new Rect(r.x + 14 * s, r.y + 44 * s, w - 28 * s, 30 * s), line2, small);
        }

        // ---- Things other heroes did to you ------------------------------------------
        void DrawStatus(Kid k)
        {
            var bits = new System.Collections.Generic.List<string>();
            if (k.Revealed) bits.Add("REVEALED");
            if (k.Blinded) bits.Add("Hot sauce in your eyes");
            if (k.Disoriented) bits.Add("Dizzy");
            if (k.Rattled) bits.Add("Rattled (shaky aim)");
            if (k.Laughing) bits.Add("Laughing (can't squirt)");
            if (k.Slowed) bits.Add("Slowed");
            if (HeroWorld.AbilitiesBlockedAt(k.transform.position)) bits.Add("A parent is watching: no abilities");
            if (bits.Count == 0) return;
            var st = new GUIStyle(center);
            st.normal.textColor = new Color(1f, 0.75f, 0.4f);
            Label(new Rect(0, Screen.height - 135 * s, Screen.width, 30 * s), string.Join("   |   ", bits.ToArray()), st);
        }

        void DrawBlind(Kid k)
        {
            if (!k.Alive || !k.Blinded) return;
            float a = Mathf.Clamp01(k.BlindLeft / 0.6f) * 0.85f;
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(1f, 0.55f, 0.35f, a));
        }

        /// Tattle Tale's passive: which enemies have their ultimate ready.
        void DrawEnemyUlts(RoundManager rm)
        {
            string t = "I'M TELLING (enemy ultimates):\n";
            foreach (var k in rm.Kids)
            {
                if (k.Team == rm.Player.Team || k.Hero == null) continue;
                t += k.DisplayName + " (" + k.Hero.Name + "): " + (k.UltReady ? "READY" : k.UltPoints + "/" + k.Hero.UltCost) + "\n";
            }
            Label(new Rect(20 * s, 230 * s, 520 * s, 200 * s), t, small);
        }

        // ---- What should I be doing? -------------------------------------------------
        void DrawHints(RoundManager rm, Kid k)
        {
            string hint = null;
            if (rm.Phase == Phase.Buy) hint = "Allowance phase: press B to shop. Press Enter to start the round now.";
            else if (rm.Phase == Phase.Play)
            {
                if (rm.Big.Carrier == k)
                {
                    bool inSite = false;
                    foreach (var site in rm.Map.Sites) if (site.Contains(k.transform.position)) inSite = true;
                    hint = inSite ? "You're on a site: HOLD E to plant the Big One!" : "You have the Big One. Get to site A or B and hold E to plant it (G drops it).";
                }
                else if (k.Team == rm.Defenders && rm.Big.State == BigOneState.Planted)
                    hint = "The Big One is planted at " + rm.Big.PlantedAt.Name + "! Get next to it and HOLD E to disarm.";
                else if (k.Team == rm.Attackers && rm.Big.State == BigOneState.Dropped)
                    hint = "The Big One was dropped! Walk over it to pick it up.";
            }
            if (hint != null) Label(new Rect(0, Screen.height * 0.62f, Screen.width, 40 * s), hint, center);
            // Which 3 houses this map uses (shown while shopping, and on the scoreboard).
            if (rm.Phase == Phase.Buy) Label(new Rect(20 * s, 100 * s, 520 * s, 120 * s), rm.Map.Describe(), small);
        }

        void DrawProgress(RoundManager rm, Kid k)
        {
            float progress = -1f;
            string what = null;
            if (rm.Planter == k) { progress = rm.PlantProgress / Tuning.PlantSeconds; what = "Planting..."; }
            else if (rm.Disarmer == k) { progress = rm.Big.DisarmProgress / Tuning.DisarmSeconds; what = "Disarming..."; }
            if (progress < 0f) return;
            float w = 400 * s;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.56f, w, 22 * s);
            Fill(r, Panel);
            Fill(new Rect(r.x, r.y, w * Mathf.Clamp01(progress), r.height), Water);
            if (rm.Disarmer == k)
            {
                float cp = Tuning.DisarmCheckpoint / Tuning.DisarmSeconds;
                Fill(new Rect(r.x + w * cp - 1, r.y - 4 * s, 3 * s, r.height + 8 * s), Color.white);
            }
            Label(new Rect(r.x, r.y - 32 * s, w, 30 * s), what, center);
        }

        // ---- Getting hit: blue edges, stronger the wetter you are -----------------
        void DrawWetOverlay(Kid k)
        {
            if (!k.Alive) return;
            float recent = Mathf.Clamp01(1f - (Time.time - k.LastHitTime) / 0.4f);
            float a = recent * 0.35f + (k.Soak / k.MaxSoak) * 0.12f;
            if (a <= 0.01f) return;
            float edge = 90 * s;
            var c = new Color(0.2f, 0.5f, 1f, a);
            Fill(new Rect(0, 0, Screen.width, edge), c);
            Fill(new Rect(0, Screen.height - edge, Screen.width, edge), c);
            Fill(new Rect(0, edge, edge, Screen.height - edge * 2), c);
            Fill(new Rect(Screen.width - edge, edge, edge, Screen.height - edge * 2), c);
        }

        // ---- Labels floating over things in the world ---------------------------------
        void DrawWorldMarkers(RoundManager rm, PlayerController pc)
        {
            var cam = pc != null ? pc.Cam : null;
            if (cam == null) return;
            // Site letters: small and see-through, high in the sky, and they hide when
            // they'd cover your crosshair or when you're standing on that site.
            foreach (var site in rm.Map.Sites)
            {
                if (site.Contains(rm.Player.transform.position)) continue;
                Marker(cam, site.Center + Vector3.up * 14f, site.Name, new Color(0.6f, 0.85f, 1f, 0.6f), small, true);
            }

            var big = rm.Big;
            bool showBig = big.State == BigOneState.Planted || rm.Player.Team == rm.Attackers;
            if (showBig && big.Carrier != rm.Player && big.State != BigOneState.Burst)
                Marker(cam, big.Position + Vector3.up * 2.6f, big.State == BigOneState.Planted ? "BIG ONE " + Mathf.CeilToInt(big.FuseLeft) : "BIG ONE", new Color(0.4f, 0.6f, 1f), small);

            foreach (var k in rm.Kids)
                if (k != rm.Player && k.Team == rm.Player.Team && k.Alive)
                    Marker(cam, k.transform.position + Vector3.up * 2.2f, k.DisplayName, TeamInfo.Color(k.Team), small, true);

            // Enemies someone on your crew revealed (Satellite, Phone Call, Jenny, cushions...): seen through walls.
            bool bigEars = rm.Player.Hero == HeroDef.ClassClown && rm.Player.Alive;
            foreach (var k in rm.Kids)
            {
                if (k.Team == rm.Player.Team || !k.Alive) continue;
                if (k.Revealed)
                    Marker(cam, k.transform.position + Vector3.up * 2.3f, "[ " + k.DisplayName + " ]", new Color(1f, 0.3f, 0.25f), small);
                else if (bigEars && k.HorizontalSpeed > Tuning.WalkSpeed + 0.5f &&
                         Vector3.Distance(k.transform.position, rm.Player.transform.position) <= 25f)
                    Marker(cam, k.transform.position + Vector3.up * 1.2f, "(( footsteps ))", new Color(1f, 0.8f, 0.4f, 0.8f), small);
            }
        }

        void Marker(Camera cam, Vector3 world, string text, Color color, GUIStyle st, bool hideNearCrosshair = false)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return; // behind the camera
            if (hideNearCrosshair)
            {
                float d = Vector2.Distance(new Vector2(sp.x, sp.y), new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                if (d < 160f * s) color.a *= Mathf.Clamp01((d - 60f * s) / (100f * s));
                if (color.a <= 0.02f) return;
            }
            var style = new GUIStyle(st) { alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = color;
            Shadowed(new Rect(sp.x - 150 * s, Screen.height - sp.y - 20 * s, 300 * s, 40 * s), text, style);
        }

        // ---- Buy menu ("Lemonade Stand") -----------------------------------------------
        // One column per toy type, plus towels and your ability along the bottom.
        void DrawBuyMenu(RoundManager rm, Kid k)
        {
            var cats = ToyDef.Categories;
            float colW = 250 * s, gap = 12 * s, btnH = 78 * s;
            float w = cats.Length * colW + (cats.Length + 1) * gap;
            if (w > Screen.width - 20) { float f = (Screen.width - 20) / w; colW *= f; gap *= f; w = Screen.width - 20; }
            float h = 640 * s;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            Fill(r, new Color(0.15f, 0.1f, 0.02f, 0.9f));
            Label(new Rect(r.x, r.y + 8 * s, w, 40 * s), "LEMONADE STAND", mid);
            Label(new Rect(r.x, r.y + 46 * s, w, 30 * s), "Allowance: $" + k.Money + "      (B to close)", center);

            var btn = new GUIStyle(button) { fontSize = (int)(17 * s) };
            hoveredToy = null;
            for (int c = 0; c < cats.Length; c++)
            {
                float x = r.x + gap + c * (colW + gap);
                float y = r.y + 85 * s;
                Label(new Rect(x, y, colW, 28 * s), cats[c].ToUpper() + (cats[c] == "Sidearm" ? " (slot 2)" : ""), center);
                y += 32 * s;
                foreach (var toy in ToyDef.InCategory(cats[c]))
                {
                    string why;
                    bool ok = rm.CanBuyToy(k, toy, out why);
                    string text = toy.Name + "\n" + (toy.Cost == 0 ? "Free" : "$" + toy.Cost) + (ok ? "" : "  (" + why + ")");
                    var br = new Rect(x, y, colW, btnH);
                    GUI.enabled = ok;
                    if (GUI.Button(br, text, btn)) rm.BuyToy(k, toy);
                    GUI.enabled = true;
                    if (br.Contains(Event.current.mousePosition)) hoveredToy = toy;
                    y += btnH + 8 * s;
                }
            }

            // Bottom row: towels and ability.
            int items = System.Enum.GetValues(typeof(ShopItem)).Length;
            float by = r.yMax - 175 * s, bw = (w - gap * (items + 1)) / items;
            int i = 0;
            foreach (ShopItem item in System.Enum.GetValues(typeof(ShopItem)))
            {
                string why;
                bool ok = rm.CanBuy(k, item, out why);
                string text = RoundManager.ItemName(item, k) + "\n$" + RoundManager.Cost(item, k) + (ok ? "" : "  (" + why + ")");
                GUI.enabled = ok;
                if (GUI.Button(new Rect(r.x + gap + i * (bw + gap), by, bw, 70 * s), text, btn)) rm.Buy(k, item);
                GUI.enabled = true;
                i++;
            }

            // Stats for the toy under the mouse.
            if (hoveredToy != null)
            {
                var t = hoveredToy;
                string kind = t.Arcing ? "arcing water" : "instant hit";
                if (t.Pellets > 1) kind += ", " + t.Pellets + " drops per shot";
                if (t.Burst > 1) kind += ", " + t.Burst + "-shot burst";
                if (t.ZoomFov > 0f) kind += ", right-click scope";
                string info = t.Name + ": " + t.SoakPerHit + " soak per hit (" + kind + "), " + t.TankShots + " shots, " +
                              (t.HoldToFire ? "hold to fire" : "click per shot") + ". Based on: " + t.Reference;
                Label(new Rect(r.x + 20 * s, r.yMax - 95 * s, w - 40 * s, 80 * s), info, small);
            }
        }

        ToyDef hoveredToy;

        // ---- Esc menu ---------------------------------------------------------------
        void DrawPauseMenu(PlayerController pc)
        {
            float w = 460 * s, h = 330 * s;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            Fill(r, new Color(0f, 0f, 0f, 0.8f));
            Label(new Rect(r.x, r.y + 10 * s, w, 40 * s), "Paused (the game keeps running)", mid);
            Label(new Rect(r.x, r.y + 60 * s, w, 30 * s), "Mouse sensitivity: " + PlayerController.MouseSensitivity.ToString("0.0"), center);
            if (GUI.Button(new Rect(r.x + 40 * s, r.y + 100 * s, 170 * s, 50 * s), "Slower", button))
                PlayerController.MouseSensitivity = Mathf.Max(0.2f, PlayerController.MouseSensitivity - 0.2f);
            if (GUI.Button(new Rect(r.xMax - 210 * s, r.y + 100 * s, 170 * s, 50 * s), "Faster", button))
                PlayerController.MouseSensitivity += 0.2f;
            if (GUI.Button(new Rect(r.x + 40 * s, r.y + 170 * s, w - 80 * s, 55 * s), "Back to the game", button)) pc.CursorFree = false;
            if (GUI.Button(new Rect(r.x + 40 * s, r.y + 240 * s, w - 80 * s, 55 * s), "Restart the match", button)) GameBootstrap.Restart();
        }

        void DrawMatchOver()
        {
            float w = 360 * s;
            if (GUI.Button(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.75f, w, 60 * s), "Play again", button))
                GameBootstrap.Restart();
        }

        // ---- Tab: who's who ------------------------------------------------------------
        void DrawScoreboard(RoundManager rm)
        {
            float w = 760 * s, row = 34 * s;
            float h = row * (rm.Kids.Count + 2) + 20 * s;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.3f, w, h);
            Fill(r, new Color(0f, 0f, 0f, 0.75f));
            float y = r.y + 10 * s;
            Label(new Rect(r.x + 20 * s, y, w, row), "Kid                     Crew        Hero                               Soak     $", small);
            y += row;
            foreach (var k in rm.Kids)
            {
                var st = new GUIStyle(small);
                st.normal.textColor = k.Alive ? TeamInfo.Color(k.Team) : Color.gray;
                string hero = k.Hero != null ? k.Hero.Name : "-";
                GUI.Label(new Rect(r.x + 20 * s, y, 220 * s, row), k.DisplayName + (k == rm.Big.Carrier ? " (Big One)" : ""), st);
                GUI.Label(new Rect(r.x + 200 * s, y, 120 * s, row), k.Team.ToString(), st);
                GUI.Label(new Rect(r.x + 310 * s, y, 300 * s, row), hero, st);
                GUI.Label(new Rect(r.x + 600 * s, y, 80 * s, row), k.Alive ? Mathf.RoundToInt(k.Soak).ToString() : "OUT", st);
                GUI.Label(new Rect(r.x + 680 * s, y, 80 * s, row), k.Team == rm.Player.Team ? "$" + k.Money : "?", st);
                y += row;
            }
        }
    }
}
