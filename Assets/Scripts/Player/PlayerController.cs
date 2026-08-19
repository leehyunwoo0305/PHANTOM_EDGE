using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float sprintMultiplier = 1.6f;
    public float acceleration = 50f;
    public float airAcceleration = 15f;
    public float jumpForce = 12f;
    public float gravity = -30f;
    public float maxFallSpeed = -40f;

    [Header("Dash")]
    public float dashSpeed = 30f;
    public float dashDuration = 0.15f;
    public int maxDashes = 2;
    public float dashRefillTime = 1.5f;

    [Header("Slide")]
    public float slideSpeed = 14f;
    public float slideDuration = 0.6f;
    public float slideCooldown = 0.4f;
    public float slideGravity = -15f;

    [Header("Wall")]
    public float wallSlideSpeed = 2f;
    public float wallJumpForce = 14f;
    public float wallJumpUpForce = 10f;
    public float wallCheckDistance = 0.6f;
    public float wallJumpLockTime = 0.15f;

    [Header("Game Feel")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.12f;

    [Header("Katana")]
    public int meleeDamage = 35;
    public float meleeRange = 4f;
    public float meleeCooldown = 0.35f;

    [Header("Parry")]
    public float parryWindow = 0.2f;
    public float parryCooldown = 0.5f;

    [Header("Effects")]
    public KatanaTrail katanaTrail;
    public KatanaSwingDetector swingDetector;
    public KatanaAura katanaAura;

    private CharacterController controller;
    private Camera cam;
    private Vector3 velocity;
    private Vector3 moveDir;
    private bool isGrounded;
    private bool isSliding;
    private bool isDashing;
    private bool isWallSliding;
    private bool isWallJumpLocked;
    private float nextMeleeTime;
    private int dashesLeft;
    private float dashRefillTimer;
    private float slideTimer;
    private float slideCooldownTimer;
    private Vector3 wallNormal;
    #pragma warning disable 0414
    private int wallDirection;
#pragma warning restore 0414
    private float inputH, inputV;
    private bool inputJump, inputSprint, inputSlide, inputDash, inputParry;
    private float mouseSensitivity = 2f;
    private float cameraPitch;
    private GameObject katanaObj;
    private bool weaponsSearched;
    private GrapplingHook grapple;
    #pragma warning disable 0414
    private bool isSwinging;
#pragma warning restore 0414
    private float parryTimer;
    private float nextParryTime;
    private float coyoteTimer;
    private float jumpBufferTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        dashesLeft = maxDashes;
        if (cam != null) cam.nearClipPlane = 0.05f;
        grapple = GetComponent<GrapplingHook>();
        if (grapple == null) grapple = gameObject.AddComponent<GrapplingHook>();
    }

    public void OnGameStart()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void FindWeapons()
    {
        if (cam == null) return;
        foreach (Transform child in cam.transform)
        {
            if (child.name.Contains("Katana")) { katanaObj = child.gameObject; break; }
        }
        weaponsSearched = true;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        
        // Ensure cursor is locked during gameplay
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        if (!weaponsSearched) FindWeapons();

        GatherInput();
        CameraLook();
        bool wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;
        
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else if (coyoteTimer > 0)
            coyoteTimer -= Time.deltaTime;
        
        if (inputJump)
            jumpBufferTimer = jumpBufferTime;
        else if (jumpBufferTimer > 0)
            jumpBufferTimer -= Time.deltaTime;

        WallCheck();

        bool grappled = grapple != null && grapple.IsGrappled();

        if (!grappled && !isDashing && !isWallJumpLocked)
            HandleMovement();

        HandleJumpActions();
        if (!grappled) HandleSlide();
        HandleMeleeAttack();
        HandleParry();
        HandleDashRefill();

        if (!grappled && !isDashing)
            ApplyGravity();

        ApplyFinalMovement();
    }

    void GatherInput()
    {
        inputH = Input.GetAxisRaw("Horizontal");
        inputV = Input.GetAxisRaw("Vertical");
        inputJump = Input.GetButtonDown("Jump");
        inputSprint = Input.GetKey(KeyCode.LeftShift);
        inputSlide = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C);
        inputDash = Input.GetKeyDown(KeyCode.LeftAlt);
        inputParry = Input.GetKeyDown(KeyCode.F);
    }

    void CameraLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        if (Mathf.Abs(mx) > 0.001f)
        {
            transform.Rotate(Vector3.up * mx);
        }
        
        cameraPitch -= my;
        cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);
        if (cam != null) cam.transform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
    }

    void WallCheck()
    {
        if (isGrounded) { isWallSliding = false; return; }
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 origin = transform.position + Vector3.up * 0.8f;
        bool hitF = Physics.Raycast(origin, forward, wallCheckDistance);
        bool hitR = Physics.Raycast(origin, right, wallCheckDistance);
        bool hitL = Physics.Raycast(origin, -right, wallCheckDistance);
        bool hitB = Physics.Raycast(origin, -forward, wallCheckDistance);
        if (hitF || hitR || hitL || hitB)
        {
            isWallSliding = true;
            if (hitR) { wallDirection = 1; wallNormal = -right; }
            else if (hitL) { wallDirection = -1; wallNormal = right; }
            else if (hitF) { wallDirection = 0; wallNormal = -forward; }
            else { wallDirection = 0; wallNormal = forward; }
            if (velocity.y < -wallSlideSpeed) velocity.y = -wallSlideSpeed;
        }
        else { isWallSliding = false; }
    }

    void HandleMovement()
    {
        Vector3 forward = cam != null ? cam.transform.forward : transform.forward;
        Vector3 right = cam != null ? cam.transform.right : transform.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();
        Vector3 desiredDir = (forward * inputV + right * inputH).normalized;
        float speed = moveSpeed;
        if (inputSprint && inputV > 0) speed *= sprintMultiplier;
        if (isSliding) speed = slideSpeed;
        float accel = isGrounded ? acceleration : airAcceleration;
        if (desiredDir.magnitude > 0.1f)
            moveDir = Vector3.Lerp(moveDir, desiredDir * speed, accel * Time.deltaTime);
        else
            moveDir = Vector3.Lerp(moveDir, Vector3.zero, (isGrounded ? 15f : 5f) * Time.deltaTime);
    }

    void HandleJumpActions()
    {
        if (jumpBufferTimer <= 0) return;
        if (coyoteTimer > 0 || isGrounded || (isSliding && slideTimer <= 0))
        {
            velocity.y = jumpForce;
            isSliding = false;
            moveDir.y = 0;
            coyoteTimer = 0;
            jumpBufferTimer = 0;
            AudioManager.Instance?.PlayProcedural("jumpSound");
        }
        else if (isWallSliding)
        {
            velocity = wallNormal * wallJumpForce + Vector3.up * wallJumpUpForce;
            isWallSliding = false;
            isWallJumpLocked = true;
            isSliding = false;
            jumpBufferTimer = 0;
            AudioManager.Instance?.PlayProcedural("wallJumpSound");
            StartCoroutine(WallJumpLock());
        }
    }

    IEnumerator WallJumpLock()
    {
        yield return new WaitForSeconds(wallJumpLockTime);
        isWallJumpLocked = false;
    }

    void HandleSlide()
    {
        if (inputSlide && isGrounded && !isSliding && slideCooldownTimer <= 0 && moveDir.magnitude > 2f)
        {
            isSliding = true;
            slideTimer = slideDuration;
            controller.height = 1f;
            controller.center = new Vector3(0, 0.5f, 0);
            AudioManager.Instance?.PlayProcedural("slideSound");
        }
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0 || !isGrounded)
            {
                isSliding = false;
                slideCooldownTimer = slideCooldown;
                controller.height = 1.8f;
                controller.center = new Vector3(0, 0.9f, 0);
            }
        }
        if (slideCooldownTimer > 0) slideCooldownTimer -= Time.deltaTime;
    }

    void HandleDashRefill()
    {
        if (dashesLeft < maxDashes)
        {
            dashRefillTimer += Time.deltaTime;
            if (dashRefillTimer >= dashRefillTime) { dashesLeft++; dashRefillTimer = 0f; }
        }
        if (inputDash && dashesLeft > 0 && !isDashing)
            StartCoroutine(Dash());
    }

    IEnumerator Dash()
    {
        isDashing = true;
        dashesLeft--;
        dashRefillTimer = 0f;
        AudioManager.Instance?.PlayProcedural("dashSound");
        float elapsed = 0f;
        Vector3 dashDir = moveDir.magnitude > 0.1f ? moveDir.normalized : transform.forward;
        while (elapsed < dashDuration)
        {
            velocity = dashDir * dashSpeed;
            velocity.y = 0;
            elapsed += Time.deltaTime;
            yield return null;
        }
        isDashing = false;
    }

    void HandleMeleeAttack()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextMeleeTime && Cursor.lockState == CursorLockMode.Locked)
        {
            nextMeleeTime = Time.time + meleeCooldown;
            AudioManager.Instance?.PlayProcedural("katanaSwing");
            StartCoroutine(KatanaSwing());
        }
    }

    void HandleParry()
    {
        if (inputParry && Time.time >= nextParryTime && Cursor.lockState == CursorLockMode.Locked)
        {
            nextParryTime = Time.time + parryCooldown;
            parryTimer = parryWindow;
            parrySuccessful = false;
            AudioManager.Instance?.PlayProcedural("katanaSwing");
            StartCoroutine(ParryWindow());
        }
    }

    IEnumerator ParryWindow()
    {
        if (katanaObj == null) yield break;
        
        var renderers = katanaObj.GetComponentsInChildren<Renderer>();
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        Color originalEmission = Color.black;
        
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            originalEmission = propBlock.GetColor("_EmissionColor");
            break;
        }
        
        float elapsed = 0f;
        while (elapsed < parryWindow)
        {
            float t = elapsed / parryWindow;
            float pulse = Mathf.Sin(Time.time * 20f) * 0.5f + 0.5f;
            Color parryColor = Color.Lerp(Color.cyan, Color.white, pulse) * 3f;
            
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor("_EmissionColor", parryColor);
                r.SetPropertyBlock(propBlock);
            }
            
            elapsed += Time.deltaTime;
            CheckParry();
            yield return null;
        }
        
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", originalEmission);
            r.SetPropertyBlock(propBlock);
        }
        
        if (!parrySuccessful)
        {
            StartCoroutine(KatanaSwing());
        }
    }

    bool parrySuccessful = false;

    void CheckParry()
    {
        if (parrySuccessful) return;
        
        Collider[] hits = Physics.OverlapSphere(transform.position, meleeRange * 1.5f);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyController>();
            if (enemy != null && enemy.TryParry())
            {
                parrySuccessful = true;
                nextMeleeTime = Time.time + 0.1f;
                nextParryTime = Time.time + 0.2f;
                
                CameraShake.Instance?.Shake(0.5f, 0.15f, 40f);
                HitPause.Instance?.Pause(0.15f, 0.01f);
                AudioManager.Instance?.PlayProceduralAtPoint("katanaHit", enemy.transform.position);
                
                var deathEffect = enemy.GetComponent<EnemyDeathEffect>();
                if (deathEffect != null)
                {
                    Vector3 hitDir = (enemy.transform.position - transform.position).normalized;
                    deathEffect.TriggerDeath(hitDir, 10f);
                }
                else
                {
                    enemy.TakeDamage(999);
                }
                
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddKill();
                    GameManager.Instance.AddScore(200);
                    ComboSystem.Instance?.AddHit(true);
                }
                break;
            }
        }
    }

