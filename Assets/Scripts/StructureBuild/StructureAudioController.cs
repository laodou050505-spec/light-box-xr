using UnityEngine;

namespace StructureBuild
{
    public enum StructureSound
    {
        UiHover,
        UiClick,
        Grab,
        Place,
        Reject,
        Hint,
        Undo,
        Reset,
        Complete,
        Continue,
    }

    /// <summary>
    /// Owns the game's two audio paths so the editor installer can be run repeatedly
    /// without creating a growing set of AudioSources or duplicated playback.
    /// </summary>
    public sealed class StructureAudioController : MonoBehaviour
    {
        [Header("Music")]
        public AudioClip musicClip;
        [Range(0f, 1f)] public float musicVolume = 0.18f;

        [Header("Interaction clips")]
        public AudioClip uiHoverClip;
        public AudioClip uiClickClip;
        public AudioClip grabClip;
        public AudioClip placeClip;
        public AudioClip rejectClip;
        public AudioClip hintClip;
        public AudioClip undoClip;
        public AudioClip resetClip;
        public AudioClip completeClip;
        public AudioClip continueClip;
        [Range(0f, 1f)] public float effectsVolume = 0.52f;

        private AudioSource musicSource;
        private AudioSource effectsSource;

        private void Awake()
        {
            musicSource = EnsureSource("MusicSource");
            effectsSource = EnsureSource("EffectsSource");
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicVolume;
            effectsSource.playOnAwake = false;
            effectsSource.loop = false;
            effectsSource.spatialBlend = 0f;
            effectsSource.volume = effectsVolume;
        }

        private void Start()
        {
            if (musicClip == null || musicSource == null) return;
            musicSource.clip = musicClip;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void Play(StructureSound sound)
        {
            if (effectsSource == null) return;
            var clip = ClipFor(sound);
            if (clip == null) return;
            effectsSource.PlayOneShot(clip, Mathf.Clamp01(effectsVolume * VolumeFor(sound)));
        }

        public void PlayUiHover() => Play(StructureSound.UiHover);
        public void PlayUiClick() => Play(StructureSound.UiClick);
        public void PlayGrab() => Play(StructureSound.Grab);
        public void PlayPlace() => Play(StructureSound.Place);
        public void PlayReject() => Play(StructureSound.Reject);
        public void PlayHint() => Play(StructureSound.Hint);
        public void PlayUndo() => Play(StructureSound.Undo);
        public void PlayReset() => Play(StructureSound.Reset);
        public void PlayComplete() => Play(StructureSound.Complete);
        public void PlayContinue() => Play(StructureSound.Continue);

        private AudioSource EnsureSource(string name)
        {
            var child = transform.Find(name);
            var source = child != null ? child.GetComponent<AudioSource>() : null;
            if (source != null) return source;
            var host = child != null ? child.gameObject : new GameObject(name);
            if (child == null) host.transform.SetParent(transform, false);
            return host.AddComponent<AudioSource>();
        }

        private AudioClip ClipFor(StructureSound sound)
        {
            switch (sound)
            {
                case StructureSound.UiHover: return uiHoverClip;
                case StructureSound.UiClick: return uiClickClip;
                case StructureSound.Grab: return grabClip;
                case StructureSound.Place: return placeClip;
                case StructureSound.Reject: return rejectClip;
                case StructureSound.Hint: return hintClip;
                case StructureSound.Undo: return undoClip;
                case StructureSound.Reset: return resetClip;
                case StructureSound.Complete: return completeClip;
                case StructureSound.Continue: return continueClip;
                default: return null;
            }
        }

        private static float VolumeFor(StructureSound sound)
        {
            switch (sound)
            {
                case StructureSound.UiHover: return 0.42f;
                case StructureSound.Reject: return 0.68f;
                case StructureSound.Complete: return 0.86f;
                default: return 0.72f;
            }
        }
    }
}
