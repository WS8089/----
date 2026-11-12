using UnityEngine;

namespace BearPi.TiltMaze
{
    public sealed class GoalBeacon : MonoBehaviour
    {
        [SerializeField] private Transform ring;
        private Vector3 baseScale;

        private void Awake() => baseScale = ring.localScale;

        private void Update()
        {
            float pulse = 1f + 0.065f * Mathf.Sin(Time.time * 3.6f);
            ring.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z * pulse);
        }
    }
}
