// Toys.cs
// ---------------------------------------------------------------------------
// The full water-toy arsenal. Each toy fills one of the genre's usual weapon
// jobs (sidearm, SMG, shotgun, rifle, sniper, heavy), and each is based on a
// REAL kind of water toy or household thing, listed under "Reference", so
// you have something to look up and model in Blender.
//
// Names and numbers are ours, not copied from any other game. Change any
// number here to rebalance a toy. Soak is out of 100. A face hit counts x2,
// legs count x0.75.
//
// How to read a toy:
//   Instant   = the water hits the moment you fire (like a laser).
//   Arcing    = visible water that flies and falls, so you aim a bit high.
//   Pellets   = drops per shot (shotgun-style spray), each doing SoakPerHit.
//   Burst     = shots per trigger pull.
//   ZoomFov   = right-click scope (smaller number = more zoom). 0 = no scope.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace WaterWars
{
    public enum ToySlot { Sidearm, Primary }

    public class ToyDef
    {
        public string Name;
        public string Category;          // "Sidearm", "SMG", "Shotgun", "Rifle", "Sniper", "Heavy"
        public ToySlot Slot;
        public string Reference;        // the real-world toy to model it on
        public int Cost;                // $ in the buy menu. 0 = free.
        public int TankShots;           // trigger pulls before you must reload
        public float SoakPerHit;        // soak per body hit (per pellet for sprays)
        public float ShotsPerSecond;    // trigger pulls per second
        public bool HoldToFire;         // keep firing while the button is held
        public bool Arcing;             // false = instant, true = water that flies and falls
        public float WaterSpeed = 40f;  // arcing only: meters per second
        public float WaterGravity = 9f; // arcing only: how fast the water drops
        public int Pellets = 1;
        public int Burst = 1;
        public float BurstInterval = 0.07f;
        public float SpreadDegrees;         // random wobble on every shot
        public float MovingSpreadDegrees;   // extra wobble while running or jumping
        public float ZoomFov;               // 0 = no scope
        public float ZoomedSpreadDegrees;   // wobble while scoped in
        public float ReloadSeconds;
        public float Range;             // water past this distance does nothing
        public float FalloffStart;      // past this distance, soak drops to 60%

        // How the grey-box version looks in your hands (until Ian's models replace it).
        public Color ViewColor;
        public float ViewLength = 0.4f; // barrel length in meters
        public bool ViewTank;           // has a water tank on top
        public bool ViewTwin;           // one in each hand

        // =====================================================================
        // SIDEARMS (slot 2). Everyone gets the Squirt Pistol free.
        // =====================================================================
        public static readonly ToyDef SquirtPistol = new ToyDef
        {
            Name = "Squirt Pistol", Category = "Sidearm", Slot = ToySlot.Sidearm,
            Reference = "Classic clear plastic squirt pistol with a trigger pump",
            Cost = 0, TankShots = 12, SoakPerHit = 18f, ShotsPerSecond = 4f,
            SpreadDegrees = 0.3f, MovingSpreadDegrees = 2.5f,
            ReloadSeconds = 1.4f, Range = 45f, FalloffStart = 25f,
            ViewColor = new Color(1f, 0.45f, 0.1f), ViewLength = 0.3f,
        };

        public static readonly ToyDef TwinSquirters = new ToyDef
        {
            Name = "Twin Squirters", Category = "Sidearm", Slot = ToySlot.Sidearm,
            Reference = "Two little dollar-store squirt pistols, one in each hand",
            Cost = 5, TankShots = 20, SoakPerHit = 11f, ShotsPerSecond = 10f, HoldToFire = true,
            SpreadDegrees = 1.3f, MovingSpreadDegrees = 2f,
            ReloadSeconds = 1.6f, Range = 25f, FalloffStart = 12f,
            ViewColor = new Color(0.95f, 0.3f, 0.6f), ViewLength = 0.22f, ViewTwin = true,
        };

        public static readonly ToyDef SprayBottle = new ToyDef
        {
            Name = "Spray Bottle", Category = "Sidearm", Slot = ToySlot.Sidearm,
            Reference = "Trigger spray bottle (plant mister / window cleaner bottle)",
            Cost = 4, TankShots = 6, SoakPerHit = 7f, Pellets = 6, ShotsPerSecond = 2f,
            SpreadDegrees = 5f, MovingSpreadDegrees = 1f,
            ReloadSeconds = 1.8f, Range = 14f, FalloffStart = 7f,
            ViewColor = new Color(0.85f, 0.9f, 0.95f), ViewLength = 0.2f, ViewTank = true,
        };

        public static readonly ToyDef BulbBlaster = new ToyDef
        {
            Name = "Bulb Blaster", Category = "Sidearm", Slot = ToySlot.Sidearm,
            Reference = "Big squeeze-bulb water pistol (rubber bulb you squeeze to shoot)",
            Cost = 8, TankShots = 6, SoakPerHit = 45f, ShotsPerSecond = 1.5f,
            SpreadDegrees = 0.2f, MovingSpreadDegrees = 4f,
            ReloadSeconds = 1.8f, Range = 50f, FalloffStart = 30f,
            ViewColor = new Color(0.9f, 0.8f, 0.2f), ViewLength = 0.35f, ViewTank = true,
        };

        // =====================================================================
        // SMGs: fast and cheap, best up close to medium range.
        // =====================================================================
        public static readonly ToyDef MotorSquirter = new ToyDef
        {
            Name = "Motor Squirter", Category = "SMG", Slot = ToySlot.Primary,
            Reference = "Battery-powered motorized water gun (buzzes when it fires)",
            Cost = 11, TankShots = 30, SoakPerHit = 13f, ShotsPerSecond = 11f, HoldToFire = true,
            SpreadDegrees = 1.4f, MovingSpreadDegrees = 2f,
            ReloadSeconds = 2f, Range = 30f, FalloffStart = 18f,
            ViewColor = new Color(0.55f, 0.35f, 0.9f), ViewLength = 0.4f, ViewTank = true,
        };

        public static readonly ToyDef BottleRocketBlaster = new ToyDef
        {
            Name = "Bottle Rocket Blaster", Category = "SMG", Slot = ToySlot.Primary,
            Reference = "Blaster with an upside-down 2-liter soda bottle as its tank",
            Cost = 14, TankShots = 8, SoakPerHit = 22f, Burst = 3, BurstInterval = 0.07f, ShotsPerSecond = 2.5f,
            Arcing = true, WaterSpeed = 50f, WaterGravity = 6f,
            SpreadDegrees = 0.6f, MovingSpreadDegrees = 1.5f,
            ReloadSeconds = 2f, Range = 40f, FalloffStart = 25f,
            ViewColor = new Color(0.2f, 0.7f, 0.9f), ViewLength = 0.45f, ViewTank = true,
        };

        // =====================================================================
        // SHOTGUNS: a wide splash of drops. Deadly close, useless far away.
        // =====================================================================
        public static readonly ToyDef WateringCan = new ToyDef
        {
            Name = "Watering Can", Category = "Shotgun", Slot = ToySlot.Primary,
            Reference = "Metal garden watering can with a sprinkler head on the spout",
            Cost = 9, TankShots = 5, SoakPerHit = 9f, Pellets = 8, ShotsPerSecond = 1.3f,
            Arcing = true, WaterSpeed = 20f, WaterGravity = 12f,
            SpreadDegrees = 7f, MovingSpreadDegrees = 1f,
            ReloadSeconds = 2f, Range = 13f, FalloffStart = 8f,
            ViewColor = new Color(0.6f, 0.65f, 0.7f), ViewLength = 0.5f, ViewTank = true,
        };

        public static readonly ToyDef BucketToss = new ToyDef
        {
            Name = "Bucket Toss", Category = "Shotgun", Slot = ToySlot.Primary,
            Reference = "Plastic beach bucket, thrown water and all",
            Cost = 12, TankShots = 2, SoakPerHit = 8f, Pellets = 12, ShotsPerSecond = 0.9f,
            Arcing = true, WaterSpeed = 15f, WaterGravity = 14f,
            SpreadDegrees = 9f, MovingSpreadDegrees = 1f,
            ReloadSeconds = 2.6f, Range = 10f, FalloffStart = 6f,
            ViewColor = new Color(0.95f, 0.85f, 0.2f), ViewLength = 0.3f, ViewTank = true,
        };

        // =====================================================================
        // RIFLES: the default buy. Good at every range.
        // =====================================================================
        public static readonly ToyDef PumpSoaker = new ToyDef
        {
            Name = "Pump-Action Soaker", Category = "Rifle", Slot = ToySlot.Primary,
            Reference = "Classic pump-action water blaster with a big tank on top (pump the slide to pressurize)",
            Cost = 26, TankShots = 30, SoakPerHit = 26f, ShotsPerSecond = 6f, HoldToFire = true,
            Arcing = true, WaterSpeed = 42f, WaterGravity = 9f,
            SpreadDegrees = 0.8f, MovingSpreadDegrees = 3.5f,
            ReloadSeconds = 2.2f, Range = 60f, FalloffStart = 35f,
            ViewColor = new Color(0.3f, 0.8f, 0.25f), ViewLength = 0.6f, ViewTank = true,
        };

        public static readonly ToyDef PressureRifle = new ToyDef
        {
            Name = "Pressure Rifle", Category = "Rifle", Slot = ToySlot.Primary,
            Reference = "Air-pressure water rifle with a separate pressure chamber and hand pump",
            Cost = 28, TankShots = 25, SoakPerHit = 25f, ShotsPerSecond = 7f, HoldToFire = true,
            SpreadDegrees = 0.25f, MovingSpreadDegrees = 4f,
            ReloadSeconds = 2.4f, Range = 70f, FalloffStart = 40f,
            ViewColor = new Color(0.9f, 0.35f, 0.25f), ViewLength = 0.65f, ViewTank = true,
        };

        // =====================================================================
        // SNIPERS: hold right-click to scope in. Very shaky if you don't.
        // =====================================================================
        public static readonly ToyDef PoolTube = new ToyDef
        {
            Name = "Pool Tube", Category = "Sniper", Slot = ToySlot.Primary,
            Reference = "Foam pool-noodle pump shooter (pull back to suck water in, push to shoot)",
            Cost = 15, TankShots = 4, SoakPerHit = 55f, ShotsPerSecond = 1.2f,
            SpreadDegrees = 3f, MovingSpreadDegrees = 4f, ZoomFov = 40f, ZoomedSpreadDegrees = 0.05f,
            ReloadSeconds = 2.2f, Range = 80f, FalloffStart = 70f,
            ViewColor = new Color(0.2f, 0.9f, 0.6f), ViewLength = 0.8f,
        };

        public static readonly ToyDef PressureCannon = new ToyDef
        {
            Name = "Pressure Cannon", Category = "Sniper", Slot = ToySlot.Primary,
            Reference = "Big high-pressure blaster fed by a backpack water tank",
            Cost = 40, TankShots = 5, SoakPerHit = 80f, ShotsPerSecond = 0.75f,
            SpreadDegrees = 4f, MovingSpreadDegrees = 5f, ZoomFov = 25f, ZoomedSpreadDegrees = 0f,
            ReloadSeconds = 3f, Range = 120f, FalloffStart = 120f,
            ViewColor = new Color(0.15f, 0.3f, 0.75f), ViewLength = 0.85f, ViewTank = true,
        };

        // =====================================================================
        // HEAVY: a huge tank of water and a lot of spray.
        // =====================================================================
        public static readonly ToyDef GardenHose = new ToyDef
        {
            Name = "Garden Hose Nozzle", Category = "Heavy", Slot = ToySlot.Primary,
            Reference = "Pistol-grip garden hose spray nozzle, with a hose reel worn as a backpack",
            Cost = 30, TankShots = 80, SoakPerHit = 10f, ShotsPerSecond = 12f, HoldToFire = true,
            Arcing = true, WaterSpeed = 34f, WaterGravity = 8f,
            SpreadDegrees = 2f, MovingSpreadDegrees = 4f,
            ReloadSeconds = 3.5f, Range = 40f, FalloffStart = 25f,
            ViewColor = new Color(0.25f, 0.55f, 0.25f), ViewLength = 0.5f, ViewTank = true,
        };

        /// Every toy, in buy-menu order.
        public static readonly ToyDef[] All =
        {
            SquirtPistol, TwinSquirters, SprayBottle, BulbBlaster,
            MotorSquirter, BottleRocketBlaster,
            WateringCan, BucketToss,
            PumpSoaker, PressureRifle,
            PoolTube, PressureCannon,
            GardenHose,
        };

        public static readonly string[] Categories = { "Sidearm", "SMG", "Shotgun", "Rifle", "Sniper", "Heavy" };

        public static List<ToyDef> InCategory(string category)
        {
            var list = new List<ToyDef>();
            foreach (var t in All) if (t.Category == category) list.Add(t);
            return list;
        }
    }
}
