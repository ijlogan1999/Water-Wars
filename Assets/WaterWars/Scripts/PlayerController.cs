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
//   X            hero ultimate        F            throw a water balloon
//   V            hero action (the Bully's shove, the Twins' switch)
//   Right click  scope in, or your hero sidearm's extra (refill, nozzle...)
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
            Cam.fieldOfView = Tuning.FieldOfView;
            Cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
            if (RenderSettings.skybox == null)
            {
                // No sky in this scene: paint a plain summer-sky color behind everything.
                Cam.clearFlags = CameraClearFlags.SolidColor;
                Cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            }

            // The water toy you see in your hands (rebuilt whenever you switch toys).
            viewToy = new GameObject("View Toy").transform;
            viewToy.SetParent(camGo.transform, false);

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
            // Right-click scopes in with sniper toys. Aim slows down while zoomed.
            var def = kid.Current.Def;
            kid.Zoomed = def.ZoomFov > 0f && WWInput.AimHeld() && !kid.Reloading;
            float wantFov = kid.Zoomed ? def.ZoomFov : Tuning.FieldOfView;
            Cam.fieldOfView = Mathf.MoveTowards(Cam.fieldOfView, wantFov, Time.deltaTime * 400f);
            Vector2 look = WWInput.MouseDelta() * MouseSensitivity * (Cam.fieldOfView / Tuning.FieldOfView);
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, -88f, 88f);
            // Disoriented (barked at, shoved): your view sways on its own.
            if (kid.Disoriented)
            {
                yaw += Mathf.Sin(Time.time * 5.3f) * 40f * Time.deltaTime;
                pitch = Mathf.Clamp(pitch + Mathf.Sin(Time.time * 3.7f) * 25f * Time.deltaTime, -88f, 88f);
            }
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
            if (WWInput.Pressed(WWKey.X)) kid.TryUseUltimate(kid.AimDirection);
            if (WWInput.Pressed(WWKey.F)) kid.ThrowBalloon(kid.AimDirection);
            if (WWInput.Pressed(WWKey.V)) kid.TryHeroAction(kid.AimDirection);
            if (def.ZoomFov <= 0f && WWInput.AimPressed()) kid.TryAltFire(kid.AimDirection);

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
            var def = kid.Current.Def;
            if (def != shownToy) { BuildViewModel(def); shownToy = def; }
            viewToy.gameObject.SetActive(kid.Alive && !kid.Zoomed);
            kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime * 8f);
            float reloadDip = kid.Reloading ? Mathf.Sin(kid.ReloadProgress * Mathf.PI) * 0.12f : 0f;
            float bob = Mathf.Sin(Time.time * 9f) * 0.008f * Mathf.Clamp01(kid.HorizontalSpeed / Tuning.RunSpeed);
            viewToy.localPosition = new Vector3(0.2f, -0.2f - reloadDip + bob, 0.4f - kick * 0.06f);
            viewToy.localRotation = Quaternion.Euler(-kick * 6f + reloadDip * 200f, 0f, 0f);
        }

        ToyDef shownToy;

        /// A grey-box stand-in for the toy: a grip, a body, a barrel, and maybe a tank.
        /// Ian's Blender models will replace these.
        void BuildViewModel(ToyDef def)
        {
            for (int i = viewToy.childCount - 1; i >= 0; i--) Destroy(viewToy.GetChild(i).gameObject);
            MakeOneToy(def, Vector3.zero);
            if (def.ViewTwin) MakeOneToy(def, new Vector3(-0.4f, 0f, 0f)); // the second one, in your left hand
        }

        void MakeOneToy(ToyDef def, Vector3 offset)
        {
            var holder = new GameObject(def.Name).transform;
            holder.SetParent(viewToy, false);
            holder.localPosition = offset;
            float len = def.ViewLength;
            Part(holder, "Grip", new Vector3(0f, -0.07f, -0.02f), new Vector3(0.05f, 0.12f, 0.06f), def.ViewColor * 0.7f);
            Part(holder, "Body", new Vector3(0f, 0f, len * 0.25f), new Vector3(0.08f, 0.08f, len * 0.6f), def.ViewColor);
            Part(holder, "Barrel", new Vector3(0f, 0.01f, len * 0.65f), new Vector3(0.035f, 0.035f, len * 0.4f), def.ViewColor * 0.8f);
            if (def.ViewTank)
                Part(holder, "Tank", new Vector3(0f, 0.08f, len * 0.2f), new Vector3(0.1f, 0.09f, len * 0.4f), new Color(0.6f, 0.85f, 1f));
            if (def.ZoomFov > 0f)
                Part(holder, "Scope", new Vector3(0f, 0.07f, len * 0.3f), new Vector3(0.04f, 0.04f, 0.16f), new Color(0.15f, 0.15f, 0.15f));
        }

        static void Part(Transform parent, string name, Vector3 pos, Vector3 size, Color c)
        {
            c.a = 1f;
            var p = Shapes.Box(name, Vector3.zero, size, c, parent, false);
            p.transform.localPosition = pos;
            p.transform.localRotation = Quaternion.identity;
            p.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ---- Watching a teammate while you sit on the curb ---------------------
        void Spectate()
        {
            var rm = RoundManager.Instance;
            spectating = true;
            Cam.fieldOfView = Tuning.FieldOfView;
            var mates = rm.Kids.FindAll(k => k.Team == kid.Team && k.Alive && k != kid);
            if (WWInput.FirePressed()) spectateIndex++;
            if (mates.Count == 0)
            {
                // Nobody left: watch from high above the middle of the map.
                Spectating = null;
                Cam.transform.SetParent(transform.parent, false);
                Cam.transform.position = rm.Map.Center + new Vector3(0f, 60f, -40f);
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
