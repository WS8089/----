using System;
using UnityEngine;

namespace BearPi.TiltMaze
{
    [RequireComponent(typeof(BallController), typeof(SkillManager))]
    public sealed class BallDamageSystem : MonoBehaviour
    {
        [SerializeField] private GameFeedback feedback;
        [SerializeField, Min(1f)] private float maximum = 100f;
        [SerializeField, Min(0f)] private float impactThreshold = 3.65f;
        [SerializeField, Min(0f)] private float damagePerUnitSpeed = 8.5f;
        [SerializeField, Min(0f)] private float impactInterval = 0.28f;
        private float nextImpact;
        private float nextTrap;
        private float nextContact;
        private BallController ball;
        private SkillManager skills;

        public event Action<float, float> DurabilityChanged;
        public event Action Depleted;
        public float Durability { get; private set; }
        public float Maximum => maximum;

        private void Awake()
        {
            ball = GetComponent<BallController>();
            skills = GetComponent<SkillManager>();
            ResetDurability();
        }

        public void ResetDurability()
        {
            Durability = maximum;
            nextImpact = 0f;
            nextTrap = 0f;
            nextContact = 0f;
            DurabilityChanged?.Invoke(Durability, maximum);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (ball.Body.isKinematic || Durability <= 0f || collision.contactCount == 0) return;

            Vector3 velocity = collision.relativeVelocity;
            float frontal = 0f;
            for (int i = 0; i < collision.contactCount; i++)
                frontal = Mathf.Max(frontal, Mathf.Abs(Vector3.Dot(velocity, collision.GetContact(i).normal)));

            Vector3 point = collision.GetContact(0).point;
            float damage = 0f;
            if (frontal >= impactThreshold && Time.time >= nextImpact)
            {
                nextImpact = Time.time + impactInterval;
                nextContact = Time.time + 0.2f;
                damage = (frontal - impactThreshold) * damagePerUnitSpeed;
                feedback.Publish(FeedbackKind.HeavyImpact, point, Mathf.Clamp01(frontal / 8f));
            }
            else if (frontal >= 1.15f && Time.time >= nextContact)
            {
                nextContact = Time.time + 0.22f;
                feedback.Publish(FeedbackKind.Contact, point, Mathf.Clamp01(frontal / 4f));
            }

            if (collision.rigidbody != null && Time.time >= nextTrap &&
                collision.rigidbody.TryGetComponent(out MechanismActuator actuator) &&
                actuator.TrapDamage > 0f)
            {
                nextTrap = Time.time + 0.55f;
                damage += actuator.TrapDamage;
                feedback.Publish(FeedbackKind.Trap, point);
            }

            if (damage <= 0f || skills.IsShielded) return;
            Durability = Mathf.Max(0f, Durability - damage);
            DurabilityChanged?.Invoke(Durability, maximum);
            if (Durability <= 0f) Depleted?.Invoke();
        }
    }
}
