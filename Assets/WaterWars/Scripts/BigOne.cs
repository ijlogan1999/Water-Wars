// BigOne.cs
// ---------------------------------------------------------------------------
// The Big One: a giant, overfilled water balloon. One attacker carries it on
// their back. If that kid gets Soaked, it drops on the ground and any attacker
// can grab it by walking over it (or press G to drop it for a teammate).
//
// Planting (attackers): stand inside site A or B and hold E for 4 seconds.
// Then a 40-second fuse starts and the balloon slowly swells up.
// Disarming (defenders): stand next to it and hold E for 7 seconds. Once you
// pass the halfway checkpoint (3.5 s), letting go doesn't lose that progress.
//
// The RoundManager decides who is holding E and calls the methods here.
// ---------------------------------------------------------------------------

using UnityEngine;

namespace WaterWars
{
    public enum BigOneState { Carried, Dropped, Planted, Burst, Disarmed }

    public class BigOne : MonoBehaviour
    {
        public BigOneState State { get; private set; }
        public Kid Carrier { get; private set; }
        public Site PlantedAt { get; private set; }
        public float FuseLeft { get; private set; }
        public float DisarmProgress;   // seconds of disarming done
        public Vector3 Position { get { return Carrier != null ? Carrier.transform.position : transform.position; } }

        Transform balloon;
        Transform launcher;
        float wobble;

        public static BigOne Create(Transform parent)
        {
            var go = new GameObject("The Big One");
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<BigOne>();
            b.balloon = Shapes.Ball("Balloon", Vector3.zero, 1f, new Color(0.2f, 0.45f, 1f), go.transform).transform;
            // The "launcher" stand you hook it onto when planting.
            b.launcher = Shapes.Box("Launcher", Vector3.zero, new Vector3(1.2f, 0.5f, 1.2f), new Color(0.85f, 0.85f, 0.2f), go.transform, false).transform;
            b.launcher.gameObject.SetActive(false);
            return b;
        }

        public void GiveTo(Kid kid)
        {
            State = BigOneState.Carried;
            Carrier = kid;
            PlantedAt = null;
            DisarmProgress = 0f;
            launcher.gameObject.SetActive(false);
            balloon.gameObject.SetActive(true);
            transform.SetParent(kid.BackSlot, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            balloon.localPosition = Vector3.zero;
            balloon.localScale = Vector3.one * 0.55f;
            // Don't block your own view with the balloon on your back.
            var r = balloon.GetComponent<Renderer>();
            r.shadowCastingMode = kid.IsPlayer ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
        }

        public void Drop(Vector3 at)
        {
            State = BigOneState.Dropped;
            Carrier = null;
            transform.SetParent(null, true);
            // Drop it on the ground under the kid.
            RaycastHit hit;
            if (Physics.Raycast(at + Vector3.up * 1.5f, Vector3.down, out hit, 10f, Shapes.ShotMask, QueryTriggerInteraction.Ignore))
                at = hit.point;
            transform.position = at;
            transform.rotation = Quaternion.identity;
            balloon.localPosition = Vector3.up * 0.4f;
            balloon.localScale = Vector3.one * 0.8f;
            balloon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        public void Plant(Site site, Vector3 at)
        {
            Drop(at);
            State = BigOneState.Planted;
            PlantedAt = site;
            FuseLeft = Tuning.FuseSeconds;
            DisarmProgress = 0f;
            launcher.gameObject.SetActive(true);
            launcher.localPosition = Vector3.up * 0.25f;
            balloon.localPosition = Vector3.up * 1.1f;
        }

        public void MarkBurst() { State = BigOneState.Burst; balloon.gameObject.SetActive(false); }
        public void MarkDisarmed() { State = BigOneState.Disarmed; balloon.localScale = Vector3.one * 0.6f; }

        /// Counts the fuse down. Returns true the moment it bursts.
        public bool TickFuse(float dt)
        {
            if (State != BigOneState.Planted) return false;
            FuseLeft -= dt;
            // Swell up and wobble faster as the fuse runs out.
            float t = 1f - Mathf.Clamp01(FuseLeft / Tuning.FuseSeconds);
            wobble += dt * Mathf.Lerp(2f, 14f, t);
            float s = Mathf.Lerp(1.2f, 2.4f, t) * (1f + Mathf.Sin(wobble) * 0.04f);
            balloon.localScale = new Vector3(s, s * 1.1f, s);
            balloon.localPosition = Vector3.up * (0.5f + s * 0.55f);
            return FuseLeft <= 0f;
        }
    }
}
