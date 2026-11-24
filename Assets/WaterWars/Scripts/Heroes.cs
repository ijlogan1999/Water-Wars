// Heroes.cs
// ---------------------------------------------------------------------------
// Ian's twelve heroes, as data: name, class, what each part of the kit does,
// prices, and each hero's own sidearm. What the kits actually DO in the game
// is in HeroKits.cs.
//
// Every hero has:
//   Passive   always on
//   Ability   [Q]  bought with allowance in the Lemonade Stand, in charges
//   Ultimate  [X]  free, charges up during the match (see "Ult points" below)
//   Sidearm   your own free sidearm instead of the Squirt Pistol
//
// Ult points: +1 for every enemy you soak out, +1 at the end of every round,
// +1 for planting or disarming the Big One. When you have enough, press X.
//
// Class rules (from the design doc):
//   Duelist     wins fights alone                        normal everything
//   DPS         most soak per second                     Soak meter is 85, not 100
//   Sentinel    heals and tanks                          about 5% slower
//   Utility     information and options                  longest Ultimate charge
//   Specialist  new ways to play                         hardest to learn
// Anything that blinds or disorients lasts 2 seconds or less.
//
// Parts the design doc marks "(new)" are Claude's drafts. They're built as
// written so Ian can try them, and are easy to change here.
// ---------------------------------------------------------------------------

using UnityEngine;

namespace WaterWars
{
    public enum HeroClass { Duelist, DPS, Sentinel, Utility, Specialist }

    public class HeroDef
    {
        public string Name;
        public HeroClass Class;
        public Color Color;                 // hat color, so you can tell heroes apart
        public string PassiveName, PassiveHelp;
        public string AbilityName, AbilityHelp;
        public int AbilityCost, AbilityMaxCharges;
        public string UltName, UltHelp;
        public string ActionHelp;           // [V] or right-click extras, if any
        public ToyDef Sidearm;
        public System.Func<HeroKit> MakeKit;

        public float MaxSoak = 100f;
        public float SpeedMultiplier = 1f;

        public int UltCost
        {
            get
            {
                switch (Class)
                {
                    case HeroClass.Utility: return 8;
                    case HeroClass.Sentinel:
                    case HeroClass.Specialist: return 7;
                    default: return 6;
                }
            }
        }

        static HeroDef Make(string name, HeroClass cls, Color color)
        {
            var h = new HeroDef { Name = name, Class = cls, Color = color };
            if (cls == HeroClass.DPS) h.MaxSoak = 85f;
            if (cls == HeroClass.Sentinel) h.SpeedMultiplier = 0.95f;
            return h;
        }

        // =====================================================================
        // DUELISTS
        // =====================================================================
        public static readonly HeroDef Richy = Init(Make("Richy", HeroClass.Duelist, new Color(0.95f, 0.8f, 0.2f)), h =>
        {
            h.PassiveName = "Silver Spoon"; h.PassiveHelp = "+$4 extra allowance every round.";
            h.AbilityName = "Butler's Fresh Clothes"; h.AbilityHelp = "His butler jogs in with dry clothes and takes away half your soak. Soaking the butler cancels it. 2 charges (shirt, then pants).";
            h.AbilityCost = 3; h.AbilityMaxCharges = 2;
            h.UltName = "Dad's Satellite"; h.UltHelp = "Shows every enemy on the map, three pings 2 s apart.";
            h.Sidearm = HeroToys.MotorizedBlaster;
            h.MakeKit = () => new RichyKit();
        });

        public static readonly HeroDef SweetTooth = Init(Make("Sweet Tooth", HeroClass.Duelist, new Color(1f, 0.45f, 0.7f)), h =>
        {
            h.PassiveName = "Hot Sauce Balloons"; h.PassiveHelp = "Your balloons sting the eyes of anyone hit (blurry for 1.5 s). One free balloon every round.";
            h.AbilityName = "Dodge Roll"; h.AbilityHelp = "A quick roll the way you're moving.";
            h.AbilityCost = 2; h.AbilityMaxCharges = 2;
            h.UltName = "Sugar Rush"; h.UltHelp = "10 s of faster running, squirting and reloading. Then a 3 s Sugar Crash (slow).";
            h.Sidearm = HeroToys.PoleGun;
            h.MakeKit = () => new SweetToothKit();
        });

