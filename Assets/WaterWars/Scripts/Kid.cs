// Kid.cs
// ---------------------------------------------------------------------------
// A "Kid" is anyone playing a round: you or a bot. This script holds what
// every kid has in common:
//
//   * a body (head / body / legs zones that water can hit)
//   * the Soak meter (0 to 100; at 100 you're Soaked and sit on the curb)
//   * a towel (armor that soaks up water before your meter fills)
//   * allowance money, water toys, and a hero (see HeroKit.cs)
//   * moving, jumping, crouching
//   * status effects heroes put on each other: blinded, disoriented,
//     rattled, slowed, laughing, revealed, shields
//
// WHO controls the kid is a separate script: PlayerController (your mouse and
// keyboard) or BotBrain (the computer). Both just call the methods here, like
// Move(), TryFire(), TryUseAbility() and TryUseUltimate(). Later, for online play, a third
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
        public HeroKit Kit;        // the code behind the hero (HeroKits.cs)
        public float MaxSoak { get { return Hero != null ? Hero.MaxSoak : Tuning.SoakedAt; } }
        public Vector3 Spawn;      // where this kid started the round

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
        public int Balloons;                  // water balloons [F]
        public int UltPoints;
        public bool UltReady { get { return Hero != null && UltPoints >= Hero.UltCost; } }
        ToyDef HeroSidearm { get { return Kit != null ? Kit.Sidearm : ToyDef.SquirtPistol; } }
        public ToyState Current { get { return UsingPrimary && Primary != null ? Primary : Sidearm; } }
        public bool Reloading { get { return reloadingToy != null; } }
        public float ReloadProgress
        {
            get { return reloadingToy == null ? 0f : 1f - Mathf.Clamp01((reloadDoneTime - Time.time) / reloadDuration); }
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

        // ---- Status effects (from other heroes' kits) ------------------------
        float revealUntil, blindUntil, disorientUntil, rattleUntil, laughUntil, slowUntil;
        float slowFactor = 1f;
        float shield, shieldUntil;
        public float Sunscreen;      // Mama's Boy: soaks up water before your towel, until it's used up
        public bool Revealed { get { return Time.time < revealUntil; } }
        public bool Blinded { get { return Time.time < blindUntil; } }
        public float BlindLeft { get { return Mathf.Max(0f, blindUntil - Time.time); } }
        public bool Disoriented { get { return Time.time < disorientUntil; } }
        public bool Rattled { get { return Time.time < rattleUntil; } }
        public bool Laughing { get { return Time.time < laughUntil; } }
        public bool Slowed { get { return Time.time < slowUntil || HeroWorld.OnBricks(transform.position); } }
        public float Shield { get { return Time.time < shieldUntil ? shield : 0f; } }

        // All of these are capped at 2 seconds (design doc rule), except reveals and slows.
        public void Reveal(float seconds) { revealUntil = Mathf.Max(revealUntil, Time.time + seconds); }
        public void Blind(float seconds) { blindUntil = Mathf.Max(blindUntil, Time.time + Mathf.Min(2f, seconds)); }
        public void Disorient(float seconds) { disorientUntil = Mathf.Max(disorientUntil, Time.time + Mathf.Min(2f, seconds)); }
        public void Rattle(float seconds) { rattleUntil = Mathf.Max(rattleUntil, Time.time + seconds); }
        public void Laugh(float seconds) { laughUntil = Mathf.Max(laughUntil, Time.time + Mathf.Min(2f, seconds)); }
        public void Slow(float factor, float seconds)
        {
            if (Time.time >= slowUntil || factor < slowFactor) slowFactor = factor;
            slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
        }
        public void GiveShield(float amount, float seconds) { shield = amount; shieldUntil = Time.time + seconds; }
        public void AddUltPoints(int n) { if (Hero != null) UltPoints = Mathf.Min(Hero.UltCost, UltPoints + n); }

        /// Everything that changes how fast you run right now.
        public float SpeedMultiplier
        {
            get
            {
                float m = Hero != null ? Hero.SpeedMultiplier : 1f;
                if (Kit != null) m *= Kit.SpeedFactor;
                if (Time.time < slowUntil) m *= slowFactor;
                if (HeroWorld.OnBricks(transform.position)) m *= 0.6f;
                if (Laughing) m *= 0.5f;
                return m;
            }
        }

        // Dodge roll and shoves.
        Vector3 dashVelocity, knockVelocity;
        float dashUntil;
        public Vector3 LastMoveWish { get; private set; }
        Transform hat;

        float nextShotTime;
        float reloadDoneTime;
        ToyState reloadingToy;
        float reloadDuration = 1f;
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

            // A cap in the hero's color (set when a hero is picked).
            var cap = Shapes.Box("Hero Cap", Vector3.zero, new Vector3(0.44f, 0.1f, 0.56f), Color.white, model, false);
            cap.transform.localPosition = new Vector3(0f, 1.84f, 0.04f);
            cap.SetActive(false);
            kid.hat = cap.transform;

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
            if (Kit != null && Alive) Kit.Tick();
            if (reloadingToy != null && Time.time >= reloadDoneTime)
            {
                reloadingToy.Ammo = reloadingToy.Def.TankShots;
                reloadingToy = null;
            }
            // Finish a burst that's in progress.
            if (burstLeft > 0 && Time.time >= nextBurstShot)
            {
                var toy = Current;
                if (!CanAct || !FireAllowed || toy.Ammo <= 0) burstLeft = 0;
                else
                {
                    toy.Ammo--;
                    burstLeft--;
                    nextBurstShot = Time.time + toy.Def.BurstInterval / FireRate;
                    FireOneShot(toy.Def, AimDirection);
                    if (toy.Ammo <= 0 && burstLeft == 0) StartReload();
                }
            }
            // Smoothly move the eye up/down when crouching.
            var e = Eye.localPosition;
            e.y = Mathf.MoveTowards(e.y, eyeHeight, Time.deltaTime * 6f);
            Eye.localPosition = e;

            if (heldToy != null)
            {
                var r = heldToy.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = Shapes.Mat(Current.Def.ViewColor);
                float len = Current.Def.ViewLength * 1.3f;
                heldToy.localScale = new Vector3(0.1f + len * 0.1f, 0.12f + len * 0.12f, len);
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
            LastMoveWish = wish;
            Vector3 horizontal = wish * speed * SpeedMultiplier;
            if (Time.time < dashUntil) horizontal = dashVelocity;
            horizontal += knockVelocity;
            knockVelocity = Vector3.MoveTowards(knockVelocity, Vector3.zero, 30f * Time.deltaTime);
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

        /// A quick 6 m roll the way you're moving (or forward if standing still).
        public bool DodgeRoll()
        {
            if (!Controller.enabled || !Grounded) return false;
            Vector3 dir = LastMoveWish.sqrMagnitude > 0.01f ? LastMoveWish.normalized : transform.forward;
            dir.y = 0f;
            dashVelocity = dir.normalized * 24f;
            dashUntil = Time.time + 0.25f;
            return true;
        }

        /// Get pushed (the Bully's shove).
        public void Knock(Vector3 velocity)
        {
            knockVelocity = new Vector3(velocity.x, 0f, velocity.z);
            if (velocity.y > 0f && Grounded) verticalVelocity = velocity.y;
        }

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
        bool FireAllowed { get { return !Laughing && (Kit == null || Kit.CanFire); } }
        float FireRate { get { return Kit != null ? Kit.FireRateFactor : 1f; } }

        public void SwitchTo(bool primary)
        {
            if (primary && Primary == null) return;
            if (UsingPrimary == primary) return;
            UsingPrimary = primary;
            reloadingToy = null; // switching cancels a reload
            burstLeft = 0;
            Zoomed = false;
            nextShotTime = Mathf.Max(nextShotTime, Time.time + 0.25f);
        }

        /// True while a scoped toy is zoomed in (set by whoever controls the kid).
        public bool Zoomed;

        /// Try to shoot in a direction. `triggerJustPressed` matters for click-per-shot toys like the pistol.
        public bool TryFire(Vector3 aimDirection, bool triggerJustPressed)
        {
            if (!CanAct || !FireAllowed || Reloading || burstLeft > 0) return false;
            var toy = Current;
            if (!toy.Def.HoldToFire && !triggerJustPressed) return false;
            if (Time.time < nextShotTime) return false;
            if (toy.Ammo <= 0) { StartReload(); return false; }

            nextShotTime = Time.time + 1f / (toy.Def.ShotsPerSecond * FireRate);
            toy.Ammo--;
            FireOneShot(toy.Def, aimDirection);
            // Burst toys fire the rest of the burst over the next few frames (see Update).
            burstLeft = toy.Def.Burst - 1;
            nextBurstShot = Time.time + toy.Def.BurstInterval / FireRate;
            if (toy.Ammo <= 0 && burstLeft == 0) StartReload();
            return true;
        }

        int burstLeft;
        float nextBurstShot;

        void FireOneShot(ToyDef def, Vector3 aimDirection)
        {
            // Moving, jumping, crouching and scoping change how steady your aim is.
            float spread = Zoomed && def.ZoomFov > 0f ? def.ZoomedSpreadDegrees : def.SpreadDegrees;
            if (HorizontalSpeed > Tuning.WalkSpeed + 0.5f || !Grounded) spread += def.MovingSpreadDegrees;
            if (Crouching) spread *= 0.7f;
            if (Rattled) spread += 5f;        // the Bully's insults
            if (Disoriented) spread += 4f;

            // Spray toys fire several drops at once. Each drop wobbles on its own.
            Vector3 center = WaterShots.Wobble(aimDirection, def.Pellets > 1 ? 0f : spread);
            for (int i = 0; i < def.Pellets; i++)
            {
                Vector3 dir = def.Pellets > 1 ? WaterShots.Wobble(center, spread) : center;
                if (def.Arcing)
                    WaterShots.LaunchGlob(this, Eye.position + dir * 0.6f - Vector3.up * 0.08f, dir * def.WaterSpeed,
                        def.WaterGravity, def.SoakPerHit, def.Range, def.FalloffStart, 0f, def.Pellets > 1 ? 0.1f : 0.14f);
                else
                    WaterShots.Instant(this, Eye.position, dir, def, i == 0);
            }
            if (OnFired != null) OnFired();
            if (Kit != null) Kit.OnFired();
        }

        public void StartReload()
        {
            var toy = Current;
            if (Reloading || toy.Ammo >= toy.Def.TankShots || !Alive) return;
            reloadingToy = toy;
            reloadDuration = toy.Def.ReloadSeconds / (Kit != null ? Kit.ReloadFactor : 1f);
            reloadDoneTime = Time.time + reloadDuration;
        }

        // =====================================================================
        // Hero: picking one, ability [Q], ultimate [X], extras [V] / right-click,
        // water balloons [F]
        // =====================================================================
        public void SetHero(HeroDef hero)
        {
            Hero = hero;
            Kit = hero.MakeKit();
            Kit.Kid = this;
            UltPoints = 0;
            Sidearm = new ToyState(HeroSidearm);
            hat.gameObject.SetActive(true);
            var r = hat.GetComponent<Renderer>();
            r.sharedMaterial = Shapes.Mat(hero.Color);
            if (IsPlayer) r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        bool AbilitiesAllowed { get { return CanAct && Kit != null && !HeroWorld.AbilitiesBlockedAt(transform.position); } }

        public bool TryUseAbility(Vector3 aimDirection)
        {
            if (!AbilitiesAllowed || AbilityCharges <= 0) return false;
            if (!Kit.UseAbility(aimDirection)) return false;
            AbilityCharges--;
            return true;
        }

        public bool TryUseUltimate(Vector3 aimDirection)
        {
            if (!AbilitiesAllowed || !UltReady) return false;
            if (!Kit.UseUltimate(aimDirection)) return false;
            UltPoints = 0;
            if (RoundManager.Instance != null) RoundManager.Instance.AddFeed(DisplayName + " used " + Hero.UltName + "!");
            return true;
        }

        /// [V]: the Bully's shove, the Twins' switch.
        public bool TryHeroAction(Vector3 aimDirection)
        {
            return CanAct && Kit != null && Kit.HeroAction(aimDirection);
        }

        /// Right-click with a toy that has no scope.
        public bool TryAltFire(Vector3 aimDirection)
        {
            return CanAct && Kit != null && !Laughing && Kit.AltFire(aimDirection);
        }

        /// [F]: throw a water balloon. Bursts and soaks everyone within 3.5 m.
        public bool ThrowBalloon(Vector3 aimDirection)
        {
            if (!CanAct || Balloons <= 0 || Laughing) return false;
            Balloons--;
            float speed = Kit != null ? Kit.BalloonSpeed : 19f;
            Vector3 throwDir = (aimDirection + Vector3.up * 0.15f).normalized;
            var glob = WaterShots.LaunchGlob(this, Eye.position + aimDirection * 0.6f, throwDir * speed, 14f, 0f, 80f, 80f, 3.5f, 0.35f);
            if (Kit != null && Kit.HotSauce)
            {
                glob.GetComponent<Renderer>().sharedMaterial = Shapes.Mat(new Color(0.9f, 0.25f, 0.1f));
                var me = this;
                glob.OnBurst = at =>
                {
                    // Hot sauce: stings the eyes of everyone the burst reaches.
                    foreach (var k in All)
                    {
                        if (!k.Alive || k.Team == me.Team) continue;
                        Vector3 chest = k.transform.position + Vector3.up;
                        if (Vector3.Distance(chest, at) > 3.5f) continue;
                        if (Physics.Linecast(at + Vector3.up * 0.2f, chest, Shapes.ShotMask, QueryTriggerInteraction.Ignore)) continue;
                        k.Blind(1.5f);
                    }
                };
            }
            return true;
        }

        // =====================================================================
        // Getting wet and drying off
        // =====================================================================

        /// Add soak. Towels absorb it first. At 100 the RoundManager sends you to the curb.
        public void TakeSoak(float amount, Kid attacker, Vector3 fromPosition)
        {
            if (IsOut || amount <= 0f) return;
            // Darling's Look at Me!: enemy water meant for a teammate goes to her instead.
            if (attacker != null && attacker.Team != Team)
            {
                var guard = DarlingKit.GuardFor(this);
                if (guard != null) { guard.TakeSoak(amount, attacker, fromPosition); return; }
            }
            if (Kit != null) amount = Kit.ModifyIncoming(amount, attacker);
            if (amount <= 0f) return;
            amount = Absorb(ref Sunscreen, amount);
            if (Time.time < shieldUntil) amount = Absorb(ref shield, amount);
            amount = Absorb(ref Towel, amount);
            LastHitTime = Time.time;
            LastHitFrom = fromPosition;
            LastAttacker = attacker;
            if (amount <= 0f) return;
            Soak = Mathf.Min(MaxSoak, Soak + amount);
            if (Kit != null) Kit.OnSoakTaken(amount);
            RefreshWetLook();
            if (Soak >= MaxSoak && RoundManager.Instance != null)
            {
                if (Kit != null && Kit.KeepInWhenSoaked()) return;   // the Bully's Tantrum
                RoundManager.Instance.OnKidSoaked(this, attacker);
            }
        }

        static float Absorb(ref float pool, float amount)
        {
            if (pool <= 0f) return amount;
            float taken = Mathf.Min(pool, amount);
            pool -= taken;
            return amount - taken;
        }

        /// Out right now, no matter what (the Bully when his Tantrum runs out while soaked).
        public void ForceOut()
        {
            if (IsOut || RoundManager.Instance == null) return;
            Soak = MaxSoak;
            RefreshWetLook();
            RoundManager.Instance.OnKidSoaked(this, LastAttacker);
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
            float wet = Soak / MaxSoak;
            Color soaked = new Color(0.12f, 0.2f, 0.45f);
            for (int i = 0; i < wetRenderers.Length; i++)
                wetRenderers[i].sharedMaterial.color = Color.Lerp(dryColors[i], soaked, wet * 0.75f);
        }

        /// Soaked: out for the round. Walk off and sit on the curb.
        public void SendToCurb(Vector3 curbSpot)
        {
            IsOut = true;
            reloadingToy = null;
            dashUntil = 0f;
            knockVelocity = Vector3.zero;
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
                Sidearm = new ToyState(HeroSidearm);
                Towel = 0f;
                Balloons = 0;
            }
            Spawn = spawn;
            revealUntil = blindUntil = disorientUntil = rattleUntil = laughUntil = slowUntil = shieldUntil = 0f;
            Sunscreen = 0f;
            dashUntil = 0f;
            knockVelocity = Vector3.zero;
            burstLeft = 0;
            Zoomed = false;
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
            if (Kit != null) Kit.OnRoundStart(survivedLastRound);
        }

        /// Lose everything (used when teams swap sides at half time).
        public void StripGear()
        {
            Primary = null;
            Sidearm = new ToyState(HeroSidearm);
            Towel = 0f;
            AbilityCharges = 0;
            Balloons = 0;
            UsingPrimary = false;
        }
    }
}
