using System;
using UnityEngine;

namespace BearPi.TiltMaze
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField] private LevelManager levels;
        [SerializeField] private CameraManager cameras;
        [SerializeField] private SkillManager skills;
        [SerializeField] private BallDamageSystem damage;
        [SerializeField] private TerrainController terrain;
        [SerializeField] private GameFeedback feedback;
        [SerializeField] private KeyboardInputSource keyboardInput;
        [SerializeField] private UdpInputSource udpInput;
        [SerializeField, Min(0f)] private float readyDuration = 0.8f;

        private IMazeInputSource input;
        private float readyRemaining;

        public event Action<GameState> StateChanged;
        public event Action<InputMode> InputModeChanged;
        public GameState State { get; private set; }
        public FailureReason Failure { get; private set; }
        public float Elapsed { get; private set; }
        public float ReadyRemaining => readyRemaining;
        public MazeInput LastInput { get; private set; }
        public InputMode CurrentInputMode { get; private set; }
        public UdpInputSource UdpSource => udpInput;

        private void Start()
        {
            if (keyboardInput == null) keyboardInput = GetComponent<KeyboardInputSource>();
            if (udpInput == null) udpInput = GetComponent<UdpInputSource>();

            input = (IMazeInputSource)inputSource;
            if (input == null) input = keyboardInput;
            CurrentInputMode = input is UdpInputSource ? InputMode.Motion : InputMode.Keyboard;
            if (udpInput != null) udpInput.Active = CurrentInputMode == InputMode.Motion;
            Debug.Log($"[TiltMaze] input mode -> {CurrentInputMode}");

            levels.GoalReached += Complete;
            damage.Depleted += OnDepleted;
            levels.Select(0);
            SetState(GameState.MainMenu);
        }

        /// <summary>
        /// Swaps the active input adapter. Returns false when the requested
        /// adapter is not present on the GameManager object.
        /// </summary>
        public bool SetInputMode(InputMode mode)
        {
            if (mode == InputMode.Motion)
            {
                if (udpInput == null) udpInput = GetComponent<UdpInputSource>();
                if (udpInput == null) return false;
                inputSource = udpInput;
                input = udpInput;
            }
            else
            {
                if (keyboardInput == null) keyboardInput = GetComponent<KeyboardInputSource>();
                if (keyboardInput == null) return false;
                inputSource = keyboardInput;
                input = keyboardInput;
            }

            CurrentInputMode = mode;
            if (udpInput != null) udpInput.Active = mode == InputMode.Motion;
            Debug.Log($"[TiltMaze] input mode -> {mode}");
            InputModeChanged?.Invoke(mode);
            return true;
        }

        private void OnDestroy()
        {
            if (levels != null) levels.GoalReached -= Complete;
            if (damage != null) damage.Depleted -= OnDepleted;
            if (State == GameState.Paused) Time.timeScale = 1f;
        }

        private void Update()
        {
            LastInput = input.Read();
            if ((LastInput.Button & MazeButton.Pause) != 0)
            {
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
                return;
            }

            if (State == GameState.Ready)
            {
                readyRemaining -= Time.deltaTime;
                if (readyRemaining <= 0f)
                {
                    levels.Ball.SetActivePhysics(true);
                    levels.Current.Tilt.AcceptingTilt = true;
                    SetState(GameState.Playing);
                }
                return;
            }

            if (State != GameState.Playing) return;

            Elapsed += Time.deltaTime;
            skills.Handle(LastInput.Gesture);
            var tilt = levels.Current.Tilt;
            tilt.SetAttitude(LastInput);
            if ((LastInput.Button & MazeButton.Calibrate) != 0) tilt.Calibrate(LastInput);
            if ((LastInput.Button & MazeButton.Camera) != 0) cameras.Cycle();

            if (levels.Ball.Body.position.y < levels.Current.FallY)
                Fail(FailureReason.FellOffBoard);
        }

        public void StartGame() => Begin(0);
        public void Retry() => Begin(levels.Index);

        public void NextOrRetry()
        {
            if (State == GameState.Completed)
                Begin(levels.HasNext ? levels.Index + 1 : levels.Index);
        }

        public void Pause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void ReturnToMenu()
        {
            levels.Select(0);
            skills.ResetSkills();
            damage.ResetDurability();
            terrain.ResetTerrain();
            cameras.ResetView();
            SetState(GameState.MainMenu);
        }

        public void Quit() => Application.Quit();

        private void Begin(int index)
        {
            levels.Select(index);
            damage.ResetDurability();
            skills.ResetSkills();
            terrain.ResetTerrain();
            cameras.ResetView();
            Elapsed = 0f;
            Failure = FailureReason.None;
            readyRemaining = readyDuration;
            SetState(GameState.Ready);
        }

        private void OnDepleted() => Fail(FailureReason.DurabilityDepleted);

        private void Fail(FailureReason reason)
        {
            if (State != GameState.Playing) return;
            Failure = reason;
            StopRun(GameState.Failed);
        }

        private void Complete()
        {
            if (State == GameState.Playing) StopRun(GameState.Completed);
        }

        private void StopRun(GameState outcome)
        {
            levels.Ball.SetActivePhysics(false);
            levels.Current.Tilt.AcceptingTilt = false;
            skills.StopSkills();
            SetState(outcome);
            feedback.Publish(outcome == GameState.Completed ? FeedbackKind.Completed : FeedbackKind.Failed,
                levels.Ball.Body.position);
        }

        private void SetState(GameState next)
        {
            State = next;
            cameras.MenuFraming = next == GameState.MainMenu;
            Time.timeScale = next == GameState.Paused ? 0f : 1f;
            StateChanged?.Invoke(next);
        }
    }
}