        // =====================================================================
        // DPS (Soak meter 85)
        // =====================================================================
        public static readonly HeroDef Bully = Init(Make("The Bully", HeroClass.DPS, new Color(0.55f, 0.1f, 0.1f)), h =>
        {
            h.PassiveName = "Knuckle Sandwich"; h.PassiveHelp = "[V] Shove whoever's right in front of you: pushes and disorients for 1 s (6 s cooldown). Also knocks over gadgets.";
            h.AbilityName = "Sticks and Stones"; h.AbilityHelp = "Yell an insult in a cone. Enemies hit are Rattled: shaky aim for 3 s.";
            h.AbilityCost = 3; h.AbilityMaxCharges = 2;
            h.UltName = "Tantrum"; h.UltHelp = "8 s of extra speed. If you get Soaked during it, you keep running and shoving (no gun) until it ends.";
            h.ActionHelp = "[V] Shove";
            h.Sidearm = HeroToys.SwipedSoaker;
            h.MakeKit = () => new BullyKit();
        });

        public static readonly HeroDef LilTimmy = Init(Make("Lil' Timmy", HeroClass.DPS, new Color(0.45f, 0.75f, 1f)), h =>
        {
            h.PassiveName = "Noble Steed"; h.PassiveHelp = "Rides his dog Maximilian: 10% faster while Max is with him.";
            h.AbilityName = "Max! Speak!"; h.AbilityHelp = "Max barks: enemies within 8 m are slowed and disoriented for 1.5 s.";
            h.AbilityCost = 3; h.AbilityMaxCharges = 2;
            h.UltName = "Max! Shake!"; h.UltHelp = "Max runs to where you aim and shakes himself dry, soaking everyone around. You're slower on foot until he's back. Charges faster by taking soak.";
            h.Sidearm = HeroToys.DoubleSquirters;
            h.MakeKit = () => new TimmyKit();
        });

        public static readonly HeroDef BoyScout = Init(Make("Boy Scout", HeroClass.DPS, new Color(0.35f, 0.5f, 0.25f)), h =>
        {
            h.PassiveName = "Slingshot"; h.PassiveHelp = "Throws balloons farther and can carry 2.";
            h.AbilityName = "Dodge Roll"; h.AbilityHelp = "A quick roll the way you're moving.";
            h.AbilityCost = 2; h.AbilityMaxCharges = 2;
            h.UltName = "Merit Badge: Tracking"; h.UltHelp = "For 12 s, enemy footprints show up everywhere and enemies ping every 3 s.";
            h.ActionHelp = "[Right-click] with the Camelbak: refill the teammate you're looking at";
            h.Sidearm = HeroToys.CamelbakSprayer;
            h.MakeKit = () => new ScoutKit();
        });

        // =====================================================================
        // SENTINELS (about 5% slower)
        // =====================================================================
        public static readonly HeroDef MamaLogan = Init(Make("Mama Logan", HeroClass.Sentinel, new Color(0.6f, 0.35f, 0.75f)), h =>
        {
            h.MaxSoak = 125f;
            h.PassiveName = "Big Heart"; h.PassiveHelp = "Bigger Soak meter (125).";
            h.AbilityName = "Box Fan"; h.AbilityHelp = "Set down a fan that dries teammates in front of it. Enemies can soak it out or shove it over.";
            h.AbilityCost = 4; h.AbilityMaxCharges = 1;
            h.UltName = "Laundry Day"; h.UltHelp = "Teammates within 8 m are fully dried and get a 50 fresh-clothes shield for 10 s.";
            h.ActionHelp = "[Right-click] with the nozzle: switch mist / jet";
            h.Sidearm = HeroToys.NozzleJet;
            h.MakeKit = () => new MamaLoganKit();
        });

        public static readonly HeroDef Darling = Init(Make("Darling", HeroClass.Sentinel, new Color(1f, 0.6f, 0.8f)), h =>
        {
            h.PassiveName = "Tough Cookie"; h.PassiveHelp = "Takes 15% less soak.";
            h.AbilityName = "Pretty Please"; h.AbilityHelp = "You and teammates within 7 m get 25 towel.";
            h.AbilityCost = 4; h.AbilityMaxCharges = 1;
            h.UltName = "Look at Me!"; h.UltHelp = "For 6 s, enemy water aimed at teammates within 15 m bends to you instead, and you take 60% less soak.";
            h.Sidearm = HeroToys.PrincessSoaker;
            h.MakeKit = () => new DarlingKit();
        });

