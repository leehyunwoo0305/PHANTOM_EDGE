using UnityEngine;
using System.Collections.Generic;

public class GrapplePolish : MonoBehaviour
{
    [Header("Line Settings")]
    public LineRenderer lineRenderer;
    public Gradient connectedGradient;
    public Gradient connectingGradient;
    public AnimationCurve widthCurve = AnimationCurve.EaseInOut(0, 0.05f, 1, 0.02f);
    public float maxWidth = 0.1f;

    [Header("Particles")]
    public ParticleSystem grappleParticles;
    public ParticleSystem impactParticles;
    public ParticleSystem trailParticles;

    [Header("Effects")]
    public float reelInSpeed = 30f;
    public float swingForce = 20f;
    public float screenShakeOnConnect = 0.2f;

    private GrapplingHook grapple;
    private bool wasGrappled;
    private float connectTime;

    void Awake()
    {
        grapple = GetComponent<GrapplingHook>();

        SetupLineRenderer();
        SetupParticles();
    }

    void SetupLineRenderer()
    {
        if (lineRenderer == null) return;

        lineRenderer.widthCurve = widthCurve;
        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineRenderer.material.EnableKeyword("_EMISSION");
    }

    void SetupParticles()
    {
        if (grappleParticles != null)
        {
            var main = grappleParticles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            grappleParticles.Stop();
        }
        if (trailParticles != null)
        {
            var main = trailParticles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            trailParticles.Stop();
        }
    }

    void Update()
    {
        bool isGrappled = grapple != null && grapple.IsGrappled();

        if (isGrappled && !wasGrappled)
        {
            OnGrappleConnect();
        }
        else if (!isGrappled && wasGrappled)
        {
            OnGrappleRelease();
        }

        if (isGrappled)
        {
            UpdateLineVisuals();
            UpdateParticles();
            PlayReelSound();
        }

        wasGrappled = isGrappled;
    }

    void OnGrappleConnect()
    {
        connectTime = Time.time;
        CameraShake.Instance?.Shake(screenShakeOnConnect, 0.15f, 30f);
        AudioManager.Instance?.PlayProcedural("grappleConnect");

        if (impactParticles != null && grapple != null)
        {
            var pos = grapple.GetGrapplePoint();
            var p = Instantiate(impactParticles, pos, Quaternion.identity);
            Destroy(p, 2f);
        }

        if (grappleParticles != null)
        {
            grappleParticles.transform.position = grapple.GetGrapplePoint();
            grappleParticles.Play();
        }

        if (trailParticles != null)
        {
            trailParticles.Play();
        }
    }

    void OnGrappleRelease()
    {
        AudioManager.Instance?.PlayProcedural("grappleRelease");

        if (grappleParticles != null) grappleParticles.Stop();
        if (trailParticles != null) trailParticles.Stop();
    }

