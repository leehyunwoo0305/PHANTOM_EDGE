using UnityEngine;

public class ArmAnimation : MonoBehaviour
{
    [Header("Idle")]
    public float idleSwayAmount = 2f;
    public float idleSwaySpeed = 1f;

    [Header("Breathing")]
    public float breathAmount = 0.5f;
    public float breathSpeed = 0.7f;

    [Header("Move")]
    public float moveSwayAmount = 3f;
    public float moveSwaySpeed = 5f;

    [Header("Recoil")]
    public float recoilPitch = -8f;
    public float recoilRecovery = 10f;

    private Vector3 originEuler;
    private float currentRecoilX;
    private CharacterController cc;

    void Start()
    {
        originEuler = transform.localEulerAngles;
        cc = GetComponentInParent<CharacterController>();
    }

    void Update()
    {
        HandleIdleSway();
        HandleBreathing();
        HandleMoveSway();
        HandleRecoil();
    }

    void HandleIdleSway()
    {
        float x = Mathf.Sin(Time.time * idleSwaySpeed) * idleSwayAmount;
        float y = Mathf.Cos(Time.time * idleSwaySpeed * 0.6f) * idleSwayAmount * 0.5f;
        transform.localRotation = Quaternion.Euler(
            originEuler.x + x,
            originEuler.y + y,
            originEuler.z
        );
    }

    void HandleBreathing()
    {
        float by = Mathf.Sin(Time.time * breathSpeed) * breathAmount;
        transform.localRotation *= Quaternion.Euler(0, 0, by);
    }

    void HandleMoveSway()
    {
        if (cc == null) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float targetX = -h * moveSwayAmount;
        float targetY = Mathf.Abs(v) * moveSwayAmount * 0.3f;

        Quaternion moveRot = Quaternion.Euler(targetX, targetY, 0);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, originEuler != Vector3.zero ? moveRot * Quaternion.Euler(originEuler) : moveRot, Time.deltaTime * moveSwaySpeed);
    }

    void HandleRecoil()
    {
        if (Input.GetMouseButtonDown(0))
        {
            currentRecoilX = recoilPitch;
        }

        currentRecoilX = Mathf.Lerp(currentRecoilX, 0, Time.deltaTime * recoilRecovery);
        transform.localRotation *= Quaternion.Euler(currentRecoilX, 0, 0);
    }
}
