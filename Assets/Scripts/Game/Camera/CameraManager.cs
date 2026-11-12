using UnityEngine;

namespace BearPi.TiltMaze
{
    public enum CameraMode { Overview, Follow, Close }

    [RequireComponent(typeof(Camera))]
    public sealed class CameraManager : MonoBehaviour
    {
        [SerializeField] private LevelManager levels;
        [SerializeField, Min(0.1f)] private float response = 6.5f;
        private Camera view;
        private Vector3 focus;
        private Vector3 offset;
        private float size;

        public CameraMode Mode { get; private set; }
        public bool MenuFraming { get; set; }

        private void Awake()
        {
            view = GetComponent<Camera>();
            ResetView();
        }

        public void Cycle() => Mode = (CameraMode)(((int)Mode + 1) % 3);

        public void ResetView()
        {
            Mode = CameraMode.Overview;
            GetPose(out focus, out offset, out size);
            ApplyPose();
        }

        private void LateUpdate()
        {
            GetPose(out var target, out var targetOffset, out var targetSize);
            float t = 1f - Mathf.Exp(-response * Time.unscaledDeltaTime);
            focus = Vector3.Lerp(focus, target, t);
            offset = Vector3.Lerp(offset, targetOffset, t);
            size = Mathf.Lerp(size, targetSize, t);
            ApplyPose();
        }

        private void GetPose(out Vector3 target, out Vector3 direction, out float zoom)
        {
            Vector3 ball = levels.Ball.transform.position;
            switch (Mode)
            {
                case CameraMode.Follow:
                    target = ball + Vector3.up * 0.32f + Vector3.forward * 0.55f;
                    direction = new Vector3(0f, 10f, -5.8f);
                    zoom = 4.65f;
                    break;
                case CameraMode.Close:
                    target = ball + Vector3.up * 0.22f;
                    direction = new Vector3(0f, 4.7f, -3.5f);
                    zoom = 2.7f;
                    break;
                default:
                    var level = levels.Current;
                    target = level.transform.position;
                    if (MenuFraming) target.x -= Mathf.Clamp((view.aspect - 1.24f) * 6.4f, 0f, 3.7f);
                    direction = new Vector3(0f, 21f, -10f);
                    zoom = Mathf.Max(level.BoardSize.y * 0.57f,
                        level.BoardSize.x * 0.58f / Mathf.Max(view.aspect, 0.1f));
                    break;
            }
        }

        private void ApplyPose()
        {
            Vector3 resolvedOffset = offset;
            if (Mode != CameraMode.Overview &&
                Physics.SphereCast(focus, 0.12f, offset.normalized, out _,
                    offset.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                resolvedOffset = new Vector3(0f, offset.magnitude, -0.1f);

            transform.SetPositionAndRotation(focus + resolvedOffset,
                Quaternion.LookRotation(-resolvedOffset, Vector3.up));
            view.orthographicSize = size;
        }
    }
}
