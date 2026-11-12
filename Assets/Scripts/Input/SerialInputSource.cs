using UnityEngine;

namespace BearPi.TiltMaze
{
    // Future UART adapter. It reports neutral attitude until a transport is implemented.
    public sealed class SerialInputSource : MonoBehaviour, IMazeInputSource
    {
        public MazeInput Read() => default;
    }
}
