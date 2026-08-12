using UnityEngine;
using System;

[RequireComponent(typeof(TrailRenderer))]
public class KatanaTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    public float minVelocity = 5f;
    public float maxVelocity = 20f;
    public AnimationCurve widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
    public Gradient colorGradient;
    public Material trailMaterial;

    [Header("Hit Effect")]
    public GameObject hitFlashPrefab;
    public float hitFreezeTime = 0.05f;

    private TrailRenderer trail;
    private Vector3 lastPosition;
    private KatanaSwingDetector swingDetector;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        SetupTrail();
        swingDetector = GetComponentInParent<KatanaSwingDetector>();
        lastPosition = transform.position;
    }

    void SetupTrail()
    {
        trail.time = 0.15f;
        trail.minVertexDistance = 0.02f;
        trail.widthCurve = widthCurve;
        trail.colorGradient = colorGradient;
        trail.material = trailMaterial;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;
    }

    void LateUpdate()
    {
        float velocity = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        bool isSwinging = swingDetector != null && swingDetector.IsSwinging;

        if (isSwinging && velocity > minVelocity)
        {
            if (!trail.emitting) trail.emitting = true;
            trail.time = Mathf.Lerp(0.1f, 0.25f, Mathf.InverseLerp(minVelocity, maxVelocity, velocity));
        }
        else if (!isSwinging)
        {
            trail.emitting = false;
        }
    }

    public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (hitFlashPrefab != null)
        {
            var flash = Instantiate(hitFlashPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
            flash.transform.localScale = Vector3.one * 0.5f;
            Destroy(flash, 0.1f);
        }

        AudioManager.Instance?.PlayProceduralAtPoint("katanaHit", hitPoint);
        GameManager.Instance?.HitStop(hitFreezeTime);
        CameraShake.Instance?.Shake(0.3f, 0.1f, 20f);
    }
}

public class KatanaSwingDetector : MonoBehaviour
{
    public bool IsSwinging { get; private set; }
    public event System.Action<Vector3, Vector3> OnHit;

    private PlayerController player;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    void Update()
    {
        IsSwinging = player != null && IsInSwingPhase();
    }

    bool IsInSwingPhase()
    {
        return player != null && player.IsSwinging();
    }

    public void TriggerHit(Vector3 point, Vector3 normal)
    {
        OnHit?.Invoke(point, normal);
    }
}