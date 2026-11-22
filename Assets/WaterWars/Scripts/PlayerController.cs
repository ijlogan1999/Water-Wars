// PlayerController.cs
// ---------------------------------------------------------------------------
// Turns your mouse and keyboard into actions for your Kid, and sets up the
// first-person camera and the toy you see in your hands.
//
// Controls:
//   Mouse        look around          Left click   squirt
//   W A S D      move                 R            reload
//   Space        jump                 1 / 2        Soaker / Squirt Pistol
//   Ctrl or C    crouch (steadier)    Q            hero ability
//   Shift        walk (steadier)      E (hold)     plant / disarm the Big One
//   G            drop the Big One     B            buy menu (allowance phase)
//   Enter        skip the allowance phase          Tab   scoreboard
//   Esc          free the mouse / settings
//
// When you're Soaked, the camera follows a teammate who's still in (click to
// switch who you watch).
// ---------------------------------------------------------------------------

using UnityEngine;

namespace WaterWars
{
    public class PlayerController : MonoBehaviour
    {
        public static float MouseSensitivity = Tuning.DefaultMouseSensitivity;

        public Camera Cam { get; private set; }
        public bool BuyMenuOpen;
        public bool CursorFree;   // Esc menu
        public Kid Spectating { get; private set; }

        Kid kid;
        float yaw, pitch;
        Transform viewToy;
        float kick;
        int spectateIndex;
        int seenTeleports;
        bool spectating;

        void Start()
        {
            kid = GetComponent<Kid>();

            var camGo = new GameObject("Player Camera");
            camGo.transform.SetParent(kid.Eye, false);
            Cam = camGo.AddComponent<Camera>();
            Cam.fieldOfView = 85f;
            Cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
            if (RenderSettings.skybox == null)
            {
                // No sky in this scene: paint a plain summer-sky color behind everything.
                Cam.clearFlags = CameraClearFlags.SolidColor;
                Cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            }

            // The water toy you see in your hands.
            viewToy = Shapes.Box("View Toy", Vector3.zero, new Vector3(0.1f, 0.12f, 0.4f), ToyDef.SquirtPistol.ViewColor, camGo.transform, false).transform;
            viewToy.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var tank = Shapes.Box("Tank", Vector3.zero, new Vector3(0.14f, 0.14f, 0.18f), new Color(0.6f, 0.85f, 1f), viewToy, false);
            tank.transform.localPosition = new Vector3(0f, 0.9f, -0.1f);
            tank.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            kid.OnFired += () => kick = 1f;
            yaw = transform.eulerAngles.y;
        }

        void Update()
        {
            var rm = RoundManager.Instance;
            if (rm == null) return;

            // Keep our look angles in sync when the round manager teleports us to a spawn.
            if (kid.TeleportCount != seenTeleports) { seenTeleports = kid.TeleportCount; yaw = transform.eulerAngles.y; pitch = 0f; }

            // ---- Menus and the mouse cursor ---------------------------------
            if (WWInput.Pressed(WWKey.Escape)) { CursorFree = !CursorFree; BuyMenuOpen = false; }
            if (WWInput.Pressed(WWKey.B) && rm.Phase == Phase.Buy && kid.Alive) BuyMenuOpen = !BuyMenuOpen;
            if (rm.Phase != Phase.Buy) BuyMenuOpen = false;
            if (WWInput.Pressed(WWKey.Enter)) rm.SkipBuyPhase();

            bool wantCursor = CursorFree || BuyMenuOpen || rm.Phase == Phase.HeroSelect || rm.Phase == Phase.MatchOver;
            Cursor.lockState = wantCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = wantCursor;

            UpdateViewToy();

            if (!kid.Alive) { Spectate(); return; }
            if (spectating) StopSpectating();
            if (wantCursor) { kid.WantsInteract = false; kid.Move(Vector3.zero, 0f, false); return; }

            // ---- Looking ----------------------------------------------------
            Vector2 look = WWInput.MouseDelta() * MouseSensitivity;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -88f, 88f);
            kid.SetLook(yaw, pitch);

            // ---- Interact (plant / disarm): hold E. You can't move while doing it.
            kid.WantsInteract = WWInput.Held(WWKey.E);
            bool busy = rm.IsInteracting(kid);

