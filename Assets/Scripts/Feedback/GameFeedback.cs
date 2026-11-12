using UnityEngine;

namespace BearPi.TiltMaze
{
    public enum FeedbackKind { Contact, HeavyImpact, Brake, Shield, Trap, Failed, Completed }

    public readonly struct FeedbackEvent
    {
        public readonly FeedbackKind Kind;
        public readonly float Strength;
        public readonly Vector3 Position;

        public FeedbackEvent(FeedbackKind kind, float strength, Vector3 position)
        {
            Kind = kind;
            Strength = strength;
            Position = position;
        }
    }

    // A second-device adapter can consume these events without depending on gameplay classes.
    public interface IGameFeedbackSink
    {
        void Receive(FeedbackEvent message);
    }

    public sealed class GameFeedback : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] sinks;

        public void Publish(FeedbackKind kind, Vector3 position, float strength = 1f)
        {
            var message = new FeedbackEvent(kind, strength, position);
            foreach (var sink in sinks)
                if (sink != null && sink.isActiveAndEnabled && sink is IGameFeedbackSink output)
                    output.Receive(message);
        }
    }
}
