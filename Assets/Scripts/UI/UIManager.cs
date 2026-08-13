using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HP")]
    public Slider hpBar;
    public TextMeshProUGUI hpText;
    public Image damageVignette;
    private float vignetteTimer;

    [Header("Stats")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI waveText;
    public TextMeshProUGUI killText;

    [Header("Speed")]
    public Image speedGauge;
    public TextMeshProUGUI speedText;

    [Header("Crosshair")]
    public Image crosshair;
    public RectTransform crosshairRect;
    private float crosshairExpansion;

    [Header("Dash")]
    public Image[] dashIcons;

    [Header("Grapple")]
    public Image grappleCooldownFill;
    public TextMeshProUGUI grappleKeyText;

    [Header("Damage Direction")]
    public RectTransform damageDirectionContainer;
    private List<Image> _damageIndicators = new List<Image>();

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI finalScoreText;

    private MenuManager menuManager;

    [Header("Wave Announce")]
    public GameObject waveAnnouncePanel;
    public TextMeshProUGUI waveAnnounceText;
    private float waveAnnounceTimer;

    [Header("Combo")]
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI comboRankText;
    public RectTransform comboPanel;

    private PlayerController playerRef;
    private GrapplingHook grappleRef;
    private float lastHP;
    private Camera cam;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHPChanged += UpdateHP;
            GameManager.Instance.OnScoreChanged += UpdateScore;
            GameManager.Instance.OnWaveChanged += ShowWaveAnnounce;
            GameManager.Instance.OnKillChanged += UpdateKills;
            GameManager.Instance.OnGameOver += ShowGameOver;
            lastHP = GameManager.Instance.maxHP;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (waveAnnouncePanel != null) waveAnnouncePanel.SetActive(false);
        if (comboPanel != null) comboPanel.gameObject.SetActive(false);
        if (comboRankText != null) comboRankText.gameObject.SetActive(false);
        if (damageVignette != null) damageVignette.color = Color.clear;
        if (crosshairRect != null) crosshairRect.sizeDelta = new Vector2(12, 12);

        for (int i = 0; i < dashIcons.Length; i++)
            if (dashIcons[i] != null) dashIcons[i].color = Color.white;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHPChanged -= UpdateHP;
            GameManager.Instance.OnScoreChanged -= UpdateScore;
            GameManager.Instance.OnWaveChanged -= ShowWaveAnnounce;
            GameManager.Instance.OnKillChanged -= UpdateKills;
            GameManager.Instance.OnGameOver -= ShowGameOver;
        }
    }

    void Update()
    {
        if (playerRef == null)
            playerRef = FindAnyObjectByType<PlayerController>();
        if (grappleRef == null)
            grappleRef = playerRef != null ? playerRef.GetComponent<GrapplingHook>() : null;
        if (cam == null)
            cam = Camera.main;

        if (GameManager.Instance != null && !GameManager.Instance.isGameOver)
        {
            UpdateDamageVignette();
            UpdateSpeed();
            UpdateDashUI();
            UpdateGrappleUI();
            UpdateCrosshair();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance != null && GameManager.Instance.isGameOver)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }

        if (waveAnnounceTimer > 0)
        {
            waveAnnounceTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(waveAnnounceTimer / 0.5f);
            if (waveAnnounceText != null)
            {
                var c = waveAnnounceText.color;
                c.a = waveAnnounceTimer > 2f ? 1f : alpha;
                waveAnnounceText.color = c;
            }
            if (waveAnnounceTimer <= 0 && waveAnnouncePanel != null)
                waveAnnouncePanel.SetActive(false);
        }
    }

    void UpdateHP(int current, int max)
    {
        if (hpBar != null) hpBar.value = (float)current / max;
        if (hpText != null) hpText.text = $"{current}";
        if (current < lastHP)
        {
            vignetteTimer = 0.4f;
            ShowDamageDirection(lastHP - current);
        }
        lastHP = current;
    }

    void UpdateDamageVignette()
    {
        if (damageVignette == null) return;
        if (vignetteTimer > 0)
        {
            vignetteTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(vignetteTimer / 0.4f) * 0.6f;
            float hpRatio = GameManager.Instance != null ? (float)GameManager.Instance.currentHP / GameManager.Instance.maxHP : 1f;
            float lowHpVignette = Mathf.Clamp01((1f - hpRatio) * 0.3f);
            damageVignette.color = new Color(0.8f, 0f, 0f, Mathf.Max(alpha, lowHpVignette));
        }
        else
        {
            float hpRatio = GameManager.Instance != null ? (float)GameManager.Instance.currentHP / GameManager.Instance.maxHP : 1f;
            float lowHpVignette = Mathf.Clamp01((1f - hpRatio) * 0.3f);
            damageVignette.color = new Color(0.8f, 0f, 0f, lowHpVignette);
        }
    }

    void UpdateSpeed()
    {
        if (speedGauge == null || playerRef == null) return;
        float speed = playerRef.GetSpeed();
        float fill = Mathf.Clamp01(speed / 20f);
        speedGauge.fillAmount = fill;
        speedGauge.color = Color.Lerp(new Color(0.3f, 0.3f, 0.3f), new Color(0f, 1f, 0.5f), fill);
        if (speedText != null) speedText.text = $"{speed:F0} km/h";
    }

    void UpdateDashUI()
    {
        if (dashIcons == null || playerRef == null) return;
        int dashes = playerRef.GetDashes();
        for (int i = 0; i < dashIcons.Length; i++)
        {
            if (dashIcons[i] == null) continue;
            bool active = i < dashes;
            dashIcons[i].color = active ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            dashIcons[i].transform.localScale = active ? Vector3.one : Vector3.one * 0.8f;
        }
    }

    void UpdateGrappleUI()
    {
        if (grappleCooldownFill != null && grappleRef != null)
        {
            grappleCooldownFill.fillAmount = 1f - grappleRef.GetCooldownRatio();
        }
        if (grappleKeyText != null && grappleRef != null)
        {
            grappleKeyText.color = grappleRef.CanGrapple() ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }
    }

    void UpdateCrosshair()
    {
        if (crosshairRect == null || playerRef == null) return;
        float speed = playerRef.GetSpeed();
        float targetExpansion = playerRef.IsDashing() ? 8f : (playerRef.IsSliding() ? 4f : Mathf.Clamp(speed * 0.3f, 0f, 5f));
        crosshairExpansion = Mathf.Lerp(crosshairExpansion, targetExpansion, Time.deltaTime * 10f);
        crosshairRect.sizeDelta = new Vector2(12 + crosshairExpansion, 12 + crosshairExpansion);
        float alpha = playerRef.IsDashing() ? 0.4f : 0.8f;
        if (crosshair != null)
        {
            var c = crosshair.color;
            c.a = alpha;
            crosshair.color = c;
        }
    }

    void ShowDamageDirection(float damageAmount)
    {
        if (damageDirectionContainer == null || cam == null || playerRef == null) return;
        Vector3 playerPos = playerRef.transform.position;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        bool anyShown = false;
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            Vector3 dirToEnemy = (enemy.transform.position - playerPos).normalized;
            float dist = Vector3.Distance(enemy.transform.position, playerPos);
            if (dist > 15f) continue;
            float dot = Vector3.Dot(cam.transform.forward, dirToEnemy);
            if (dot < 0.3f) continue;
            SpawnDamageIndicator(dirToEnemy);
            anyShown = true;
        }
        if (!anyShown)
        {
            SpawnDamageIndicator(-cam.transform.forward);
        }
    }

    void SpawnDamageIndicator(Vector3 worldDir)
    {
        if (damageDirectionContainer == null) return;
        var indicator = new GameObject("DamageIndicator");
        indicator.transform.SetParent(damageDirectionContainer, false);
        var rect = indicator.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(40, 40);
        float angle = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
        rect.localRotation = Quaternion.Euler(0, 0, -angle);
        var img = indicator.AddComponent<Image>();
        img.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        _damageIndicators.Add(img);
        StartCoroutine(FadeDamageIndicator(indicator, img));
    }

    System.Collections.IEnumerator FadeDamageIndicator(GameObject obj, Image img)
    {
        float elapsed = 0f;
        float duration = 0.6f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            img.color = new Color(1f, 0.3f, 0.3f, Mathf.Lerp(0.8f, 0f, t));
            obj.transform.localScale = Vector3.one * (1f + t * 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        _damageIndicators.Remove(img);
        Destroy(obj);
    }

    void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString("N0");
    }

    void UpdateKills(int kills)
    {
        if (killText != null) killText.text = kills.ToString();
    }

    void ShowWaveAnnounce(int wave)
    {
        if (waveAnnouncePanel == null || waveAnnounceText == null) return;
        waveAnnouncePanel.SetActive(true);
        waveAnnounceText.text = $"WAVE {wave}";
        waveAnnounceText.color = new Color(1f, 0.85f, 0.4f, 1f);
        waveAnnounceTimer = 2.5f;
    }

    void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverText != null) gameOverText.text = "YOU DIED";
            if (finalScoreText != null && GameManager.Instance != null)
                finalScoreText.text = $"SCORE: {GameManager.Instance.score}";
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
