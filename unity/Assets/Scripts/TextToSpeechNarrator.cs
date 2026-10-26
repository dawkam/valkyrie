using UnityEngine;
using ValkyrieTools;

// Plays speech synthesized by TextToSpeech with a stone corridor reverb and lowers the music while speaking
public class TextToSpeechNarrator : MonoBehaviour
{
    private const float MUSIC_VOLUME_WHILE_SPEAKING = 0.35f;
    private const float VOICE_VOLUME = 0.4f;
    private const AudioReverbPreset REVERB = AudioReverbPreset.StoneCorridor;
    private const float REVERB_LEVEL_REDUCTION_MB = 900f;
    private const float REVERB_DECAY_SCALE = 0.5f;

    private AudioSource voice;
    private bool musicLowered;

    public static TextToSpeechNarrator Create()
    {
        GameObject narratorObject = new GameObject("narrator");
        DontDestroyOnLoad(narratorObject);
        return narratorObject.AddComponent<TextToSpeechNarrator>();
    }

    void Awake()
    {
        voice = gameObject.AddComponent<AudioSource>();
        AudioReverbFilter reverb = gameObject.AddComponent<AudioReverbFilter>();
        reverb.reverbPreset = REVERB;
        reverb.reverbLevel -= REVERB_LEVEL_REDUCTION_MB;
        reverb.decayTime *= REVERB_DECAY_SCALE;
    }

    void Update()
    {
        byte[] wav = voice.isPlaying ? null : TextToSpeech.TakeSpeech();
        if (wav != null)
        {
            Play(wav);
        }
        UpdateMusicVolume();
    }

    public void StopSpeaking()
    {
        voice.Stop();
    }

    private void Play(byte[] wav)
    {
        if (!TextToSpeech.TryReadWav(wav, out float[] samples, out int channels, out int sampleRate))
        {
            ValkyrieDebug.Log("Warning: Text to speech returned unsupported audio.");
            return;
        }
        if (samples.Length == 0) return;

        AudioClip clip = AudioClip.Create("speech", samples.Length / channels, channels, sampleRate, false);
        clip.SetData(samples, 0);
        if (voice.clip != null)
        {
            Destroy(voice.clip);
        }
        voice.clip = clip;
        voice.volume = Game.Get().audioControl.effectVolume * VOICE_VOLUME;
        voice.Play();
    }

    private void UpdateMusicVolume()
    {
        Audio audio = Game.Get().audioControl;
        if (audio == null || audio.audioSource == null || audio.fadeOut || musicLowered == voice.isPlaying) return;

        musicLowered = voice.isPlaying;
        audio.audioSource.volume = musicLowered ? audio.musicVolume * MUSIC_VOLUME_WHILE_SPEAKING : audio.musicVolume;
    }
}
