using UnityEngine;

namespace BearPi.TiltMaze
{
    public sealed class LevelDefinition : MonoBehaviour
    {
        [SerializeField] private string title = "Switchback";
        [SerializeField] private MazeTiltController tilt;
        [SerializeField] private Transform spawn;
        [SerializeField] private GoalTrigger goal;
        [SerializeField] private MechanismManager mechanisms;
        [SerializeField] private float fallHeight = 4.5f;
        [SerializeField] private Vector2 boardSize = new(19.25f, 15.75f);

        public string Title => title;
        public MazeTiltController Tilt => tilt;
        public GoalTrigger Goal => goal;
        public MechanismManager Mechanisms => mechanisms;
        public Vector2 BoardSize => boardSize;
        public float FallY => tilt.Body.position.y - fallHeight;

        public Vector3 SpawnPosition =>
            tilt.Body.position + tilt.Body.rotation * spawn.localPosition;
    }
}
