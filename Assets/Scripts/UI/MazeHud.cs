using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BearPi.TiltMaze
{
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MazeHud : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private LevelManager levels;
        [SerializeField] private CameraManager cameras;
        [SerializeField] private KeyboardInputSource bindings;
        [SerializeField] private BallDamageSystem damage;
        [SerializeField] private SkillManager skills;
        [SerializeField] private TerrainController terrain;
        [Header("Menu tilt navigation")]
        [SerializeField, Min(1f)] private float navTiltEnter = 10f;
        [SerializeField, Min(0.5f)] private float navTiltRelease = 4f;
        [SerializeField, Min(0.1f)] private float navRepeatDelay = 0.7f;
        [SerializeField, Min(0.1f)] private float navRepeatInterval = 0.6f;
        [Tooltip("Only flips menu navigation; gameplay tilt is unaffected.")]
        [SerializeField] private bool navInvert = true;

        private VisualElement shell;
        private VisualElement debugCard;
        private VisualElement durabilityFill;
        private Label durability;
        private Label elapsed;
        private Label cameraLabel;
        private Label terrainLabel;
        private Label brakeLabel;
        private Label shieldLabel;
        private Label debugLabel;
        private Button keyboardButton;
        private Button motionButton;
        private Label inputStatus;
        private float nextRefresh;

        private readonly List<(Button button, Action action)> navItems = new();
        private int navIndex;
        private int navDir;
        private float navHold;

        private void Start()
        {
            shell = GetComponent<UIDocument>().rootVisualElement.Q("shell");
            debugCard = shell.Q("debug-card");
            durabilityFill = shell.Q("durability-fill");
            durability = shell.Q<Label>("durability");
            elapsed = shell.Q<Label>("time");
            cameraLabel = shell.Q<Label>("camera");
            terrainLabel = shell.Q<Label>("surface");
            brakeLabel = shell.Q<Label>("brake");
            shieldLabel = shell.Q<Label>("shield");
            debugLabel = shell.Q<Label>("debug");
            keyboardButton = shell.Q<Button>("input-keyboard");
            motionButton = shell.Q<Button>("input-motion");
            inputStatus = shell.Q<Label>("input-status");
            shell.Q<Label>("controls").text =
                $"{bindings.MoveKeys}  Tilt   ·   {bindings.CameraKey}  View   ·   {bindings.CalibrateKey}  Center";
            shell.Q<Label>("menu-controls").text = $"{bindings.MoveKeys}  Tilt   ·   {bindings.CameraKey}  Change view";
            shell.Q<Label>("brake-label").text = $"{bindings.BrakeKey.ToUpperInvariant()}  BRAKE";
            shell.Q<Label>("shield-label").text = $"{bindings.ShieldKey.ToUpperInvariant()}  SHIELD";
            shell.Q<Button>("pause").text = $"Pause  {bindings.PauseKey}";
            shell.Q<Button>("start").clicked += game.StartGame;
            shell.Q<Button>("exit").clicked += game.Quit;
            shell.Q<Button>("pause").clicked += game.Pause;
            shell.Q<Button>("resume").clicked += game.Resume;
            shell.Q<Button>("retry").clicked += Retry;
            shell.Q<Button>("menu-back").clicked += game.ReturnToMenu;
            shell.Q<Button>("debug-toggle").clicked += ToggleDiagnostics;
            keyboardButton.clicked += OnKeyboardClicked;
            motionButton.clicked += OnMotionClicked;
            game.InputModeChanged += OnInputModeChanged;
            shell.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            game.StateChanged += ShowState;
            damage.DurabilityChanged += ShowDurability;
            ShowDurability(damage.Durability, damage.Maximum);
            ShowState(game.State);
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.StateChanged -= ShowState;
                game.InputModeChanged -= OnInputModeChanged;
            }
            if (damage != null) damage.DurabilityChanged -= ShowDurability;
        }

        private void OnKeyboardClicked()
        {
            if (!game.SetInputMode(InputMode.Keyboard) && inputStatus != null)
            {
                inputStatus.text = "Keyboard source missing";
            }
        }

        private void OnMotionClicked()
        {
            if (!game.SetInputMode(InputMode.Motion) && inputStatus != null)
            {
                inputStatus.text = "UDP source missing";
            }
        }

        private void OnInputModeChanged(InputMode mode) => RefreshInputMode();

        private void RefreshInputMode()
        {
            if (keyboardButton == null || motionButton == null || inputStatus == null)
            {
                return;
            }

            bool keyboard = game.CurrentInputMode == InputMode.Keyboard;
            keyboardButton.EnableInClassList("selected", keyboard);
            motionButton.EnableInClassList("selected", !keyboard);

            shell.Q<Label>("controls").text = keyboard
                ? $"{bindings.MoveKeys}  Tilt   ·   {bindings.CameraKey}  View   ·   {bindings.CalibrateKey}  Center"
                : $"Tilt board  ·   {bindings.CameraKey}  View   ·   {bindings.CalibrateKey}  Center";
            shell.Q<Label>("menu-controls").text = keyboard
                ? $"{bindings.MoveKeys}  Tilt   ·   {bindings.CameraKey}  Change view"
                : "Tilt the BearPi board to steer";

            if (keyboard)
            {
                inputStatus.text = $"{bindings.MoveKeys}  Tilt   ·   {bindings.CalibrateKey}  Center";
                return;
            }

            UdpInputSource udp = game.UdpSource;
            if (udp == null)
            {
                inputStatus.text = "UDP source missing";
                return;
            }
            if (!string.IsNullOrEmpty(udp.LastError))
            {
                inputStatus.text = $"UDP error: {udp.LastError}";
                return;
            }

            inputStatus.text = udp.IsConnected
                ? $"Board connected  ·  {udp.Packets} packets"
                : "Waiting for board...";
        }

        /// <summary>
        /// Tilt up/down moves the highlight, F1 confirms, F2 goes back.
        /// Active only outside Playing/Ready so it never fights the maze.
        /// </summary>
        private void HandleMenuNavigation()
        {
            if (game == null)
            {
                return;
            }

            if (game.State == GameState.Playing)
            {
                if ((game.LastInput.Button & MazeButton.Back) != 0)
                {
                    game.Pause();
                }
                return;
            }

            if (game.State == GameState.Ready || navItems.Count == 0)
            {
                return;
            }

            float pitch = game.LastInput.Pitch;
            int direction = 0;
            int sign = navInvert ? -1 : 1;

            if (pitch > navTiltEnter)
            {
                direction = sign;
            }
            else if (pitch < -navTiltEnter)
            {
                direction = -sign;
            }

            if (direction == 0)
            {
                /* 回到中立区才允许下一次方向切换 */
                if (Mathf.Abs(pitch) < navTiltRelease)
                {
                    navDir = 0;
                    navHold = 0f;
                }
            }
            else if (direction != navDir)
            {
                navDir = direction;
                MoveSelection(direction);
                navHold = navRepeatDelay;
            }
            else
            {
                /* 持续同一方向倾斜时，按固定间隔缓慢重复 */
                navHold -= Time.unscaledDeltaTime;
                if (navHold <= 0f)
                {
                    MoveSelection(direction);
                    navHold = navRepeatInterval;
                }
            }

            MazeButton buttons = game.LastInput.Button;
            if ((buttons & MazeButton.Confirm) != 0)
            {
                ActivateSelection();
            }
            if ((buttons & MazeButton.Back) != 0)
            {
                BackOut();
            }
        }

        private void RebuildNavigation(GameState state)
        {
            ClearNavigationHighlight();
            navItems.Clear();
            navIndex = 0;
            navDir = 0;
            navHold = 0f;

            if (state == GameState.MainMenu)
            {
                AddNav("input-keyboard", () => game.SetInputMode(InputMode.Keyboard));
                AddNav("input-motion", () => game.SetInputMode(InputMode.Motion));
                AddNav("start", game.StartGame);
                AddNav("exit", game.Quit);
            }
            else if (state == GameState.Paused)
            {
                AddNav("resume", game.Resume);
                AddNav("retry", Retry);
                AddNav("menu-back", game.ReturnToMenu);
            }
            else if (state is GameState.Completed or GameState.Failed)
            {
                AddNav("retry", Retry);
                AddNav("menu-back", game.ReturnToMenu);
            }

            HighlightNavigation();
        }

        private void AddNav(string buttonName, Action action)
        {
            Button button = shell.Q<Button>(buttonName);
            if (button != null)
            {
                navItems.Add((button, action));
            }
        }

        private void MoveSelection(int direction)
        {
            navIndex = (navIndex + direction + navItems.Count) % navItems.Count;
            HighlightNavigation();
        }

        private void ActivateSelection()
        {
            if (navIndex >= 0 && navIndex < navItems.Count)
            {
                navItems[navIndex].action?.Invoke();
            }
        }

        private void BackOut()
        {
            if (game.State == GameState.MainMenu)
            {
                game.Quit();
            }
            else
            {
                game.ReturnToMenu();
            }
        }

        private void HighlightNavigation()
        {
            for (int i = 0; i < navItems.Count; i++)
            {
                navItems[i].button.EnableInClassList("nav-selected", i == navIndex);
            }
        }

        private void ClearNavigationHighlight()
        {
            foreach (Button button in shell.Query<Button>(className: "nav-selected").ToList())
            {
                button.RemoveFromClassList("nav-selected");
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent change)
        {
            bool narrow = change.newRect.width < 830f;
            shell.EnableInClassList("narrow", narrow);
            shell.Q<Button>("debug-toggle").text = narrow ? "Info" : "Input diagnostics";
        }

        private void ToggleDiagnostics()
        {
            debugCard.ToggleInClassList("hidden");
            shell.Q<Button>("debug-toggle").Blur();
            Refresh();
        }

        private void Retry()
        {
            if (game.State == GameState.Completed) game.NextOrRetry();
            else game.Retry();
        }

        private void ShowState(GameState state)
        {
            bool menu = state == GameState.MainMenu;
            bool result = state is GameState.Paused or GameState.Completed or GameState.Failed;
            string level = $"{levels.Index + 1:00}  /  {levels.Current.Title}";
            Visible("menu", menu);
            Visible("hud", !menu);
            Visible("pause", state == GameState.Playing);
            Visible("ready", state == GameState.Ready);
            Visible("scrim", result);
            shell.Q<Label>("menu-level").text = level;
            shell.Q<Label>("hud-level").text = level;

            if (result)
            {
                shell.Q<Label>("result-level").text = level;
                shell.Q<Label>("result-title").text = state switch
                {
                    GameState.Paused => "Paused",
                    GameState.Completed => "Maze cleared",
                    _ => "Run ended"
                };
                shell.Q<Label>("result-details").text = state switch
                {
                    GameState.Completed => $"{FormatTime(game.Elapsed)}   ·   {damage.Durability:0} durability left",
                    GameState.Failed when game.Failure == FailureReason.FellOffBoard => "The ball fell off the platform.",
                    GameState.Failed => "The ball ran out of durability.",
                    _ => $"{FormatTime(game.Elapsed)}   ·   {damage.Durability:0} durability left"
                };
                bool paused = state == GameState.Paused;
                Visible("resume", paused);
                var retry = shell.Q<Button>("retry");
                retry.EnableInClassList("primary", !paused);
                retry.EnableInClassList("secondary", paused);
                retry.text = paused ? "Restart level" :
                    state == GameState.Completed && levels.HasNext ? "Next level" : "Retry";
            }

            Refresh();

            RebuildNavigation(state);
        }

        private void Visible(string name, bool visible) => shell.Q(name).EnableInClassList("hidden", !visible);

        private void ShowDurability(float value, float maximum)
        {
            durability.text = $"{Mathf.CeilToInt(value)} / {maximum:0}";
            float ratio = value / maximum;
            durabilityFill.style.width = Length.Percent(ratio * 100f);
            durabilityFill.EnableInClassList("low", ratio < 0.28f);
        }

        private void Update()
        {
            HandleMenuNavigation();

            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.08f;
            Refresh();
        }

        private void Refresh()
        {
            RefreshInputMode();
            elapsed.text = FormatTime(game.Elapsed);
            cameraLabel.text = cameras.Mode.ToString();
            terrainLabel.text = terrain.Current switch
            {
                TerrainKind.Ice => "Ice  ·  Low grip",
                TerrainKind.Water => "Water  ·  Slows the ball",
                TerrainKind.Wet => "Wet  ·  Reduced stability",
                _ => "Slate"
            };
            brakeLabel.text = SkillStatus(skills.BrakeRemaining, skills.BrakeRecharge);
            shieldLabel.text = SkillStatus(skills.ShieldRemaining, skills.ShieldRecharge);

            if (debugCard.ClassListContains("hidden")) return;
            var sample = game.LastInput;
            var angles = levels.Current.Tilt.CurrentAngles;
            debugLabel.text = $"{sample.ConnectionStatus}\nRoll  {sample.Roll:+0.0;-0.0;0.0}°  ·  Pitch  {sample.Pitch:+0.0;-0.0;0.0}°" +
                $"\nPlatform X  {angles.y:+0.0;-0.0;0.0}°  /  Z  {-angles.x:+0.0;-0.0;0.0}°";
        }

        private static string SkillStatus(float active, float recharge)
        {
            if (active > 0f) return $"Active {active:0.0}s";
            return recharge > 0f ? $"{recharge:0.0}s" : "Ready";
        }

        private static string FormatTime(float seconds) =>
            $"{(int)seconds / 60:00}:{(int)seconds % 60:00}.{(int)(seconds * 10f) % 10}";
    }
}
