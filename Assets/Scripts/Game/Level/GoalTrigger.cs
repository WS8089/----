using System;
using UnityEngine;

namespace BearPi.TiltMaze
{
    public sealed class GoalTrigger : MonoBehaviour
    {
        public event Action<BallController> Reached;

        private void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != null &&
                other.attachedRigidbody.TryGetComponent(out BallController ball))
                Reached?.Invoke(ball);
        }
    }
}
