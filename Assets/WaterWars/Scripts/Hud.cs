// Hud.cs
// ---------------------------------------------------------------------------
// Everything drawn flat on the screen: score and timer, your Soak meter,
// ammo, allowance, the buy menu, the hero picker, messages, and markers for
// sites A/B and the Big One.
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

        /// Water calls this when YOU hit someone, so the crosshair flashes.
        public static void ShowHitMarker(bool head)
        {
            hitMarkerUntil = Time.time + 0.15f;
            hitMarkerHead = head;
        }

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
            DrawWetOverlay(player);
            DrawTopBar(rm);
            DrawFeed(rm);
            if (player.Alive)
            {
                DrawCrosshair();
                DrawSoak(player);
                DrawToys(player);
                DrawMoneyAndAbility(player, rm);
                DrawHints(rm, player);
                DrawProgress(rm, player);
            }
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

        // ---- Hero picker ---------------------------------------------------------
        void DrawHeroSelect(RoundManager rm)
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.05f, 0.1f, 0.2f, 0.75f));
            Shadowed(new Rect(0, 60 * s, Screen.width, 80 * s), "WATER WARS", big);
            Label(new Rect(0, 140 * s, Screen.width, 40 * s), "Grey-box prototype: Big Splash, 5v5 against bots", center);
            Label(new Rect(0, 200 * s, Screen.width, 40 * s), "Pick a hero (placeholders until the real heroes are ready)", mid);

            float w = 420 * s, h = 170 * s, gap = 40 * s;
            float x = (Screen.width - (w * HeroDef.All.Length + gap * (HeroDef.All.Length - 1))) * 0.5f;
            for (int i = 0; i < HeroDef.All.Length; i++)
            {
                var hero = HeroDef.All[i];
                var r = new Rect(x + i * (w + gap), 270 * s, w, h);
                string text = hero.Name + "\nRole: " + hero.Role + "\n[Q] " + hero.AbilityName + " ($" + hero.AbilityCost + "): " + hero.AbilityHelp;
                if (GUI.Button(r, text, button)) rm.StartMatch(hero);
            }

            string controls =
                "CONTROLS\n" +
                "Mouse: look    Left click: squirt    WASD: move    Space: jump    Ctrl/C: crouch    Shift: walk\n" +
                "R: reload    1 / 2: Soaker / Squirt Pistol    Q: hero ability    E (hold): plant or disarm the Big One\n" +
                "G: drop the Big One    B: buy menu (allowance phase)    Enter: skip the allowance phase\n" +
                "Tab: scoreboard    Esc: free the mouse / settings    [ and ]: mouse sensitivity";
            Label(new Rect(Screen.width * 0.1f, 480 * s, Screen.width * 0.8f, 200 * s), controls, center);
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
            bool hit = Time.time < hitMarkerUntil;
            Color c = hit ? (hitMarkerHead ? new Color(1f, 0.85f, 0.2f) : Water) : Color.white;
            float len = (hit ? 12 : 8) * s, thick = Mathf.Max(2f, 2 * s), gap = 4 * s;
            Fill(new Rect(cx - thick / 2, cy - gap - len, thick, len), c);
            Fill(new Rect(cx - thick / 2, cy + gap, thick, len), c);
            Fill(new Rect(cx - gap - len, cy - thick / 2, len, thick), c);
            Fill(new Rect(cx + gap, cy - thick / 2, len, thick), c);
        }

        // ---- Soak meter (a glass that fills with water) --------------------------
        void DrawSoak(Kid k)
        {
            float w = 70 * s, h = 200 * s;
            var glass = new Rect(30 * s, Screen.height - h - 40 * s, w, h);
            Fill(new Rect(glass.x - 4 * s, glass.y - 4 * s, glass.width + 8 * s, glass.height + 8 * s), new Color(1f, 1f, 1f, 0.5f));
            Fill(glass, Panel);
            float fill = k.Soak / Tuning.SoakedAt;
            Fill(new Rect(glass.x, glass.yMax - h * fill, w, h * fill), Water);
            Label(new Rect(glass.x - 20 * s, glass.y + h * 0.35f, w + 40 * s, 40 * s), Mathf.RoundToInt(k.Soak).ToString(), mid);
            Label(new Rect(glass.x - 10 * s, glass.yMax + 4 * s, 200 * s, 30 * s), "SOAK", small);

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
            float w = 420 * s;
            var r = new Rect((Screen.width - w) * 0.5f, Screen.height - 70 * s, w, 50 * s);
            Fill(r, Panel);
            string ability = k.Hero != null ? "[Q] " + k.Hero.AbilityName + " x" + k.AbilityCharges : "";
            Label(new Rect(r.x + 14 * s, r.y + 10 * s, w, 30 * s), "$" + k.Money + "      " + ability, small);
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
            float a = recent * 0.35f + (k.Soak / Tuning.SoakedAt) * 0.12f;
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
            foreach (var site in rm.Map.Sites)
                Marker(cam, site.Center + Vector3.up * 6f, site.Name, new Color(0.6f, 0.85f, 1f), mid);

            var big = rm.Big;
            bool showBig = big.State == BigOneState.Planted || rm.Player.Team == rm.Attackers;
            if (showBig && big.Carrier != rm.Player && big.State != BigOneState.Burst)
                Marker(cam, big.Position + Vector3.up * 2.6f, big.State == BigOneState.Planted ? "BIG ONE " + Mathf.CeilToInt(big.FuseLeft) : "BIG ONE", new Color(0.4f, 0.6f, 1f), small);

            foreach (var k in rm.Kids)
                if (k != rm.Player && k.Team == rm.Player.Team && k.Alive)
                    Marker(cam, k.transform.position + Vector3.up * 2.2f, k.DisplayName, TeamInfo.Color(k.Team), small);
        }

        void Marker(Camera cam, Vector3 world, string text, Color color, GUIStyle st)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return; // behind the camera
            var style = new GUIStyle(st) { alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = color;
            Shadowed(new Rect(sp.x - 150 * s, Screen.height - sp.y - 20 * s, 300 * s, 40 * s), text, style);
        }

        // ---- Buy menu ("Lemonade Stand") -----------------------------------------------
        void DrawBuyMenu(RoundManager rm, Kid k)
        {
            float w = 560 * s, h = 470 * s;
            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            Fill(r, new Color(0.15f, 0.1f, 0.02f, 0.85f));
            Label(new Rect(r.x, r.y + 10 * s, w, 40 * s), "LEMONADE STAND", mid);
            Label(new Rect(r.x, r.y + 50 * s, w, 30 * s), "Allowance: $" + k.Money + "      (B to close)", center);

            float y = r.y + 95 * s;
            foreach (ShopItem item in System.Enum.GetValues(typeof(ShopItem)))
            {
                string why;
                bool ok = rm.CanBuy(k, item, out why);
                string text = RoundManager.ItemName(item, k) + "   $" + RoundManager.Cost(item, k) + (ok ? "" : "   (" + why + ")");
                GUI.enabled = ok;
                if (GUI.Button(new Rect(r.x + 30 * s, y, w - 60 * s, 70 * s), text, button)) rm.Buy(k, item);
                GUI.enabled = true;
                y += 85 * s;
            }
        }

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
