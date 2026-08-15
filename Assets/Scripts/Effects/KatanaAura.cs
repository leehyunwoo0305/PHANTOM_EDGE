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
    private bool isSwinging;
    private Renderer auraRenderer;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        auraRenderer = GetComponent<Renderer>();
        if (auraMaterial == null)
        {
            auraMaterial = auraRenderer.material;
        }
        SetAuraVisible(false);
        gameObject.SetActive(false);
        if (trailRenderer != null) trailRenderer.emitting = false;
    }

    void Update()
    {
        if (isSwinging)
        {
            UpdateSwingAura();
        }
        else
        {
            SetAuraVisible(false);
            if (trailRenderer != null && trailRenderer.emitting)
                trailRenderer.emitting = false;
        }
    }

    void UpdateSwingAura()
    {
        float pulse = Mathf.Sin(Time.time * pulseSpeed * 3f) * 0.5f + 0.5f;
        float intensity = auraIntensity * (0.5f + pulse * 1.5f + swingProgress * 2f);
        
        propBlock.SetFloat("_Intensity", intensity);
        propBlock.SetFloat("_PulseSpeed", pulseSpeed * 3f);
        propBlock.SetFloat("_Distortion", 0.15f + swingProgress * 0.3f);
        auraRenderer.SetPropertyBlock(propBlock);
        SetAuraVisible(true);

        if (trailRenderer != null)
        {
            trailRenderer.colorGradient = swingTrailGradient;
            trailRenderer.emitting = true;
            trailRenderer.time = Mathf.Lerp(0.1f, 0.4f, swingProgress);
        }
    }

    void SetAuraVisible(bool visible)
    {
        if (auraRenderer != null)
        {
            auraRenderer.enabled = visible;
            gameObject.SetActive(visible);
        }
    }

    public void SetSwingState(bool swinging, float progress = 0f)
    {
        isSwinging = swinging;
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