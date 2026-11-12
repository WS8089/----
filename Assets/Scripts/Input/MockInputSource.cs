using UnityEngine;

namespace BearPi.TiltMaze
{
    public sealed class MockInputSource : MonoBehaviour, IMazeInputSource
    {
        [SerializeField, Range(-45f, 45f)] private float roll;
        [SerializeField, Range(-45f, 45f)] private float pitch;
        private Gesture gestures;
        private MazeButton buttons;

        public void SetAttitude(float rollDegrees, float pitchDegrees)
        {
            roll = rollDegrees;
            pitch = pitchDegrees;
        }

        public void Emit(Gesture gesture) => gestures |= gesture;
        public void Emit(MazeButton button) => buttons |= button;

        [ContextMenu("Emit Brake")] private void Brake() => Emit(Gesture.Brake);
        [ContextMenu("Emit Shield")] private void Shield() => Emit(Gesture.Shield);
        [ContextMenu("Calibrate")] private void Calibrate() => Emit(MazeButton.Calibrate);

        public MazeInput Read()
        {
            var sample = new MazeInput(roll, pitch, gestures, buttons, ConnectionStatus.Simulated);
            gestures = Gesture.None;
            buttons = MazeButton.None;
            return sample;
        }
    }
}
