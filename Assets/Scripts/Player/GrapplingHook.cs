using UnityEngine;

public class GrapplingHook : MonoBehaviour
{
    [Header("Grapple Settings")]
    public float maxDistance = 50f;
    public float pullSpeed = 30f;
    public float releaseSpeed = 25f;
    public float grappleCooldown = 0.5f;
    public float minDistance = 2f;

    [Header("Swing")]
    public float swingForce = 15f;
    public float airControl = 8f;

    [Header("Visual")]
    public LineRenderer lineRenderer;
    public LayerMask grappleLayer = ~0;

    private Camera cam;
    private CharacterController controller;
    private bool isGrappled;
    private bool canGrapple = true;
    private Vector3 grapplePoint;
    private float grappleTimer;
    private Vector3 velocity;
    private float cooldownTimer;

    void Start()
    {
        cam = GetComponentInChildren<Camera>();
        controller = GetComponent<CharacterController>();

        if (lineRenderer == null)
        {
            var lineObj = new GameObject("GrappleLine");
            lineObj.transform.SetParent(transform);
            lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineRenderer.startColor = new Color(1f, 0.4f, 0.1f);
            lineRenderer.endColor = new Color(1f, 0.7f, 0.2f);
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            canGrapple = cooldownTimer <= 0;
        }

        if (Input.GetKeyDown(KeyCode.Q) && canGrapple && !isGrappled)
        {
            TryGrapple();
        }

        if (isGrappled)
        {
            UpdateGrapple();
        }

        if (Input.GetKeyUp(KeyCode.Q) && isGrappled)
        {
            ReleaseGrapple();
        }

        UpdateLine();
    }

    void TryGrapple()
    {
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, grappleLayer))
        {
            grapplePoint = hit.point;
            isGrappled = true;
            grappleTimer = 0f;
            velocity = Vector3.zero;
            AudioManager.Instance?.PlayProcedural("grappleShoot");
        }
    }

    void UpdateGrapple()
    {
        grappleTimer += Time.deltaTime;

        Vector3 dir = (grapplePoint - transform.position);
        float dist = dir.magnitude;

        if (dist < minDistance)
        {
            ReleaseGrapple();
            return;
        }

        dir.Normalize();

        float currentSpeed = pullSpeed + grappleTimer * 5f;
        velocity = dir * currentSpeed;

        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            if (right.sqrMagnitude < 0.01f) right = transform.right;
            velocity += right * swingForce;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 moveInput = (cam.transform.right * h + cam.transform.forward * v).normalized;
        moveInput.y = 0;
        velocity += moveInput * airControl;
        
        if (grappleTimer > 0.1f && grappleTimer < 0.15f)
        {
            AudioManager.Instance?.PlayProcedural("grappleConnect");
        }
    }

    void ReleaseGrapple()
    {
        isGrappled = false;
        cooldownTimer = grappleCooldown;
        AudioManager.Instance?.PlayProcedural("grappleRelease");

        Vector3 releaseDir = (grapplePoint - transform.position).normalized;
        Vector3 playerVel = new Vector3(controller.velocity.x, 0, controller.velocity.z);

        if (playerVel.magnitude < 1f)
            playerVel = transform.forward * releaseSpeed * 0.5f;

        velocity = playerVel.normalized * Mathf.Max(playerVel.magnitude, releaseSpeed) + Vector3.up * 3f;
    }

    void UpdateLine()
    {
        if (isGrappled && lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, cam != null ? cam.transform.position : transform.position);
            lineRenderer.SetPosition(1, grapplePoint);
        }
        else if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    public bool IsGrappled() { return isGrappled; }
    public bool CanGrapple() { return canGrapple; }
    public float GetCooldownRatio() { return Mathf.Clamp01(cooldownTimer / grappleCooldown); }
    public Vector3 GetGrappleVelocity() { return isGrappled ? velocity : Vector3.zero; }
    public Vector3 GetGrapplePoint() { return grapplePoint; }
}
