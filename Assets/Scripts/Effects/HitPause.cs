using UnityEngine;

public class HitPause : MonoBehaviour
{
    public static HitPause Instance { get; private set; }

    [Header("Settings")]
    public float defaultHitPause = 0.05f;
    public float maxHitPause = 0.2f;
    public AnimationCurve timeScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float timer;
    private float duration;
    private bool isPaused;
    private float previousTimeScale;
    private float previousFixedDeltaTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Update()
    {
        if (!isPaused) return;

        timer += Time.unscaledDeltaTime;
        float t = timer / duration;
        Time.timeScale = timeScaleCurve.Evaluate(t);

        if (timer >= duration)
        {
            Resume();
        }
    }

    public void Pause(float duration, float timeScale = 0f)
    {
        this.duration = Mathf.Clamp(duration, 0, maxHitPause);
        timer = 0f;
        previousTimeScale = 1f;
        previousFixedDeltaTime = 0.02f;
        
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        isPaused = true;
    }

    public void Resume()
    {
        if (!isPaused) return;

        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime;
        isPaused = false;
    }

    public void KillPause(float killDuration = 0.1f)
    {
        Pause(killDuration, 0.05f);
    }
}