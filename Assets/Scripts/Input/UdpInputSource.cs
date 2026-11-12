using UnityEngine;

namespace BearPi.TiltMaze
{
    // Future UDP adapter. It reports neutral attitude until a transport is implemented.
    public sealed class UdpInputSource : MonoBehaviour, IMazeInputSource
    {
        public MazeInput Read() => default;
    }
}
