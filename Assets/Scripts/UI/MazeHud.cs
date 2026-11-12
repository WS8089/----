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
        private float nextRefresh;

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
            shell.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            game.StateChanged += ShowState;
            damage.DurabilityChanged += ShowDurability;
            ShowDurability(damage.Durability, damage.Maximum);
            ShowState(game.State);
        }

        private void OnDestroy()
        {
            if (game != null) game.StateChanged -= ShowState;
            if (damage != null) damage.DurabilityChanged -= ShowDurability;
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
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.08f;
            Refresh();
        }

        private void Refresh()
        {
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