            // ---- Moving -------------------------------------------------------
            kid.SetCrouch(WWInput.Held(WWKey.LeftCtrl) || WWInput.Held(WWKey.C));
            Vector3 wish = Vector3.zero;
            if (!rm.MovementLocked && !busy)
            {
                if (WWInput.Held(WWKey.W)) wish += transform.forward;
                if (WWInput.Held(WWKey.S)) wish -= transform.forward;
                if (WWInput.Held(WWKey.D)) wish += transform.right;
                if (WWInput.Held(WWKey.A)) wish -= transform.right;
            }
            float speed = kid.Crouching ? Tuning.CrouchSpeed : WWInput.Held(WWKey.LeftShift) ? Tuning.WalkSpeed : Tuning.RunSpeed;
            kid.Move(wish, speed, !rm.MovementLocked && !busy && WWInput.Pressed(WWKey.Space));

            // ---- Toys -----------------------------------------------------------
            if (WWInput.Pressed(WWKey.Alpha1)) kid.SwitchTo(true);
            if (WWInput.Pressed(WWKey.Alpha2)) kid.SwitchTo(false);
            if (WWInput.Pressed(WWKey.R)) kid.StartReload();
            if (!busy && (WWInput.FireHeld() || WWInput.FirePressed())) kid.TryFire(kid.AimDirection, WWInput.FirePressed());
            if (WWInput.Pressed(WWKey.Q)) kid.TryUseAbility(kid.AimDirection);

            // ---- Drop the Big One for a teammate -------------------------------
            if (WWInput.Pressed(WWKey.G) && rm.Big.Carrier == kid && rm.Phase == Phase.Play)
            {
                rm.Big.Drop(transform.position + transform.forward * 1.8f);
                rm.AddFeed("You dropped the Big One");
            }

            // ---- Settings: [ and ] change mouse sensitivity ---------------------
            if (WWInput.Pressed(WWKey.LeftBracket)) MouseSensitivity = Mathf.Max(0.2f, MouseSensitivity - 0.2f);
            if (WWInput.Pressed(WWKey.RightBracket)) MouseSensitivity += 0.2f;
        }

        void UpdateViewToy()
        {
            if (viewToy == null) return;
            viewToy.gameObject.SetActive(kid.Alive);
            bool big = kid.UsingPrimary && kid.Primary != null;
            viewToy.GetComponent<Renderer>().sharedMaterial = Shapes.Mat(kid.Current.Def.ViewColor);
            viewToy.localScale = big ? new Vector3(0.12f, 0.14f, 0.6f) : new Vector3(0.08f, 0.1f, 0.3f);
            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 8f);
            float reloadDip = kid.Reloading ? Mathf.Sin(kid.ReloadProgress * Mathf.PI) * 0.12f : 0f;
            float bob = Mathf.Sin(Time.time * 9f) * 0.008f * Mathf.Clamp01(kid.HorizontalSpeed / Tuning.RunSpeed);
            viewToy.localPosition = new Vector3(0.22f, -0.2f - reloadDip + bob, 0.45f - kick * 0.06f);
            viewToy.localRotation = Quaternion.Euler(-kick * 6f + reloadDip * 200f, 0f, 0f);
        }

        // ---- Watching a teammate while you sit on the curb ---------------------
        void Spectate()
        {
            var rm = RoundManager.Instance;
            spectating = true;
            var mates = rm.Kids.FindAll(k => k.Team == kid.Team && k.Alive && k != kid);
            if (WWInput.FirePressed()) spectateIndex++;
            if (mates.Count == 0)
            {
                // Nobody left: watch from high above the middle of the map.
                Spectating = null;
                Cam.transform.SetParent(transform.parent, false);
                Cam.transform.position = new Vector3(0f, 60f, -40f);
                Cam.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
                return;
            }
            var target = mates[spectateIndex % mates.Count];
            if (target != Spectating)
            {
                Spectating = target;
                Cam.transform.SetParent(target.Eye, false);
                Cam.transform.localPosition = new Vector3(0f, 0.6f, -2.8f); // a little behind their head
                Cam.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            }
        }

        void StopSpectating()
        {
            spectating = false;
            Spectating = null;
            Cam.transform.SetParent(kid.Eye, false);
            Cam.transform.localPosition = Vector3.zero;
            Cam.transform.localRotation = Quaternion.identity;
        }
    }
}
