using UnityEngine;

namespace BearPi.TiltMaze
{
    public enum TerrainKind { Normal, Ice, Water, Wet }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class TerrainZone : MonoBehaviour
    {
        [SerializeField] private TerrainKind kind;
        [SerializeField, Min(0f)] private float drag;
        [SerializeField, Min(0f)] private float lateralSlip;

        public TerrainKind Kind => kind;

        public Vector3 SurfaceAcceleration(Vector3 velocity, Vector3 normal, float time)
        {
            Vector3 travel = Vector3.ProjectOnPlane(velocity, normal);
            Vector3 side = Vector3.Cross(normal, travel);
            return -travel * drag + side * (Mathf.Sin(time * 5.1f) * lateralSlip);
        }
    }
}