    void UpdateLineVisuals()
    {
        if (lineRenderer == null || grapple == null) return;

        float t = Mathf.Clamp01((Time.time - connectTime) / 0.5f);
        
        Gradient lerpedGradient = new Gradient();
        var colorKeys1 = connectingGradient.colorKeys;
        var colorKeys2 = connectedGradient.colorKeys;
        var alphaKeys1 = connectingGradient.alphaKeys;
        var alphaKeys2 = connectedGradient.alphaKeys;
        
        int keyCount = Mathf.Max(colorKeys1.Length, colorKeys2.Length);
        if (keyCount == 0) keyCount = 2;
        
        GradientColorKey[] colorKeys = new GradientColorKey[keyCount];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[keyCount];
        
        for (int i = 0; i < keyCount; i++)
        {
            Color c1 = i < colorKeys1.Length ? colorKeys1[i].color : colorKeys1[colorKeys1.Length - 1].color;
            Color c2 = i < colorKeys2.Length ? colorKeys2[i].color : colorKeys2[colorKeys2.Length - 1].color;
            float time1 = i < colorKeys1.Length ? colorKeys1[i].time : (float)i / (keyCount - 1);
            float time2 = i < colorKeys2.Length ? colorKeys2[i].time : (float)i / (keyCount - 1);
            
            colorKeys[i] = new GradientColorKey(
                Color.Lerp(c1, c2, t),
                Mathf.Lerp(time1, time2, t)
            );
        }
        
        int alphaCount = Mathf.Max(alphaKeys1.Length, alphaKeys2.Length);
        if (alphaCount == 0) alphaCount = 2;
        
        for (int i = 0; i < alphaCount; i++)
        {
            float a1 = i < alphaKeys1.Length ? alphaKeys1[i].alpha : alphaKeys1[alphaKeys1.Length - 1].alpha;
            float a2 = i < alphaKeys2.Length ? alphaKeys2[i].alpha : alphaKeys2[alphaKeys2.Length - 1].alpha;
            float time1 = i < alphaKeys1.Length ? alphaKeys1[i].time : (float)i / (alphaCount - 1);
            float time2 = i < alphaKeys2.Length ? alphaKeys2[i].time : (float)i / (alphaCount - 1);
            
            alphaKeys[i] = new GradientAlphaKey(
                Mathf.Lerp(a1, a2, t),
                Mathf.Lerp(time1, time2, t)
            );
        }
        lerpedGradient.SetKeys(colorKeys, alphaKeys);
        lineRenderer.colorGradient = lerpedGradient;

        Vector3 grapplePoint = grapple.GetGrapplePoint();
        lineRenderer.SetPosition(0, Camera.main.transform.position);
        lineRenderer.SetPosition(1, grapplePoint);

        float dist = Vector3.Distance(transform.position, grapplePoint);
        lineRenderer.widthMultiplier = Mathf.Lerp(maxWidth, 0.01f, Mathf.InverseLerp(5f, 50f, dist));

        int segments = Mathf.Clamp(Mathf.RoundToInt(dist * 0.5f), 10, 50);
        lineRenderer.positionCount = segments + 2;
        
        for (int i = 0; i <= segments; i++)
        {
            float segmentT = i / (float)segments;
            Vector3 pos = Vector3.Lerp(Camera.main.transform.position, grapplePoint, segmentT);
            
            float wave = Mathf.Sin(Time.time * 15f + segmentT * 20f) * 0.15f * (1f - segmentT);
            float wave2 = Mathf.Cos(Time.time * 12f + segmentT * 15f) * 0.1f * (1f - segmentT);
            
            Vector3 perpendicular = Vector3.Cross((grapplePoint - Camera.main.transform.position).normalized, Camera.main.transform.up).normalized;
            Vector3 perpendicular2 = Vector3.Cross((grapplePoint - Camera.main.transform.position).normalized, perpendicular).normalized;
            
            pos += perpendicular * wave + perpendicular2 * wave2;
            
            lineRenderer.SetPosition(i + 1, pos);
        }

        if (lineRenderer.material != null)
        {
            float pulse = Mathf.Sin(Time.time * 20f) * 0.5f + 0.5f;
            lineRenderer.material.SetColor("_EmissionColor", Color.Lerp(Color.red, Color.yellow, pulse) * 5f * (1f + pulse));
        }
    }

    void UpdateParticles()
    {
        if (trailParticles != null)
        {
            trailParticles.transform.position = transform.position;
            var main = trailParticles.main;
            main.startSizeMultiplier = Mathf.Lerp(0.5f, 2f, Mathf.Abs(Mathf.Sin(Time.time * 5f)));
        }

        if (grappleParticles != null)
        {
            Vector3 grapplePoint = grapple.GetGrapplePoint();
            grappleParticles.transform.position = grapplePoint;
            var main = grappleParticles.main;
            main.startSizeMultiplier = Mathf.Lerp(0.8f, 1.5f, Mathf.Abs(Mathf.Sin(Time.time * 8f)));
        }
    }

    void PlayReelSound()
    {
        AudioManager.Instance?.PlayProcedural("grappleReel", 0.3f);
    }

    public Vector3 GetGrapplePoint()
    {
        return grapple != null ? grapple.GetGrapplePoint() : transform.position;
    }
}