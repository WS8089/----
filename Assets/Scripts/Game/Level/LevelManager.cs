using System;
using UnityEngine;

namespace BearPi.TiltMaze
{
    public sealed class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelDefinition[] levels;
        [SerializeField] private BallController ball;

        public event Action GoalReached;
        public LevelDefinition Current => levels[Index];
        public BallController Ball => ball;
        public int Index { get; private set; }
        public bool HasNext => Index + 1 < levels.Length;

        public void Select(int index)
        {
            Current.Goal.Reached -= OnGoal;
            Index = Mathf.Clamp(index, 0, levels.Length - 1);
            for (int i = 0; i < levels.Length; i++)
                levels[i].gameObject.SetActive(i == Index);

            Current.Goal.Reached += OnGoal;
            ResetBall();
        }

        public void ResetBall()
        {
            Current.Tilt.AcceptingTilt = false;
            Current.Tilt.ResetTilt();
            ball.ResetAt(Current.SpawnPosition);
            Current.Mechanisms?.ResetMechanisms();
        }

        private void OnGoal(BallController reached)
        {
            if (reached == ball) GoalReached?.Invoke();
        }

        private void OnDestroy()
        {
            if (levels != null && levels.Length > 0 && Current != null)
                Current.Goal.Reached -= OnGoal;
        }
    }
}
