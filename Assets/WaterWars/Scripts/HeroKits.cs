// HeroKits.cs
// ---------------------------------------------------------------------------
// What each of the twelve heroes actually does. One class per hero. Names,
// prices and descriptions are in Heroes.cs; shared helpers in HeroKit.cs.
//
// Everything here is grey-box: plain shapes, made-in-code sounds. Numbers
// that decide balance are written right next to what they do, with a note.
// ---------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaterWars
{
    // =========================================================================
    // RICHY (Duelist)
    // =========================================================================
    public class RichyKit : HeroKit
    {
        float heat;          // seconds of nonstop fire with the Motorized Blaster
        float lastShot = -9f;
        float overheatedUntil;
        GameObject butler;

        public override void OnRoundStart(bool survived)
        {
            Kid.Money = Mathf.Min(Tuning.MaxMoney, Kid.Money + 4);   // Silver Spoon
            heat = 0f;
            butler = null;
        }

        public override bool CanFire { get { return Time.time >= overheatedUntil; } }

        public override void OnFired()
        {
            if (Kid.Current.Def != HeroToys.MotorizedBlaster) return;
            heat = Time.time - lastShot < 0.35f ? heat + (Time.time - lastShot) : 0f;
            lastShot = Time.time;
            if (heat > 4f)   // overheats after 4 s of nonstop fire
            {
                overheatedUntil = Time.time + 1.5f;
                heat = 0f;
                if (Kid.IsPlayer) Hud.Toast("Battery overheated!");
            }
        }

        public override string Status { get { return Time.time < overheatedUntil ? "Overheated!" : butler != null ? "Butler on the way" : null; } }

        // Butler's Fresh Clothes: he jogs in from behind you. Soak him to cancel.
        public override bool UseAbility(Vector3 aim)
        {
            if (butler != null) return false;
            Vector3 back = -Kid.transform.forward;
            Vector3 start = Pos + back * 7f;
            if (!NavGraph.SpotIsFree(start) || !Clear(Pos + Vector3.up, start + Vector3.up)) start = Pos + back * 2.5f;
            butler = MakePerson("Butler", start, new Color(0.12f, 0.12f, 0.14f), new Color(0.95f, 0.95f, 0.95f), 1.95f);
            var soak = butler.AddComponent<Soakable>();
            soak.Team = Kid.Team; soak.Health = 40f;
            var me = Kid;
            soak.OnSoakedOut = () => { if (me.IsPlayer) Hud.Toast("Your butler got soaked!"); };
            var walker = butler.AddComponent<Walker>();
            walker.Speed = 6f;
            walker.Target = () => me.transform.position;
            var go = butler;
            walker.OnArrive = () =>
            {
                if (me.Alive) me.Dry(me.Soak * 0.5f);
                WaterShots.Splash(me.transform.position + Vector3.up, new Color(0.95f, 0.95f, 0.95f), 10, 2f);
                Object.Destroy(go);
            };
            return true;
        }

        // Dad's Satellite: three pings, 2 s apart.
        public override bool UseUltimate(Vector3 aim)
        {
            Kid.StartCoroutine(Pings());
            return true;
        }

        IEnumerator Pings()
        {
            for (int i = 0; i < 3; i++)
            {
                foreach (var e in Enemies()) e.Reveal(1.4f);
                HeroWorld.Play(HeroWorld.Sound.Ring, Pos, 0.3f);
                yield return new WaitForSeconds(2f);
            }
        }

        public override void BotFight(Kid target, float distance)
        {
            if (Kid.Soak > Kid.MaxSoak * 0.5f && Chance(0.3f)) Kid.TryUseAbility(Kid.AimDirection);
            if (Kid.UltReady && Chance(0.1f)) Kid.TryUseUltimate(Kid.AimDirection);
        }

        public override void BotIdle()
        {
            if (Kid.Soak > Kid.MaxSoak * 0.4f && Chance(0.05f)) Kid.TryUseAbility(Kid.AimDirection);
            if (Kid.UltReady && Chance(0.01f)) Kid.TryUseUltimate(Kid.AimDirection);
        }

        /// A grown-up shape: trigger colliders so water can hit them but kids can walk past.
        public static GameObject MakePerson(string name, Vector3 at, Color suit, Color shirt, float height)
        {
            var root = new GameObject(name);
            root.transform.SetParent(HeroWorld.Root, false);
            root.transform.position = at;
            var legs = Shapes.BoxOnGround(name + " Legs", at, new Vector3(0.5f, height * 0.45f, 0.35f), suit, root.transform);
            var body = Shapes.BoxOnGround(name + " Body", at + Vector3.up * height * 0.45f, new Vector3(0.65f, height * 0.4f, 0.4f), shirt, root.transform);
            var head = Shapes.Box(name + " Head", at + Vector3.up * (height * 0.85f + 0.15f), Vector3.one * 0.38f, new Color(0.96f, 0.8f, 0.66f), root.transform);
            foreach (var g in new[] { legs, body, head }) g.GetComponent<Collider>().isTrigger = true;
            return root;
        }
    }

    // =========================================================================
    // SWEET TOOTH (Duelist)
    // =========================================================================
    public class SweetToothKit : HeroKit
    {
        float rushUntil, crashUntil;

        public override void OnRoundStart(bool survived)
        {
            Kid.Balloons = Mathf.Max(Kid.Balloons, 1);   // one free Hot Sauce Balloon every round
            rushUntil = crashUntil = 0f;
        }

        public override bool HotSauce { get { return true; } }
        bool Rushing { get { return Time.time < rushUntil; } }
        bool Crashing { get { return !Rushing && Time.time < crashUntil; } }
        public override float SpeedFactor { get { return Rushing ? 1.25f : Crashing ? 0.7f : 1f; } }
        public override float FireRateFactor { get { return Rushing ? 1.3f : 1f; } }
        public override float ReloadFactor { get { return Rushing ? 1.5f : 1f; } }
        public override string Status { get { return Rushing ? "SUGAR RUSH " + Mathf.CeilToInt(rushUntil - Time.time) : Crashing ? "Sugar crash..." : null; } }

        public override bool UseAbility(Vector3 aim) { return Kid.DodgeRoll(); }

        public override bool UseUltimate(Vector3 aim)
        {
            rushUntil = Time.time + 10f;
            crashUntil = rushUntil + 3f;
            HeroWorld.Play(HeroWorld.Sound.Swoosh, Pos);
            return true;
        }

        public override void BotFight(Kid target, float distance)
        {
            if (Time.time - Kid.LastHitTime < 0.3f && Chance(0.25f)) Kid.TryUseAbility(Kid.AimDirection);
            if (Kid.UltReady && Chance(0.15f)) Kid.TryUseUltimate(Kid.AimDirection);
            if (Kid.Balloons > 0 && distance > 7f && distance < 22f && Chance(0.12f)) Kid.ThrowBalloon(Kid.AimDirection);
        }
    }

    // =========================================================================
    // THE BULLY (DPS)
    // =========================================================================
    public class BullyKit : HeroKit
    {
        float shoveReadyAt;
        float tantrumUntil;
        bool raging;

        public override void OnRoundStart(bool survived) { tantrumUntil = 0f; raging = false; shoveReadyAt = 0f; }

        bool InTantrum { get { return Time.time < tantrumUntil; } }
        public override float SpeedFactor { get { return raging ? 1.4f : InTantrum ? 1.15f : 1f; } }
        public override bool CanFire { get { return !raging; } }
        public override string Status
        {
            get
            {
                if (raging) return "TANTRUM! " + Mathf.CeilToInt(tantrumUntil - Time.time) + " (no squirting, shove with V)";
                if (InTantrum) return "Tantrum " + Mathf.CeilToInt(tantrumUntil - Time.time);
                return Time.time < shoveReadyAt ? "Shove ready in " + Mathf.CeilToInt(shoveReadyAt - Time.time) : null;
            }
        }

        // Knuckle Sandwich (passive, on V): shove whoever is right in front of you.
        public override bool HeroAction(Vector3 aim)
        {
            if (Time.time < shoveReadyAt) return false;
            Vector3 flat = new Vector3(aim.x, 0f, aim.z).normalized;
            bool any = false;
            foreach (var e in Enemies())
            {
                Vector3 to = e.transform.position - Pos;
                if (to.magnitude > 2.6f || Vector3.Angle(flat, new Vector3(to.x, 0f, to.z)) > 50f) continue;
                e.Knock(flat * 9f + Vector3.up * 1f);
                e.Disorient(1f);
                any = true;
            }
            // Knock over gadgets (fans, cushions, walls...) too.
            foreach (var c in Physics.OverlapSphere(Pos + flat * 1.5f + Vector3.up, 1.6f, Shapes.ShotMask, QueryTriggerInteraction.Collide))
            {
                var s = c.GetComponentInParent<Soakable>();
                if (s != null && s.Team != Kid.Team) { s.KnockOver(Kid); any = true; }
            }
            HeroWorld.Play(HeroWorld.Sound.Pop, Pos, 0.6f);
            shoveReadyAt = Time.time + (any ? 6f : 1.5f);
            return true;
        }

        // Sticks and Stones: an insult in a cone. Enemies hit are Rattled.
        public override bool UseAbility(Vector3 aim)
        {
            foreach (var e in Enemies())
            {
                Vector3 to = Chest(e) - Eye;
                if (to.magnitude > 14f || Vector3.Angle(aim, to) > 30f || !Clear(Eye, Chest(e))) continue;
                e.Rattle(3f);
            }
            HeroWorld.Play(HeroWorld.Sound.Yell, Pos);
            HeroWorld.Ring(Pos + Kid.transform.forward * 5f, 3f, new Color(0.9f, 0.3f, 0.2f), 0.4f);
            return true;
        }

        public override bool UseUltimate(Vector3 aim)
        {
            tantrumUntil = Time.time + 8f;
            raging = false;
            HeroWorld.Play(HeroWorld.Sound.Yell, Pos);
            return true;
        }

        public override bool KeepInWhenSoaked()
        {
            if (!InTantrum) return false;
            if (!raging && Kid.IsPlayer) Hud.Toast("TANTRUM! Keep running!");
            raging = true;
            return true;
        }

        public override void Tick()
        {
            if (raging && !InTantrum)
            {
                raging = false;
                Kid.ForceOut();
            }
        }

        public override void BotFight(Kid target, float distance)
        {
            if (distance < 2.5f) Kid.TryHeroAction(Kid.AimDirection);
            if (distance < 13f && Chance(0.15f)) Kid.TryUseAbility(Kid.AimDirection);
            if (Kid.UltReady && Kid.Soak > Kid.MaxSoak * 0.4f && Chance(0.3f)) Kid.TryUseUltimate(Kid.AimDirection);
        }
    }

    // =========================================================================
    // LIL' TIMMY (DPS) and Maximilian the dog
    // =========================================================================
    public class TimmyKit : HeroKit
    {
        GameObject riddenDog;   // Max under Timmy
        GameObject runningDog;  // Max out on a mission
        float soakForUlt;

        bool MaxIsHome { get { return runningDog == null; } }
        public override float SpeedFactor { get { return MaxIsHome ? 1.1f : 0.85f; } }
        public override string Status { get { return MaxIsHome ? null : "Max is out! (slower on foot)"; } }

        public override void OnRoundStart(bool survived)
        {
            if (runningDog != null) Object.Destroy(runningDog);
            runningDog = null;
            if (riddenDog == null) riddenDog = MakeDog(Kid.Model, Vector3.zero);
            riddenDog.SetActive(true);
        }

        static GameObject MakeDog(Transform parent, Vector3 at)
        {
            var dog = new GameObject("Maximilian");
            dog.transform.SetParent(parent, false);
            dog.transform.localPosition = at;
            var brown = new Color(0.55f, 0.38f, 0.22f);
            Shapes.BoxOnGround("Max Body", Vector3.zero, new Vector3(0.45f, 0.55f, 1.1f), brown, dog.transform, false).transform.localPosition = new Vector3(0f, 0.1f + 0.275f, 0f);
            Shapes.Box("Max Head", Vector3.zero, new Vector3(0.35f, 0.35f, 0.4f), brown * 0.9f, dog.transform, false).transform.localPosition = new Vector3(0f, 0.7f, 0.65f);
            return dog;
        }

        // Charges faster by taking soak: every 40 soak taken is worth one ult point.
        public override void OnSoakTaken(float amount)
        {
            soakForUlt += amount;
            while (soakForUlt >= 40f) { soakForUlt -= 40f; Kid.AddUltPoints(1); }
        }

        // Max! Speak!
        public override bool UseAbility(Vector3 aim)
        {
            Vector3 from = runningDog != null ? runningDog.transform.position : Pos;
            foreach (var e in Enemies())
                if (Vector3.Distance(e.transform.position, from) <= 8f && Clear(from + Vector3.up, Chest(e)))
                {
                    e.Slow(0.6f, 1.5f);
                    e.Disorient(1.5f);
                }
            HeroWorld.Play(HeroWorld.Sound.Bark, from);
            HeroWorld.Ring(from, 8f, new Color(0.95f, 0.85f, 0.5f), 0.35f);
            return true;
        }

        // Max! Shake!
        public override bool UseUltimate(Vector3 aim)
        {
            if (!MaxIsHome) return false;
            Vector3 spot = GroundSpot(aim, 30f);
            riddenDog.SetActive(false);
            runningDog = MakeDog(HeroWorld.Root, Vector3.zero);
            runningDog.transform.position = Pos;
            var walker = runningDog.AddComponent<Walker>();
            walker.Speed = 14f;
            walker.Target = () => spot;
            var me = Kid;
            var dog = runningDog;
            walker.OnArrive = () =>
            {
                HeroWorld.Play(HeroWorld.Sound.Bark, spot);
                WaterShots.AreaSoak(me, spot + Vector3.up * 0.6f, 5f, 60f, 30f);
                WaterShots.Splash(spot + Vector3.up * 0.6f, WaterShots.WaterColor, 30, 5f);
                HeroWorld.Ring(spot, 5f, WaterShots.WaterColor, 0.5f);
                walker.Speed = 10f;
                walker.ArriveDistance = 1f;
                walker.Target = () => me.transform.position;
                walker.OnArrive = () => { Object.Destroy(dog); };
            };
            return true;
        }

        public override void Tick()
        {
            if (runningDog == null && riddenDog != null && !riddenDog.activeSelf) riddenDog.SetActive(true);
        }

        public override void BotFight(Kid target, float distance)
        {
            if (distance < 7f && Chance(0.2f)) Kid.TryUseAbility(Kid.AimDirection);
            if (Kid.UltReady && distance < 28f && Chance(0.2f)) Kid.TryUseUltimate(Kid.AimDirection);
        }
    }

    // =========================================================================
    // BOY SCOUT (DPS)
    // =========================================================================
    public class ScoutKit : HeroKit
    {
        float trackingUntil;
        float nextPrint, nextPing;
        float refillReadyAt;

        public override int MaxBalloons { get { return 2; } }
        public override float BalloonSpeed { get { return 28f; } }
        public override void OnRoundStart(bool survived) { trackingUntil = 0f; }
        public override string Status { get { return Time.time < trackingUntil ? "Tracking " + Mathf.CeilToInt(trackingUntil - Time.time) : null; } }

        public override bool UseAbility(Vector3 aim) { return Kid.DodgeRoll(); }

        public override bool UseUltimate(Vector3 aim)
        {
            trackingUntil = Time.time + 12f;
            nextPing = Time.time;
            return true;
        }

        // Camelbak alt-fire: top off the teammate you're looking at.
        public override bool AltFire(Vector3 aim)
        {
            if (Kid.Current.Def != HeroToys.CamelbakSprayer || Time.time < refillReadyAt) return false;
            var mate = AllyNearCrosshair(aim, 6f, 25f);
            if (mate == null) return false;
            if (mate.Primary != null) mate.Primary.Ammo = mate.Primary.Def.TankShots;
            mate.Sidearm.Ammo = mate.Sidearm.Def.TankShots;
            WaterShots.Splash(Chest(mate), WaterShots.WaterColor, 8, 1.5f);
            if (Kid.IsPlayer) Hud.Toast("Refilled " + mate.DisplayName);
            refillReadyAt = Time.time + 1f;
            return true;
        }

        public override void Tick()
        {
            if (Time.time >= trackingUntil) return;
            bool show = RoundManager.Instance != null && RoundManager.Instance.Player.Team == Kid.Team;
            if (show && Time.time >= nextPrint)
            {
                nextPrint = Time.time + 0.35f;
                foreach (var e in Enemies())
                {
                    if (e.HorizontalSpeed < 0.5f) continue;
                    var print = Shapes.Box("Footprint", e.transform.position + Vector3.up * 0.03f, new Vector3(0.25f, 0.02f, 0.4f), new Color(1f, 0.55f, 0.1f), HeroWorld.Root, false);
                    print.transform.rotation = e.transform.rotation;
                    Object.Destroy(print, 4f);
                }
            }
            if (Time.time >= nextPing)
            {
                nextPing = Time.time + 3f;
                foreach (var e in Enemies()) e.Reveal(0.8f);
            }
        }

        public override void BotFight(Kid target, float distance)
        {
            if (Time.time - Kid.LastHitTime < 0.3f && Chance(0.25f)) Kid.TryUseAbility(Kid.AimDirection);
            if (Kid.UltReady && Chance(0.1f)) Kid.TryUseUltimate(Kid.AimDirection);
            if (Kid.Balloons > 0 && distance > 8f && distance < 30f && Chance(0.12f)) Kid.ThrowBalloon(Kid.AimDirection);
        }

        public override void BotIdle()
        {
            if (Kid.UltReady && Chance(0.01f)) Kid.TryUseUltimate(Kid.AimDirection);
        }
    }

    // =========================================================================
    // MAMA LOGAN (Sentinel)
    // =========================================================================
    public class MamaLoganKit : HeroKit
    {
        GameObject fan;

        public override void OnRoundStart(bool survived) { fan = null; }

        // Box Fan: dries teammates in front of it, 8 soak per second, for 20 s.
        public override bool UseAbility(Vector3 aim)
        {
            if (fan != null) Object.Destroy(fan);
            Vector3 flat = new Vector3(aim.x, 0f, aim.z).normalized;
            Vector3 at = Pos + flat * 1.6f;
            if (!NavGraph.PathIsClear(Pos, at)) at = Pos + flat * 0.8f;
            fan = new GameObject("Box Fan");
            fan.transform.SetParent(HeroWorld.Root, false);
            fan.transform.position = at;
            fan.transform.rotation = Quaternion.LookRotation(flat);
            Shapes.BoxOnGround("Fan Box", at, new Vector3(0.8f, 0.9f, 0.35f), new Color(0.85f, 0.85f, 0.82f), fan.transform);
            Shapes.Box("Fan Blades", at + Vector3.up * 0.45f + flat * 0.19f, new Vector3(0.6f, 0.6f, 0.03f), new Color(0.4f, 0.6f, 0.9f), fan.transform, false);
            var soak = fan.AddComponent<Soakable>();
            soak.Team = Kid.Team; soak.Health = 60f;
            var f = fan.AddComponent<BoxFan>();
            f.Team = Kid.Team;
            Object.Destroy(fan, 20f);
            return true;
        }

        // Laundry Day: fully dry and shield everyone nearby.
        public override bool UseUltimate(Vector3 aim)
        {
            foreach (var a in Allies(true))
                if (Vector3.Distance(a.transform.position, Pos) <= 8f)
                {
                    a.Dry(a.Soak);
                    a.GiveShield(50f, 10f);
                    WaterShots.Splash(Chest(a), new Color(1f, 1f, 1f), 10, 2f);
                }
            HeroWorld.Ring(Pos, 8f, new Color(1f, 1f, 1f), 0.6f);
            return true;
        }

        // Right-click: switch the nozzle between jet and mist.
        public override bool AltFire(Vector3 aim)
        {
            var def = Kid.Sidearm.Def;
            if (def != HeroToys.NozzleJet && def != HeroToys.NozzleMist) return false;
            var next = def == HeroToys.NozzleJet ? HeroToys.NozzleMist : HeroToys.NozzleJet;
            int ammo = Kid.Sidearm.Ammo;
            Kid.Sidearm = new ToyState(next) { Ammo = ammo };
            if (Kid.IsPlayer) Hud.Toast(next == HeroToys.NozzleMist ? "Nozzle: MIST" : "Nozzle: JET");
            return true;
        }

        public override void BotFight(Kid target, float distance)
        {
            if (fan == null && Kid.Soak > 30f && Chance(0.1f)) Kid.TryUseAbility(-Kid.transform.forward);
            if (Kid.UltReady && WetAlliesNear(8f) >= 2 && Chance(0.3f)) Kid.TryUseUltimate(Kid.AimDirection);
        }

        public override void BotIdle()
        {
            if (fan == null && WetAlliesNear(7f) >= 1 && Chance(0.05f)) Kid.TryUseAbility(Kid.AimDirection);
            if (Kid.UltReady && WetAlliesNear(8f) >= 3) Kid.TryUseUltimate(Kid.AimDirection);
        }

        int WetAlliesNear(float r)
        {
            int n = 0;
            foreach (var a in Allies(true)) if (a.Soak > 30f && Vector3.Distance(a.transform.position, Pos) <= r) n++;
            return n;
        }
    }

    public class BoxFan : MonoBehaviour
    {
        public Team Team;
        float next;

        void Update()
        {
            if (Time.time < next) return;
            next = Time.time + 0.25f;
            Vector3 p = transform.position + Vector3.up * 0.5f;
            foreach (var k in Kid.All)
            {
                if (!k.Alive || k.Team != Team) continue;
                Vector3 to = k.transform.position + Vector3.up - p;
                if (to.magnitude > 7f || Vector3.Angle(transform.forward, to) > 35f) continue;
                if (Physics.Linecast(p, k.transform.position + Vector3.up, Shapes.ShotMask, QueryTriggerInteraction.Ignore)) continue;
                k.Dry(2f);   // 8 per second
            }
        }
    }

    // =========================================================================
    // DARLING (Sentinel)
    // =========================================================================
    public class DarlingKit : HeroKit
    {
        static readonly List<DarlingKit> active = new List<DarlingKit>();
        float lookUntil;

        bool Looking { get { return Time.time < lookUntil && Kid.Alive; } }
        public override void OnRoundStart(bool survived) { lookUntil = 0f; }
        public override string Status { get { return Looking ? "LOOK AT ME! " + Mathf.CeilToInt(lookUntil - Time.time) : null; } }

        // Tough Cookie: 15% less soak. Look at Me!: 60% less on top.
        public override float ModifyIncoming(float amount, Kid attacker)
        {
            amount *= 0.85f;
            if (Looking) amount *= 0.4f;
            return amount;
        }

        /// During Look at Me!, enemy water meant for teammates within 15 m comes to Darling instead.
        public static Kid GuardFor(Kid victim)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var d = active[i];
                if (d.Kid == null || !d.Looking) { active.RemoveAt(i); continue; }
                if (d.Kid != victim && d.Kid.Team == victim.Team && Vector3.Distance(d.Kid.transform.position, victim.transform.position) <= 15f)
                    return d.Kid;
            }
            return null;
        }

        public override bool UseAbility(Vector3 aim)
        {
            foreach (var a in Allies(true))
                if (Vector3.Distance(a.transform.position, Pos) <= 7f)
                {
                    a.Towel = Mathf.Min(75f, a.Towel + 25f);
                    WaterShots.Splash(Chest(a), new Color(1f, 0.6f, 0.8f), 8, 1.5f);
                }
            HeroWorld.Ring(Pos, 7f, new Color(1f, 0.6f, 0.8f), 0.4f);
            return true;
        }

        public override bool UseUltimate(Vector3 aim)
        {
            lookUntil = Time.time + 6f;
            if (!active.Contains(this)) active.Add(this);
            HeroWorld.Ring(Pos, 15f, new Color(1f, 0.6f, 0.8f), 0.8f);
            HeroWorld.Play(HeroWorld.Sound.Yell, Pos);
            return true;
        }

        public override void BotFight(Kid target, float distance)
        {
            if (Chance(0.05f)) Kid.TryUseAbility(Kid.AimDirection);
            if (Kid.UltReady && Chance(0.15f)) Kid.TryUseUltimate(Kid.AimDirection);
        }

        public override void BotIdle()
        {
            if (RoundManager.Instance.Phase == Phase.Play && Chance(0.01f)) Kid.TryUseAbility(Kid.AimDirection);
        }
    }

    // =========================================================================
    // MAMA'S BOY (Sentinel)
    // =========================================================================
    public class MamasBoyKit : HeroKit
    {
        float jacketUntil;
        static int bouncing;   // stops two Life Jackets bouncing water back and forth forever

        bool Jacket { get { return Time.time < jacketUntil; } }
        public override void OnRoundStart(bool survived) { jacketUntil = 0f; }
        public override string Status { get { return Jacket ? "Life Jacket " + Mathf.CeilToInt(jacketUntil - Time.time) : null; } }

        // Running Home: faster when moving toward your spawn.
        public override float SpeedFactor
        {
            get
            {
                Vector3 home = Kid.Spawn - Pos;
                home.y = 0f;
                Vector3 v = Kid.LastMoveWish;
                if (home.magnitude < 3f || v.sqrMagnitude < 0.01f) return 1f;
                return Vector3.Dot(v.normalized, home.normalized) > 0.5f ? 1.15f : 1f;
            }
        }

        public override float ModifyIncoming(float amount, Kid attacker)
        {
            if (!Jacket || attacker == null || bouncing > 0) return amount;
            bouncing++;
            attacker.TakeSoak(amount, Kid, Pos);
            WaterShots.Splash(Chest(Kid), new Color(1f, 0.6f, 0.1f), 6, 3f);
            bouncing--;
            return 0f;
        }

        // Sunscreen Slather: the teammate you're looking at, or yourself.
        public override bool UseAbility(Vector3 aim)
        {
            var who = AllyNearCrosshair(aim, 6f, 25f) ?? Kid;
            who.Sunscreen = 40f;
            WaterShots.Splash(Chest(who), new Color(1f, 0.95f, 0.8f), 10, 1.5f);
            if (Kid.IsPlayer) Hud.Toast(who == Kid ? "Sunscreen on you" : "Sunscreen on " + who.DisplayName);
            return true;
        }

        public override bool UseUltimate(Vector3 aim)
        {
            jacketUntil = Time.time + 6f;
            return true;
        }

        public override void BotFight(Kid target, float distance)
        {
            if (Kid.Sunscreen <= 0f && Chance(0.1f)) Kid.TryUseAbility(Kid.AimDirection);
            if (Kid.UltReady && Kid.Soak > 30f && Chance(0.3f)) Kid.TryUseUltimate(Kid.AimDirection);
        }

        public override void BotIdle()
        {
            if (RoundManager.Instance.Phase == Phase.Play && Kid.Sunscreen <= 0f && Chance(0.01f)) Kid.TryUseAbility(-Kid.AimDirection);
        }
    }

    // =========================================================================
    // TATTLE TALE (Utility)
    // =========================================================================
    public class TattleKit : HeroKit
    {
        // I'm Telling is drawn by the HUD (it asks every enemy's UltReady).

        // Phone Call: reveal the enemy closest to your crosshair for 3 s.
        public override bool UseAbility(Vector3 aim)
        {
            var e = EnemyNearCrosshair(aim, 80f, 60f, false);
            if (e == null) { if (Kid.IsPlayer) Hud.Toast("Nobody to call that way"); return false; }
            e.Reveal(3f);
            HeroWorld.Play(HeroWorld.Sound.Ring, e.transform.position);
            if (Kid.IsPlayer) Hud.Toast("Ring ring! " + e.DisplayName + " revealed");
            return true;
        }

        // Over-Protective Parent: no abilities near them, for 10 s, for anyone.
        public override bool UseUltimate(Vector3 aim)
        {
            Vector3 at = GroundSpot(aim, 25f);
            var parent = RichyKit.MakePerson("Over-Protective Parent", at, new Color(0.55f, 0.45f, 0.3f), new Color(0.7f, 0.75f, 0.85f), 2.1f);
            Object.Destroy(parent, 10f);
            HeroWorld.ParentZones.Add(new HeroWorld.Zone { Center = at, Radius = 10f, Until = Time.time + 10f, Team = Kid.Team });
            HeroWorld.Ring(at, 10f, new Color(0.95f, 0.85f, 0.4f), 1f);
            HeroWorld.Play(HeroWorld.Sound.Yell, at);
            return true;
        }

        public override void BotFight(Kid target, float distance)
        {
            if (Kid.UltReady && distance < 20f && Chance(0.15f)) Kid.TryUseUltimate((target.transform.position - Eye).normalized);
        }

        public override void BotIdle()
        {
            if (RoundManager.Instance.Phase == Phase.Play && Chance(0.008f) && NearestEnemyDistance() < 60f)
            {
                Kid nearest = null;
                float best = float.MaxValue;
                foreach (var e in Enemies())
                {
                    float d = Vector3.Distance(e.transform.position, Pos);
                    if (d < best) { best = d; nearest = e; }
                }
                if (nearest != null) Kid.TryUseAbility((Chest(nearest) - Eye).normalized);
            }
        }
    }

    // =========================================================================
    // CLASS CLOWN (Utility)
    // =========================================================================
    public class ClownKit : HeroKit
    {
        readonly List<GameObject> cushions = new List<GameObject>();
        bool peelUsed;

        public override void OnRoundStart(bool survived) { cushions.Clear(); peelUsed = false; }

        // Whoopee Cushion: a hidden trap.
        public override bool UseAbility(Vector3 aim)
        {
            cushions.RemoveAll(c => c == null);
            if (cushions.Count >= 2) { Object.Destroy(cushions[0]); cushions.RemoveAt(0); }
            Vector3 at = GroundSpot(aim, 10f);
            var go = Shapes.Box("Whoopee Cushion", at + Vector3.up * 0.04f, new Vector3(0.45f, 0.08f, 0.45f), new Color(0.95f, 0.45f, 0.6f), HeroWorld.Root, false);
            var trap = go.AddComponent<Trap>();
            trap.Team = Kid.Team;
            trap.Radius = 2.2f;
            trap.OnTrigger = victim =>
            {
                victim.Reveal(4f);
                HeroWorld.Play(HeroWorld.Sound.Fart, victim.transform.position, 1f);
                if (RoundManager.Instance.Player.Team == trap.Team) Hud.Toast("Whoopee! " + victim.DisplayName + " stepped on a cushion");
            };
            cushions.Add(go);
            return true;
        }

        // The Big Gag: a can that pops a spring snake. Anyone looking at it laughs.
        public override bool UseUltimate(Vector3 aim)
        {
            Vector3 at = GroundSpot(aim, 20f);
            var can = Shapes.BoxOnGround("Joke Can", at, new Vector3(0.3f, 0.4f, 0.3f), new Color(0.9f, 0.2f, 0.2f), HeroWorld.Root, false);
            var me = Kid;
            Kid.StartCoroutine(Pop(can, at, me));
            return true;
        }

        IEnumerator Pop(GameObject can, Vector3 at, Kid me)
        {
            yield return new WaitForSeconds(0.6f);
            HeroWorld.Play(HeroWorld.Sound.Pop, at);
            HeroWorld.Play(HeroWorld.Sound.Laugh, at);
            var snake = Shapes.Box("Spring Snake", at + Vector3.up * 1.6f, new Vector3(0.25f, 2.2f, 0.25f), new Color(0.2f, 0.8f, 0.3f), HeroWorld.Root, false);
            Object.Destroy(snake, 2f);
            Object.Destroy(can, 2f);
            Vector3 p = at + Vector3.up * 1.2f;
            foreach (var e in Kid.All)
            {
                if (!e.Alive || e.Team == me.Team) continue;
                Vector3 to = p - e.Eye.position;
                if (to.magnitude > 15f || Vector3.Angle(e.AimDirection, to) > 60f) continue;
                if (Physics.Linecast(e.Eye.position, p, Shapes.ShotMask, QueryTriggerInteraction.Ignore)) continue;
                e.Laugh(1.5f);
            }
        }

        // Banana Peel: right-click with the Squirting Flower, once per round.
        public override bool AltFire(Vector3 aim)
        {
            if (peelUsed || Kid.Current.Def != HeroToys.SquirtingFlower) return false;
            peelUsed = true;
            Vector3 flat = new Vector3(aim.x, 0f, aim.z).normalized;
            var peel = Shapes.Box("Banana Peel", Pos + flat * 1.3f + Vector3.up * 0.04f, new Vector3(0.35f, 0.06f, 0.25f), new Color(1f, 0.9f, 0.2f), HeroWorld.Root, false);
            var trap = peel.AddComponent<Trap>();
            trap.Team = Kid.Team;
            trap.Radius = 1f;
            trap.OnTrigger = victim =>
            {
                victim.Slow(0.5f, 2f);
                victim.Disorient(0.6f);
                HeroWorld.Play(HeroWorld.Sound.Swoosh, victim.transform.position);
            };
            if (Kid.IsPlayer) Hud.Toast("Banana peel down");
            return true;
        }

        public override string Status { get { return peelUsed ? null : "Banana peel ready (right-click)"; } }

        public override void BotFight(Kid target, float distance)
        {
            if (Kid.UltReady && distance < 18f && Chance(0.2f)) Kid.TryUseUltimate((target.transform.position - Eye).normalized);
            if (!peelUsed && distance < 6f && Chance(0.1f) && Kid.Current.Def == HeroToys.SquirtingFlower) AltFire(Kid.AimDirection);
        }

        public override void BotIdle()
        {
            if (RoundManager.Instance.Phase == Phase.Play && Kid.AbilityCharges > 0 && Kid.HorizontalSpeed < 0.5f && Chance(0.02f))
                Kid.TryUseAbility(Kid.AimDirection);
        }
    }

    /// Something on the ground that goes off when an enemy walks over it.
    public class Trap : MonoBehaviour
    {
        public Team Team;
        public float Radius = 1.5f;
        public System.Action<Kid> OnTrigger;

        void Update()
        {
            foreach (var k in Kid.All)
            {
                if (!k.Alive || k.Team == Team) continue;
                Vector3 d = k.transform.position - transform.position;
                if (Mathf.Abs(d.y) > 1.5f) continue;
                d.y = 0f;
                if (d.magnitude > Radius) continue;
                if (OnTrigger != null) OnTrigger(k);
                Destroy(gameObject);
                return;
            }
        }
    }

    // =========================================================================
    // NERD (Utility)
    // =========================================================================
    public class NerdKit : HeroKit
    {
        int level;   // Upgrades: 0 = stock, 3 = fully upgraded

        public override ToyDef Sidearm { get { return HeroToys.Homemade[level]; } }
        public override string Status { get { return "Upgrades: " + (level == 0 ? "none yet" : "Mk " + (level + 1)); } }

        public override void OnRoundStart(bool survived)
        {
            level = survived ? Mathf.Min(3, level + 1) : 0;
            // Swap the homemade gun for this round's version (if he's still carrying one).
            if (System.Array.IndexOf(HeroToys.Homemade, Kid.Sidearm.Def) >= 0 && Kid.Sidearm.Def != Sidearm)
                Kid.Sidearm = new ToyState(Sidearm);
            if (survived && level > 0 && Kid.IsPlayer) Hud.Toast("Homemade gun upgraded to Mk " + (level + 1) + "!");
        }

        // Stink Bomb: lob it, and a cloud blooms where it lands.
        public override bool UseAbility(Vector3 aim)
        {
            Vector3 dir = (aim + Vector3.up * 0.15f).normalized;
            var glob = WaterShots.LaunchGlob(Kid, Eye + aim * 0.6f, dir * 18f, 14f, 0f, 60f, 60f, 0f, 0.3f);
            glob.GetComponent<Renderer>().sharedMaterial = Shapes.Mat(new Color(0.55f, 0.7f, 0.2f));
            var team = Kid.Team;
            glob.OnBurst = p => StinkCloud.Make(p, team);
            return true;
        }

        // Toy Wall.
        public override bool UseUltimate(Vector3 aim)
        {
            Vector3 at = GroundSpot(aim, 15f);
            Vector3 flat = new Vector3(aim.x, 0f, aim.z).normalized;
            var wall = new GameObject("Toy Wall");
            wall.transform.SetParent(HeroWorld.Root, false);
            wall.transform.position = at;
            wall.transform.rotation = Quaternion.LookRotation(flat);
            Color[] bricks = { new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.45f, 0.9f), new Color(0.95f, 0.8f, 0.15f), new Color(0.2f, 0.7f, 0.3f) };
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 6; col++)
                {
                    var b = Shapes.Box("Toy Brick", Vector3.zero, new Vector3(1f, 0.65f, 0.6f), bricks[(row + col) % 4], wall.transform);
                    b.transform.localPosition = new Vector3(-2.5f + col + (row % 2) * 0.25f, 0.33f + row * 0.65f, 0f);
                    b.transform.localRotation = Quaternion.identity;
                }
            var soak = wall.AddComponent<Soakable>();
            soak.Team = Kid.Team; soak.Health = 250f;
            soak.OnSoakedOut = () =>
            {
                HeroWorld.Bricks.Add(new HeroWorld.Zone { Center = at, Radius = 3.5f, Until = Time.time + 12f });
                for (int i = 0; i < 14; i++)
                {
                    Vector2 r = Random.insideUnitCircle * 3.2f;
                    var piece = Shapes.Box("Loose Brick", at + new Vector3(r.x, 0.12f, r.y), new Vector3(0.5f, 0.25f, 0.3f), bricks[i % 4], HeroWorld.Root, false);
                    piece.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    Object.Destroy(piece, 12f);
                }
            };
            Physics.SyncTransforms();
            return true;
        }

        public override void BotFight(Kid target, float distance)
        {
            if (distance > 9f && distance < 25f && Chance(0.08f)) Kid.TryUseAbility(Kid.AimDirection);
            if (Kid.UltReady && Kid.Soak > 35f && distance > 8f && Chance(0.25f)) Kid.TryUseUltimate(Kid.AimDirection);
        }
    }

    public class StinkCloud : MonoBehaviour
    {
        public Team Team;
        float nextCough;
        const float Radius = 4.5f;

        public static void Make(Vector3 at, Team team)
        {
            var go = Shapes.Ball("Stink Cloud", at + Vector3.up * 1.2f, Radius * 2f, new Color(0.55f, 0.65f, 0.3f), HeroWorld.Root);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var c = go.AddComponent<StinkCloud>();
            c.Team = team;
            HeroWorld.Clouds.Add(new HeroWorld.Zone { Center = at + Vector3.up * 1.2f, Radius = Radius, Until = Time.time + 8f, Team = team });
            HeroWorld.Play(HeroWorld.Sound.Pop, at);
            Destroy(go, 8f);
        }

        void Update()
        {
            if (Time.time < nextCough) return;
            nextCough = Time.time + 0.5f;
            foreach (var k in Kid.All)
                if (k.Alive && k.Team != Team && Vector3.Distance(k.transform.position + Vector3.up, transform.position) < Radius)
                    k.Reveal(0.7f);   // coughing gives them away
        }
    }

    // =========================================================================
    // THE TWINS (Specialist): Susan (you) and Jenny (the helper)
    // =========================================================================
    public class TwinsKit : HeroKit
    {
        GameObject jenny;
        Vector3 jennyGround;   // where Susan lands for a Twin Switch
        bool switchUsed;

        public override void OnRoundStart(bool survived) { jenny = null; switchUsed = false; }
        public override string Status
        {
            get
            {
                string s = jenny != null ? "Jenny is watching" : "Jenny is inside";
                if (!switchUsed && jenny != null) s += "  (V: Twin Switch)";
                return s;
            }
        }

        // Laser Pointer Squirter: hits mark enemies; a face hit dazzles them.
        public override void OnShotHit(Kid victim, bool head)
        {
            if (Kid.Current.Def != HeroToys.LaserPointer) return;
            victim.Reveal(3f);
            if (head) victim.Blind(1f);
        }

        // Jenny Thompson: send her to the window you're aiming at.
        public override bool UseAbility(Vector3 aim)
        {
            Vector3 spot = AimSpot(aim, 30f, out var hit, out bool any);
            if (!any || Mathf.Abs(hit.normal.y) > 0.5f)
            {
                if (Kid.IsPlayer) Hud.Toast("Aim at a window or a wall");
                return false;
            }
            if (jenny != null) Object.Destroy(jenny);
            Vector3 n = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
            Vector3 at = hit.point + n * 0.3f;
            at.y = Mathf.Clamp(at.y, 1.2f, 4.5f);
            jenny = new GameObject("Jenny");
            jenny.transform.SetParent(HeroWorld.Root, false);
            jenny.transform.position = at;
            jenny.transform.rotation = Quaternion.LookRotation(n);
            var body = Shapes.Box("Jenny Body", at - Vector3.up * 0.25f, new Vector3(0.6f, 0.5f, 0.35f), Def.Color, jenny.transform);
            var head = Shapes.Box("Jenny Head", at + Vector3.up * 0.25f, new Vector3(0.4f, 0.4f, 0.4f), new Color(0.96f, 0.8f, 0.66f), jenny.transform);
            body.GetComponent<Collider>().isTrigger = true;
            head.GetComponent<Collider>().isTrigger = true;
            var soak = jenny.AddComponent<Soakable>();
            soak.Team = Kid.Team; soak.Health = 40f;
            var me = Kid;
            soak.OnSoakedOut = () => { if (me.IsPlayer) Hud.Toast("Jenny got soaked and ducked inside!"); };
            var lookout = jenny.AddComponent<Lookout>();
            lookout.Team = Kid.Team;
            jennyGround = hit.point + n * 1.2f;
            jennyGround.y = Pos.y;
            if (Physics.Raycast(jennyGround + Vector3.up * 2f, Vector3.down, out var down, 10f, Shapes.ShotMask, QueryTriggerInteraction.Ignore))
                jennyGround = down.point;
            return true;
        }

        // Twin Switch: Susan jumps to Jenny's spot, once per round.
        public override bool HeroAction(Vector3 aim)
        {
            if (switchUsed || jenny == null) return false;
            if (!NavGraph.SpotIsFree(jennyGround)) { if (Kid.IsPlayer) Hud.Toast("No room down there"); return false; }
            switchUsed = true;
            Kid.Teleport(jennyGround, Kid.transform.eulerAngles.y);
            HeroWorld.Play(HeroWorld.Sound.Swoosh, jennyGround);
            return true;
        }

        // Water Bucket: Jenny (or Susan, if Jenny's inside) dumps a bucket on one enemy.
        public override bool UseUltimate(Vector3 aim)
        {
            Vector3 from = jenny != null ? jenny.transform.position : Eye;
            float range = jenny != null ? 15f : 9f;
            Kid best = null;
            float bestD = range;
            foreach (var e in Enemies())
            {
                float d = Vector3.Distance(from, Chest(e));
                if (d < bestD && Clear(from, Chest(e))) { best = e; bestD = d; }
            }
            if (best == null) { if (Kid.IsPlayer) Hud.Toast("No enemy close enough for the bucket"); return false; }
            Vector3 top = Chest(best) + Vector3.up * 2.5f;
            var bucket = Shapes.Box("Water Bucket", top, new Vector3(0.6f, 0.6f, 0.6f), new Color(0.3f, 0.5f, 0.9f), HeroWorld.Root, false);
            Object.Destroy(bucket, 0.8f);
            WaterShots.Splash(top, WaterShots.WaterColor, 30, 3f);
            best.TakeSoak(80f, Kid, from);
            if (Kid.IsPlayer) Hud.ShowHitMarker(true, 80f, Chest(best), best.IsOut);
            return true;
        }

        public override void BotFight(Kid target, float distance)
        {
            if (Kid.UltReady && Chance(0.2f)) Kid.TryUseUltimate(Kid.AimDirection);
        }

        public override void BotIdle()
        {
            if (jenny == null && Kid.AbilityCharges > 0 && RoundManager.Instance.Phase == Phase.Play && Chance(0.03f))
            {
                // Look around for a wall to send Jenny to.
                Vector3 dir = Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f) * Kid.transform.forward;
                dir.y = 0.1f;
                Kid.TryUseAbility(dir.normalized);
            }
        }
    }

    /// Jenny in a window: pings enemies near her every 2 s.
    public class Lookout : MonoBehaviour
    {
        public Team Team;
        float next;

        void Update()
        {
            if (Time.time < next) return;
            next = Time.time + 2f;
            foreach (var k in Kid.All)
            {
                if (!k.Alive || k.Team == Team) continue;
                Vector3 chest = k.transform.position + Vector3.up * 1.1f;
                if (Vector3.Distance(chest, transform.position) > 18f) continue;
                if (Physics.Linecast(transform.position + transform.forward * 0.4f, chest, Shapes.ShotMask, QueryTriggerInteraction.Ignore)) continue;
                k.Reveal(1.5f);
            }
        }
    }
}
