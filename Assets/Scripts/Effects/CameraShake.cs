using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Default Settings")]
    public float defaultIntensity = 0.5f;
    public float defaultDuration = 0.2f;
    public float defaultFrequency = 25f;

    private Transform camTransform;
    private Vector3 originalPos;
    private Vector3 originalRot;
    private float shakeTimer;
    private float shakeDuration;
    private float shakeIntensity;
    private float shakeFrequency;
    private float shakeDecay;
    private bool isShaking;
    private bool isRotationalShake;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        camTransform = Camera.main?.transform;
    }

    void LateUpdate()
    {
        if (camTransform == null) return;

        if (isShaking)
        {
            shakeTimer += Time.unscaledDeltaTime;
            float t = shakeTimer / shakeDuration;

            if (t >= 1f)
            {
                StopShake();
                return;
            }

            float decay = 1f - t * shakeDecay;
            float noise = Mathf.PerlinNoise(Time.unscaledTime * shakeFrequency, 0f) * 2f - 1f;
            float noise2 = Mathf.PerlinNoise(0f, Time.unscaledTime * shakeFrequency) * 2f - 1f;
            float noise3 = Mathf.PerlinNoise(Time.unscaledTime * shakeFrequency, Time.unscaledTime * shakeFrequency) * 2f - 1f;

            Vector3 offset = new Vector3(noise, noise2, noise3) * shakeIntensity * decay;

            if (isRotationalShake)
            {
                camTransform.localRotation = Quaternion.Euler(originalRot + offset * 5f);
            }
            else
            {
                camTransform.localPosition = originalPos + offset;
            }
        }
    }

    public void Shake(float intensity, float duration, float frequency = 25f, bool rotational = false, float decay = 1f)
    {
        if (camTransform == null) camTransform = Camera.main?.transform;
        if (camTransform == null) return;

        if (!isShaking)
        {
            originalPos = camTransform.localPosition;
            originalRot = camTransform.localEulerAngles;
        }

        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeFrequency = frequency;
        shakeDecay = decay;
        isRotationalShake = rotational;
        shakeTimer = 0f;
        isShaking = true;
    }

    public void StopShake()
    {
        if (camTransform == null) return;
        isShaking = false;
        camTransform.localPosition = originalPos;
        camTransform.localRotation = Quaternion.Euler(originalRot);
    }

    public void ExplosionShake(float distance, float maxDistance = 20f, float maxIntensity = 1f)
    {
        float intensity = Mathf.Lerp(maxIntensity, 0f, distance / maxDistance);
        if (intensity > 0.05f)
            Shake(intensity, 0.5f, 15f, true, 0.8f);
    }

    public void DirectionalShake(Vector3 direction, float intensity, float duration)
    {
        if (camTransform == null) return;
        
        if (!isShaking)
            originalPos = camTransform.localPosition;
        
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeFrequency = 30f;
        shakeDecay = 1f;
        isRotationalShake = false;
        shakeTimer = 0f;
        isShaking = true;
    }
}