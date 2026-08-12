using UnityEngine;
using System.Collections;

public class MovementEffects : MonoBehaviour
{
    [Header("Dash")]
    public ParticleSystem dashParticles;
    public TrailRenderer dashTrail;
    public float dashTrailTime = 0.15f;
    public float dashShakeIntensity = 0.3f;
    public float dashShakeDuration = 0.1f;

    [Header("Slide")]
    public ParticleSystem slideParticles;
    public float slideDustInterval = 0.1f;
    private float slideDustTimer;

    [Header("Wall Jump")]
    public ParticleSystem wallJumpParticles;
    public float wallJumpShakeIntensity = 0.25f;
    public float wallJumpShakeDuration = 0.1f;

    [Header("Landing")]
    public ParticleSystem landParticles;
    public float hardLandThreshold = 15f;
    public float hardLandShake = 0.4f;

    [Header("References")]
    public PlayerController player;
    public CharacterController controller;

    private bool wasGrounded;
    private bool wasSliding;
    private bool wasDashing;
    private bool wasWallJumpLocked;
    private Vector3 lastVelocity;

    void Awake()
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (controller == null) controller = GetComponent<CharacterController>();

        SetupDashTrail();
    }

    void SetupDashTrail()
    {
        if (dashTrail == null) return;
        dashTrail.time = dashTrailTime;
        dashTrail.minVertexDistance = 0.1f;
        dashTrail.emitting = false;
        dashTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        dashTrail.receiveShadows = false;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.cyan * 3f);
        dashTrail.material = mat;
    }

    void Update()
    {
        if (player == null) return;

        bool isGrounded = controller.isGrounded;
        bool isSliding = player.IsSliding();
        bool isDashing = GetIsDashing();

        HandleDash(isDashing);
        HandleSlide(isSliding, isGrounded);
        HandleLanding(isGrounded);
        HandleWallJump();

        wasGrounded = isGrounded;
        wasSliding = isSliding;
        wasDashing = isDashing;
        lastVelocity = controller.velocity;
    }

    void HandleDash(bool isDashing)
    {
        if (isDashing && !wasDashing)
        {
            OnDashStart();
        }
        else if (!isDashing && wasDashing)
        {
            OnDashEnd();
        }

        if (isDashing && dashTrail != null && !dashTrail.emitting)
        {
            dashTrail.emitting = true;
        }
    }

    void OnDashStart()
    {
        CameraShake.Instance?.Shake(dashShakeIntensity * 2f, dashShakeDuration, 50f, true);
        HitPause.Instance?.Pause(0.03f, 0.1f);
        AudioManager.Instance?.PlayProcedural("dashSound");
        Time.timeScale = 0.3f;
        StartCoroutine(ResetTimeScale(0.05f));

        if (dashParticles != null)
        {
            var p = Instantiate(dashParticles, transform.position, Quaternion.LookRotation(-transform.forward));
            p.transform.parent = transform;
            var ps = p.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSizeMultiplier = 2f;
            }
            Destroy(p.gameObject, 1f);
        }

        if (dashTrail != null)
        {
            dashTrail.emitting = true;
            dashTrail.time = 0.3f;
        }
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

    void OnDashEnd()
    {
        if (dashTrail != null)
        {
            dashTrail.emitting = false;
            dashTrail.time = 0.15f;
        }
    }

    void HandleSlide(bool isSliding, bool isGrounded)
    {
        if (isSliding && !wasSliding)
        {
            OnSlideStart();
        }
        else if (!isSliding && wasSliding)
        {
            OnSlideEnd();
        }

        if (isSliding && isGrounded && slideParticles != null)
        {
            slideDustTimer += Time.deltaTime;
            if (slideDustTimer >= slideDustInterval)
            {
                slideDustTimer = 0f;
                var p = Instantiate(slideParticles, transform.position + Vector3.up * 0.2f, Quaternion.Euler(-90, 0, 0));
                p.transform.parent = transform;
                var ps = p.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startSpeedMultiplier = 2f;
                    main.startSizeMultiplier = 1.5f;
                }
                Destroy(p.gameObject, 1f);
            }
        }
    }

    void OnSlideStart()
    {
        AudioManager.Instance?.PlayProcedural("slideSound");
        CameraShake.Instance?.Shake(0.2f, 0.1f, 30f);
    }

    void OnSlideEnd()
    {
    }

    void HandleLanding(bool isGrounded)
    {
        if (isGrounded && !wasGrounded)
        {
            float fallSpeed = -lastVelocity.y;
            if (fallSpeed > hardLandThreshold)
            {
                HardLand(fallSpeed);
            }
            else
            {
                SoftLand();
            }
        }
    }

    void SoftLand()
    {
        if (landParticles != null)
        {
            var p = Instantiate(landParticles, transform.position, Quaternion.Euler(-90, 0, 0));
            Destroy(p, 1f);
        }
        AudioManager.Instance?.PlayProcedural("landSound", 0.5f);
        CameraShake.Instance?.Shake(0.15f, 0.08f, 20f);
    }

    void HardLand(float fallSpeed)
    {
        float intensity = Mathf.Lerp(hardLandShake * 0.5f, hardLandShake, Mathf.InverseLerp(hardLandThreshold, 40f, fallSpeed));
        CameraShake.Instance?.Shake(intensity, 0.3f, 20f, true);
        HitPause.Instance?.Pause(Mathf.Lerp(0.05f, 0.15f, Mathf.InverseLerp(hardLandThreshold, 40f, fallSpeed)), 0.05f);

        if (landParticles != null)
        {
            var p = Instantiate(landParticles, transform.position, Quaternion.Euler(-90, 0, 0));
            var ps = p.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSizeMultiplier = Mathf.Lerp(2f, 5f, Mathf.InverseLerp(hardLandThreshold, 40f, fallSpeed));
                main.startSpeedMultiplier = Mathf.Lerp(1f, 3f, Mathf.InverseLerp(hardLandThreshold, 40f, fallSpeed));
            }
            Destroy(p, 1f);
        }

        var shockwave = Instantiate(landParticles, transform.position, Quaternion.Euler(-90, 0, 0));
        var shockPs = shockwave.GetComponent<ParticleSystem>();
        if (shockPs != null)
        {
            var shockMain = shockPs.main;
            shockMain.startSizeMultiplier = Mathf.Lerp(3f, 8f, Mathf.InverseLerp(hardLandThreshold, 40f, fallSpeed));
            shockMain.startSpeedMultiplier = 0.5f;
        }
        Destroy(shockwave, 1.5f);

        AudioManager.Instance?.PlayProcedural("landSound", Mathf.Lerp(0.7f, 1.2f, Mathf.InverseLerp(hardLandThreshold, 40f, fallSpeed)));
        AudioManager.Instance?.PlayProceduralAtPoint("enemyDeath", transform.position, 0.3f);
    }

    void HandleWallJump()
    {
        bool wallJumpLocked = player.IsWallJumpLocked();
        bool isWallSliding = player.IsWallSliding();

        if (wallJumpLocked && !GetWasWallJumpLocked())
        {
            OnWallJump();
        }
        SetWasWallJumpLocked(wallJumpLocked);
    }

    void OnWallJump()
    {
        CameraShake.Instance?.Shake(wallJumpShakeIntensity * 2f, wallJumpShakeDuration, 40f, true);
        HitPause.Instance?.Pause(0.04f, 0.15f);
        AudioManager.Instance?.PlayProcedural("wallJumpSound");
        AudioManager.Instance?.PlayProceduralAtPoint("dashSound", transform.position, 0.5f);

        if (wallJumpParticles != null)
        {
            Vector3 wallNormal = player.GetWallNormal();
            
            var p = Instantiate(wallJumpParticles, transform.position, Quaternion.LookRotation(wallNormal));
            var ps = p.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startSizeMultiplier = 3f;
                main.startSpeedMultiplier = 2f;
            }
            Destroy(p, 1.5f);
        }

        var impactParticles = Instantiate(wallJumpParticles, transform.position, Quaternion.LookRotation(-transform.forward));
        var impactMain = impactParticles.main;
        impactMain.startSizeMultiplier = 2f;
        impactMain.startSpeedMultiplier = 5f;
        Destroy(impactParticles, 1f);
    }

    bool GetWasWallJumpLocked()
    {
        return wasWallJumpLocked;
    }

    void SetWasWallJumpLocked(bool value)
    {
        wasWallJumpLocked = value;
    }

    bool GetIsDashing()
    {
        return player.IsDashing();
    }
}