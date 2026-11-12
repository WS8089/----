using System;

namespace BearPi.TiltMaze
{
    public enum ConnectionStatus { Disconnected, Connected, Simulated }

    [Flags]
    public enum Gesture { None = 0, Brake = 1, Shield = 2 }

    [Flags]
    public enum MazeButton { None = 0, Camera = 1, Calibrate = 2, Pause = 4 }

    // Attitude in degrees. Events are consumed on the first read after arrival.
    public readonly struct MazeInput
    {
        public readonly float Roll;
        public readonly float Pitch;
        public readonly Gesture Gesture;
        public readonly MazeButton Button;
        public readonly ConnectionStatus ConnectionStatus;

        public MazeInput(float roll, float pitch, Gesture gesture, MazeButton button,
            ConnectionStatus connectionStatus)
        {
            Roll = roll;
            Pitch = pitch;
            Gesture = gesture;
            Button = button;
            ConnectionStatus = connectionStatus;
        }
    }

    public interface IMazeInputSource
    {
        MazeInput Read();
    }
}
