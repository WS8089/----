using UnityEngine;

namespace BearPi.TiltMaze
{
    [RequireComponent(typeof(BallController))]
    public sealed class SkillManager : MonoBehaviour
    {
        [SerializeField] private GameFeedback feedback;
        [SerializeField] private LevelManager levels;
        [SerializeField, Min(0f)] private float brakeDuration = 0.75f;
        [SerializeField, Min(0f)] private float brakeCooldown = 3.5f;
        [SerializeField, Min(0f)] private float shieldDuration = 2.2f;
        [SerializeField, Min(0f)] private float shieldCooldown = 8f;
        [SerializeField] private GameObject shieldVisual;

        private BallController ball;
        private float brakeUntil;
        private float shieldUntil;
        private float brakeReady;
        private float shieldReady;

        public float BrakeRemaining => Mathf.Max(0f, brakeUntil - Time.time);
        public float ShieldRemaining => Mathf.Max(0f, shieldUntil - Time.time);
        public float BrakeRecharge => Mathf.Max(0f, brakeReady - Time.time);
        public float ShieldRecharge => Mathf.Max(0f, shieldReady - Time.time);
        public bool IsShielded => ShieldRemaining > 0f;

        private void Awake() => ball = GetComponent<BallController>();

        public void ResetSkills()
        {
            brakeReady = 0f;
            shieldReady = 0f;
            StopSkills();
        }

        public void StopSkills()
        {
            brakeUntil = 0f;
            shieldUntil = 0f;
            if (shieldVisual != null) shieldVisual.SetActive(false);
        }

        public void Handle(Gesture gestures)
        {
            if (ball.Body.isKinematic) return;
            if ((gestures & Gesture.Brake) != 0 && BrakeRecharge <= 0f)
            {
                brakeUntil = Time.time + brakeDuration;
                brakeReady = brakeUntil + brakeCooldown;
                feedback.Publish(FeedbackKind.Brake, ball.Body.position);
            }
            if ((gestures & Gesture.Shield) != 0 && ShieldRecharge <= 0f)
            {
                shieldUntil = Time.time + shieldDuration;
                shieldReady = shieldUntil + shieldCooldown;
                feedback.Publish(FeedbackKind.Shield, ball.Body.position);
            }
        }

        private void Update()
        {
            if (shieldVisual != null && shieldVisual.activeSelf != IsShielded)
                shieldVisual.SetActive(IsShielded);
        }

        private void FixedUpdate()
        {
            var body = ball.Body;
            if (body.isKinematic || BrakeRemaining <= 0f) return;

            Vector3 normal = levels.Current.Tilt.Body.rotation * Vector3.up;
            Vector3 tangent = Vector3.ProjectOnPlane(body.linearVelocity, normal);
            float damping = Mathf.Exp(-11f * Time.fixedDeltaTime);
            body.linearVelocity -= tangent * (1f - damping);
            body.angularVelocity *= damping;
        }
    }
}
