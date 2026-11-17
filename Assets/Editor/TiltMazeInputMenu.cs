using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BearPi.TiltMaze.Editor
{
    /// <summary>
    /// One-click swap between the keyboard adapter and the UDP adapter.
    /// The UDP component is added to the same object as GameManager if missing.
    /// </summary>
    public static class TiltMazeInputMenu
    {
        [MenuItem("Tilt Maze/Use UDP Input")]
        public static void UseUdp()
        {
            GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (game == null)
            {
                Debug.LogError("[TiltMaze] GameManager not found in the open scene.");
                return;
            }

            UdpInputSource udp = game.GetComponent<UdpInputSource>();
            if (udp == null)
            {
                udp = game.gameObject.AddComponent<UdpInputSource>();
            }

            var serialized = new SerializedObject(game);
            SerializedProperty property = serialized.FindProperty("inputSource");
            if (property == null)
            {
                Debug.LogError("[TiltMaze] GameManager.inputSource was not found.");
                return;
            }

            property.objectReferenceValue = udp;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Selection.activeObject = udp;
            EditorGUIUtility.PingObject(udp);
            Debug.Log("[TiltMaze] Input Source -> UdpInputSource. Save the scene to keep it.");
        }

        [MenuItem("Tilt Maze/Use Keyboard Input")]
        public static void UseKeyboard()
        {
            GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (game == null)
            {
                Debug.LogError("[TiltMaze] GameManager not found in the open scene.");
                return;
            }

            KeyboardInputSource keyboard = game.GetComponent<KeyboardInputSource>();
            if (keyboard == null)
            {
                Debug.LogError("[TiltMaze] KeyboardInputSource not found on the GameManager object.");
                return;
            }

            var serialized = new SerializedObject(game);
            SerializedProperty property = serialized.FindProperty("inputSource");
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = keyboard;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            Debug.Log("[TiltMaze] Input Source -> KeyboardInputSource.");
        }
    }
}
