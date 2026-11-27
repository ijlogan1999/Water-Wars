// RoundManager.cs
// ---------------------------------------------------------------------------
// The referee. It runs the Big Splash match from start to finish:
//
//   1. Pick your hero. The bots take the other heroes (no two kids share one).
//   2. Allowance phase (30 s): buy toys, towels, balloons and ability charges. Nobody
//      can move yet. Press Enter to skip ahead.
//   3. Play (1:40): attackers try to plant the Big One at site A or B.
//      * Soak the whole other crew        -> you win the round.
//      * Big One planted and bursts        -> attackers win.
//      * Big One disarmed                  -> defenders win.
//      * Clock runs out with no plant      -> "the streetlights come on",
//                                             everyone goes home, defenders win.
//   4. Round over: hand out allowance, then the next round.
//   Ultimate points: +1 per enemy you soak, +1 per round, +1 for planting or
//   disarming.
//   First crew to 7 rounds wins. Crews swap sides after round 6. At 6-6 the
//   last round is sudden death with a fixed loadout.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace WaterWars
{
    public enum Phase { HeroSelect, Buy, Play, RoundOver, MatchOver }
    public enum ShopItem { HandTowel, BeachTowel, Balloon, Ability }

    public class RoundManager : MonoBehaviour
    {
        public static RoundManager Instance { get; private set; }

        public MapData Map;
        public readonly List<Kid> Kids = new List<Kid>();
        public Kid Player;
        public BigOne Big;

        public Phase Phase { get; private set; }
        public float PhaseTimeLeft { get; private set; }
        public int Round { get; private set; }
        public bool SuddenDeath { get; private set; }
        public readonly int[] Score = new int[2];
        readonly int[] lossStreak = new int[2];

        public Team Attackers { get; private set; }
        public Team Defenders { get { return TeamInfo.Other(Attackers); } }

        // Plant / disarm progress (the HUD draws bars from these).
        public float PlantProgress { get; private set; }
        public Kid Planter { get; private set; }
        public Kid Disarmer { get; private set; }

        // Hints the bots read.
        public Site AttackPlan { get; private set; }   // the site attacking bots go for this round
        public Site HotSite { get; private set; }      // a site where defenders are getting soaked

        public bool CombatAllowed { get { return Phase == Phase.Play; } }
        public bool MovementLocked { get { return Phase == Phase.HeroSelect || Phase == Phase.Buy || Phase == Phase.MatchOver; } }
        public bool IsInteracting(Kid k) { return k != null && (k == Planter || k == Disarmer); }

        // Messages for the HUD.
        public string Banner { get; private set; }
        public float BannerUntil { get; private set; }
        public readonly List<string> Feed = new List<string>();
        public readonly List<float> FeedTimes = new List<float>();

        readonly Dictionary<Kid, bool> survivedLastRound = new Dictionary<Kid, bool>();

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        // =====================================================================
        // Setup
        // =====================================================================
        static readonly string[] OrangeNames = { "Danny", "Keisha", "Mo", "Theo" };
        static readonly string[] TealNames = { "Ravi", "Becca", "Lou", "Tasha", "Gus" };

        public void Setup(MapData map, Transform world)
        {
            Map = map;
            Big = BigOne.Create(world);

            Player = Kid.Create("You", Team.Orange, true, world);
            Player.gameObject.AddComponent<PlayerController>();
            Kids.Add(Player);
            for (int i = 0; i < Tuning.TeamSize - 1; i++) AddBot(OrangeNames[i % OrangeNames.Length], Team.Orange, world);
            for (int i = 0; i < Tuning.TeamSize; i++) AddBot(TealNames[i % TealNames.Length], Team.Teal, world);

            // Stand everyone at their spawns while you pick a hero.
            Attackers = Tuning.YouStartOnDefense ? Team.Teal : Team.Orange;
            PlaceEveryone(false);
            Phase = Phase.HeroSelect;
        }

        void AddBot(string name, Team team, Transform world)
        {
            var bot = Kid.Create(name, team, false, world);
            bot.gameObject.AddComponent<BotBrain>();
            Kids.Add(bot);
        }

        /// Called by the hero picker on screen.
        public void StartMatch(HeroDef playerHero)
        {
            // Everyone gets a different hero: shuffle the rest and deal them out.
            var pool = new List<HeroDef>(HeroDef.All);
            pool.Remove(playerHero);
            for (int i = pool.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); var t = pool[i]; pool[i] = pool[j]; pool[j] = t; }
            int next = 0;
            foreach (var k in Kids)
            {
                k.SetHero(k == Player ? playerHero : pool[next++ % pool.Count]);
                k.Money = Tuning.StartMoney;
                k.StripGear();
                survivedLastRound[k] = false;
            }
            Score[0] = Score[1] = 0;
            lossStreak[0] = lossStreak[1] = 0;
            Round = 1;
            StartRound();
        }

        // =====================================================================
        // Rounds
        // =====================================================================
        void StartRound()
        {
            HeroWorld.Clear();   // throw away last round's fans, walls, traps and clouds
            if (Tuning.NewMapEachRound && Round > 1) Map = GameBootstrap.RegenerateMap();
            SuddenDeath = Round > Tuning.SwapSidesAfterRound * 2;
            // You are on Orange. FirstAttackers is whoever attacks in the first half.
            Team firstAttackers = Tuning.YouStartOnDefense ? Team.Teal : Team.Orange;
            Team firstDefenders = firstAttackers == Team.Orange ? Team.Teal : Team.Orange;
            Attackers = Round <= Tuning.SwapSidesAfterRound || SuddenDeath ? firstAttackers : firstDefenders;
            PlaceEveryone(true);

            if (SuddenDeath)
            {
                // Fixed loadout: everyone gets the same gear, money doesn't matter.
                foreach (var k in Kids)
                {
                    k.Primary = new ToyState(ToyDef.PumpSoaker);
                    k.UsingPrimary = true;
                    k.Towel = Tuning.HandTowelAbsorb;
                    if (k.Hero != null) k.AbilityCharges = k.Hero.AbilityMaxCharges;
                }
            }

            // Hand the Big One to an attacker. If you're attacking, it's yours.
            Kid carrier = Player.Team == Attackers ? Player : null;
            if (carrier == null)
            {
                var options = Kids.FindAll(k => k.Team == Attackers);
                carrier = options[Random.Range(0, options.Count)];
            }
            Big.GiveTo(carrier);

            PlantProgress = 0f;
            Planter = null;
            Disarmer = null;
            HotSite = null;
            AttackPlan = Map.Sites[Random.Range(0, Map.Sites.Length)];

            Phase = Phase.Buy;
            PhaseTimeLeft = SuddenDeath ? Tuning.SuddenDeathBuySeconds : Tuning.BuyPhaseSeconds;

            // Bots do their shopping and pick their jobs.
            foreach (var k in Kids)
            {
                var brain = k.GetComponent<BotBrain>();
                if (brain != null) brain.OnRoundStart();
            }
            string side = Player.Team == Attackers ? "ATTACK: carry the Big One to site A or B" : "DEFEND: protect sites A and B";
            ShowBanner((SuddenDeath ? "SUDDEN DEATH" : "Round " + Round) + "\n" + side, 5f);
        }

        void PlaceEveryone(bool newRound)
        {
            int a = 0, d = 0;
            foreach (var k in Kids)
            {
                bool attacking = k.Team == Attackers;
                Vector3 spawn = attacking ? Map.AttackerSpawns[a++ % Map.AttackerSpawns.Count] : Map.DefenderSpawns[d++ % Map.DefenderSpawns.Count];
                float yaw = attacking ? Map.AttackerFacing : Map.DefenderFacing; // face the other crew
                bool survived = newRound && survivedLastRound.ContainsKey(k) && survivedLastRound[k];
                k.ResetForRound(spawn, yaw, survived);
            }
        }

        public void SkipBuyPhase()
        {
            if (Phase == Phase.Buy) PhaseTimeLeft = Mathf.Min(PhaseTimeLeft, 0.01f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            switch (Phase)
            {
                case Phase.Buy:
                    PhaseTimeLeft -= dt;
                    if (PhaseTimeLeft <= 0f)
                    {
                        Phase = Phase.Play;
                        PhaseTimeLeft = Tuning.RoundSeconds;
                        ShowBanner("GO!", 1.5f);
                    }
                    break;

                case Phase.Play:
                    UpdatePlay(dt);
                    break;

                case Phase.RoundOver:
                    PhaseTimeLeft -= dt;
                    if (PhaseTimeLeft <= 0f) NextRound();
                    break;
            }
        }

        void UpdatePlay(float dt)
        {
            HandleBigOnePickup();
            HandlePlanting(dt);
            if (Phase != Phase.Play) return;
            HandleDisarming(dt);
            if (Phase != Phase.Play) return;

            if (Big.State == BigOneState.Planted)
            {
                if (Big.TickFuse(dt))
                {
                    Vector3 at = Big.Position + Vector3.up;
                    Big.MarkBurst();
                    EndRound(Attackers, "The Big One BURST!");
                    WaterShots.Splash(at, WaterShots.WaterColor, 60, 9f);
                    WaterShots.AreaSoak(null, at, 8f, 100f, 60f); // anyone too close gets drenched
                    return;
                }
            }
            else
            {
                PhaseTimeLeft -= dt;
                if (PhaseTimeLeft <= 0f)
                {
                    EndRound(Defenders, "The streetlights came on. Everybody home!");
                    return;
                }
            }

            int attackersUp = 0, defendersUp = 0;
            foreach (var k in Kids)
            {
                if (!k.Alive) continue;
                if (k.Team == Attackers) attackersUp++; else defendersUp++;
            }
            if (defendersUp == 0) EndRound(Attackers, "Every defender got soaked!");
            else if (attackersUp == 0 && Big.State != BigOneState.Planted) EndRound(Defenders, "Every attacker got soaked!");
        }

        void HandleBigOnePickup()
        {
            if (Big.State != BigOneState.Dropped) return;
            foreach (var k in Kids)
            {
                if (!k.Alive || k.Team != Attackers) continue;
                if (Vector3.Distance(k.transform.position, Big.Position) <= Tuning.PickupReach)
                {
                    Big.GiveTo(k);
                    AddFeed(k.DisplayName + " picked up the Big One");
                    return;
                }
            }
        }

        void HandlePlanting(float dt)
        {
            var carrier = Big.State == BigOneState.Carried ? Big.Carrier : null;
            Site site = null;
            if (carrier != null && carrier.WantsInteract && carrier.Alive && carrier.Grounded)
                foreach (var s in Map.Sites) if (s.Contains(carrier.transform.position)) site = s;

            if (site == null)
            {
                PlantProgress = 0f;
                Planter = null;
                return;
            }
            Planter = carrier;
            PlantProgress += dt;
            if (PlantProgress >= Tuning.PlantSeconds)
            {
                Big.Plant(site, carrier.transform.position + carrier.transform.forward * 0.8f);
                Planter = null;
                PlantProgress = 0f;
                foreach (var k in Kids) if (k.Team == Attackers) k.Money = Mathf.Min(Tuning.MaxMoney, k.Money + Tuning.PlantReward);
                carrier.AddUltPoints(1);
                AddFeed(carrier.DisplayName + " planted the Big One at " + site.Name + "!");
                ShowBanner("The Big One is planted at " + site.Name + "!", 3f);
            }
        }

        void HandleDisarming(float dt)
        {
            if (Big.State != BigOneState.Planted) { Disarmer = null; return; }
            Kid who = null;
            foreach (var k in Kids)
            {
                if (!k.Alive || k.Team != Defenders || !k.WantsInteract) continue;
                Vector3 d = k.transform.position - Big.Position;
                if (Mathf.Abs(d.y) < 2.5f && new Vector2(d.x, d.z).magnitude <= Tuning.DisarmReach) { who = k; break; }
            }
            Disarmer = who;
            if (who == null)
            {
                // Let go: keep progress only if past the halfway checkpoint.
                Big.DisarmProgress = Big.DisarmProgress >= Tuning.DisarmCheckpoint ? Tuning.DisarmCheckpoint : 0f;
                return;
            }
            Big.DisarmProgress += dt;
            if (Big.DisarmProgress >= Tuning.DisarmSeconds)
            {
                Big.MarkDisarmed();
                Disarmer = null;
                who.AddUltPoints(1);
                AddFeed(who.DisplayName + " disarmed the Big One");
                EndRound(Defenders, who.DisplayName + " disarmed the Big One!");
            }
        }

        /// Called by Kid.TakeSoak when someone's meter hits 100.
        public void OnKidSoaked(Kid victim, Kid attacker)
        {
            if (victim.IsOut) return;
            if (Big.Carrier == victim)
            {
                Big.Drop(victim.transform.position);
                AddFeed(victim.DisplayName + " dropped the Big One");
            }

            // Send them to their crew's curb.
            bool attacking = victim.Team == Attackers;
            var curb = attacking ? Map.AttackerCurb : Map.DefenderCurb;
            int seat = 0;
            foreach (var k in Kids) if (k.IsOut && k.Team == victim.Team) seat++;
            Vector3 where = victim.transform.position;
            victim.SendToCurb(curb[seat % curb.Count]);

            if (attacker != null && attacker.Team != victim.Team)
            {
                attacker.AddUltPoints(1);
                attacker.Money = Mathf.Min(Tuning.MaxMoney, attacker.Money + Tuning.SoakReward);
                AddFeed(attacker.DisplayName + " soaked " + victim.DisplayName);
            }
            else AddFeed(victim.DisplayName + " got drenched");

            // Defenders being soaked near a site makes the other defenders rotate there.
            if (!attacking)
                foreach (var s in Map.Sites)
                    if (Vector3.Distance(s.Center, where) < 25f) HotSite = s;
        }

        void EndRound(Team winner, string reason)
        {
            if (Phase != Phase.Play) return;
            Phase = Phase.RoundOver;
            PhaseTimeLeft = Tuning.RoundOverSeconds;
            Planter = null;
            Disarmer = null;
            Team loser = TeamInfo.Other(winner);
            Score[(int)winner]++;
            lossStreak[(int)winner] = 0;
            lossStreak[(int)loser]++;

            int lossPay = Mathf.Min(Tuning.LossMoneyCap, Tuning.LossMoney + Tuning.LossStreakBonus * (lossStreak[(int)loser] - 1));
            foreach (var k in Kids)
            {
                int pay = k.Team == winner ? Tuning.WinMoney : lossPay;
                k.Money = Mathf.Min(Tuning.MaxMoney, k.Money + pay);
                k.AddUltPoints(1);
                survivedLastRound[k] = k.Alive;
            }

            bool youWon = winner == Player.Team;
            ShowBanner((youWon ? "ROUND WON" : "ROUND LOST") + "\n" + reason, Tuning.RoundOverSeconds);
        }

        void NextRound()
        {
            if (Score[0] >= Tuning.RoundsToWin || Score[1] >= Tuning.RoundsToWin)
            {
                Phase = Phase.MatchOver;
                Team champ = Score[0] >= Tuning.RoundsToWin ? Team.Orange : Team.Teal;
                ShowBanner(champ == Player.Team ? "YOU WIN THE MATCH!" : TeamInfo.Name(champ) + " wins the match", 9999f);
                return;
            }
            Round++;
            if (Round == Tuning.SwapSidesAfterRound + 1)
            {
                // Half time: switch sides and start fresh, like the genre.
                foreach (var k in Kids)
                {
                    k.StripGear();
                    k.Money = Tuning.StartMoney;
                    survivedLastRound[k] = false;
                }
                lossStreak[0] = lossStreak[1] = 0;
                AddFeed("Switching sides!");
            }
            StartRound();
        }

        // =====================================================================
        // The Lemonade Stand (buy menu)
        // =====================================================================
        public static string ItemName(ShopItem item, Kid kid)
        {
            switch (item)
            {
                case ShopItem.HandTowel: return "Hand Towel (absorbs " + Tuning.HandTowelAbsorb + ")";
                case ShopItem.BeachTowel: return "Beach Towel (absorbs " + Tuning.BeachTowelAbsorb + ")";
                case ShopItem.Balloon: return "Water Balloon [F]" + (kid.Kit != null && kid.Kit.HotSauce ? " (hot sauce)" : "");
                default: return kid.Hero != null ? kid.Hero.AbilityName + " charge" : "Ability charge";
            }
        }

        public static int Cost(ShopItem item, Kid kid)
        {
            switch (item)
            {
                case ShopItem.HandTowel: return Tuning.HandTowelCost;
                case ShopItem.BeachTowel: return Tuning.BeachTowelCost;
                case ShopItem.Balloon: return Tuning.BalloonCost;
                default: return kid.Hero != null ? kid.Hero.AbilityCost : 999;
            }
        }

        /// Can this kid buy this item right now? `why` explains if not.
        public bool CanBuy(Kid kid, ShopItem item, out string why)
        {
            why = null;
            if (Phase != Phase.Buy) { why = "Only during the allowance phase"; return false; }
            if (kid.Money < Cost(item, kid)) { why = "Not enough allowance"; return false; }
            switch (item)
            {
                case ShopItem.HandTowel:
                    if (kid.Towel >= Tuning.HandTowelAbsorb) { why = "Towel already on"; return false; }
                    break;
                case ShopItem.BeachTowel:
                    if (kid.Towel >= Tuning.BeachTowelAbsorb) { why = "Towel already on"; return false; }
                    break;
                case ShopItem.Balloon:
                    if (kid.Balloons >= (kid.Kit != null ? kid.Kit.MaxBalloons : 1)) { why = "Can't carry more"; return false; }
                    break;
                case ShopItem.Ability:
                    if (kid.Hero == null || kid.AbilityCharges >= kid.Hero.AbilityMaxCharges) { why = "Charges full"; return false; }
                    break;
            }
            return true;
        }

        public bool Buy(Kid kid, ShopItem item)
        {
            string why;
            if (!CanBuy(kid, item, out why)) return false;
            kid.Money -= Cost(item, kid);
            switch (item)
            {
                case ShopItem.HandTowel: kid.Towel = Tuning.HandTowelAbsorb; break;
                case ShopItem.BeachTowel: kid.Towel = Tuning.BeachTowelAbsorb; break;
                case ShopItem.Balloon: kid.Balloons++; break;
                case ShopItem.Ability: kid.AbilityCharges++; break;
            }
            return true;
        }

        /// Toys: buying one replaces whatever is in that slot (sidearm or primary).
        public bool CanBuyToy(Kid kid, ToyDef toy, out string why)
        {
            why = null;
            if (Phase != Phase.Buy) { why = "Only during the allowance phase"; return false; }
            var owned = toy.Slot == ToySlot.Primary ? kid.Primary : kid.Sidearm;
            if (owned != null && owned.Def == toy) { why = "Owned"; return false; }
            if (kid.Money < toy.Cost) { why = "Need $" + toy.Cost; return false; }
            return true;
        }

        public bool BuyToy(Kid kid, ToyDef toy)
        {
            string why;
            if (!CanBuyToy(kid, toy, out why)) return false;
            kid.Money -= toy.Cost;
            if (toy.Slot == ToySlot.Primary)
            {
                kid.Primary = new ToyState(toy);
                kid.UsingPrimary = false;
                kid.SwitchTo(true);
            }
            else
            {
                kid.Sidearm = new ToyState(toy);
                if (kid.Primary == null) kid.SwitchTo(false);
            }
            return true;
        }

        // =====================================================================
        // Messages
        // =====================================================================
        public void ShowBanner(string text, float seconds)
        {
            Banner = text;
            BannerUntil = Time.time + seconds;
        }

        public void AddFeed(string line)
        {
            Feed.Add(line);
            FeedTimes.Add(Time.time);
            if (Feed.Count > 6) { Feed.RemoveAt(0); FeedTimes.RemoveAt(0); }
        }

        public int AliveCount(Team t)
        {
            int n = 0;
            foreach (var k in Kids) if (k.Team == t && k.Alive) n++;
            return n;
        }
    }
}
