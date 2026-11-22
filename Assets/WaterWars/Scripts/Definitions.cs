// Definitions.cs
// ---------------------------------------------------------------------------
// The "cards" that describe things in the game: which teams exist, what each
// water toy does, and what the placeholder heroes can do. These are plain data
// (no behavior). Other scripts read them.
// ---------------------------------------------------------------------------

using UnityEngine;

namespace WaterWars
{
    /// The two crews. Colors come from the design doc (orange vs teal bandanas).
    public enum Team { Orange, Teal }

    /// Where a splash landed on a kid. Face counts double, legs count less.
    public enum HitZoneType { Head, Body, Legs }

    public static class TeamInfo
    {
        public static Team Other(Team t) { return t == Team.Orange ? Team.Teal : Team.Orange; }
        public static string Name(Team t) { return t == Team.Orange ? "Orange crew" : "Teal crew"; }
        public static Color Color(Team t)
        {
            return t == Team.Orange ? new Color(1f, 0.55f, 0.15f) : new Color(0.1f, 0.75f, 0.75f);
        }
    }

    /// One water toy. "Instant" toys hit the moment you click (like a laser).
    /// "Arcing" toys shoot visible blobs of water that fall with gravity, so you
    /// aim a little above far-away targets.
    public class ToyDef
    {
        public string Name;
        public int Cost;               // $ in the buy menu. 0 = free.
        public int TankShots;          // Shots before you must reload.
        public float SoakPerHit;       // Soak added by a body hit.
        public float ShotsPerSecond;
        public bool HoldToFire;        // true = keep firing while the button is held.
        public bool Arcing;            // false = instant hit, true = blobs that fall.
        public float WaterSpeed;       // Arcing only: meters per second.
        public float WaterGravity;     // Arcing only: how fast the water drops.
        public float SpreadDegrees;    // Random wobble on every shot.
        public float MovingSpreadDegrees; // Extra wobble while running or jumping.
        public float ReloadSeconds;
        public float Range;            // Water past this distance does nothing.
        public float FalloffStart;     // Past this distance, soak drops to 60%.
        public Color ViewColor;        // Color of the toy you hold on screen.

        // From the design doc's arsenal table. Tank sizes in ml became "shots".
        public static readonly ToyDef SquirtPistol = new ToyDef
        {
            Name = "Squirt Pistol",
            Cost = 0,
            TankShots = 12,
            SoakPerHit = 18f,
            ShotsPerSecond = 4f,
            HoldToFire = false,
            Arcing = false,
            SpreadDegrees = 0.3f,
            MovingSpreadDegrees = 2.5f,
            ReloadSeconds = 1.4f,
            Range = 45f,
            FalloffStart = 25f,
            ViewColor = new Color(1f, 0.45f, 0.1f),
        };

        public static readonly ToyDef PumpSoaker = new ToyDef
        {
            Name = "Pump-Action Soaker",
            Cost = 26,
            TankShots = 30,
            SoakPerHit = 26f,
            ShotsPerSecond = 6f,
            HoldToFire = true,
            Arcing = true,
            WaterSpeed = 42f,
            WaterGravity = 9f,
            SpreadDegrees = 0.8f,
            MovingSpreadDegrees = 3.5f,
            ReloadSeconds = 2.2f,
            Range = 60f,
            FalloffStart = 35f,
            ViewColor = new Color(0.3f, 0.8f, 0.25f),
        };
    }

    /// What a hero's bought ability does. Placeholder heroes only have one each
    /// for now; Ian's real heroes will replace these.
    public enum AbilityKind { WaterBalloon, JuiceBox }

    /// A placeholder hero. Ian will supply the real heroes later, so these are
    /// deliberately plain: one damage kid and one healing kid.
    public class HeroDef
    {
        public string Name;
        public string Role;
        public string AbilityName;
        public string AbilityHelp;
        public AbilityKind Ability;
        public int AbilityCost;
        public int AbilityMaxCharges;

        public static readonly HeroDef BalloonKid = new HeroDef
        {
            Name = "Balloon Kid (placeholder)",
            Role = "Damage",
            AbilityName = "Water Balloon",
            AbilityHelp = "Throw a balloon that bursts and soaks enemies within 3.5 m.",
            Ability = AbilityKind.WaterBalloon,
            AbilityCost = 3,
            AbilityMaxCharges = 2,
        };

        public static readonly HeroDef JuiceKid = new HeroDef
        {
            Name = "Juice Kid (placeholder)",
            Role = "Healing",
            AbilityName = "Juice Box",
            AbilityHelp = "Dries you and teammates within 5 m by 40 soak.",
            Ability = AbilityKind.JuiceBox,
            AbilityCost = 3,
            AbilityMaxCharges = 2,
        };

        public static readonly HeroDef[] All = { BalloonKid, JuiceKid };
    }
}
