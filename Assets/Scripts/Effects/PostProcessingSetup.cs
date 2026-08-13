using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class PostProcessingSetup : MonoBehaviour
{
    [Header("Volume Profile")]
    public VolumeProfile volumeProfile;

    [Header("Bloom")]
    public bool enableBloom = true;
    public float bloomIntensity = 1.2f;
    public float bloomThreshold = 0.8f;
    public float bloomScatter = 0.9f;
    public Color bloomTint = new Color(1f, 0.95f, 0.85f);

    [Header("Color Adjustments")]
    public bool enableColorGrading = true;
    public float postExposure = 0.5f;
    public float contrast = 25f;
    public float saturation = 15f;
    public Color colorFilter = new Color(1f, 0.98f, 0.92f);

    [Header("Vignette")]
    public bool enableVignette = true;
    public float vignetteIntensity = 0.45f;
    public float vignetteSmoothness = 0.5f;
    public Color vignetteColor = new Color(0.05f, 0.02f, 0.02f);

    [Header("Chromatic Aberration")]
    public bool enableChromaticAberration = true;
    public float chromaticIntensity = 0.2f;

    [Header("Film Grain")]
    public bool enableFilmGrain = true;
    public float grainIntensity = 0.15f;

    [Header("Lens Distortion")]
    public bool enableLensDistortion = true;
    public float lensDistortion = -0.15f;
    public float lensScale = 0.9f;

    private Volume volume;

    void Awake()
    {
        SetupPostProcessing();
    }

    void SetupPostProcessing()
    {
        volume = GetComponent<Volume>();
        if (volume == null)
        {
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100;
        }

        if (volumeProfile == null)
        {
            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = volumeProfile;
        }

        SetupBloom();
        SetupColorAdjustments();
        SetupVignette();
        SetupChromaticAberration();
        SetupFilmGrain();
        SetupLensDistortion();
    }

    void SetupBloom()
    {
        if (!enableBloom) return;

        var bloom = volumeProfile.TryGet<Bloom>(out var b) ? b : volumeProfile.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(bloomScatter);
        bloom.tint.Override(bloomTint);
        bloom.highQualityFiltering.Override(true);
    }

    void SetupColorAdjustments()
    {
        if (!enableColorGrading) return;

        var colorAdj = volumeProfile.TryGet<ColorAdjustments>(out var ca) ? ca : volumeProfile.Add<ColorAdjustments>(true);
        colorAdj.active = true;
        colorAdj.postExposure.Override(postExposure);
        colorAdj.contrast.Override(contrast);
        colorAdj.saturation.Override(saturation);
        colorAdj.colorFilter.Override(colorFilter);
    }

    void SetupVignette()
    {
        if (!enableVignette) return;

        var vignette = volumeProfile.TryGet<Vignette>(out var v) ? v : volumeProfile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(vignetteSmoothness);
        vignette.color.Override(vignetteColor);
        vignette.rounded.Override(true);
        vignette.center.Override(new Vector2(0.5f, 0.5f));
    }

    void SetupChromaticAberration()
    {
        if (!enableChromaticAberration) return;

        var ca = volumeProfile.TryGet<ChromaticAberration>(out var c) ? c : volumeProfile.Add<ChromaticAberration>(true);
        ca.active = true;
        ca.intensity.Override(chromaticIntensity);
    }

    void SetupFilmGrain()
    {
        if (!enableFilmGrain) return;

        var grain = volumeProfile.TryGet<FilmGrain>(out var g) ? g : volumeProfile.Add<FilmGrain>(true);
        grain.active = true;
        grain.intensity.Override(grainIntensity);
    }

    void SetupLensDistortion()
    {
        if (!enableLensDistortion) return;

        var lens = volumeProfile.TryGet<LensDistortion>(out var l) ? l : volumeProfile.Add<LensDistortion>(true);
        lens.active = true;
        lens.intensity.Override(lensDistortion);
        lens.scale.Override(lensScale);
        lens.center.Override(Vector2.zero);
        lens.xMultiplier.Override(1f);
        lens.yMultiplier.Override(1f);
    }

    public void SetIntensity(float bloomMult, float vignetteMult, float chromaticMult)
    {
        if (volumeProfile.TryGet<Bloom>(out var bloom))
            bloom.intensity.Override(bloomIntensity * bloomMult);
        
        if (volumeProfile.TryGet<Vignette>(out var vignette))
            vignette.intensity.Override(vignetteIntensity * vignetteMult);
        
        if (volumeProfile.TryGet<ChromaticAberration>(out var ca))
            ca.intensity.Override(chromaticIntensity * chromaticMult);
    }
}