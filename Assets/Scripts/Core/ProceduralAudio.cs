using UnityEngine;
using System;

public class ProceduralAudio : MonoBehaviour
{
    public static ProceduralAudio Instance { get; private set; }

    public AudioClip katanaSwing;
    public AudioClip katanaHit;
    public AudioClip katanaKill;
    public AudioClip grappleShoot;
    public AudioClip grappleConnect;
    public AudioClip grappleReel;
    public AudioClip grappleRelease;
    public AudioClip dashSound;
    public AudioClip slideSound;
    public AudioClip wallJumpSound;
    public AudioClip landSound;
    public AudioClip enemyHit;
    public AudioClip enemyDeath;
    public AudioClip gibSound;
    public AudioClip enemyShoot;
    public AudioClip playerHit;
    public AudioClip jumpSound;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        GenerateAllClips();
    }

    void GenerateAllClips()
    {
        katanaSwing = GenerateTone(440f, 0.1f, 0.3f, AnimationCurve.EaseInOut(0, 1, 1, 0), 0.5f);
        katanaHit = GenerateNoise(0.08f, 0.5f, AnimationCurve.EaseInOut(0, 1, 1, 0));
        katanaKill = GenerateChord(new float[] { 440f, 554f, 659f }, 0.3f, 0.4f);
        grappleShoot = GenerateTone(220f, 0.15f, 0.2f, AnimationCurve.EaseInOut(0, 0.5f, 1, 0), 0.3f);
        grappleConnect = GenerateTone(660f, 0.1f, 0.4f, AnimationCurve.EaseInOut(0, 1, 1, 0.5f), 0.4f);
        grappleReel = GenerateTone(150f, 0.5f, 0.1f, AnimationCurve.Linear(0, 1, 1, 1), 0.2f);
        grappleRelease = GenerateTone(330f, 0.2f, 0.3f, AnimationCurve.EaseInOut(0, 1, 1, 0), 0.3f);
        dashSound = GenerateNoise(0.1f, 0.8f, AnimationCurve.EaseInOut(0, 1, 1, 0));
        slideSound = GenerateNoise(0.3f, 0.2f, AnimationCurve.Linear(0, 1, 1, 1));
        wallJumpSound = GenerateTone(500f, 0.1f, 0.3f, AnimationCurve.EaseInOut(0, 1, 1, 0), 0.4f);
        landSound = GenerateTone(120f, 0.15f, 0.2f, AnimationCurve.EaseInOut(0, 1, 1, 0), 0.3f);
        enemyHit = GenerateTone(300f, 0.08f, 0.3f, AnimationCurve.EaseInOut(0, 1, 1, 0), 0.4f);
        enemyDeath = GenerateChord(new float[] { 200f, 150f, 100f }, 0.5f, 0.4f);
        gibSound = GenerateNoise(0.2f, 0.5f, AnimationCurve.EaseInOut(0, 1, 1, 0));
        enemyShoot = GenerateTone(800f, 0.05f, 0.2f, AnimationCurve.EaseInOut(0, 1, 1, 0), 0.3f);
        playerHit = GenerateNoise(0.15f, 0.6f, AnimationCurve.EaseInOut(0, 1, 1, 0));
        jumpSound = GenerateTone(350f, 0.08f, 0.25f, AnimationCurve.EaseInOut(0, 1, 1, 0), 0.3f);
    }

    AudioClip GenerateTone(float frequency, float duration, float volume, AnimationCurve envelope, float harmonicMix = 0f)
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int samples = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = envelope.Evaluate(t / duration);
            float phase = 2f * Mathf.PI * frequency * t;
            float sample = Mathf.Sin(phase) * env;
            
            if (harmonicMix > 0)
            {
                sample += Mathf.Sin(phase * 2) * env * harmonicMix * 0.5f;
                sample += Mathf.Sin(phase * 3) * env * harmonicMix * 0.25f;
            }
            
            data[i] = sample * volume;
        }

        var clip = AudioClip.Create("Tone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip GenerateNoise(float duration, float volume, AnimationCurve envelope)
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int samples = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[samples];

        System.Random rand = new System.Random(42);
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = envelope.Evaluate(t / duration);
            float noise = (float)(rand.NextDouble() * 2 - 1);
            data[i] = noise * env * volume;
        }

        var clip = AudioClip.Create("Noise", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip GenerateChord(float[] frequencies, float duration, float volume)
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int samples = Mathf.RoundToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = 1f - t / duration;
            float sample = 0f;
            foreach (float freq in frequencies)
            {
                sample += Mathf.Sin(2f * Mathf.PI * freq * t);
            }
            sample /= frequencies.Length;
            data[i] = sample * env * volume;
        }

        var clip = AudioClip.Create("Chord", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}