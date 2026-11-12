using UnityEngine;

namespace BearPi.TiltMaze
{
    internal static class MechanismMotion
    {
        public static Vector3 Door(float phase)
        {
            float closing = Mathf.InverseLerp(0.34f, 0.46f, phase);
            float opening = Mathf.InverseLerp(0.68f, 0.82f, phase);
            return Vector3.up * (1.48f * (1f - Mathf.SmoothStep(0f, 1f, closing - opening)));
        }

        public static Vector3 Sweep(float phase) =>
            Vector3.forward * (0.55f * Mathf.Clamp(1.55f * Mathf.Sin(phase * Mathf.PI * 2f), -1f, 1f));
    }
}
