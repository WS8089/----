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
            public KeyCode brake = KeyCode.Space;
            public KeyCode shield = KeyCode.Q;
            public KeyCode camera = KeyCode.V;
            public KeyCode calibrate = KeyCode.R;
            public KeyCode pause = KeyCode.Escape;
        }

        [SerializeField] private Bindings keys = new();
        [SerializeField, Min(0f)] private float attitudeDegrees = 14f;

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

            return new MazeInput(Axis(keys.right, keys.left), Axis(keys.forward, keys.backward),
                gestures, buttons, ConnectionStatus.Simulated);
        }

        private float Axis(KeyCode positive, KeyCode negative) =>
            attitudeDegrees * ((Input.GetKey(positive) ? 1 : 0) - (Input.GetKey(negative) ? 1 : 0));
    }
}
