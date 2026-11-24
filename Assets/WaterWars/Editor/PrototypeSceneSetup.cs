// PrototypeSceneSetup.cs  (Editor only: this never ships in the game)
// ---------------------------------------------------------------------------
// The first time you open the project, this makes the scene
// "Assets/Scenes/Prototype.unity", puts the GameBootstrap object in it,
// opens it, and adds it to the build list. After that, you just press Play.
//
// It also adds a "Water Wars" menu at the top of the Unity editor:
//   Water Wars > Open Prototype Scene
//   Water Wars > Rebuild Prototype Scene   (if you ever break or delete it)
// ---------------------------------------------------------------------------

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WaterWars.EditorTools
{
    [InitializeOnLoad]
    public static class PrototypeSceneSetup
    {
        const string ScenePath = "Assets/Scenes/Prototype.unity";

        static PrototypeSceneSetup()
        {
            // Wait until Unity has finished loading before touching scenes.
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath)) CreateScene(true);
            };
        }

        [MenuItem("Water Wars/Open Prototype Scene")]
        static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { CreateScene(true); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Water Wars/Rebuild Prototype Scene")]
        static void RebuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateScene(false);
        }

        static void CreateScene(bool firstTime)
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject("Water Wars (press Play)").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log(firstTime
                ? "Water Wars: created and opened " + ScenePath + ". Press the Play button (top middle) to play!"
                : "Water Wars: rebuilt " + ScenePath + ".");
        }
    }
}
