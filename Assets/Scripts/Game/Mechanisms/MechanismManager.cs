using UnityEngine;

namespace BearPi.TiltMaze
{
    [DefaultExecutionOrder(10)]
    public sealed class MechanismManager : MonoBehaviour
    {
        [SerializeField] private MazeTiltController tilt;
        [SerializeField] private BallController ball;
        [SerializeField] private MechanismActuator[] actuators;
        private float elapsed;

        private void OnEnable()
        {
            foreach (var actuator in actuators) actuator.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            foreach (var actuator in actuators)
                if (actuator != null) actuator.gameObject.SetActive(false);
        }

        public void ResetMechanisms()
        {
            elapsed = 0f;
            foreach (var actuator in actuators)
                actuator.Apply(tilt.Body, tilt.CommandedRotation, ball.Body.position, elapsed, true);
        }

        private void FixedUpdate()
        {
            if (!tilt.AcceptingTilt) return;
            elapsed += Time.fixedDeltaTime;
            foreach (var actuator in actuators)
                actuator.Apply(tilt.Body, tilt.CommandedRotation, ball.Body.position, elapsed, false);
        }
    }
}
