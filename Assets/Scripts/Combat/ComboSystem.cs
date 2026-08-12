using UnityEngine;
using TMPro;
using System.Collections;

public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance { get; private set; }

    [Header("Combo Settings")]
    public float comboWindow = 1.5f;
    public int maxCombo = 99;
    public int[] comboThresholds = { 5, 10, 20, 30, 50 };
    public string[] comboRanks = { "GOOD", "GREAT", "AWESOME", "SICK", "GODLIKE" };
    public Color[] rankColors = {
        new Color(0.5f, 1f, 0.5f),
        new Color(0.5f, 0.8f, 1f),
        new Color(1f, 0.8f, 0.3f),
        new Color(1f, 0.4f, 0.4f),
        new Color(1f, 0.2f, 1f)
    };

    [Header("UI")]
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI comboRankText;
    public RectTransform comboPanel;
    public float comboFadeTime = 0.3f;

    [Header("Rewards")]
    public int scorePerCombo = 10;
    public int slowMoThreshold = 10;

    private int currentCombo;
    private float comboTimer;
    private int currentRankIndex;
    private bool isComboActive;
    private float comboDisplayTimer;
    private Vector3 comboPanelOriginalScale;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (comboPanel != null)
        {
            comboPanelOriginalScale = comboPanel.localScale;
            comboPanel.gameObject.SetActive(false);
        }
        if (comboRankText != null) comboRankText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isComboActive)
        {
            comboTimer += Time.deltaTime;
            if (comboTimer >= comboWindow)
            {
                EndCombo();
            }
        }

        if (comboDisplayTimer > 0)
        {
            comboDisplayTimer -= Time.deltaTime;
            if (comboDisplayTimer <= 0 && comboPanel != null)
            {
                comboPanel.gameObject.SetActive(false);
                if (comboRankText != null) comboRankText.gameObject.SetActive(false);
            }
        }

        if (isComboActive && comboPanel != null)
        {
            float pulse = Mathf.Sin(Time.time * 10f) * 0.05f + 1f;
            comboPanel.localScale = comboPanelOriginalScale * pulse;
        }
    }

    public void AddHit(bool isKill = false)
    {
        if (!isComboActive)
        {
            StartCombo();
        }
        else
        {
            comboTimer = 0f;
        }

        currentCombo = Mathf.Min(currentCombo + 1, maxCombo);
        UpdateComboUI();

        if (GameManager.Instance != null)
        {
            int bonusScore = currentCombo * scorePerCombo;
            if (isKill) bonusScore *= 2;
            GameManager.Instance.AddScore(bonusScore);
        }

        if (currentCombo >= slowMoThreshold && GameManager.Instance != null)
        {
            float slowDuration = Mathf.Lerp(0.1f, 0.3f, Mathf.InverseLerp(slowMoThreshold, 50, currentCombo));
            GameManager.Instance.HitStop(slowDuration);
        }

        CheckRankUp();
    }

    void StartCombo()
    {
        currentCombo = 1;
        currentRankIndex = -1;
        isComboActive = true;
        comboTimer = 0f;
        comboDisplayTimer = comboWindow + 1f;

        if (comboPanel != null) comboPanel.gameObject.SetActive(true);
        if (comboRankText != null) comboRankText.gameObject.SetActive(false);
        UpdateComboUI();
    }

    void EndCombo()
    {
        isComboActive = false;
        comboDisplayTimer = comboFadeTime;
        
        if (comboRankText != null && currentRankIndex >= 0)
        {
            comboRankText.gameObject.SetActive(true);
            comboRankText.text = comboRanks[currentRankIndex];
            comboRankText.color = rankColors[currentRankIndex];
        }
    }

    void CheckRankUp()
    {
        for (int i = comboThresholds.Length - 1; i >= 0; i--)
        {
            if (currentCombo >= comboThresholds[i] && currentRankIndex < i)
            {
                currentRankIndex = i;
                ShowRankUp(i);
                break;
            }
        }
    }

    void ShowRankUp(int rankIndex)
    {
        if (comboRankText == null) return;

        comboRankText.gameObject.SetActive(true);
        comboRankText.text = comboRanks[rankIndex];
        comboRankText.color = rankColors[rankIndex];
        comboRankText.transform.localScale = Vector3.one * 2f;
        StartCoroutine(AnimateRankUp());
    }

    System.Collections.IEnumerator AnimateRankUp()
    {
        float elapsed = 0f;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = 1f - (1f - t) * (1f - t);
            if (comboRankText != null)
                comboRankText.transform.localScale = Vector3.Lerp(Vector3.one * 2f, Vector3.one, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (comboRankText != null)
            comboRankText.transform.localScale = Vector3.one;
    }

    void UpdateComboUI()
    {
        if (comboText != null)
        {
            comboText.text = currentCombo.ToString();
            comboText.transform.localScale = Vector3.one * 1.3f;
            StartCoroutine(ScaleBack(comboText.transform));
        }
    }

    System.Collections.IEnumerator ScaleBack(Transform t)
    {
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            t.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, elapsed / 0.1f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    public int GetCombo() => currentCombo;
    public bool IsActive() => isComboActive;
}