using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BearPi.TiltMaze.Editor
{
    internal static class DuelSmoke
    {
        private const string Session = "TiltMaze.DuelSmoke";
        private static double deadline;
        private static double pausedAt;
        private static GameManager game;
        private static LevelManager levels;
        private static MockInputSource mock;
        private static BallController ball;
        private static KeyboardInputSource keys;
        private static float began;
        private static float pausedTime;
        private static Vector3 origin;
        private static int step;
        private static int corner;
        private static readonly Vector2[] Route =
        {
            new(-7f, -1.75f), new(-3.1f, -1.35f), new(0.65f, -1.35f),
            new(3.5f, -1.75f), new(3.5f, 1.75f), new(7f, 1.75f), new(7f, 5.25f)
        };

        public static void Run()
        {
            EditorSceneManager.OpenScene(DuelSceneSetup.ScenePath);
            var manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("inputSource").objectReferenceValue = manager.GetComponent<MockInputSource>();
            serialized.FindProperty("roundDuration").floatValue = 4f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetBool(Session, true);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Session, false)) return;
            deadline = EditorApplication.timeSinceStartup + 85;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                Check(EditorApplication.timeSinceStartup < deadline, $"Duel timed out at step {step}.");
                if (!EditorApplication.isPlaying || Time.time < 0.15f) return;
                if (step == 0)
                {
                    game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
                    levels = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
                    ball = levels.Ball;
                    mock = game.GetComponent<MockInputSource>();
                    keys = game.GetComponent<KeyboardInputSource>();
                    Check(game.DuelEnabled && game.State == GameState.MainMenu, "Duel scene configuration failed.");
                    Check(!game.SetInputMode(InputMode.Motion), "Duel accepted the single-player motion adapter.");
                    game.StartGame();
                    step = 1;
                }
                else if (step == 1 && game.State == GameState.Playing)
                {
                    began = Time.time;
                    origin = ball.Body.position;
                    mock.SetMovement(Vector2.up);
                    step = 2;
                }
                else if (step == 2 && Time.time - began > 0.9f)
                {
                    Check(ball.Body.position.z > origin.z + 0.45f, "Ball drive did not produce physical movement.");
                    Check(levels.Current.Tilt.CurrentAngles.magnitude < 0.1f, "Ball input leaked into maze tilt.");
                    mock.SetMovement(Vector2.zero);
                    mock.SetAttitude(14f, 0f);
                    began = Time.time;
                    step = 3;
                }
                else if (step == 3 && Time.time - began > 0.7f)
                {
                    Check(levels.Current.Tilt.CurrentAngles.x > 10f, "Maze side could not tilt the platform.");
                    pausedTime = game.Elapsed;
                    game.Pause();
                    pausedAt = EditorApplication.timeSinceStartup;
                    step = 4;
                }
                else if (step == 4 && EditorApplication.timeSinceStartup - pausedAt > 0.4)
                {
                    Check(Mathf.Abs(game.Elapsed - pausedTime) < 0.03f, "Countdown continued during pause.");
                    mock.SetAttitude(0f, 0f);
                    game.Resume();
                    step = 5;
                }
                else if (step == 5 && game.State == GameState.Failed)
                {
                    Check(game.Failure == FailureReason.TimeExpired && game.TimeRemaining == 0f,
                        "Maze did not win at the deadline.");
                    game.SwapRolesAndRetry();
                    Check(keys.RolesSwapped && game.Elapsed == 0f && game.State == GameState.Ready,
                        "Swap and retry failed.");
                    step = 6;
                }
                else if (step == 6 && game.State == GameState.Playing)
                {
                    ball.Body.position = Vector3.down * 6f;
                    step = 7;
                }
                else if (step == 7 && game.State == GameState.Failed)
                {
                    Check(game.Failure == FailureReason.FellOffBoard, "Falling did not award the maze a win.");
                    game.ReturnToMenu();
                    var serialized = new SerializedObject(game);
                    serialized.FindProperty("roundDuration").floatValue = 90f;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    game.StartGame();
                    step = 8;
                }
                else if (step == 8 && game.State == GameState.Playing)
                {
                    began = Time.time;
                    step = 9;
                }
                else if (step == 9)
                {
                    if (game.State == GameState.Completed)
                    {
                        Check(corner == Route.Length - 1, "Full drive route skipped checkpoints.");
                        float elapsed = game.Elapsed;
                        game.ReturnToMenu();
                        game.SetDuelEnabled(false);
                        Check(!game.DuelEnabled && !keys.RolesSwapped, "Solo mode did not reset keyboard roles.");
                        Finish(0, $"DUEL SMOKE PASS: independent ball/maze controls, pause, timeout, " +
                            $"role swap, fall and full Rigidbody route in {elapsed:F1}s.");
                        return;
                    }
                    Check(game.State == GameState.Playing, $"Drive route failed with {game.Failure}.");
                    Check(Time.time - began < 65f, $"Drive route stalled at {corner}, ball {ball.Body.position}.");
                    DriveRoute();
                }
            }
            catch (Exception error) { Finish(1, error.ToString()); }
        }

        private static void DriveRoute()
        {
            var tilt = levels.Current.Tilt;
            Quaternion inverse = Quaternion.Inverse(tilt.Body.rotation);
            Vector3 local = inverse * (ball.Body.position - tilt.Body.position);
            var position = new Vector2(local.x, local.z);
            if (corner < Route.Length - 1 && Vector2.Distance(position, Route[corner]) < 0.5f) corner++;
            Vector3 speed = inverse * ball.Body.linearVelocity;
            Vector2 command = (Route[corner] - position) * 1.4f - new Vector2(speed.x, speed.z) * 0.9f;
            mock.SetMovement(command);
            // Both adapters affect physics in this route. The maze adds a small opposing tilt.
            mock.SetAttitude(-Mathf.Sign(command.x) * 4f, -Mathf.Sign(command.y) * 4f);
            if (corner == 2 && ball.GetComponent<SkillManager>().ShieldRecharge <= 0f)
                mock.Emit(Gesture.Shield);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish(int code, string message)
        {
            SessionState.EraseBool(Session);
            EditorApplication.update -= Tick;
            if (code == 0) Debug.Log(message);
            else Debug.LogError(message);
            EditorApplication.Exit(code);
        }
    }
}
