using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BearPi.TiltMaze.Editor
{
    public static class DuelSceneSetup
    {
        public const string ScenePath = "Assets/Scenes/TiltMazeDuel.unity";

        [MenuItem("Tilt Maze/Create Keyboard Duel Scene")]
        public static void Create()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/TiltMaze.unity");
            var game = Object.FindAnyObjectByType<GameManager>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var drive = ball.GetComponent<BallDriveController>();
            if (drive == null) drive = ball.gameObject.AddComponent<BallDriveController>();
            var serialized = new SerializedObject(game);
            serialized.FindProperty("duelEnabled").boolValue = true;
            serialized.FindProperty("roundDuration").floatValue = 90f;
            serialized.FindProperty("ballDrive").objectReferenceValue = drive;
            serialized.FindProperty("inputSource").objectReferenceValue = game.GetComponent<KeyboardInputSource>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/TiltMaze.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("Keyboard duel scene saved with 90-second rounds.");
        }
    }
}
