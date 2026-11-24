// HeroKit.cs
// ---------------------------------------------------------------------------
// The machinery every hero kit shares. A "kit" is the code behind one hero's
// Passive, Ability [Q], Ultimate [X] and extras ([V] / right-click). The
// twelve kits themselves are in HeroKits.cs; their names and numbers are in
// Heroes.cs.
//
// Also here: the things kits put into the world.
//   Soakable    anything that isn't a kid but can be soaked out (Richy's
//               butler, Mama Logan's fan, Jenny, the Toy Wall).
//   Walker      a helper that jogs to a spot (the butler, Max the dog).
//   HeroWorld   keeps track of zones and clouds (the Over-Protective Parent,
//               Stink Bombs), makes little sounds, and clears everything away
//               at the start of each round.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace WaterWars
{
    public abstract class HeroKit
    {
        public Kid Kid;
        public HeroDef Def { get { return Kid.Hero; } }

        // ---- Things each kit can change (defaults = do nothing special) -------
        public virtual void OnRoundStart(bool survivedLastRound) { }
        public virtual void Tick() { }
        public abstract bool UseAbility(Vector3 aim);
        public abstract bool UseUltimate(Vector3 aim);
        public virtual bool AltFire(Vector3 aim) { return false; }     // right-click with your hero sidearm
        public virtual bool HeroAction(Vector3 aim) { return false; }  // [V]
        public virtual float SpeedFactor { get { return 1f; } }
        public virtual float FireRateFactor { get { return 1f; } }
        public virtual float ReloadFactor { get { return 1f; } }       // above 1 = faster reloads
        public virtual bool CanFire { get { return true; } }
        public virtual float ModifyIncoming(float amount, Kid attacker) { return amount; }
        public virtual bool KeepInWhenSoaked() { return false; }       // the Bully's Tantrum
        public virtual void OnSoakTaken(float amount) { }
        public virtual void OnFired() { }
        public virtual void OnShotHit(Kid victim, bool head) { }
        public virtual int MaxBalloons { get { return 1; } }
        public virtual float BalloonSpeed { get { return 19f; } }
        public virtual bool HotSauce { get { return false; } }
        public virtual ToyDef Sidearm { get { return Def.Sidearm; } }
        public virtual string Status { get { return null; } }          // a short line on your HUD

        // ---- What bots do with this kit --------------------------------------
        public virtual void BotFight(Kid target, float distance) { }
        public virtual void BotIdle() { }

        // ---- Helpers for kits -----------------------------------------------------
        protected Vector3 Pos { get { return Kid.transform.position; } }
        protected Vector3 Eye { get { return Kid.Eye.position; } }
        protected static bool Chance(float p) { return Random.value < p; }

        protected IEnumerable<Kid> Enemies()
        {
            foreach (var k in Kid.All) if (k.Alive && k.Team != Kid.Team) yield return k;
        }

        protected IEnumerable<Kid> Allies(bool includeMe)
        {
            foreach (var k in Kid.All) if (k.Alive && k.Team == Kid.Team && (includeMe || k != Kid)) yield return k;
        }

        protected static Vector3 Chest(Kid k) { return k.transform.position + Vector3.up * 1.1f; }

        /// True when nothing solid is between two points.
        protected static bool Clear(Vector3 a, Vector3 b)
        {
            return !Physics.Linecast(a, b, Shapes.ShotMask, QueryTriggerInteraction.Ignore);
        }

        /// Where your aim meets the world (wall or ground), up to `max` meters away.
        protected Vector3 AimSpot(Vector3 aim, float max, out RaycastHit hit, out bool hitSomething)
        {
            hitSomething = Physics.Raycast(Eye, aim, out hit, max, Shapes.ShotMask, QueryTriggerInteraction.Ignore);
            return hitSomething ? hit.point : Eye + aim * max;
        }

        /// A spot on the ground where you're aiming (or straight below where the aim ends).
        protected Vector3 GroundSpot(Vector3 aim, float max)
        {
            Vector3 p = AimSpot(aim, max, out _, out _);
            p -= aim * 0.4f;  // step back off a wall
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var down, 30f, Shapes.ShotMask, QueryTriggerInteraction.Ignore))
                return down.point;
            p.y = 0f;
            return p;
        }

        /// The enemy closest to where you're aiming (smallest angle), within `range`.
        protected Kid EnemyNearCrosshair(Vector3 aim, float range, float maxAngle, bool needSight)
        {
            Kid best = null;
            float bestAngle = maxAngle;
            foreach (var e in Enemies())
            {
                Vector3 to = Chest(e) - Eye;
                if (to.magnitude > range) continue;
                float a = Vector3.Angle(aim, to);
                if (a >= bestAngle) continue;
                if (needSight && !Clear(Eye, Chest(e))) continue;
                best = e; bestAngle = a;
            }
            return best;
        }

        protected Kid AllyNearCrosshair(Vector3 aim, float range, float maxAngle)
        {
            Kid best = null;
            float bestAngle = maxAngle;
            foreach (var a in Allies(false))
            {
                Vector3 to = Chest(a) - Eye;
                if (to.magnitude > range) continue;
                float ang = Vector3.Angle(aim, to);
                if (ang < bestAngle && Clear(Eye, Chest(a))) { best = a; bestAngle = ang; }
            }
            return best;
        }

        protected float NearestEnemyDistance()
        {
            float best = float.MaxValue;
            foreach (var e in Enemies()) best = Mathf.Min(best, Vector3.Distance(Pos, e.transform.position));
            return best;
        }
    }

    // =========================================================================
    // Things in the world that can be soaked out
    // =========================================================================
    public class Soakable : MonoBehaviour
    {
        public Team Team;
        public float Health = 50f;
        public System.Action OnSoakedOut;
        public static readonly List<Soakable> All = new List<Soakable>();

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void TakeSoak(float amount, Kid from)
        {
            if (Health <= 0f) return;
            if (from != null && from.Team == Team) return;
            Health -= amount;
            WaterShots.Splash(transform.position + Vector3.up * 0.5f, WaterShots.WaterColor, 6, 2f);
            if (from != null && from.IsPlayer) Hud.ShowHitMarker(false, amount, transform.position + Vector3.up, Health <= 0f);
            if (Health <= 0f)
            {
                if (OnSoakedOut != null) OnSoakedOut();
                Destroy(gameObject);
            }
        }

        /// A solid knock (the Bully's shove) takes it out at once.
        public void KnockOver(Kid from) { TakeSoak(9999f, from); }
    }

    /// Jogs toward a moving or fixed target, then calls OnArrive.
    public class Walker : MonoBehaviour
    {
        public System.Func<Vector3> Target;
        public float Speed = 6f;
        public float ArriveDistance = 1.2f;
        public System.Action OnArrive;

        void Update()
        {
            if (Target == null) return;
            Vector3 t = Target();
            Vector3 d = t - transform.position;
            d.y = 0f;
            if (d.magnitude <= ArriveDistance)
            {
                var a = OnArrive;
                Target = null;
                if (a != null) a();
                return;
            }
            Vector3 step = d.normalized * Mathf.Min(d.magnitude, Speed * Time.deltaTime);
            transform.position += step;
            transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }

    // =========================================================================
    // Shared bookkeeping for hero stuff in the world
    // =========================================================================
    public static class HeroWorld
    {
        static Transform root;

        /// Parent for everything kits create. Thrown away at the start of each round.
        public static Transform Root
        {
            get
            {
                if (root == null) root = new GameObject("Hero Stuff").transform;
                return root;
            }
        }

        public class Zone { public Vector3 Center; public float Radius; public float Until; public Team Team; }
        public static readonly List<Zone> ParentZones = new List<Zone>();  // Over-Protective Parent: no abilities
        public static readonly List<Zone> Clouds = new List<Zone>();       // Stink Bombs: block sight
        public static readonly List<Zone> Bricks = new List<Zone>();       // Toy Wall leftovers: slow

        public static void Clear()
        {
            if (root != null) Object.Destroy(root.gameObject);
            root = null;
            ParentZones.Clear();
            Clouds.Clear();
            Bricks.Clear();
        }

        static bool Inside(List<Zone> zones, Vector3 p)
        {
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                if (Time.time > zones[i].Until) { zones.RemoveAt(i); continue; }
                Vector3 d = p - zones[i].Center;
                d.y = 0f;
                if (d.magnitude <= zones[i].Radius) return true;
            }
            return false;
        }

        public static bool AbilitiesBlockedAt(Vector3 p) { return Inside(ParentZones, p); }
        public static bool OnBricks(Vector3 p) { return Inside(Bricks, p); }
        public static bool InCloud(Vector3 p) { return Inside(Clouds, p + Vector3.up * 1f); }

        /// True when a stink cloud is between two points (used for bot eyesight).
        public static bool CloudBlocks(Vector3 a, Vector3 b)
        {
            for (int i = Clouds.Count - 1; i >= 0; i--)
            {
                var c = Clouds[i];
                if (Time.time > c.Until) { Clouds.RemoveAt(i); continue; }
                Vector3 ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(c.Center - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                if (Vector3.Distance(a + ab * t, c.Center) < c.Radius) return true;
            }
            return false;
        }

        // ---- Little effects ---------------------------------------------------------
        /// A flat colored ring on the ground that fades (shows where an effect hit).
        public static void Ring(Vector3 at, float radius, Color c, float life = 0.6f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Effect Ring";
            go.transform.SetParent(Root, false);
            go.transform.position = new Vector3(at.x, at.y + 0.05f, at.z);
            go.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = Shapes.Mat(c);
            Object.Destroy(go, life);
        }

        public enum Sound { Bark, Ring, Fart, Yell, Pop, Swoosh, Laugh }
        static readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();

        /// Play a made-in-code sound at a spot in the world.
        public static void Play(Sound s, Vector3 at, float volume = 1f)
        {
            if (!clips.TryGetValue(s, out var clip) || clip == null)
            {
                switch (s)
                {
                    case Sound.Bark: clip = Hud.MakeTone("Bark", 0.18f, 420f, 220f, 0.35f); break;
                    case Sound.Ring: clip = Hud.MakeTone("Ring", 0.6f, 1100f, 1100f, 0f, 18f); break;
                    case Sound.Fart: clip = Hud.MakeTone("Fart", 0.6f, 110f, 70f, 0.8f); break;
                    case Sound.Yell: clip = Hud.MakeTone("Yell", 0.35f, 320f, 520f, 0.2f); break;
                    case Sound.Pop: clip = Hud.MakeTone("Pop", 0.12f, 900f, 200f); break;
                    case Sound.Swoosh: clip = Hud.MakeTone("Swoosh", 0.2f, 300f, 900f, 0.6f); break;
                    default: clip = Hud.MakeTone("Laugh", 0.5f, 500f, 700f, 0.1f, 9f); break;
                }
                clips[s] = clip;
            }
            AudioSource.PlayClipAtPoint(clip, at, volume);
        }
    }
}
