// Definitions.cs
// ---------------------------------------------------------------------------
// The "cards" that describe things in the game: which teams exist, what each
// water toy does, and what the placeholder heroes can do. These are plain data
// (no behavior). Other scripts read them. The water toys are in Toys.cs.
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

    // Water toys live in Toys.cs.

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
