using UnityEngine;

namespace BearPi.TiltMaze
{
    [RequireComponent(typeof(BallController))]
    public sealed class BallDriveController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float acceleration = 4.3f;
        [SerializeField, Min(0.1f)] private float topDriveSpeed = 4f;
        [SerializeField] private float groundProbe = 0.43f;
        private BallController ball;
        private Vector2 movement;
        private Vector3 surfaceNormal = Vector3.up;

        private void Awake() => ball = GetComponent<BallController>();

        public void SetDrive(Vector2 direction, Vector3 normal)
        {
            movement = Vector2.ClampMagnitude(direction, 1f);
            surfaceNormal = normal.normalized;
        }

        public void StopDrive() => movement = Vector2.zero;

        private void FixedUpdate()
        {
            var body = ball.Body;
            if (body.isKinematic || movement.sqrMagnitude < 0.001f) return;
            if (!UnityEngine.Physics.Raycast(body.position, -surfaceNormal, groundProbe,
                UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;

            // World-space directions stay readable in every camera mode.
            Vector3 direction = Vector3.ProjectOnPlane(
                new Vector3(movement.x, 0f, movement.y), surfaceNormal).normalized;
            if (Vector3.Dot(body.linearVelocity, direction) >= topDriveSpeed) return;
            body.AddForce(direction * (acceleration * movement.magnitude), ForceMode.Acceleration);
        }
    }
}
