using UnityEngine;

namespace BearPi.TiltMaze
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MazeTiltController : MonoBehaviour
    {
        [SerializeField, Range(1f, 24f)] private float maxTilt = 13f;
        [SerializeField, Min(0f)] private float deadZone = 0.5f;
        [SerializeField, Min(0.1f)] private float response = 5.5f;
        [SerializeField, Min(1f)] private float tiltSpeed = 24f;
        private Rigidbody body;
        private Quaternion neutral;
        private Vector2 target;
        private Vector2 offset;
        private bool waitForNeutral;

        public Vector2 CurrentAngles { get; private set; }
        public Rigidbody Body => body;
        public Quaternion CommandedRotation { get; private set; }
        public bool AcceptingTilt { get; set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            CommandedRotation = neutral = body.rotation;
        }

        public void SetAttitude(MazeInput sample)
        {
            if (waitForNeutral && sample.ConnectionStatus == ConnectionStatus.Simulated)
            {
                waitForNeutral = Mathf.Abs(sample.Roll) > deadZone || Mathf.Abs(sample.Pitch) > deadZone;
                target = Vector2.zero;
                return;
            }

            target = sample.ConnectionStatus == ConnectionStatus.Disconnected
                ? Vector2.zero
                : Vector2.ClampMagnitude(new Vector2(
                    ApplyDeadZone(sample.Roll - offset.x), ApplyDeadZone(sample.Pitch - offset.y)), maxTilt);
        }

        public void Calibrate(MazeInput sample)
        {
            // Release held simulated controls before applying tilt again.
            waitForNeutral = sample.ConnectionStatus == ConnectionStatus.Simulated &&
                (Mathf.Abs(sample.Roll) > deadZone || Mathf.Abs(sample.Pitch) > deadZone);
            offset = sample.ConnectionStatus == ConnectionStatus.Connected
                ? new Vector2(sample.Roll, sample.Pitch) : Vector2.zero;
            target = Vector2.zero;
        }

        public void ResetTilt()
        {
            target = Vector2.zero;
            CurrentAngles = Vector2.zero;
            waitForNeutral = false;
            body.rotation = CommandedRotation = neutral;
        }

        private float ApplyDeadZone(float angle) =>
            Mathf.Sign(angle) * Mathf.Max(0f, Mathf.Abs(angle) - deadZone);

        private void FixedUpdate()
        {
            if (!AcceptingTilt) return;
            var softenedTarget = Vector2.Lerp(CurrentAngles, target,
                1f - Mathf.Exp(-response * Time.fixedDeltaTime));
            CurrentAngles = Vector2.MoveTowards(CurrentAngles, softenedTarget, tiltSpeed * Time.fixedDeltaTime);
            CommandedRotation = neutral * Quaternion.Euler(CurrentAngles.y, 0f, -CurrentAngles.x);
            body.MoveRotation(CommandedRotation);
        }
    }
}
