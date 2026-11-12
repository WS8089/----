using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BearPi.TiltMaze.Editor
{
    internal static class TiltMazeSmoke
    {
        private const string Running = "TiltMaze.Smoke.Running";
        private static double deadline;
        private static float began;
        private static Vector3 origin;
        private static MockInputSource mock;
        private static MazeTiltController tilt;
        private static BallController ball;
        private static int step;
        private static GameManager game;
        private static CameraManager cameras;
        private static LevelManager levels;
        private static SkillManager skills;
        private static BallDamageSystem damage;
        private static TerrainController terrain;
        private static MechanismActuator trap;
        private static Rigidbody gateBody;
        private static Vector3 initialGate;
        private static float baseline;
        private static int corner;
        private static readonly Vector2[] Route =
        {
            new(-7f, -1.75f),
            new(-3.1f, -1.35f),
            new(0.65f, -1.35f),
            new(3.5f, -1.75f),
            new(3.5f, 1.75f),
            new(7f, 1.75f),
            new(7f, 5.25f)
        };

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TiltMaze.unity");
            var game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            var serialized = new SerializedObject(game);
            serialized.FindProperty("inputSource").objectReferenceValue = game.GetComponent<MockInputSource>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetBool(Running, true);
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void OnReload()
        {
            if (!SessionState.GetBool(Running, false)) return;
            deadline = EditorApplication.timeSinceStartup + 70;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                Check(EditorApplication.timeSinceStartup < deadline, "Timed out entering or running Play Mode.");
                if (!EditorApplication.isPlaying) return;
                if (step == 0 && Time.time > 0.15f)
                {
                    mock = UnityEngine.Object.FindAnyObjectByType<MockInputSource>();
                    tilt = UnityEngine.Object.FindAnyObjectByType<MazeTiltController>();
                    ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
                    game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
                    levels = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
                    cameras = UnityEngine.Object.FindAnyObjectByType<CameraManager>();
                    skills = ball.GetComponent<SkillManager>();
                    damage = ball.GetComponent<BallDamageSystem>();
                    terrain = ball.GetComponent<TerrainController>();
                    foreach (var actuator in UnityEngine.Object.FindObjectsByType<MechanismActuator>())
                    {
                        if (actuator.TrapDamage > 0f) trap = actuator;
                        else gateBody = actuator.GetComponent<Rigidbody>();
                    }
                    initialGate = gateBody.position;
                    Check(game.State == GameState.MainMenu, "Scene did not open at the main menu.");
                    game.StartGame();
                    Check(game.State == GameState.Ready, "Start did not enter Ready.");
                    step = -1;
                }
                else if (step == -1 && game.State == GameState.Playing)
                {
                    Check(!ball.Body.isKinematic, "Ball should be simulated during play.");
                    origin = ball.Body.position;
                    mock.SetAttitude(0f, 14f);
                    began = Time.time;
                    step = 1;
                }
                else if (step == 1 && Time.time - began > 2.3f)
                {
                    Check(tilt.CurrentAngles.y > 10f && tilt.CurrentAngles.magnitude <= 13.01f,
                        "Pitch failed to track its bounded target.");
                    Check(ball.Body.position.z > origin.z + 0.65f, $"W pitch did not roll ball north. Start {origin}, current {ball.Body.position}, velocity {ball.Body.linearVelocity}, tilt {tilt.CurrentAngles}.");
                    Check(ball.Body.position.z < origin.z + 4.375f, "Ball crossed the corridor wall.");
                    mock.SetAttitude(14f, 14f);
                    began = Time.time;
                    step = 2;
                }
                else if (step == 2 && Time.time - began > 1.0f)
                {
                    Check(tilt.CurrentAngles.x > 8f && tilt.CurrentAngles.magnitude <= 13.01f,
                        "Diagonal tilt exceeded its limit or failed to roll.");
                    mock.Emit(MazeButton.Calibrate);
                    began = Time.time;
                    step = -2;
                }
                else if (step == -2 && Time.time - began > 1.1f)
                {
                    Check(tilt.CurrentAngles.magnitude < 0.25f, "Calibration did not neutralize held input.");
                    ball.Body.position = Vector3.down * 5f;
                    mock.SetAttitude(0f, 0f);
                    step = 3;
                }
                else if (step == 3 && game.State == GameState.Failed)
                {
                    Check(game.Failure == FailureReason.FellOffBoard, "Fall reason was incorrect.");
                    game.Retry();
                    Check(Vector3.Distance(ball.Body.position, origin) < 0.25f, "Retry did not reset at spawn.");
                    step = 4;
                }
                else if (step == 4 && game.State == GameState.Playing)
                {
                    mock.Emit(MazeButton.Pause);
                    step = 5;
                }
                else if (step == 5 && game.State == GameState.Paused)
                {
                    Check(Time.timeScale == 0f, "Pause did not stop simulation.");
                    mock.Emit(MazeButton.Pause);
                    step = 6;
                }
                else if (step == 6 && game.State == GameState.Playing)
                {
                    mock.Emit(MazeButton.Camera);
                    step = 7;
                }
                else if (step == 7 && cameras.Mode == CameraMode.Follow)
                {
                    mock.Emit(MazeButton.Camera);
                    step = 8;
                }
                else if (step == 8 && cameras.Mode == CameraMode.Close)
                {
                    mock.Emit(Gesture.Shield | Gesture.Brake);
                    ball.Body.linearVelocity = Vector3.forward * 4f;
                    began = Time.time;
                    step = 9;
                }
                else if (step == 9 && Time.time - began > 0.24f)
                {
                    Check(skills.IsShielded && skills.BrakeRemaining > 0f && skills.BrakeRecharge > 3f,
                        "Skill timers did not start.");
                    Check(Mathf.Abs(ball.Body.linearVelocity.z) < 1.5f, "Brake did not slow the ball.");
                    baseline = damage.Durability;
                    ApproachTrap();
                    began = Time.time;
                    step = 10;
                }
                else if (step == 10 && Time.time - began > 0.3f)
                {
                    Check(damage.Durability == baseline, "Shield did not block trap damage.");
                    ball.Body.position = origin + Vector3.up * 0.12f;
                    ball.Body.linearVelocity = Vector3.zero;
                    began = Time.time;
                    step = 11;
                }
                else if (step == 11 && Time.time - began > 2.1f)
                {
                    Check(!skills.IsShielded && skills.ShieldRecharge > 0f,
                        "Shield did not expire into cooldown.");
                    Check(Vector3.Distance(gateBody.position, initialGate) > 1f,
                        "Automatic gate failed to close.");
                    ApproachTrap();
                    began = Time.time;
                    step = 12;
                }
                else if (step == 12 && Time.time - began > 0.35f)
                {
                    Check(damage.Durability < baseline, "Trap failed to deduct durability.");
                    ball.Body.position = new Vector3(5.25f, 0.29f, 1.75f);
                    ball.Body.linearVelocity = Vector3.right * 2.5f;
                    began = Time.time;
                    step = 13;
                }
                else if (step == 13 && Time.time - began > 0.17f)
                {
                    Check(terrain.Current == TerrainKind.Water &&
                        ball.Body.linearVelocity.x < 1.95f, "Water did not add drag.");
                    ball.Body.position = levels.Current.Goal.transform.position + Vector3.up * 0.32f;
                    step = 14;
                }
                else if (step == 14 && game.State == GameState.Completed)
                {
                    game.NextOrRetry();
                    Check(game.State == GameState.Ready && cameras.Mode == CameraMode.Overview,
                        "Result action did not restart the sole level.");
                    game.ReturnToMenu();
                    Check(game.State == GameState.MainMenu && Time.timeScale == 1f,
                        "Return to menu did not restore initial flow.");
                    game.StartGame();
                    step = 15;
                }
                else if (step == 15 && game.State == GameState.Playing)
                {
                    began = Time.time;
                    step = 16;
                }
                else if (step == 16)
                {
                    if (game.State == GameState.Completed)
                    {
                        Check(corner == Route.Length - 1, "Full route skipped a waypoint.");
                        Finish(0, $"TILT MAZE SMOKE PASS: full physics route in {game.Elapsed:F1}s, " +
                            $"durability {damage.Durability:F0}, plus states, cameras, terrain, trap and skills.");
                        return;
                    }

                    Check(game.State == GameState.Playing, $"Full route failed: {game.Failure}.");
                    Check(Time.time - began < 52f,
                        $"Full route stalled at waypoint {corner}, ball {ball.Body.position}.");
                    DriveRoute();
                }
            }
            catch (Exception exception)
            {
                Finish(1, exception.ToString());
            }
        }

        private static void DriveRoute()
        {
            Quaternion inverse = Quaternion.Inverse(tilt.Body.rotation);
            Vector3 local = inverse * (ball.Body.position - tilt.Body.position);
            Vector3 velocity = inverse * ball.Body.linearVelocity;
            var position = new Vector2(local.x, local.z);
            if (corner < Route.Length - 1 && Vector2.Distance(position, Route[corner]) < 0.51f)
                corner++;

            Vector2 error = Route[corner] - position;
            Vector2 command = error * 12f - new Vector2(velocity.x, velocity.z) * 7f;
            mock.SetAttitude(Mathf.Clamp(command.x, -14f, 14f), Mathf.Clamp(command.y, -14f, 14f));

            if (corner == 2 && local.x > -3.4f && local.x < -1.1f && skills.ShieldRecharge <= 0f)
                mock.Emit(Gesture.Shield);
        }

        private static void ApproachTrap()
        {
            var center = trap.GetComponent<Rigidbody>().position;
            ball.Body.position = new Vector3(center.x - 0.62f, 0.29f, center.z);
            ball.Body.linearVelocity = Vector3.right * 3.15f;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish(int code, string message)
        {
            SessionState.EraseBool(Running);
            EditorApplication.update -= Tick;
            if (code == 0) Debug.Log(message);
            else Debug.LogError(message);
            EditorApplication.Exit(code);
        }
    }
}