IEnumerator KatanaSwing()
        {
            if (katanaObj == null) yield break;

            isSwinging = true;
            if (katanaAura != null) katanaAura.SetSwingState(true, 0f);
            WeaponSway sway = katanaObj.GetComponent<WeaponSway>();
            if (sway != null) sway.isAnimated = true;

            Quaternion origRot = katanaObj.transform.localRotation;
            Vector3 origPos = katanaObj.transform.localPosition;

            Vector3 readyPos = origPos + new Vector3(0.1f, 0.08f, -0.15f);
            Quaternion readyRot = origRot * Quaternion.Euler(-30f, -25f, 20f);

            Vector3 slashStartPos = readyPos + new Vector3(-0.03f, 0.03f, 0.08f);
            Quaternion slashStartRot = readyRot * Quaternion.Euler(8f, 15f, -8f);

            Vector3 slashEndPos = origPos + new Vector3(-0.2f, -0.12f, 0.15f);
            Quaternion slashEndRot = origRot * Quaternion.Euler(20f, 85f, -40f);

            Vector3 followPos = slashEndPos + new Vector3(-0.06f, -0.03f, 0.05f);
            Quaternion followRot = slashEndRot * Quaternion.Euler(-12f, 20f, -15f);

            float readyTime = 0.05f;
            float slashWindTime = 0.02f;
            float slashTime = 0.05f;
            float followTime = 0.04f;
            float recoverTime = 0.12f;

            float elapsed = 0f;

            while (elapsed < readyTime)
            {
                float t = elapsed / readyTime;
                t = t * t * (3f - 2f * t);
                katanaObj.transform.localPosition = Vector3.Lerp(origPos, readyPos, t);
                katanaObj.transform.localRotation = Quaternion.Slerp(origRot, readyRot, t);
                if (katanaAura != null) katanaAura.SetSwingProgress(t * 0.2f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < slashWindTime)
            {
                float t = elapsed / slashWindTime;
                katanaObj.transform.localPosition = Vector3.Lerp(readyPos, slashStartPos, t);
                katanaObj.transform.localRotation = Quaternion.Slerp(readyRot, slashStartRot, t);
                if (katanaAura != null) katanaAura.SetSwingProgress(0.2f + t * 0.15f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < slashTime)
            {
                float t = elapsed / slashTime;
                t = t * t;
                katanaObj.transform.localPosition = Vector3.Lerp(slashStartPos, slashEndPos, t);
                katanaObj.transform.localRotation = Quaternion.Slerp(slashStartRot, slashEndRot, t);
                if (katanaAura != null) katanaAura.SetSwingProgress(0.35f + t * 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Ray ray = new Ray(cam.transform.position + cam.transform.forward * 0.5f, cam.transform.forward);
            bool killed = false;
            int enemyLayer = 1 << LayerMask.NameToLayer("Enemy");
            int defaultLayer = 1 << 0;
            int layerMask = enemyLayer | defaultLayer;
            if (Physics.SphereCast(ray, 0.3f, out RaycastHit hit, meleeRange, layerMask))
            {
                EnemyController ec = hit.collider.GetComponent<EnemyController>();
                if (ec == null) ec = hit.collider.GetComponentInParent<EnemyController>();
                if (ec != null)
                {
                    float speedBonus = GetSpeed() > 5f ? 1.5f : 1f;
                    killed = ec.TakeDamage(Mathf.RoundToInt(meleeDamage * speedBonus));
                    if (killed && GameManager.Instance != null)
                    {
                        GameManager.Instance.AddKill();
                        GameManager.Instance.AddScore(100);
                        GameManager.Instance.HitStop(0.05f);
                        ComboSystem.Instance?.AddHit(true);
                    }
                    else if (GameManager.Instance != null)
                    {
                        GameManager.Instance.HitStop(0.02f);
                        ComboSystem.Instance?.AddHit(false);
                    }
                }
                SpawnHitEffect(hit);
                
                if (katanaTrail != null)
                    katanaTrail.OnHit(hit.point, hit.normal);
                if (swingDetector != null)
                    swingDetector.TriggerHit(hit.point, hit.normal);
            }
            else
            {
                ComboSystem.Instance?.AddHit(false);
            }

            if (katanaAura != null) katanaAura.SetSwingProgress(0.85f);

            elapsed = 0f;
            while (elapsed < followTime)
            {
                float t = elapsed / followTime;
                t = 1f - (1f - t) * (1f - t);
                katanaObj.transform.localPosition = Vector3.Lerp(slashEndPos, followPos, t);
                katanaObj.transform.localRotation = Quaternion.Slerp(slashEndRot, followRot, t);
                if (katanaAura != null) katanaAura.SetSwingProgress(0.85f + t * 0.1f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < recoverTime)
            {
                float t = elapsed / recoverTime;
                t = t * t * (3f - 2f * t);
                katanaObj.transform.localPosition = Vector3.Lerp(followPos, origPos, t);
                katanaObj.transform.localRotation = Quaternion.Slerp(followRot, origRot, t);
                if (katanaAura != null) katanaAura.SetSwingProgress(0.95f + t * 0.05f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            katanaObj.transform.localPosition = origPos;
            katanaObj.transform.localRotation = origRot;
            if (sway != null) sway.isAnimated = false;
            if (katanaAura != null) katanaAura.SetSwingState(false);
            isSwinging = false;
        }

    static Material sparkMat;
    static Material slashMat;
    static Material bloodMat;
    static bool materialsCreated;

    static void EnsureMaterials()
    {
        if (materialsCreated) return;
        materialsCreated = true;

        sparkMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        sparkMat.EnableKeyword("_EMISSION");

        slashMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        slashMat.EnableKeyword("_EMISSION");
        slashMat.renderQueue = 3000;

        bloodMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        bloodMat.renderQueue = 2999;
    }

    void SpawnHitEffect(RaycastHit hit)
    {
        EnsureMaterials();
        HitPause.Instance?.Pause(0.04f, 0.02f);
        CameraShake.Instance?.DirectionalShake(hit.normal, 0.4f, 0.1f);

        var slashEffect = GameObject.CreatePrimitive(PrimitiveType.Quad);
        slashEffect.transform.position = hit.point;
        slashEffect.transform.rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, Random.Range(0, 360), 0);
        slashEffect.transform.localScale = Vector3.one * 0.5f;
        Destroy(slashEffect.GetComponent<Collider>());
        var sr = slashEffect.GetComponent<Renderer>();
        if (sr != null)
        {
            sr.material = slashMat;
            sr.material.color = new Color(1f, 0.8f, 0.3f);
            sr.material.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.1f) * 15f);
        }
        StartCoroutine(ScaleAndFade(slashEffect, 0.15f, 2f, true));

        for (int i = 0; i < 6; i++)
        {
            var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.transform.position = hit.point + Random.insideUnitSphere * 0.15f;
            spark.transform.localScale = Vector3.one * Random.Range(0.02f, 0.05f);
            spark.transform.rotation = Random.rotation;
            Destroy(spark.GetComponent<Collider>());
            var smr = spark.GetComponent<Renderer>();
            if (smr != null)
            {
                smr.material = sparkMat;
                float hue = Random.Range(0f, 0.15f);
                smr.material.color = Color.HSVToRGB(hue, 0.8f, 1f);
                smr.material.SetColor("_EmissionColor", Color.HSVToRGB(hue, 1f, 1f) * 20f);
            }
            var rb = spark.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = (hit.normal + Random.insideUnitSphere * 0.5f).normalized * Random.Range(8f, 20f);
            Destroy(spark, 0.5f);
        }

        var bloodSplatter = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bloodSplatter.transform.position = hit.point + hit.normal * 0.01f;
        bloodSplatter.transform.rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90, Random.Range(0, 360), 0);
        bloodSplatter.transform.localScale = Vector3.one * Random.Range(0.3f, 0.6f);
        Destroy(bloodSplatter.GetComponent<Collider>());
        var br = bloodSplatter.GetComponent<Renderer>();
        if (br != null)
        {
            br.material = bloodMat;
            br.material.color = new Color(0.8f, 0.1f, 0.05f, 0.8f);
        }
        StartCoroutine(FadeOut(bloodSplatter, 1f));
    }

    IEnumerator ScaleAndFade(GameObject obj, float duration, float maxScale, bool rotate = false)
    {
        float elapsed = 0f;
        Vector3 startScale = obj.transform.localScale;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, maxScale, t);
            obj.transform.localScale = startScale * scale;
            if (rotate) obj.transform.Rotate(0, 0, 720f * Time.deltaTime);
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                float alpha = 1f - t;
                var color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
                if (renderer.material.HasProperty("_EmissionColor"))
                {
                    var emission = renderer.material.GetColor("_EmissionColor");
                    renderer.material.SetColor("_EmissionColor", emission * alpha);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }

    IEnumerator FadeOut(GameObject obj, float duration)
    {
        float elapsed = 0f;
        var renderer = obj.GetComponent<Renderer>();
        Color startColor = renderer.material.color;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            var color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, t);
            renderer.material.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(obj);
    }

    void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0) velocity.y = -2f;
        else
        {
            float grav = isSliding ? slideGravity : gravity;
            velocity.y += grav * Time.deltaTime;
            if (velocity.y < maxFallSpeed) velocity.y = maxFallSpeed;
        }
    }

    void ApplyFinalMovement()
    {
        Vector3 finalMove = moveDir + new Vector3(0, velocity.y, 0);
        if (grapple != null && grapple.IsGrappled())
        {
            finalMove = grapple.GetGrappleVelocity();
        }
        controller.Move(finalMove * Time.deltaTime);
    }

    public float GetSpeed() { return new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude; }
    public int GetDashes() { return dashesLeft; }
    public bool IsSliding() { return isSliding; }
    public bool IsSwinging() { return isSwinging; }
    public bool IsDashing() { return isDashing; }
    public bool IsWallSliding() { return isWallSliding; }
    public bool IsWallJumpLocked() { return isWallJumpLocked; }
    public Vector3 GetWallNormal() { return wallNormal; }
}
