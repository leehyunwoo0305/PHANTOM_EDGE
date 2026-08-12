using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Idle Sway")]
    public float swayAmount = 0.003f;
    public float swaySpeed = 1.2f;

    [Header("Breathing")]
    public float breathAmount = 0.001f;
    public float breathSpeed = 0.8f;

    [Header("Recoil")]
    public float recoilAmount = 0.03f;
    public float recoilRecovery = 8f;

    [Header("Move Sway")]
    public float moveSwayAmount = 0.004f;
    public float moveSwaySpeed = 6f;

    private Vector3 originPosition;
    private Vector3 currentRecoil;
    private CharacterController playerController;
    public bool isAnimated;

    void Start()
    {
        originPosition = transform.localPosition;
        playerController = GetComponentInParent<CharacterController>();
        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>()?.GetComponent<CharacterController>();
    }

    void Update()
    {
        if (isAnimated) return;
        HandleIdleSway();
        HandleBreathing();
        HandleMoveSway();
        HandleRecoil();
    }

    void HandleIdleSway()
    {
        float mx = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        float my = Mathf.Cos(Time.time * swaySpeed * 0.7f) * swayAmount * 0.5f;
        transform.localPosition += new Vector3(mx, my, 0) * Time.deltaTime * 3f;
    }

    void HandleBreathing()
    {
        float by = Mathf.Sin(Time.time * breathSpeed) * breathAmount;
        transform.localPosition += new Vector3(0, by, 0) * Time.deltaTime * 2f;
    }

    void HandleMoveSway()
    {
        if (playerController == null) return;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float targetX = -h * moveSwayAmount;
        float targetY = Mathf.Abs(v) * moveSwayAmount * 0.3f;
        Vector3 moveOffset = new Vector3(targetX, targetY, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, originPosition + moveOffset, Time.deltaTime * moveSwaySpeed);
    }

    void HandleRecoil()
    {
        if (Input.GetMouseButtonDown(0))
            currentRecoil += new Vector3(0, 0, -recoilAmount);
        currentRecoil = Vector3.Lerp(currentRecoil, Vector3.zero, Time.deltaTime * recoilRecovery);
        transform.localPosition += currentRecoil * Time.deltaTime;
    }

    public void AddRecoil(float amount)
    {
        currentRecoil += new Vector3(0, 0, -amount);
    }
}
