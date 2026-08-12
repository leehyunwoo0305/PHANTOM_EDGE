using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    public enum EnemyType { Rusher, Shooter }
    public enum AIState { Idle, Chase, Telegraphing, Attacking, Stunned }

    [Header("Stats")]
    public EnemyType enemyType = EnemyType.Rusher;
    public int maxHP = 80;
    public int attackDamage = 15;
    public float moveSpeed = 7f;

    [Header("AI")]
    public float detectionRange = 40f;
    public float attackRange = 2.5f;
    public float attackCooldown = 1.2f;
    public float orbitDistance = 4f;

    [Header("Telegraphing")]
    public float telegraphTime = 0.5f;
    public Color telegraphColor = Color.yellow;
    public Color attackColor = Color.red;
    public bool canBeParried = true;
    public float chargeSpeed = 20f;
    public float chargeDistance = 5f;

    [Header("Shooter")]
    public float bulletSpeed = 20f;
    public int bulletDamage = 10;
    public float shootRange = 25f;

    [Header("Death Effect")]
    public EnemyDeathEffect deathEffect;

    private int currentHP;
    private Transform player;
    private CharacterController controller;
    private float nextAttackTime;
    private bool isDead;
    private Renderer[] renderers;
    private Color originalColor;
    private MaterialPropertyBlock propBlock;
    private Vector3 velocity;
    private AIState currentState;
    private float stateTimer;
    private Vector3 chargeTarget;
    private bool isCharging;
    private bool attackParried;

    void Start()
    {
        currentHP = maxHP;
        controller = GetComponent<CharacterController>();
        if (controller == null) controller = gameObject.AddComponent<CharacterController>();
        controller.height = 2.2f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0, 1.1f, 0);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        CacheOriginalColor();

        if (deathEffect == null) deathEffect = GetComponent<EnemyDeathEffect>();

        if (enemyType == EnemyType.Shooter)
        {
            attackRange = shootRange;
        }

        currentState = AIState.Idle;
    }

    void CacheOriginalColor()
    {
        if (renderers != null && renderers.Length > 0)
        {
            var r = renderers[0];
            r.GetPropertyBlock(propBlock);
            originalColor = propBlock.GetColor("_BaseColor");
            if (originalColor == Color.clear) originalColor = Color.white;
        }
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (enemyType == EnemyType.Rusher)
        {
            UpdateRusher(dist);
        }
        else
        {
            UpdateShooter(dist);
        }

        UpdateStateMachine();
        ApplyGravity();
        controller.Move(velocity * Time.deltaTime);
        HandleFlash();
    }

    void UpdateRusher(float dist)
    {
        Vector3 dir = Vector3.zero;
        
        switch (currentState)
        {
            case AIState.Idle:
            case AIState.Chase:
                if (dist <= attackRange && Time.time >= nextAttackTime)
                {
                    StartTelegraph();
                }
                else if (dist <= detectionRange)
                {
                    ChasePlayer();
                    currentState = AIState.Chase;
                    dir = (player.position - transform.position).normalized;
                    dir.y = 0;
                }
                else
                {
                    velocity.x = 0;
                    velocity.z = 0;
                    currentState = AIState.Idle;
                }
                break;

            case AIState.Telegraphing:
                velocity.x = 0;
                velocity.z = 0;
                break;

            case AIState.Attacking:
                if (isCharging)
                {
                    Vector3 chargeDir = (chargeTarget - transform.position).normalized;
                    chargeDir.y = 0;
                    velocity = chargeDir * chargeSpeed;
                    
                    if (Vector3.Distance(transform.position, chargeTarget) < 1f)
                    {
                        ExecuteAttack();
                    }
                }
                break;

            case AIState.Stunned:
                velocity.x = 0;
                velocity.z = 0;
                break;
        }

        if (dir.sqrMagnitude > 0.01f && currentState != AIState.Attacking)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }
    }

    void ChasePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        velocity.x = dir.x * moveSpeed;
        velocity.z = dir.z * moveSpeed;
    }

    void StartTelegraph()
    {
        currentState = AIState.Telegraphing;
        stateTimer = telegraphTime;
        nextAttackTime = Time.time + attackCooldown;
        
        SetRendererColor(telegraphColor);
        
        if (enemyType == EnemyType.Rusher && player != null && Random.value > 0.5f)
        {
            Vector3 chargeDir = (player.position - transform.position).normalized;
            chargeTarget = transform.position + chargeDir * chargeDistance;
            isCharging = true;
        }
    }

    void UpdateStateMachine()
    {
        if (currentState == AIState.Telegraphing)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0)
            {
                if (isCharging)
                {
                    currentState = AIState.Attacking;
                    SetRendererColor(attackColor);
                }
                else
                {
                    ExecuteAttack();
                }
            }
        }
    }

    void ExecuteAttack()
    {
        currentState = AIState.Attacking;
        isCharging = false;
        velocity.x = 0;
        velocity.z = 0;
        attackParried = false;
        SetRendererColor(attackColor);

        if (!GameManager.Instance.isGameOver)
        {
            GameManager.Instance.TakeDamage(attackDamage);
            StartCoroutine(AttackAnimation());
        }

        StartCoroutine(AttackRecovery());
    }

    IEnumerator AttackRecovery()
    {
        yield return new WaitForSeconds(0.3f);
        currentState = AIState.Chase;
        ResetRendererColor();
    }

    void SetRendererColor(Color color)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", color);
            propBlock.SetColor("_EmissionColor", color * 2f);
            r.SetPropertyBlock(propBlock);
        }
    }

    void ResetRendererColor()
    {
        if (renderers == null || originalColor == Color.clear) return;
        foreach (var r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", originalColor);
            propBlock.SetColor("_EmissionColor", Color.black);
            r.SetPropertyBlock(propBlock);
        }
    }

    public bool TryParry()
    {
        if (!canBeParried || currentState != AIState.Attacking || attackParried) return false;
        
        attackParried = true;
        currentState = AIState.Stunned;
        StartCoroutine(StunRecovery());
        CameraShake.Instance?.Shake(0.4f, 0.2f, 30f);
        HitPause.Instance?.Pause(0.1f, 0.01f);
        AudioManager.Instance?.PlayProceduralAtPoint("katanaHit", transform.position);
        return true;
    }

    IEnumerator StunRecovery()
    {
        SetRendererColor(Color.cyan);
        yield return new WaitForSeconds(1.5f);
        currentState = AIState.Chase;
        ResetRendererColor();
    }

    void UpdateShooter(float dist)
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;

        if (dist <= orbitDistance)
        {
            Vector3 orbitDir = -dirToPlayer;
            Vector3 orbitTarget = player.position + orbitDir * orbitDistance;
            Vector3 moveDir = (orbitTarget - transform.position).normalized;
            moveDir.y = 0;

            Vector3 cross = Vector3.Cross(dirToPlayer, Vector3.up);
            moveDir += cross * 0.5f;
            moveDir.Normalize();

            velocity.x = moveDir.x * moveSpeed * 0.7f;
            velocity.z = moveDir.z * moveSpeed * 0.7f;
        }
        else if (dist <= detectionRange)
        {
            velocity.x = dirToPlayer.x * moveSpeed;
            velocity.z = dirToPlayer.z * moveSpeed;
        }
        else
        {
            velocity.x = 0;
            velocity.z = 0;
        }

        if (dirToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        if (dist <= shootRange && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            Shoot();
        }
    }

    void Shoot()
    {
        if (player == null) return;

        Vector3 dir = (player.position + Vector3.up * 1f - transform.position).normalized;
        AudioManager.Instance?.PlayProceduralAtPoint("enemyShoot", transform.position);

        var bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "EnemyBullet";
        bullet.transform.position = transform.position + Vector3.up * 1.5f + dir * 0.5f;
        bullet.transform.localScale = Vector3.one * 0.1f;
        var sc = bullet.GetComponent<SphereCollider>();
        sc.isTrigger = true;

        var r = bullet.GetComponent<Renderer>();
        if (r != null)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.color = new Color(1f, 0.3f, 0.1f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(2f, 0.5f, 0f));
            r.material = m;
        }

        var rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = dir * bulletSpeed;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var bc = bullet.AddComponent<BulletCollider>();
        bc.damage = bulletDamage;
        bc.lifetime = 3f;
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += -30f * Time.deltaTime;
            if (velocity.y < -40f) velocity.y = -40f;
        }
    }

    IEnumerator AttackAnimation()
    {
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 origScale = transform.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = origScale * (1f + 0.2f * Mathf.Sin(t * Mathf.PI));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = origScale;
    }

    public bool TakeDamage(int damage)
    {
        if (isDead) return false;

        currentHP -= damage;
        AudioManager.Instance?.PlayProceduralAtPoint("enemyHit", transform.position);
        StartCoroutine(FlashDamage());

        if (currentHP <= 0)
        {
            Die();
            return true;
        }
        return false;
    }

    IEnumerator FlashDamage()
    {
        if (renderers == null) yield break;

        foreach (var r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", Color.white);
            r.SetPropertyBlock(propBlock);
        }

        yield return new WaitForSeconds(0.08f);

        foreach (var r in renderers)
        {
            r.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", originalColor);
            r.SetPropertyBlock(propBlock);
        }
    }

    void HandleFlash()
    {
        if (renderers == null || renderers.Length == 0) return;
        if (originalColor == Color.clear)
        {
            originalColor = renderers[0].material.GetColor("_BaseColor");
        }
    }

    void Die()
    {
        isDead = true;
        velocity = Vector3.zero;
        AudioManager.Instance?.PlayProceduralAtPoint("enemyDeath", transform.position);
        
        Vector3 hitDirection = (transform.position - player.position).normalized;
        if (deathEffect != null)
        {
            deathEffect.TriggerDeath(hitDirection);
        }
        else
        {
            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator DeathSequence()
    {
        float elapsed = 0f;
        Vector3 origScale = transform.localScale;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = origScale * (1f - t);
            transform.Rotate(Vector3.up * 720f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}

public class BulletCollider : MonoBehaviour
{
    public int damage;
    public float lifetime;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null && !GameManager.Instance.isGameOver)
            {
                GameManager.Instance.TakeDamage(damage);
                AudioManager.Instance?.PlayProcedural("playerHit");
            }
        }
        Destroy(gameObject);
    }
}
