using UnityEngine;

namespace BearPi.TiltMaze
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class LocalFeedbackSink : MonoBehaviour, IGameFeedbackSink
    {
        private AudioSource output;
        private AudioClip[] tones;

        private void Awake()
        {
            output = GetComponent<AudioSource>();
            tones = new[]
            {
                Cue("Contact", 0.10f, 430f, 230f),
                Cue("Impact", 0.21f, 154f, 56f),
                Cue("Brake", 0.23f, 570f, 235f),
                Cue("Shield", 0.35f, 520f, 930f),
                Cue("Trap", 0.24f, 255f, 103f),
                Cue("Failed", 0.53f, 350f, 110f),
                Cue("Cleared", 0.64f, 530f, 1046f)
            };
        }

        public void Receive(FeedbackEvent message)
        {
            Debug.Log($"[Tilt Maze] {message.Kind} ({message.Strength:0.00})", this);
            output.PlayOneShot(tones[(int)message.Kind], Mathf.Lerp(0.26f, 0.64f, message.Strength));
        }

        private void OnDestroy()
        {
            foreach (var tone in tones) Destroy(tone);
        }

        private static AudioClip Cue(string title, float seconds, float startHz, float endHz)
        {
            const int sampleRate = 22050;
            var samples = new float[Mathf.CeilToInt(sampleRate * seconds)];
            float phase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float progress = i / (float)samples.Length;
                phase += 2f * Mathf.PI * Mathf.Lerp(startHz, endHz, progress) / sampleRate;
                float envelope = Mathf.Min(1f, progress * 24f) * (1f - progress) * (1f - progress);
                samples[i] = (Mathf.Sin(phase) + 0.19f * Mathf.Sin(phase * 2f)) * envelope * 0.53f;
            }
            var clip = AudioClip.Create(title, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