        public static readonly HeroDef MamasBoy = Init(Make("Mama's Boy", HeroClass.Sentinel, new Color(0.3f, 0.75f, 0.55f)), h =>
        {
            h.PassiveName = "Running Home"; h.PassiveHelp = "15% faster while running toward your own spawn.";
            h.AbilityName = "Sunscreen Slather"; h.AbilityHelp = "Coat the teammate you're looking at (or yourself): the next 40 soak slides off.";
            h.AbilityCost = 3; h.AbilityMaxCharges = 2;
            h.UltName = "Life Jacket"; h.UltHelp = "For 6 s, water that hits you bounces back at whoever shot it.";
            h.Sidearm = HeroToys.RubberDucky;
            h.MakeKit = () => new MamasBoyKit();
        });

        // =====================================================================
        // UTILITY (longest Ultimate charge)
        // =====================================================================
        public static readonly HeroDef TattleTale = Init(Make("Tattle Tale", HeroClass.Utility, new Color(0.95f, 0.55f, 0.2f)), h =>
        {
            h.PassiveName = "I'm Telling"; h.PassiveHelp = "See which enemies have their Ultimate ready.";
            h.AbilityName = "Phone Call"; h.AbilityHelp = "Ring the phone nearest the enemy closest to your crosshair: they're revealed for 3 s.";
            h.AbilityCost = 2; h.AbilityMaxCharges = 2;
            h.UltName = "Over-Protective Parent"; h.UltHelp = "A parent shows up where you aim. For 10 s, nobody (either crew) can use abilities near them.";
            h.Sidearm = HeroToys.SpySoaker;
            h.MakeKit = () => new TattleKit();
        });

        public static readonly HeroDef ClassClown = Init(Make("Class Clown", HeroClass.Utility, new Color(0.2f, 0.8f, 0.3f)), h =>
        {
            h.PassiveName = "Big Ears"; h.PassiveHelp = "See where running enemies are within 25 m, even through walls.";
            h.AbilityName = "Whoopee Cushion"; h.AbilityHelp = "Hide a cushion where you aim. When an enemy steps near it: a loud noise and they're revealed for 4 s.";
            h.AbilityCost = 2; h.AbilityMaxCharges = 2;
            h.UltName = "The Big Gag"; h.UltHelp = "Toss a spring-snake can. Enemies looking at it when it pops laugh and can't squirt for 1.5 s.";
            h.ActionHelp = "[Right-click] with the flower: drop a Banana Peel (once per round)";
            h.Sidearm = HeroToys.SquirtingFlower;
            h.MakeKit = () => new ClownKit();
        });

        public static readonly HeroDef Nerd = Init(Make("Nerd", HeroClass.Utility, new Color(0.3f, 0.4f, 0.9f)), h =>
        {
            h.PassiveName = "Upgrades"; h.PassiveHelp = "Your homemade gun gets better every round you survive (tank, then range, then fire rate). Resets when you're Soaked.";
            h.AbilityName = "Stink Bomb"; h.AbilityHelp = "Throw a cloud that blocks vision for 8 s. Enemies inside cough and give away where they are.";
            h.AbilityCost = 3; h.AbilityMaxCharges = 2;
            h.UltName = "Toy Wall"; h.UltHelp = "Build a wall of toy bricks where you aim. Soaked down, it leaves bricks that slow anyone walking through.";
            h.Sidearm = HeroToys.Homemade[0];
            h.MakeKit = () => new NerdKit();
        });

        // =====================================================================
        // SPECIALIST
        // =====================================================================
        public static readonly HeroDef Twins = Init(Make("The Twins", HeroClass.Specialist, new Color(0.85f, 0.85f, 0.3f)), h =>
        {
            h.PassiveName = "Twin Switch"; h.PassiveHelp = "[V] Once per round, Susan jumps to where Jenny is.";
            h.AbilityName = "Jenny Thompson"; h.AbilityHelp = "Send Jenny to the window or wall you're aiming at (30 m). She pings enemies near her every 2 s. Soak her and she ducks back inside.";
            h.AbilityCost = 3; h.AbilityMaxCharges = 1;
            h.UltName = "Water Bucket"; h.UltHelp = "Jenny dumps a huge bucket on the closest enemy near her (or near you, if she's not out).";
            h.ActionHelp = "[V] Twin Switch";
            h.Sidearm = HeroToys.LaserPointer;
            h.MakeKit = () => new TwinsKit();
        });

        public static readonly HeroDef[] All = { Richy, SweetTooth, Bully, LilTimmy, BoyScout, MamaLogan, Darling, MamasBoy, TattleTale, ClassClown, Nerd, Twins };

        static HeroDef Init(HeroDef h, System.Action<HeroDef> fill) { fill(h); return h; }

