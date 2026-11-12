using UnityEngine;

namespace BearPi.TiltMaze
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class BallController : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxSpeed = 8f;
        private Rigidbody body;

        public Rigidbody Body => body;

        private void Awake() => body = GetComponent<Rigidbody>();

        public void SetActivePhysics(bool active)
        {
            if (body.isKinematic == !active) return;
            if (!active)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            body.isKinematic = !active;
            if (active)
            {
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.WakeUp();
            }
        }

        public void ResetAt(Vector3 position)
        {
            SetActivePhysics(false);
            body.position = position;
            body.rotation = Quaternion.identity;
        }

        private void FixedUpdate()
        {
            if (body.isKinematic) return;
            body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, maxSpeed);
        }
    }
}
