// GameBootstrap.cs
// ---------------------------------------------------------------------------
// The starting point. When you press Play, this builds the whole game in code:
// the golden-hour sunlight, the grey-box map, the 10 kids, the referee
// (RoundManager) and the on-screen HUD.
//
// Building everything in code (instead of placing objects by hand in the
// editor) keeps the prototype in a few readable files and means nothing can
// get "unhooked" in the editor. As real art arrives, pieces will move into
// normal Unity scenes and prefabs.
//
// The scene "Assets/Scenes/Prototype.unity" holds one object with this script.
// If you press Play in an empty scene instead, it still starts by itself.
// ---------------------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WaterWars
{
    public class GameBootstrap : MonoBehaviour
    {
        static GameBootstrap instance;
        Transform world;
        Transform mapRoot;

        // Safety net: if you press Play in an empty, unsaved scene, start the game anyway.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStartInEmptyScene()
        {
            if (FindAnyObjectByTypeCompat() != null) return;
            if (!string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) return; // a real saved scene: leave it alone
            new GameObject("Water Wars").AddComponent<GameBootstrap>();
        }

        static GameBootstrap FindAnyObjectByTypeCompat()
        {
#if UNITY_2023_1_OR_NEWER
            return FindAnyObjectByType<GameBootstrap>();
#else
            return FindObjectOfType<GameBootstrap>();
#endif
        }

        void Start()
        {
            instance = this;
            Application.targetFrameRate = 144;
            QualitySettings.shadowDistance = 90f;
            StartCoroutine(BuildGame());
        }

        /// Throw everything away and start a new match (used by "Play again").
        public static void Restart()
        {
            if (instance == null) return;
            instance.StartCoroutine(instance.Rebuild());
        }

        /// Throw away the map and build a different one
        /// (used between rounds when Tuning.NewMapEachRound is on).
        public static MapData RegenerateMap()
        {
            var b = instance;
            if (b.mapRoot != null) DestroyImmediate(b.mapRoot.gameObject);
            b.mapRoot = new GameObject("Neighborhood").transform;
            b.mapRoot.SetParent(b.world, false);
            return MapBuilder.Build(b.mapRoot);
        }

        IEnumerator Rebuild()
        {
            if (world != null) Destroy(world.gameObject);
            Kid.All.Clear();
            yield return null; // wait one frame so the old map is really gone
            yield return BuildGame();
        }

        IEnumerator BuildGame()
        {
            world = new GameObject("World").transform;
            SetUpLighting(world);
            mapRoot = new GameObject("Neighborhood").transform;
            mapRoot.SetParent(world, false);
            var map = MapBuilder.Build(mapRoot);

            var rmGo = new GameObject("Round Manager");
            rmGo.transform.SetParent(world, false);
            var rm = rmGo.AddComponent<RoundManager>();
            rm.Setup(map, world);
            rmGo.AddComponent<Hud>();

            // Turn off any other camera in the scene (like the default "Main Camera"), ours is on the player.
            foreach (var cam in FindAllCameras())
                if (cam.transform.root != world && cam.transform.root != rm.Player.transform.root) cam.gameObject.SetActive(false);
            yield break;
        }

        static Camera[] FindAllCameras()
        {
#if UNITY_2023_1_OR_NEWER
            return FindObjectsByType<Camera>(FindObjectsSortMode.None);
#else
            return FindObjectsOfType<Camera>();
#endif
        }

        static void SetUpLighting(Transform world)
        {
            // Turn off any existing sun (e.g. the default "Directional Light") so there's only one.
            foreach (var l in FindAllLights())
                if (l.type == LightType.Directional) l.gameObject.SetActive(false);

            // Golden-hour sun: warm, low in the sky, long soft shadows.
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(world, false);
            sunGo.transform.rotation = Quaternion.Euler(32f, -35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.88f, 0.7f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.62f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.95f, 0.85f, 0.72f);
            RenderSettings.fogStartDistance = 80f;
            RenderSettings.fogEndDistance = 260f;
        }

        static Light[] FindAllLights()
        {
#if UNITY_2023_1_OR_NEWER
            return FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            return FindObjectsOfType<Light>();
#endif
        }
    }
}
