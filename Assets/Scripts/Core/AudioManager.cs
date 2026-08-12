using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource sfxSource2;

    private ProceduralAudio procAudio;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 0.5f;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.8f;
        }
        if (sfxSource2 == null)
        {
            sfxSource2 = gameObject.AddComponent<AudioSource>();
            sfxSource2.playOnAwake = false;
            sfxSource2.volume = 0.8f;
        }

        procAudio = GetComponent<ProceduralAudio>();
        if (procAudio == null) procAudio = gameObject.AddComponent<ProceduralAudio>();

        GenerateBGM();
    }

    void GenerateBGM()
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int duration = 30;
        int samples = sampleRate * duration;
        float[] data = new float[samples * 2];

        float[] notes = { 130.81f, 146.83f, 164.81f, 174.61f, 196.00f, 220.00f, 246.94f };
        System.Random rand = new System.Random(12345);

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float sample = 0f;

            int noteIndex = (int)(t * 0.5f) % notes.Length;
            float freq = notes[noteIndex];

            sample += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.15f;
            sample += Mathf.Sin(2f * Mathf.PI * freq * 2 * t) * 0.08f;
            sample += Mathf.Sin(2f * Mathf.PI * freq * 0.5f * t) * 0.1f;

            float noise = (float)(rand.NextDouble() * 2 - 1) * 0.02f;
            sample += noise;

            float env = Mathf.Sin(t * 0.2f) * 0.3f + 0.7f;
            sample *= env * 0.3f;

            data[i * 2] = sample;
            data[i * 2 + 1] = sample;
        }

        var clip = AudioClip.Create("ProceduralBGM", samples, 2, sampleRate, false);
        clip.SetData(data, 0);
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip) return;
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlayBGM(AudioClip clip, float volume)
    {
        if (clip == null) return;
        bgmSource.clip = clip;
        bgmSource.volume = volume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, volume);
    }

    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    public void PlaySFXPanned(AudioClip clip, float pan, float volume = 1f)
    {
        if (clip != null)
        {
            sfxSource2.panStereo = pan;
            sfxSource2.PlayOneShot(clip, volume);
        }
    }

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        sfxSource2.volume = volume;
    }

    public AudioClip GetProceduralClip(string name)
    {
        if (procAudio == null) return null;
        return procAudio.GetType().GetField(name)?.GetValue(procAudio) as AudioClip;
    }

    public void PlayProcedural(string name, float volume = 1f)
    {
        var clip = GetProceduralClip(name);
        if (clip != null) PlaySFX(clip, volume);
    }

    public void PlayProceduralAtPoint(string name, Vector3 position, float volume = 1f)
    {
        var clip = GetProceduralClip(name);
        if (clip != null) PlaySFXAtPoint(clip, position, volume);
    }
}
