// Definitions.cs
// ---------------------------------------------------------------------------
// The "cards" that describe things in the game: which teams exist, what each
// water toy does, and (heroes are in Heroes.cs). These are plain data
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

    // Heroes live in Heroes.cs (data) and HeroKits.cs (what they do).
}
