using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class KatanaAura : MonoBehaviour
{
    [Header("Aura Settings")]
    public Material auraMaterial;
    public float auraIntensity = 3f;
    public float pulseSpeed = 2f;
    public bool animateOnSwing = true;

    [Header("Trail Settings")]
    public TrailRenderer trailRenderer;
    public Gradient swingTrailGradient;
    public Gradient idleTrailGradient;

    private MaterialPropertyBlock propBlock;
    private float swingProgress;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        if (auraMaterial == null)
        {
            auraMaterial = GetComponent<Renderer>().material;
        }
    }

    void Update()
    {
        if (animateOnSwing)
        {
            UpdateSwingAura();
        }
        else
        {
            UpdateIdleAura();
        }
    }

    void UpdateIdleAura()
    {
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        float intensity = auraIntensity * (0.3f + pulse * 0.4f);
        
        propBlock.SetFloat("_Intensity", intensity);
        propBlock.SetFloat("_PulseSpeed", pulseSpeed);
        GetComponent<Renderer>().SetPropertyBlock(propBlock);

        if (trailRenderer != null && !trailRenderer.emitting)
        {
            trailRenderer.colorGradient = idleTrailGradient;
            trailRenderer.emitting = true;
        }
    }

    void UpdateSwingAura()
    {
        float pulse = Mathf.Sin(Time.time * pulseSpeed * 3f) * 0.5f + 0.5f;
        float intensity = auraIntensity * (0.5f + pulse * 1.5f + swingProgress * 2f);
        
        propBlock.SetFloat("_Intensity", intensity);
        propBlock.SetFloat("_PulseSpeed", pulseSpeed * 3f);
        propBlock.SetFloat("_Distortion", 0.15f + swingProgress * 0.3f);
        GetComponent<Renderer>().SetPropertyBlock(propBlock);

        if (trailRenderer != null)
        {
            trailRenderer.colorGradient = swingTrailGradient;
            trailRenderer.emitting = true;
            trailRenderer.time = Mathf.Lerp(0.1f, 0.4f, swingProgress);
        }
    }

    public void SetSwingState(bool swinging, float progress = 0f)
    {
        swingProgress = progress;
        
        if (!swinging && trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }
    }

    public void SetSwingProgress(float progress)
    {
        swingProgress = Mathf.Clamp01(progress);
    }
}