using UnityEngine;

namespace BearPi.TiltMaze
{
    [RequireComponent(typeof(BallController))]
    public sealed class TerrainController : MonoBehaviour
    {
        [SerializeField] private LevelManager levels;
        [SerializeField, Min(0.1f)] private float probeDistance = 0.43f;
        private BallController ball;

        public TerrainKind Current { get; private set; }

        private void Awake() => ball = GetComponent<BallController>();

        public void ResetTerrain() => Current = TerrainKind.Normal;

        private void FixedUpdate()
        {
            var body = ball.Body;
            if (body.isKinematic) return;

            Current = TerrainKind.Normal;
            var normal = levels.Current.Tilt.Body.rotation * Vector3.up;
            if (!UnityEngine.Physics.Raycast(body.position, -normal, out var hit, probeDistance,
                UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ||
                !hit.collider.TryGetComponent(out TerrainZone zone))
                return;

            Current = zone.Kind;
            body.AddForce(zone.SurfaceAcceleration(body.linearVelocity, normal, Time.fixedTime),
                ForceMode.Acceleration);
        }
    }
}
