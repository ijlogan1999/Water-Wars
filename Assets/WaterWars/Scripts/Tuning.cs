// Tuning.cs
// ---------------------------------------------------------------------------
// Every number that decides how the game FEELS lives in this one file, so you
// can tweak the game without hunting through code. Change a number, save the
// file, go back to Unity, and press Play again.
//
// Most numbers come straight from the design doc ("Big Splash" rules,
// allowance economy, toy table). Units: seconds, meters, and "soak" points.
// ---------------------------------------------------------------------------

namespace WaterWars
{
    public static class Tuning
    {
        // ---- Match ---------------------------------------------------------
        public const int TeamSize = 5;            // 5v5. You + 4 bot teammates vs 5 bots.
        public const int RoundsToWin = 7;         // First to 7 rounds.
        public const int SwapSidesAfterRound = 6; // Teams switch attack/defend after round 6.

        // ---- Map -----------------------------------------------------------
        // Every match picks one of the Maple Park maps (see NeighborhoodData.Maps):
        //   1 Park Run, 2 Willow Court, 3 Schoolyard, 4 Creekside,
        //   5 Pine St Backyards, 6 Hickory Corner.
        // 0 = a random one each match. Set a number to always play that map.
        public const int MapChoice = 0;
        // Set this to true to switch to a different map every ROUND instead.
        public const bool NewMapEachRound = false;

        // ---- Round timers --------------------------------------------------
        public const float BuyPhaseSeconds = 30f;     // "Allowance phase". Press Enter to skip it.
        public const float SuddenDeathBuySeconds = 8f;
        public const float RoundSeconds = 100f;       // 1:40 to plant before the Streetlights come on.
        public const float RoundOverSeconds = 5f;     // Pause between rounds.

        // ---- The Big One (plant / disarm) ----------------------------------
        public const float PlantSeconds = 4f;
        public const float FuseSeconds = 40f;
        public const float DisarmSeconds = 7f;
        public const float DisarmCheckpoint = 3.5f;   // Disarm progress past this is kept if you let go.
        public const float DisarmReach = 2.2f;        // How close a defender must be to disarm.
        public const float PickupReach = 1.6f;        // How close an attacker must be to grab a dropped Big One.

        // ---- Soak meter ----------------------------------------------------
        public const float SoakedAt = 100f;           // At 100 you are Soaked and out for the round.
        public const float HeadMultiplier = 2f;       // Splash to the face counts double.
        public const float BodyMultiplier = 1f;
        public const float LegsMultiplier = 0.75f;

        // ---- Towels (armor) ------------------------------------------------
        public const int HandTowelCost = 4;
        public const float HandTowelAbsorb = 25f;
        public const int BeachTowelCost = 10;
        public const float BeachTowelAbsorb = 50f;

        // ---- Allowance economy ($) -----------------------------------------
        public const int StartMoney = 8;
        public const int MaxMoney = 90;
        public const int WinMoney = 30;
        public const int LossMoney = 19;
        public const int LossStreakBonus = 5;     // +$5 per loss in a row...
        public const int LossMoneyCap = 29;       // ...up to $29.
        public const int SoakReward = 2;          // Per opponent you soak.
        public const int PlantReward = 3;         // Every attacker gets this when the Big One is planted.

        // ---- Movement ------------------------------------------------------
        public const float RunSpeed = 5.5f;
        public const float WalkSpeed = 3f;        // Hold Shift: slower but steadier aim.
        public const float CrouchSpeed = 2.5f;
        public const float JumpHeight = 1.1f;
        public const float Gravity = 20f;         // Higher than real life so jumps feel snappy.
        public const float StandHeight = 1.8f;
        public const float CrouchHeight = 1.2f;
        public const float DefaultMouseSensitivity = 2f;

        // ---- Bots ----------------------------------------------------------
        public const float BotSightRange = 45f;
        public const float BotReactionMin = 0.2f;   // Seconds before a bot shoots after seeing you.
        public const float BotReactionMax = 0.4f;
        public const float BotAimErrorDegrees = 2f;   // Bigger = worse aim.
        public const float BotSpeed = 5f;
        public const float BotSightAngle = 90f;     // How far to each side bots notice you (degrees).
    }
}
