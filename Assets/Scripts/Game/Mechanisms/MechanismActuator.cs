using UnityEngine;

namespace BearPi.TiltMaze
{
    public enum MechanismKind { LiftGate, Sweeper }

    [RequireComponent(typeof(Rigidbody))]
    public sealed class MechanismActuator : MonoBehaviour
    {
        [SerializeField] private MechanismKind kind;
        [SerializeField] private Vector3 boardAnchor;
        [SerializeField, Min(0.1f)] private float period = 5f;
        [SerializeField, Min(0f)] private float trapDamage;
        private Rigidbody body;

        public float TrapDamage => trapDamage;

        private void Awake() => body = GetComponent<Rigidbody>();

        public void Apply(Rigidbody board, Quaternion rotation, Vector3 ballPosition,
            float elapsed, bool reset)
        {
            float phase = Mathf.Repeat(elapsed / period, 1f);
            Vector3 localOffset = kind == MechanismKind.LiftGate
                ? MechanismMotion.Door(phase) : MechanismMotion.Sweep(phase);

            if (!reset && kind == MechanismKind.LiftGate)
            {
                Vector3 ballLocal = Quaternion.Inverse(board.rotation) *
                    (ballPosition - board.position) - boardAnchor;
                if (Mathf.Abs(ballLocal.x) < 1f && Mathf.Abs(ballLocal.z) < 0.48f &&
                    Mathf.Abs(ballLocal.y) < 0.9f)
                {
                    var gateLocal = Quaternion.Inverse(board.rotation) *
                        (body.position - board.position) - boardAnchor;
                    localOffset.y = Mathf.Max(localOffset.y, gateLocal.y);
                }
            }

            Vector3 world = board.position + rotation * (boardAnchor + localOffset);
            if (reset)
            {
                body.position = world;
                body.rotation = rotation;
                return;
            }
            body.Move(world, rotation);
        }
    }
}
