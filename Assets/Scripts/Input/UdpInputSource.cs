using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace BearPi.TiltMaze
{
    /// <summary>
    /// Receives the BearPi board attitude over UDP.
    ///
    /// Wire format (20 bytes, little-endian):
    ///   uint32 seq, uint32 ms, int32 nx, int32 ny, int32 nz
    /// where (nx, ny, nz) is the board normal multiplied by 10000.
    ///
    /// The board normal is converted into the Roll/Pitch degrees expected by
    /// <see cref="MazeTiltController"/>. Roll positive rolls the ball right,
    /// Pitch positive rolls the ball forward, matching the keyboard adapter.
    /// </summary>
    public sealed class UdpInputSource : MonoBehaviour, IMazeInputSource
    {
        private const int PacketSize = 20;

        [Header("UDP")]
        [SerializeField, Min(1)] private int port = 9999;
        [SerializeField, Min(0.05f)] private float staleTimeout = 0.3f;
        [SerializeField] private bool logStats = true;

        [Header("Console diagnostics")]
        [SerializeField] private bool logPackets = true;
        [SerializeField, Min(0.05f)] private float packetLogInterval = 0.2f;

        [Header("Mapping")]
        [Tooltip("Swap the board X/Y axes if the maze tilts along the wrong axis.")]
        [SerializeField] private bool swapAxes;
        [SerializeField] private bool invertRoll;
        [SerializeField] private bool invertPitch;

        [Header("Keyboard edges (attitude still comes from UDP)")]
        [SerializeField] private KeyCode brakeKey = KeyCode.Space;
        [SerializeField] private KeyCode shieldKey = KeyCode.Q;
        [SerializeField] private KeyCode cameraKey = KeyCode.V;
        [SerializeField] private KeyCode calibrateKey = KeyCode.R;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        private readonly ConcurrentQueue<ImuFrame> pending = new ConcurrentQueue<ImuFrame>();

        private UdpClient client;
        private Thread thread;
        private volatile bool running;

        private long packets;
        private long lost;
        private long lastSeq = -1;
        private double lastPacketTime = double.NegativeInfinity;
        private string lastError;

        private float roll;
        private float pitch;
        private bool hasSample;
        private float nextStatsLog;
        private float nextPacketLog;
        private uint lastMs;
        private Vector3 lastNormal;
        private bool loggedFirstPacket;
        private bool loggedBadLength;

        public int Port => port;
        public long Packets => Interlocked.Read(ref packets);
        public long Lost => Interlocked.Read(ref lost);
        public string LastError => lastError;

        /// <summary>True while the game is actually using this adapter.</summary>
        public bool Active { get; set; }

        public bool IsConnected =>
            hasSample && (Time.realtimeSinceStartupAsDouble - lastPacketTime) <= staleTimeout;

        private readonly struct ImuFrame
        {
            public readonly uint Seq;
            public readonly uint Ms;
            public readonly Vector3 Normal;

            public ImuFrame(uint seq, uint ms, Vector3 normal)
            {
                Seq = seq;
                Ms = ms;
                Normal = normal;
            }
        }

        private void Start()
        {
            try
            {
                client = new UdpClient(port);
                client.Client.ReceiveTimeout = 200;
                running = true;
                thread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = $"ImuUdp:{port}"
                };
                thread.Start();
                Debug.Log($"[IMU] listening on udp {port}");
            }
            catch (Exception e)
            {
                lastError = e.Message;
                running = false;
                Debug.LogError($"[IMU] cannot bind udp {port}: {e.Message}");
            }
        }

        private void ReceiveLoop()
        {
            var endpoint = new IPEndPoint(IPAddress.Any, 0);

            while (running)
            {
                try
                {
                    byte[] data = client.Receive(ref endpoint);
                    if (data.Length != PacketSize)
                    {
                        if (!loggedBadLength)
                        {
                            loggedBadLength = true;
                            Debug.LogWarning($"[IMU] unexpected packet length {data.Length}, " +
                                             $"expected {PacketSize}. Is the board running the " +
                                             "binary UDP firmware?");
                        }
                        continue;
                    }

                    uint seq = BitConverter.ToUInt32(data, 0);
                    uint ms = BitConverter.ToUInt32(data, 4);
                    float nx = BitConverter.ToInt32(data, 8) / 10000f;
                    float ny = BitConverter.ToInt32(data, 12) / 10000f;
                    float nz = BitConverter.ToInt32(data, 16) / 10000f;

                    var normal = new Vector3(nx, ny, nz);
                    if (normal.sqrMagnitude < 0.25f)
                    {
                        continue;
                    }

                    long previous = Interlocked.Read(ref lastSeq);
                    if (previous >= 0 && seq > previous + 1)
                    {
                        Interlocked.Add(ref lost, (long)seq - previous - 1);
                    }
                    Interlocked.Exchange(ref lastSeq, seq);
                    Interlocked.Increment(ref packets);

                    pending.Enqueue(new ImuFrame(seq, ms, normal.normalized));
                    while (pending.Count > 8)
                    {
                        pending.TryDequeue(out _);
                    }
                }
                catch (SocketException)
                {
                    // 200 ms receive timeout; keep waiting.
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    lastError = e.Message;
                }
            }
        }

        private void Update()
        {
            if (!Active || !logStats || Time.unscaledTime < nextStatsLog)
            {
                return;
            }

            nextStatsLog = Time.unscaledTime + 1f;
            bool fresh = hasSample &&
                (Time.realtimeSinceStartupAsDouble - lastPacketTime) <= staleTimeout;
            string suffix = string.IsNullOrEmpty(lastError)
                ? string.Empty
                : $" lastError={lastError}";

            Debug.Log($"[IMU] udp {port}: packets={Packets} lost={Lost} " +
                      $"status={(fresh ? "Connected" : "Disconnected")}{suffix}");
        }

        public MazeInput Read()
        {
            // Keep only the newest sample; MazeTiltController already smooths.
            bool updated = false;
            while (pending.TryDequeue(out ImuFrame frame))
            {
                Apply(frame.Normal);
                lastMs = frame.Ms;
                lastNormal = frame.Normal;
                lastPacketTime = Time.realtimeSinceStartupAsDouble;
                hasSample = true;
                updated = true;
            }

            if (updated && !loggedFirstPacket)
            {
                loggedFirstPacket = true;
                Debug.Log("[IMU] first packet received");
            }

            if (updated && logPackets && Time.unscaledTime >= nextPacketLog)
            {
                nextPacketLog = Time.unscaledTime + packetLogInterval;
                Debug.Log($"[IMU] rx seq={lastSeq} ms={lastMs} " +
                          $"n=({lastNormal.x:F3}, {lastNormal.y:F3}, {lastNormal.z:F3}) " +
                          $"-> roll={roll:F2} pitch={pitch:F2}");
            }

            bool fresh = hasSample &&
                (Time.realtimeSinceStartupAsDouble - lastPacketTime) <= staleTimeout;

            Gesture gestures = Gesture.None;
            MazeButton buttons = MazeButton.None;
            if (Input.GetKeyDown(brakeKey)) gestures |= Gesture.Brake;
            if (Input.GetKeyDown(shieldKey)) gestures |= Gesture.Shield;
            if (Input.GetKeyDown(cameraKey)) buttons |= MazeButton.Camera;
            if (Input.GetKeyDown(calibrateKey)) buttons |= MazeButton.Calibrate;
            if (Input.GetKeyDown(pauseKey)) buttons |= MazeButton.Pause;

            return new MazeInput(
                fresh ? roll : 0f,
                fresh ? pitch : 0f,
                gestures,
                buttons,
                fresh ? ConnectionStatus.Connected : ConnectionStatus.Disconnected);
        }

        private void Apply(Vector3 boardNormal)
        {
            // Board frame: X right, Y forward, Z up.
            // Unity frame: X right, Y up, Z forward.
            Vector3 up = swapAxes
                ? new Vector3(boardNormal.y, boardNormal.z, boardNormal.x)
                : new Vector3(boardNormal.x, boardNormal.z, boardNormal.y);

            if (up.sqrMagnitude < 1e-8f)
            {
                return;
            }

            // Shortest rotation from Unity up to the measured plane normal.
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, up.normalized);

            // MazeTiltController applies Quaternion.Euler(pitch, 0, -roll),
            // so extract the same X/Z pair from the shortest rotation.
            Vector3 euler = rotation.eulerAngles;
            float mappedPitch = Mathf.DeltaAngle(0f, euler.x);
            float mappedRoll = -Mathf.DeltaAngle(0f, euler.z);

            roll = invertRoll ? -mappedRoll : mappedRoll;
            pitch = invertPitch ? -mappedPitch : mappedPitch;
        }

        private void OnDestroy() => Stop();
        private void OnApplicationQuit() => Stop();

        private void Stop()
        {
            running = false;
            try { client?.Close(); } catch { }
            if (thread != null && thread.IsAlive)
            {
                thread.Join(200);
            }
            client = null;
            thread = null;
        }
    }
}