        public string ClassRule
        {
            get
            {
                switch (Class)
                {
                    case HeroClass.DPS: return "DPS: Soak meter 85";
                    case HeroClass.Sentinel: return "Sentinel: 5% slower";
                    case HeroClass.Utility: return "Utility: slow Ultimate";
                    case HeroClass.Specialist: return "Specialist";
                    default: return "Duelist";
                }
            }
        }
    }

    /// Each hero's own sidearm. Free, and you get it back when you're Soaked.
    public static class HeroToys
    {
        static ToyDef Side(string name, string reference) { return new ToyDef { Name = name, Category = "Sidearm", Slot = ToySlot.Sidearm, Reference = reference, Cost = 0 }; }

        public static readonly ToyDef MotorizedBlaster = Fill(Side("Motorized Blaster", "Battery-powered motorized water gun, no pumping"), t =>
        {
            t.TankShots = 40; t.SoakPerHit = 8f; t.ShotsPerSecond = 9f; t.HoldToFire = true;
            t.SpreadDegrees = 1.1f; t.MovingSpreadDegrees = 2f; t.ReloadSeconds = 1.8f; t.Range = 30f; t.FalloffStart = 15f;
            t.ViewColor = new Color(0.95f, 0.8f, 0.2f); t.ViewLength = 0.36f; t.ViewTank = true;
        });

        public static readonly ToyDef PoleGun = Fill(Side("Pole Gun", "A long-barreled squirter taped to a broom handle"), t =>
        {
            t.TankShots = 8; t.SoakPerHit = 24f; t.ShotsPerSecond = 2f;
            t.SpreadDegrees = 0.15f; t.MovingSpreadDegrees = 3f; t.ReloadSeconds = 1.7f; t.Range = 70f; t.FalloffStart = 45f;
            t.ViewColor = new Color(0.75f, 0.6f, 0.4f); t.ViewLength = 0.75f;
        });

        public static readonly ToyDef SwipedSoaker = Fill(Side("Swiped Soaker", "A big pump soaker taken from a little kid"), t =>
        {
            t.TankShots = 24; t.SoakPerHit = 16f; t.ShotsPerSecond = 2.6f; t.Arcing = true; t.WaterSpeed = 38f; t.WaterGravity = 8f;
            t.SpreadDegrees = 0.6f; t.MovingSpreadDegrees = 2f; t.ReloadSeconds = 2.6f; t.Range = 40f; t.FalloffStart = 22f;
            t.ViewColor = new Color(0.3f, 0.8f, 0.4f); t.ViewLength = 0.5f; t.ViewTank = true;
        });

        public static readonly ToyDef DoubleSquirters = Fill(Side("Double Squirters", "Two little squirt pistols, one in each hand"), t =>
        {
            t.TankShots = 24; t.SoakPerHit = 9f; t.ShotsPerSecond = 9f; t.HoldToFire = true;
            t.SpreadDegrees = 1.6f; t.MovingSpreadDegrees = 1.5f; t.ReloadSeconds = 1.5f; t.Range = 20f; t.FalloffStart = 11f;
            t.ViewColor = new Color(0.45f, 0.75f, 1f); t.ViewLength = 0.22f; t.ViewTwin = true;
        });

        public static readonly ToyDef CamelbakSprayer = Fill(Side("Camelbak Sprayer", "A backpack water bladder with a hose and spray nozzle"), t =>
        {
            t.TankShots = 45; t.SoakPerHit = 5f; t.Pellets = 3; t.ShotsPerSecond = 6f; t.HoldToFire = true;
            t.SpreadDegrees = 3f; t.MovingSpreadDegrees = 1f; t.ReloadSeconds = 2f; t.Range = 15f; t.FalloffStart = 9f;
            t.ViewColor = new Color(0.35f, 0.5f, 0.25f); t.ViewLength = 0.3f;
        });

        public static readonly ToyDef NozzleJet = Fill(Side("Garden Hose Nozzle (jet)", "Pistol-grip garden hose nozzle"), t =>
        {
            t.TankShots = 40; t.SoakPerHit = 10f; t.ShotsPerSecond = 6f; t.HoldToFire = true;
            t.SpreadDegrees = 0.8f; t.MovingSpreadDegrees = 2f; t.ReloadSeconds = 2f; t.Range = 24f; t.FalloffStart = 14f;
            t.ViewColor = new Color(0.6f, 0.35f, 0.75f); t.ViewLength = 0.3f;
        });

