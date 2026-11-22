// Kid.cs
// ---------------------------------------------------------------------------
// A "Kid" is anyone playing a round: you or a bot. This script holds what
// every kid has in common:
//
//   * a body (head / body / legs zones that water can hit)
//   * the Soak meter (0 to 100; at 100 you're Soaked and sit on the curb)
//   * a towel (armor that soaks up water before your meter fills)
//   * allowance money, water toys, and a hero ability
//   * moving, jumping, crouching
//
// WHO controls the kid is a separate script: PlayerController (your mouse and
// keyboard) or BotBrain (the computer). Both just call the methods here, like
// Move(), TryFire() and TryUseAbility(). Later, for online play, a third
// controller will drive kids from the network in the same way.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WaterWars
{
    /// A water toy a kid owns, plus how many shots are left in its tank.
    public class ToyState
    {
        public readonly ToyDef Def;
        public int Ammo;
        public ToyState(ToyDef def) { Def = def; Ammo = def.TankShots; }
    }

    /// Put on each body part so water knows what it hit and how much it counts.
    public class HitZone : MonoBehaviour
    {
        public Kid Owner;
        public HitZoneType Zone;

        public float Multiplier
        {
            get
            {
                switch (Zone)
                {
                    case HitZoneType.Head: return Tuning.HeadMultiplier;
                    case HitZoneType.Legs: return Tuning.LegsMultiplier;
                    default: return Tuning.BodyMultiplier;
                }
            }
        }
    }

    public class Kid : MonoBehaviour
    {
        /// Every kid in the match. Handy for bots looking for enemies.
        public static readonly List<Kid> All = new List<Kid>();

        // ---- Who this is -------------------------------------------------
        public string DisplayName;
        public Team Team;
        public bool IsPlayer;
        public HeroDef Hero;

        // ---- Soak and towel ----------------------------------------------
        public float Soak { get; private set; }
        public float Towel;
        public bool IsOut { get; private set; }
        public bool Alive { get { return !IsOut; } }
        public float LastHitTime = -99f;
        public Vector3 LastHitFrom;
        public Kid LastAttacker;

        // ---- Gear ----------------------------------------------------------
        public int Money = Tuning.StartMoney;
        public ToyState Sidearm = new ToyState(ToyDef.SquirtPistol);
        public ToyState Primary;              // null until you buy one
        public bool UsingPrimary;
        public int AbilityCharges;
        public ToyState Current { get { return UsingPrimary && Primary != null ? Primary : Sidearm; } }
        public bool Reloading { get { return reloadingToy != null; } }
        public float ReloadProgress
        {
            get { return reloadingToy == null ? 0f : 1f - Mathf.Clamp01((reloadDoneTime - Time.time) / reloadingToy.Def.ReloadSeconds); }
        }

        // ---- Body parts ------------------------------------------------------
        public CharacterController Controller;
        public Transform Model;      // the visible body
        public Transform Eye;        // where the kid looks and shoots from
        public Transform BackSlot;   // where the Big One rides when carried
        public bool Crouching { get; private set; }
        public bool Grounded { get { return Controller.enabled && Controller.isGrounded; } }
        public float HorizontalSpeed { get; private set; }

        // Set every frame by whoever controls this kid; read by the RoundManager for plant/disarm.
        public bool WantsInteract;

        /// Fired whenever this kid shoots (the on-screen toy uses it to kick back).
        public System.Action OnFired;

        float nextShotTime;
        float reloadDoneTime;
        ToyState reloadingToy;
        float verticalVelocity;
        float eyeHeight = 1.6f;
        Renderer[] wetRenderers;
        Color[] dryColors;
        HitZone[] zones;
        Transform heldToy;

        // =====================================================================
        // Building a kid out of simple shapes
        // =====================================================================
        public static Kid Create(string name, Team team, bool isPlayer, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = Shapes.IgnoreRaycastLayer; // so shots pass through the movement capsule

            var cc = go.AddComponent<CharacterController>();
            cc.height = Tuning.StandHeight;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, Tuning.StandHeight * 0.5f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.05f;

            var kid = go.AddComponent<Kid>();
            kid.DisplayName = name;
            kid.Team = team;
            kid.IsPlayer = isPlayer;
            kid.Controller = cc;

            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);
            kid.Model = model;

            Color teamColor = TeamInfo.Color(team);
            Color skin = new Color(0.96f, 0.8f, 0.66f);
            Color jeans = new Color(0.25f, 0.33f, 0.55f);

            var legs = kid.MakePart(PrimitiveType.Cube, "Legs", new Vector3(0f, 0.33f, 0f), new Vector3(0.5f, 0.66f, 0.32f), jeans, HitZoneType.Legs);
            var body = kid.MakePart(PrimitiveType.Capsule, "Body", new Vector3(0f, 1.05f, 0f), new Vector3(0.7f, 0.42f, 0.5f), teamColor, HitZoneType.Body);
            var head = kid.MakePart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.62f, 0f), new Vector3(0.48f, 0.48f, 0.48f), skin, HitZoneType.Head);
            // A team-colored bandana so you can tell crews apart at a glance.
            var bandana = Shapes.Box("Bandana", Vector3.zero, new Vector3(0.5f, 0.12f, 0.5f), teamColor, model, false);
            bandana.transform.localPosition = new Vector3(0f, 1.74f, 0f);

            kid.wetRenderers = new[] { legs.GetComponent<Renderer>(), body.GetComponent<Renderer>() };
            kid.dryColors = new[] { jeans, teamColor };
            foreach (var r in kid.wetRenderers) r.sharedMaterial = Shapes.UniqueMat(r.sharedMaterial.color);
            kid.zones = model.GetComponentsInChildren<HitZone>();

            var eye = new GameObject("Eye").transform;
            eye.SetParent(go.transform, false);
            eye.localPosition = new Vector3(0f, kid.eyeHeight, 0f);
            kid.Eye = eye;

            var back = new GameObject("BackSlot").transform;
            back.SetParent(model, false);
            back.localPosition = new Vector3(0f, 1.15f, -0.45f);
            kid.BackSlot = back;

            // A toy in the bot's hand, so you can see what they carry.
            var toy = Shapes.Box("Held Toy", Vector3.zero, new Vector3(0.12f, 0.15f, 0.45f), ToyDef.SquirtPistol.ViewColor, model, false);
            toy.transform.localPosition = new Vector3(0.3f, 1.15f, 0.35f);
            kid.heldToy = toy.transform;

            if (isPlayer)
            {
                // You don't need to see your own body from inside it, only its shadow.
                foreach (var r in model.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }

            All.Add(kid);
            return kid;
        }

        GameObject MakePart(PrimitiveType shape, string name, Vector3 localPos, Vector3 scale, Color color, HitZoneType zone)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.layer = 0; // Default layer: water CAN hit this
            part.transform.SetParent(Model, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = Shapes.Mat(color);
            // The part's collider becomes a "trigger": water can detect it, but it doesn't block walking.
            part.GetComponent<Collider>().isTrigger = true;
            var hz = part.AddComponent<HitZone>();
            hz.Owner = this;
            hz.Zone = zone;
            return part;
        }

        void OnDestroy() { All.Remove(this); }

        // =====================================================================
        // Each frame
        // =====================================================================
        void Update()
        {
            if (reloadingToy != null && Time.time >= reloadDoneTime)
            {
                reloadingToy.Ammo = reloadingToy.Def.TankShots;
                reloadingToy = null;
            }
            // Smoothly move the eye up/down when crouching.
            var e = Eye.localPosition;
            e.y = Mathf.MoveTowards(e.y, eyeHeight, Time.deltaTime * 6f);
            Eye.localPosition = e;

            if (heldToy != null)
            {
                var r = heldToy.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = Shapes.Mat(Current.Def.ViewColor);
                heldToy.localScale = UsingPrimary && Primary != null ? new Vector3(0.16f, 0.22f, 0.8f) : new Vector3(0.12f, 0.15f, 0.45f);
            }
        }

        // =====================================================================
        // Moving
        // =====================================================================

        /// Walk in a direction (world space, length 0..1) at a speed, with optional jump. Call once per frame.
        public void Move(Vector3 wish, float speed, bool jump)
        {
            if (!Controller.enabled) return;
            bool grounded = Controller.isGrounded;
            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f; // keep feet glued to the ground
            if (jump && grounded && !Crouching) verticalVelocity = Mathf.Sqrt(2f * Tuning.Gravity * Tuning.JumpHeight);
            verticalVelocity -= Tuning.Gravity * Time.deltaTime;

            wish.y = 0f;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            Vector3 horizontal = wish * speed;
            HorizontalSpeed = horizontal.magnitude;
            Controller.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        public void SetCrouch(bool crouch)
        {
            if (crouch == Crouching) return;
            if (!crouch)
            {
                // Don't stand up into a roof.
                Vector3 bottom = transform.position + Vector3.up * (Tuning.CrouchHeight - 0.3f);
                if (Physics.SphereCast(bottom, 0.35f, Vector3.up, out _, Tuning.StandHeight - Tuning.CrouchHeight + 0.2f,
                        Shapes.ShotMask, QueryTriggerInteraction.Ignore))
                    return;
            }
            Crouching = crouch;
            float h = crouch ? Tuning.CrouchHeight : Tuning.StandHeight;
            Controller.height = h;
            Controller.center = new Vector3(0f, h * 0.5f, 0f);
            Model.localScale = new Vector3(1f, h / Tuning.StandHeight, 1f);
            eyeHeight = h - 0.2f;
        }

        /// Look in a direction: yaw turns the whole kid, pitch tilts only the eye.
        public void SetLook(float yaw, float pitch)
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Eye.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public Vector3 AimDirection { get { return Eye.forward; } }

        /// Jump instantly to a spot (used at the start of each round).
        public int TeleportCount { get; private set; }

        public void Teleport(Vector3 position, float yaw)
        {
            TeleportCount++;
            bool was = Controller.enabled;
            Controller.enabled = false;
            transform.position = position;
            SetLook(yaw, 0f);
            Controller.enabled = was;
            verticalVelocity = 0f;
            Physics.SyncTransforms();
        }

        // =====================================================================
        // Shooting
        // =====================================================================

        bool CanAct { get { return Alive && RoundManager.Instance != null && RoundManager.Instance.CombatAllowed; } }

        public void SwitchTo(bool primary)
        {
            if (primary && Primary == null) return;
            if (UsingPrimary == primary) return;
            UsingPrimary = primary;
            reloadingToy = null; // switching cancels a reload
            nextShotTime = Mathf.Max(nextShotTime, Time.time + 0.25f);
        }

        /// Try to shoot in a direction. `triggerJustPressed` matters for click-per-shot toys like the pistol.
        public bool TryFire(Vector3 aimDirection, bool triggerJustPressed)
        {
            if (!CanAct || Reloading) return false;
            var toy = Current;
            if (!toy.Def.HoldToFire && !triggerJustPressed) return false;
            if (Time.time < nextShotTime) return false;
            if (toy.Ammo <= 0) { StartReload(); return false; }

            nextShotTime = Time.time + 1f / toy.Def.ShotsPerSecond;
            toy.Ammo--;

            // Moving, jumping and crouching change how steady your aim is.
            float spread = toy.Def.SpreadDegrees;
            if (HorizontalSpeed > Tuning.WalkSpeed + 0.5f || !Grounded) spread += toy.Def.MovingSpreadDegrees;
            if (Crouching) spread *= 0.7f;
            Vector3 dir = WaterShots.Wobble(aimDirection, spread);

            if (toy.Def.Arcing)
                WaterShots.LaunchGlob(this, Eye.position + dir * 0.6f - Vector3.up * 0.08f, dir * toy.Def.WaterSpeed,
                    toy.Def.WaterGravity, toy.Def.SoakPerHit, toy.Def.Range, toy.Def.FalloffStart, 0f, 0.14f);
            else
                WaterShots.Instant(this, Eye.position, dir, toy.Def);

            if (toy.Ammo <= 0) StartReload();
            if (OnFired != null) OnFired();
            return true;
        }

        public void StartReload()
        {
            var toy = Current;
            if (Reloading || toy.Ammo >= toy.Def.TankShots || !Alive) return;
            reloadingToy = toy;
            reloadDoneTime = Time.time + toy.Def.ReloadSeconds;
        }

        // =====================================================================
        // Hero ability
        // =====================================================================
        public bool TryUseAbility(Vector3 aimDirection)
        {
            if (!CanAct || Hero == null || AbilityCharges <= 0) return false;
            switch (Hero.Ability)
            {
                case AbilityKind.WaterBalloon:
                    // A lobbed balloon: slower, falls faster, bursts in an area.
                    Vector3 throwDir = (aimDirection + Vector3.up * 0.15f).normalized;
                    WaterShots.LaunchGlob(this, Eye.position + aimDirection * 0.6f, throwDir * 19f, 14f, 0f, 80f, 80f, 3.5f, 0.35f);
                    break;
                case AbilityKind.JuiceBox:
                    foreach (var k in All)
                    {
                        if (k.Team != Team || !k.Alive) continue;
                        if (Vector3.Distance(k.transform.position, transform.position) <= 5f) k.Dry(40f);
                    }
                    WaterShots.Splash(transform.position + Vector3.up * 1.2f, new Color(1f, 0.6f, 0.15f), 14, 3f);
                    break;
            }
            AbilityCharges--;
            return true;
        }

        // =====================================================================
        // Getting wet and drying off
        // =====================================================================

        /// Add soak. Towels absorb it first. At 100 the RoundManager sends you to the curb.
        public void TakeSoak(float amount, Kid attacker, Vector3 fromPosition)
        {
            if (IsOut || amount <= 0f) return;
            if (Towel > 0f)
            {
                float absorbed = Mathf.Min(Towel, amount);
                Towel -= absorbed;
                amount -= absorbed;
            }
            LastHitTime = Time.time;
            LastHitFrom = fromPosition;
            LastAttacker = attacker;
            Soak = Mathf.Min(Tuning.SoakedAt, Soak + amount);
            RefreshWetLook();
            if (Soak >= Tuning.SoakedAt && RoundManager.Instance != null) RoundManager.Instance.OnKidSoaked(this, attacker);
        }

        /// Lower soak (juice boxes, towels-as-abilities). There's no drying on its own mid-round.
        public void Dry(float amount)
        {
            if (IsOut) return;
            Soak = Mathf.Max(0f, Soak - amount);
            RefreshWetLook();
        }

        void RefreshWetLook()
        {
            // Clothes get darker and bluer as they soak up water, so everyone can see who's nearly out.
            float wet = Soak / Tuning.SoakedAt;
            Color soaked = new Color(0.12f, 0.2f, 0.45f);
            for (int i = 0; i < wetRenderers.Length; i++)
                wetRenderers[i].sharedMaterial.color = Color.Lerp(dryColors[i], soaked, wet * 0.75f);
        }

        /// Soaked: out for the round. Walk off and sit on the curb.
        public void SendToCurb(Vector3 curbSpot)
        {
            IsOut = true;
            reloadingToy = null;
            WantsInteract = false;
            SetCrouch(false);
            Controller.enabled = false;
            transform.position = curbSpot;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Model.localScale = new Vector3(1f, 0.6f, 1f); // "sitting"
            foreach (var z in zones) z.GetComponent<Collider>().enabled = false;
        }

        /// Get ready for a new round. Survivors keep their toys and towel, like the genre.
        public void ResetForRound(Vector3 spawn, float yaw, bool survivedLastRound)
        {
            if (!survivedLastRound)
            {
                Primary = null;
                Towel = 0f;
            }
            if (Primary != null) Primary.Ammo = Primary.Def.TankShots;
            Sidearm.Ammo = Sidearm.Def.TankShots;
            UsingPrimary = Primary != null;
            reloadingToy = null;
            IsOut = false;
            Soak = 0f;
            LastAttacker = null;
            WantsInteract = false;
            RefreshWetLook();
            Model.localScale = Vector3.one;
            Crouching = true; SetCrouch(false);
            foreach (var z in zones) z.GetComponent<Collider>().enabled = true;
            Controller.enabled = true;
            Teleport(spawn, yaw);
        }

        /// Lose everything (used when teams swap sides at half time).
        public void StripGear()
        {
            Primary = null;
            Towel = 0f;
            AbilityCharges = 0;
            UsingPrimary = false;
        }
    }
}
