// WaterShots.cs
// ---------------------------------------------------------------------------
// What happens when water leaves a toy.
//
//   * Instant shots (Squirt Pistol): a straight invisible line is traced from
//     your eye. The first thing it touches gets wet, right away. We draw a
//     quick streak so you can see it.
//   * Arcing shots (Pump-Action Soaker, Water Balloon): a blob of water flies
//     forward and falls with gravity, frame by frame. It soaks whatever it
//     touches first. Balloons burst and soak everyone nearby.
//
// Hits to the head count double, legs count 0.75 (see HitZone in Kid.cs).
// Water from your own crew just splashes off them: no friendly fire.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace WaterWars
{
    public static class WaterShots
    {
        public static readonly Color WaterColor = new Color(0.55f, 0.85f, 1f);
        static readonly RaycastHit[] hitBuffer = new RaycastHit[32];

        /// Turn a direction by a small random amount (shot spread).
        public static Vector3 Wobble(Vector3 dir, float degrees)
        {
            if (degrees <= 0f) return dir.normalized;
            Vector2 r = Random.insideUnitCircle * degrees;
            Quaternion q = Quaternion.LookRotation(dir);
            return (q * Quaternion.Euler(r.y, r.x, 0f)) * Vector3.forward;
        }

        /// What a water trace touched first.
        public struct Contact
        {
            public bool Hit;          // touched anything at all
            public Vector3 Point;
            public HitZone Zone;      // non-null if it touched a kid's body part
        }

        /// Trace a line and find the first thing that counts, skipping the shooter's own body
        /// and anyone already Soaked.
        public static Contact Trace(Vector3 from, Vector3 dir, float distance, Kid shooter)
        {
            var result = new Contact();
            int count = Physics.RaycastNonAlloc(from, dir, hitBuffer, distance, Shapes.ShotMask, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = hitBuffer[i];
                if (h.distance >= best) continue;
                var zone = h.collider.GetComponent<HitZone>();
                if (zone != null)
                {
                    if (zone.Owner == shooter || zone.Owner.IsOut) continue;
                }
                else if (h.collider.isTrigger) continue;
                best = h.distance;
                result.Hit = true;
                result.Point = h.point;
                result.Zone = zone;
            }
            return result;
        }

        /// Wet a kid if they're on the other crew. Returns true if soak was applied.
        public static bool Soak(Kid shooter, HitZone zone, float baseSoak, float traveled, float range, float falloffStart)
        {
            var victim = zone.Owner;
            if (victim.IsOut || (shooter != null && victim.Team == shooter.Team)) return false;
            float amount = baseSoak * zone.Multiplier;
            if (traveled > falloffStart) amount *= 0.6f; // far shots lose punch
            victim.TakeSoak(amount, shooter, shooter != null ? shooter.transform.position : zone.transform.position);
            if (shooter != null && shooter.IsPlayer)
                Hud.ShowHitMarker(zone.Zone == HitZoneType.Head, amount, zone.transform.position, victim.IsOut);
            // A big splash on whoever got hit, so everyone can see the hit land.
            Splash(zone.transform.position, WaterColor, 10, 2.5f);
            return true;
        }

        // ---- Instant toys --------------------------------------------------
        public static void Instant(Kid shooter, Vector3 origin, Vector3 dir, ToyDef toy)
        {
            var c = Trace(origin, dir, toy.Range, shooter);
            Vector3 end = c.Hit ? c.Point : origin + dir * toy.Range;
            if (c.Zone != null) Soak(shooter, c.Zone, toy.SoakPerHit, Vector3.Distance(origin, end), toy.Range, toy.FalloffStart);
            if (c.Hit) Splash(end, WaterColor, 5, 2f);

            // Draw a short-lived streak. For you, start it a bit to the right and below
            // the eye, where your toy is, so it doesn't cover the crosshair.
            Vector3 start = origin + dir * 0.5f;
            if (shooter != null && shooter.IsPlayer)
                start += shooter.Eye.right * 0.18f - shooter.Eye.up * 0.15f;
            Streak.Make(start, end);
        }

        // ---- Arcing water and balloons -------------------------------------
        public static WaterGlob LaunchGlob(Kid shooter, Vector3 position, Vector3 velocity, float gravity, float soakPerHit,
            float range, float falloffStart, float splashRadius, float size)
        {
            var go = Shapes.Ball("Water", position, size, splashRadius > 0f ? new Color(0.35f, 0.6f, 1f) : WaterColor, null);
            var glob = go.AddComponent<WaterGlob>();
            glob.Shooter = shooter;
            glob.Velocity = velocity;
            glob.Gravity = gravity;
            glob.SoakPerHit = soakPerHit;
            glob.Range = range;
            glob.FalloffStart = falloffStart;
            glob.SplashRadius = splashRadius;
            return glob;
        }

        /// Soak every enemy within `radius` of a point (balloon bursts). Less soak at the edge.
        public static void AreaSoak(Kid shooter, Vector3 center, float radius, float soakCenter, float soakEdge)
        {
            var hitKids = new HashSet<Kid>();
            foreach (var k in Kid.All)
            {
                if (k.IsOut || hitKids.Contains(k)) continue;
                if (shooter != null && k.Team == shooter.Team) continue;
                Vector3 chest = k.transform.position + Vector3.up * 1f;
                float d = Vector3.Distance(center, chest);
                if (d > radius) continue;
                // Walls protect you from a burst on the other side.
                if (Physics.Linecast(center + Vector3.up * 0.2f, chest, Shapes.ShotMask, QueryTriggerInteraction.Ignore)) continue;
                hitKids.Add(k);
                float amount = Mathf.Lerp(soakCenter, soakEdge, d / radius);
                k.TakeSoak(amount, shooter, center);
                if (shooter != null && shooter.IsPlayer) Hud.ShowHitMarker(false, amount, chest, k.IsOut);
            }
        }

        /// A burst of droplets (pure decoration).
        public static void Splash(Vector3 at, Color color, int drops, float power)
        {
            for (int i = 0; i < drops; i++)
            {
                var d = Shapes.Ball("Drop", at, Random.Range(0.06f, 0.12f), color, null);
                var drop = d.AddComponent<Droplet>();
                drop.Velocity = (Random.insideUnitSphere + Vector3.up * 0.8f) * power;
            }
        }
    }

    /// A flying blob of water (or a water balloon). Moves and falls each frame.
    public class WaterGlob : MonoBehaviour
    {
        public Kid Shooter;
        public Vector3 Velocity;
        public float Gravity;
        public float SoakPerHit;
        public float Range;
        public float FalloffStart;
        public float SplashRadius; // > 0 means it's a balloon that bursts in an area

        float traveled;
        float age;

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            Vector3 step = Velocity * dt;
            Velocity += Vector3.down * Gravity * dt;
            float len = step.magnitude;
            if (len > 0.0001f)
            {
                var c = WaterShots.Trace(transform.position, step / len, len, Shooter);
                if (c.Hit)
                {
                    Burst(c.Point, c.Zone);
                    return;
                }
            }
            transform.position += step;
            traveled += len;
            if (traveled > Range || age > 4f || transform.position.y < -5f) Destroy(gameObject);
        }

        void Burst(Vector3 at, HitZone zone)
        {
            if (SplashRadius > 0f)
            {
                WaterShots.AreaSoak(Shooter, at, SplashRadius, 45f, 20f);
                WaterShots.Splash(at, WaterShots.WaterColor, 18, 4f);
            }
            else
            {
                if (zone != null) WaterShots.Soak(Shooter, zone, SoakPerHit, traveled, Range, FalloffStart);
                WaterShots.Splash(at, WaterShots.WaterColor, 4, 1.8f);
            }
            Destroy(gameObject);
        }
    }

    /// A single decorative droplet that flies, falls and shrinks away.
    public class Droplet : MonoBehaviour
    {
        public Vector3 Velocity;
        float life = 0.5f;
        Vector3 startScale;

        void Start() { startScale = transform.localScale; }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }
            Velocity += Vector3.down * 12f * Time.deltaTime;
            transform.position += Velocity * Time.deltaTime;
            transform.localScale = startScale * (life / 0.5f);
        }
    }

    /// The quick streak an instant toy draws.
    public class Streak : MonoBehaviour
    {
        float life = 0.07f;

        public static void Make(Vector3 from, Vector3 to)
        {
            var go = new GameObject("Streak");
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = 0.05f;
            line.endWidth = 0.09f;
            line.sharedMaterial = Shapes.Mat(WaterShots.WaterColor);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<Streak>();
        }

        void Update()
        {
            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