        public static readonly ToyDef NozzleMist = Fill(Side("Garden Hose Nozzle (mist)", "Pistol-grip garden hose nozzle"), t =>
        {
            t.TankShots = 40; t.SoakPerHit = 3.5f; t.Pellets = 6; t.ShotsPerSecond = 6f; t.HoldToFire = true;
            t.SpreadDegrees = 7f; t.MovingSpreadDegrees = 0.5f; t.ReloadSeconds = 2f; t.Range = 10f; t.FalloffStart = 6f;
            t.ViewColor = new Color(0.6f, 0.35f, 0.75f); t.ViewLength = 0.3f;
        });

        public static readonly ToyDef PrincessSoaker = Fill(Side("Princess Soaker", "A sparkly pink toy water gun"), t =>
        {
            t.TankShots = 10; t.SoakPerHit = 18f; t.ShotsPerSecond = 4f;
            t.SpreadDegrees = 0.25f; t.MovingSpreadDegrees = 2f; t.ReloadSeconds = 1.3f; t.Range = 40f; t.FalloffStart = 25f;
            t.ViewColor = new Color(1f, 0.55f, 0.8f); t.ViewLength = 0.3f;
        });

        public static readonly ToyDef RubberDucky = Fill(Side("Rubber Ducky Squirter", "A rubber duck bath toy that squirts"), t =>
        {
            t.TankShots = 12; t.SoakPerHit = 15f; t.ShotsPerSecond = 4f;
            t.SpreadDegrees = 0.15f; t.MovingSpreadDegrees = 1.5f; t.ReloadSeconds = 1.3f; t.Range = 22f; t.FalloffStart = 14f;
            t.ViewColor = new Color(1f, 0.85f, 0.1f); t.ViewLength = 0.2f;
        });

        public static readonly ToyDef SpySoaker = Fill(Side("Spy Soaker", "A little black squirt pistol with a 'silencer'"), t =>
        {
            t.TankShots = 14; t.SoakPerHit = 14f; t.ShotsPerSecond = 5f;
            t.SpreadDegrees = 0.35f; t.MovingSpreadDegrees = 2f; t.ReloadSeconds = 1.4f; t.Range = 35f; t.FalloffStart = 20f;
            t.ViewColor = new Color(0.15f, 0.15f, 0.18f); t.ViewLength = 0.4f;
        });

        public static readonly ToyDef SquirtingFlower = Fill(Side("Squirting Flower", "A clown's lapel flower with a squeeze bulb"), t =>
        {
            t.TankShots = 10; t.SoakPerHit = 13f; t.ShotsPerSecond = 5f;
            t.SpreadDegrees = 0.8f; t.MovingSpreadDegrees = 2f; t.ReloadSeconds = 1.4f; t.Range = 16f; t.FalloffStart = 10f;
            t.ViewColor = new Color(1f, 0.3f, 0.5f); t.ViewLength = 0.15f;
        });

        public static readonly ToyDef LaserPointer = Fill(Side("Laser Pointer Squirter", "A squirt pistol with a laser pointer taped on"), t =>
        {
            t.TankShots = 14; t.SoakPerHit = 13f; t.ShotsPerSecond = 5f;
            t.SpreadDegrees = 0.3f; t.MovingSpreadDegrees = 2f; t.ReloadSeconds = 1.4f; t.Range = 35f; t.FalloffStart = 20f;
            t.ViewColor = new Color(0.85f, 0.85f, 0.3f); t.ViewLength = 0.32f;
        });

        /// The Nerd's Homemade Water Gun, level 0 to 3 (Upgrades: tank, then range, then fire rate).
        public static readonly ToyDef[] Homemade = MakeHomemade();

        static ToyDef[] MakeHomemade()
        {
            var levels = new ToyDef[4];
            for (int i = 0; i < 4; i++)
            {
                var t = Side(i == 0 ? "Homemade Water Gun" : "Homemade Water Gun Mk " + (i + 1), "A soda bottle, a bike pump and lots of tape");
                t.TankShots = i >= 1 ? 14 : 8;
                t.SoakPerHit = 15f;
                t.ShotsPerSecond = i >= 3 ? 4.5f : 3f;
                t.SpreadDegrees = 0.6f; t.MovingSpreadDegrees = 2.5f; t.ReloadSeconds = 1.6f;
                t.Range = i >= 2 ? 45f : 28f; t.FalloffStart = i >= 2 ? 28f : 15f;
                t.ViewColor = new Color(0.3f, 0.4f, 0.9f); t.ViewLength = 0.32f + i * 0.05f; t.ViewTank = i >= 1;
                levels[i] = t;
            }
            return levels;
        }

        static ToyDef Fill(ToyDef t, System.Action<ToyDef> f) { f(t); return t; }
    }
}
