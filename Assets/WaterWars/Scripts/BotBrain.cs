// BotBrain.cs
// ---------------------------------------------------------------------------
// Simple computer players so you can play a full 5v5 round alone. They are
// not smart, just good enough to test the game:
//
//   Attackers: all head for one site together (picked at random each round).
//     The carrier plants the Big One when they get there. If the Big One is
//     dropped, the closest attacker goes and grabs it. After the plant they
//     hang around it to protect it.
//   Defenders: split up between sites A and B and wait there. If a defender
//     gets soaked near a site, the others rotate to help. When the Big One is
//     planted, they all go to it and try to disarm it.
//   Everyone: when they see an enemy, they stop, turn, wait a moment
//     (reaction time) and squirt, with a bit of wobble in their aim.
//
// Bot difficulty lives in Tuning.cs (reaction time, aim error, sight range).
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace WaterWars
{
    public class BotBrain : MonoBehaviour
    {
        Kid kid;
        Kid target;
        float targetSeenAt;
        float reaction;
        float nextLookAround;
        float nextThink;
        float yaw, pitch;

        List<Vector3> path = new List<Vector3>();
        int pathIndex;
        Vector3 goal;
        bool hasGoal;
        float repathAt;

        Vector3 lastPos;
        float stuckTimer;
        float jumpAt;

        Site mySite;          // defenders: which site I guard
        Vector3 holdSpot;     // where I like to stand
        Vector3 aimNoise;
        float crouchUntil;

        void Awake() { kid = GetComponent<Kid>(); }

        // ---- Start of each round: shopping and picking a job -------------------
        public void OnRoundStart()
        {
            var rm = RoundManager.Instance;
            target = null;
            path.Clear();
            hasGoal = false;
            yaw = transform.eulerAngles.y;
            pitch = 0f;
            nextThink = Time.time + Random.Range(0f, 0.4f);

            // Shopping: rifle first, then a towel, then ability charges.
            rm.Buy(kid, ShopItem.PumpSoaker);
            if (!rm.Buy(kid, ShopItem.BeachTowel)) rm.Buy(kid, ShopItem.HandTowel);
            while (rm.Buy(kid, ShopItem.Ability)) { }

            if (kid.Team == rm.Defenders)
            {
                // Split up: the first few go to A, the rest to B.
                int myIndex = rm.Kids.FindAll(k => k.Team == kid.Team).IndexOf(kid);
                mySite = myIndex % 5 < 3 ? rm.Map.SiteA : rm.Map.SiteB;
                holdSpot = mySite.HoldSpots[Random.Range(0, mySite.HoldSpots.Count)];
            }
            else
            {
                mySite = rm.AttackPlan;
                holdSpot = mySite.AttackSpots[Random.Range(0, mySite.AttackSpots.Count)];
            }
        }

        void Update()
        {
            var rm = RoundManager.Instance;
            if (rm == null || !kid.Alive) return;
            kid.WantsInteract = false;
            if (rm.Phase != Phase.Play)
            {
                kid.Move(Vector3.zero, 0f, false);
                kid.SetLook(yaw, pitch);
                return;
            }

            if (Time.time >= nextThink)
            {
                nextThink = Time.time + 0.15f;
                LookForEnemies();
            }

            bool fighting = target != null && target.Alive;
            if (fighting) Fight();
            else
            {
                target = null;
                kid.SetCrouch(false);
                DoJob(rm);
            }

            if (kid.Current.Ammo == 0 || (!fighting && kid.Current.Ammo < kid.Current.Def.TankShots / 3)) kid.StartReload();
            kid.SetLook(yaw, pitch);
        }

        // ---- Seeing ------------------------------------------------------------
        void LookForEnemies()
        {
            Kid best = null;
            float bestDist = Tuning.BotSightRange;
            bool justHit = Time.time - kid.LastHitTime < 1.5f;
            foreach (var other in Kid.All)
            {
                if (!other.Alive || other.Team == kid.Team) continue;
                Vector3 to = other.transform.position - transform.position;
                float dist = to.magnitude;
                if (dist > bestDist) continue;
                // Bots see in front of them, unless something just hit them.
                if (!justHit && Vector3.Angle(transform.forward, to) > 75f) continue;
                if (!CanSee(other)) continue;
                best = other;
                bestDist = dist;
            }
            if (best != null && best != target)
            {
                target = best;
                targetSeenAt = Time.time;
                reaction = Random.Range(Tuning.BotReactionMin, Tuning.BotReactionMax);
                aimNoise = Random.insideUnitSphere;
            }
            else if (best == null && target != null && !CanSee(target))
            {
                target = null;
            }
        }

        bool CanSee(Kid other)
        {
            Vector3 eye = kid.Eye.position;
            foreach (float h in new[] { 1.6f, 1.0f })
            {
                Vector3 point = other.transform.position + Vector3.up * (other.Crouching ? h * 0.65f : h);
                Vector3 dir = point - eye;
                var c = WaterShots.Trace(eye, dir.normalized, dir.magnitude + 0.5f, kid);
                if (c.Zone != null && c.Zone.Owner == other) return true;
            }
            return false;
        }

        // ---- Fighting ----------------------------------------------------------
        void Fight()
        {
            // Stand still (sometimes crouch) and squirt.
            if (Time.time > crouchUntil && Random.value < 0.005f) crouchUntil = Time.time + 1.5f;
            kid.SetCrouch(Time.time < crouchUntil);
            kid.Move(Vector3.zero, 0f, false);
            if (kid.Primary != null && !kid.UsingPrimary) kid.SwitchTo(true);

            Vector3 eye = kid.Eye.position;
            Vector3 aimPoint = target.transform.position + Vector3.up * (target.Crouching ? 0.8f : 1.15f);
            float dist = Vector3.Distance(eye, aimPoint);

            // Aim error: shrinks the longer the bot has been tracking you.
            float tracking = Mathf.Clamp01((Time.time - targetSeenAt) / 2f);
            float errDeg = Tuning.BotAimErrorDegrees * Mathf.Lerp(1.6f, 0.6f, tracking);
            aimPoint += aimNoise * Mathf.Tan(errDeg * Mathf.Deg2Rad) * dist;
            if (Random.value < 0.05f) aimNoise = Random.insideUnitSphere;

            Vector3 dir = (aimPoint - eye).normalized;
            var toy = kid.Current.Def;
            if (toy.Arcing) dir = LobAim(eye, aimPoint, toy.WaterSpeed, toy.WaterGravity);

            // Turn toward the aim direction smoothly.
            Quaternion want = Quaternion.LookRotation(dir);
            Vector3 e = want.eulerAngles;
            float wantPitch = e.x > 180f ? e.x - 360f : e.x;
            yaw = Mathf.MoveTowardsAngle(yaw, e.y, 360f * Time.deltaTime);
            pitch = Mathf.MoveTowards(pitch, wantPitch, 360f * Time.deltaTime);
            kid.SetLook(yaw, pitch);

            bool reacted = Time.time - targetSeenAt >= reaction;
            bool onTarget = Vector3.Angle(kid.AimDirection, dir) < 6f;
            if (reacted && onTarget && kid.Current.Ammo > 0)
                kid.TryFire(kid.AimDirection, Random.value < 0.35f); // the pistol needs clicks, so "click" now and then
        }

        /// For arcing toys: how high to aim so the water lands on the target.
        static Vector3 LobAim(Vector3 from, Vector3 to, float speed, float gravity)
        {
            Vector3 d = to - from;
            Vector3 flat = new Vector3(d.x, 0f, d.z);
            float x = flat.magnitude;
            float y = d.y;
            float v2 = speed * speed;
            float root = v2 * v2 - gravity * (gravity * x * x + 2f * y * v2);
            if (root < 0f || x < 0.01f) return d.normalized;
            float angle = Mathf.Atan((v2 - Mathf.Sqrt(root)) / (gravity * x));
            return (flat.normalized * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)).normalized;
        }

        // ---- Doing their job when no enemy is in sight --------------------------
        void DoJob(RoundManager rm)
        {
            var big = rm.Big;
            Vector3 dest;
            bool interactWhenThere = false;

            if (kid.Team == rm.Attackers)
            {
                if (big.Carrier == kid)
                {
                    dest = mySite.Center;
                    if (mySite.Contains(transform.position) && Vector3.Distance(Flat(transform.position), Flat(mySite.Center)) < mySite.Radius * 0.6f)
                    {
                        kid.WantsInteract = true;      // plant!
                        StandAndLookAround();
                        return;
                    }
                }
                else if (big.State == BigOneState.Dropped && IAmClosestAttackerTo(big.Position, rm))
                    dest = big.Position;
                else if (big.State == BigOneState.Planted)
                    dest = big.Position + (holdSpot - mySite.Center).normalized * 5f;
                else
                    dest = holdSpot;
            }
            else
            {
                if (big.State == BigOneState.Planted)
                {
                    dest = big.Position;
                    interactWhenThere = true;
                }
                else if (rm.HotSite != null && rm.HotSite != mySite)
                {
                    mySite = rm.HotSite;
                    holdSpot = mySite.HoldSpots[Random.Range(0, mySite.HoldSpots.Count)];
                    dest = holdSpot;
                }
                else dest = holdSpot;
            }

            float arrive = interactWhenThere ? Tuning.DisarmReach * 0.6f : 1.2f;
            Vector3 toDest = dest - transform.position;
            if (new Vector2(toDest.x, toDest.z).magnitude <= arrive && Mathf.Abs(toDest.y) < 2f)
            {
                kid.WantsInteract = interactWhenThere;
                StandAndLookAround();
                return;
            }
            WalkTo(dest);
        }

        bool IAmClosestAttackerTo(Vector3 p, RoundManager rm)
        {
            float mine = Vector3.Distance(transform.position, p);
            foreach (var k in rm.Kids)
            {
                if (k == kid || !k.Alive || k.Team != kid.Team || k.IsPlayer) continue;
                if (Vector3.Distance(k.transform.position, p) < mine) return false;
            }
            return true;
        }

        void StandAndLookAround()
        {
            kid.Move(Vector3.zero, 0f, false);
            if (Time.time >= nextLookAround)
            {
                nextLookAround = Time.time + Random.Range(1.5f, 3.5f);
                // Mostly watch the direction enemies come from.
                var rm = RoundManager.Instance;
                float baseYaw = kid.Team == rm.Defenders ? 180f : 0f;
                lookYawTarget = baseYaw + Random.Range(-70f, 70f);
            }
            yaw = Mathf.MoveTowardsAngle(yaw, lookYawTarget, 120f * Time.deltaTime);
            pitch = Mathf.MoveTowards(pitch, 0f, 60f * Time.deltaTime);
        }
        float lookYawTarget;

        // ---- Walking along stepping stones --------------------------------------
        void WalkTo(Vector3 dest)
        {
            var nav = RoundManager.Instance.Map.Nav;
            if (!hasGoal || (dest - goal).sqrMagnitude > 4f || Time.time > repathAt || pathIndex >= path.Count)
            {
                goal = dest;
                hasGoal = true;
                path = nav.FindPath(transform.position, dest);
                pathIndex = 0;
                repathAt = Time.time + 4f;
            }
            if (path.Count == 0) return;

            Vector3 next = path[pathIndex];
            Vector3 to = next - transform.position;
            Vector2 flat = new Vector2(to.x, to.z);
            if (flat.magnitude < 0.9f && Mathf.Abs(to.y) < 1.6f)
            {
                pathIndex++;
                if (pathIndex >= path.Count) return;
                next = path[pathIndex];
                to = next - transform.position;
            }
            to.y = 0f;
            Vector3 dir = to.normalized;

            // Unstick: if we've barely moved for a while, hop and re-plan.
            if (Time.time > jumpAt)
            {
                if ((transform.position - lastPos).magnitude < 0.3f) stuckTimer += 0.5f;
                else stuckTimer = 0f;
                lastPos = transform.position;
                jumpAt = Time.time + 0.5f;
            }
            bool jump = false;
            if (stuckTimer >= 1f)
            {
                jump = true;
                dir = Quaternion.Euler(0f, Random.Range(-90f, 90f), 0f) * dir;
                repathAt = 0f;
                stuckTimer = 0f;
            }

            kid.Move(dir, Tuning.BotSpeed, jump);
            if (dir.sqrMagnitude > 0.01f)
            {
                yaw = Mathf.MoveTowardsAngle(yaw, Quaternion.LookRotation(dir).eulerAngles.y, 300f * Time.deltaTime);
                pitch = Mathf.MoveTowards(pitch, 0f, 90f * Time.deltaTime);
            }
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }
    }
}
