using UnityEngine;
using System.Collections;

public class EnemyDeathEffect : MonoBehaviour
{
    [Header("Dissolve Settings")]
    public float dissolveDuration = 1.5f;
    public AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public Material dissolveMaterial;

    [Header("Particles")]
    public GameObject deathParticles;
    public GameObject bloodMist;
    public GameObject gibs;
    public int gibCount = 5;

    [Header("Screen Effects")]
    public float cameraShakeIntensity = 0.4f;
    public float cameraShakeDuration = 0.3f;
    public float hitPauseDuration = 0.08f;

    private Renderer[] renderers;
    private Material[] originalMaterials;
    private MaterialPropertyBlock propBlock;
    private bool isDissolving;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        propBlock = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }
    }

    public void TriggerDeath(Vector3 hitDirection, float force = 5f)
    {
        if (isDissolving) return;
        StartCoroutine(DeathSequence(hitDirection, force));
    }

    IEnumerator DeathSequence(Vector3 hitDirection, float force)
    {
        isDissolving = true;

        HitPause.Instance?.Pause(0.15f, 0.01f);
        CameraShake.Instance?.Shake(cameraShakeIntensity * 2f, cameraShakeDuration * 1.5f, 30f, true);
        Time.timeScale = 0.1f;
        StartCoroutine(ResetTimeScale(0.1f));

        if (deathParticles != null)
        {
            var p = Instantiate(deathParticles, transform.position + Vector3.up, Quaternion.LookRotation(-hitDirection));
            var ps = p.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSizeMultiplier = 3f;
                main.startSpeedMultiplier = 2f;
            }
            Destroy(p, 5f);
        }

        if (bloodMist != null)
        {
            var b = Instantiate(bloodMist, transform.position + Vector3.up, Quaternion.identity);
            var ps = b.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSizeMultiplier = 4f;
            }
            Destroy(b, 4f);
        }

        var shockwave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shockwave.transform.position = transform.position + Vector3.up;
        shockwave.transform.localScale = Vector3.one * 0.5f;
        var shockRenderer = shockwave.GetComponent<Renderer>();
        var shockMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        shockMat.EnableKeyword("_EMISSION");
        shockMat.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.1f) * 10f);
        shockRenderer.material = shockMat;
        Object.DestroyImmediate(shockwave.GetComponent<Collider>());
        StartCoroutine(ExpandShockwave(shockwave, 8f, 0.4f));

        if (gibs != null)
        {
            for (int i = 0; i < gibCount * 2; i++)
            {
                Vector3 spawnPos = transform.position + Vector3.up * Random.Range(0.5f, 1.5f) + Random.insideUnitSphere * 0.3f;
                var gib = Instantiate(gibs, spawnPos, Random.rotation);
                var rb = gib.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce((-hitDirection + Vector3.up + Random.insideUnitSphere * 0.5f) * force * 2f, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * 1000f, ForceMode.Impulse);
                }
                Destroy(gib, 8f);
            }
        }

        AudioManager.Instance?.PlayProcedural("enemyDeath");
        AudioManager.Instance?.PlayProcedural("gibSound", 0.7f);
        AudioManager.Instance?.PlayProceduralAtPoint("enemyHit", transform.position, 1f);

        SetupDissolveMaterials();

        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            float t = dissolveCurve.Evaluate(elapsed / dissolveDuration);
            SetDissolveAmount(t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetDissolveAmount(1f);
        float waitElapsed = 0f;
        while (waitElapsed < 0.3f)
        {
            waitElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    IEnumerator ResetTimeScale(float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 1f;
    }

    IEnumerator ExpandShockwave(GameObject shockwave, float maxSize, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = shockwave.transform.localScale;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            shockwave.transform.localScale = Vector3.Lerp(startScale, Vector3.one * maxSize, t);
            var renderer = shockwave.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                float alpha = 1f - t;
                renderer.material.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.1f) * 10f * alpha);
            }
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Destroy(shockwave);
    }

    void SetupDissolveMaterials()
    {
        if (dissolveMaterial == null)
        {
            dissolveMaterial = new Material(Shader.Find("Custom/Dissolve"));
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = dissolveMaterial;
            renderers[i].material.CopyPropertiesFromMaterial(originalMaterials[i]);
        }
    }

    void SetDissolveAmount(float amount)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material != null && renderers[i].material.HasProperty("_DissolveAmount"))
            {
                renderers[i].material.SetFloat("_DissolveAmount", amount);
            }
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
            {
                renderers[i].material = originalMaterials[i];
            }
        }
    }
}