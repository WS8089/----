using System;
using UnityEngine;

namespace BearPi.TiltMaze
{
    public sealed class KeyboardInputSource : MonoBehaviour, IMazeInputSource
    {
        [Serializable]
        private sealed class Bindings
        {
            public KeyCode forward = KeyCode.W;
            public KeyCode backward = KeyCode.S;
            public KeyCode left = KeyCode.A;
            public KeyCode right = KeyCode.D;
            public KeyCode mazeForward = KeyCode.UpArrow;
            public KeyCode mazeBackward = KeyCode.DownArrow;
            public KeyCode mazeLeft = KeyCode.LeftArrow;
            public KeyCode mazeRight = KeyCode.RightArrow;
            public KeyCode brake = KeyCode.Space;
            public KeyCode shield = KeyCode.Q;
            public KeyCode camera = KeyCode.V;
            public KeyCode calibrate = KeyCode.R;
            public KeyCode pause = KeyCode.Escape;
        }

        [SerializeField] private Bindings keys = new();
        [SerializeField, Min(0f)] private float attitudeDegrees = 14f;
        private bool duelControls;
        public bool RolesSwapped { get; private set; }

        public string BallKeys => RolesSwapped ? MazeKeys : MoveKeys;
        public string TiltKeys => RolesSwapped ? MoveKeys : MazeKeys;
        private string MazeKeys => $"{KeyName(keys.mazeForward)}/{KeyName(keys.mazeLeft)}/{KeyName(keys.mazeBackward)}/{KeyName(keys.mazeRight)}";

        public void ConfigureDuel(bool enabled)
        {
            duelControls = enabled;
            RolesSwapped = false;
        }

        public void SwapRoles() => RolesSwapped = !RolesSwapped;

        private static string KeyName(KeyCode key) => key switch
        {
            KeyCode.UpArrow => "Up",
            KeyCode.DownArrow => "Down",
            KeyCode.LeftArrow => "Left",
            KeyCode.RightArrow => "Right",
            _ => key.ToString()
        };

        public string MoveKeys => $"{keys.forward}{keys.left}{keys.backward}{keys.right}";
        public string BrakeKey => keys.brake.ToString();
        public string ShieldKey => keys.shield.ToString();
        public string CameraKey => keys.camera.ToString();
        public string CalibrateKey => keys.calibrate.ToString();
        public string PauseKey => keys.pause == KeyCode.Escape ? "Esc" : keys.pause.ToString();

        public MazeInput Read()
        {
            var gestures = Gesture.None;
            var buttons = MazeButton.None;
            if (Input.GetKeyDown(keys.brake)) gestures |= Gesture.Brake;
            if (Input.GetKeyDown(keys.shield)) gestures |= Gesture.Shield;
            if (Input.GetKeyDown(keys.camera)) buttons |= MazeButton.Camera;
            if (Input.GetKeyDown(keys.calibrate)) buttons |= MazeButton.Calibrate;
            if (Input.GetKeyDown(keys.pause)) buttons |= MazeButton.Pause;

            Vector2 movement = new(Axis(keys.right, keys.left), Axis(keys.forward, keys.backward));
            if (!duelControls)
                return new MazeInput(movement.x, movement.y, gestures, buttons, ConnectionStatus.Simulated);

            Vector2 arrows = new(Axis(keys.mazeRight, keys.mazeLeft), Axis(keys.mazeForward, keys.mazeBackward));
            Vector2 tilt = RolesSwapped ? movement : arrows;
            Vector2 drive = RolesSwapped ? arrows : movement;
            // Axis returns degrees for tilt. The ball consumes a normalized movement vector.
            return new MazeInput(tilt.x, tilt.y, gestures, buttons, ConnectionStatus.Simulated,
                drive / Mathf.Max(attitudeDegrees, 0.001f));
        }

        private float Axis(KeyCode positive, KeyCode negative) =>
            attitudeDegrees * ((Input.GetKey(positive) ? 1 : 0) - (Input.GetKey(negative) ? 1 : 0));
    }
}
