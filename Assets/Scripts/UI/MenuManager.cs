using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject pauseMenuPanel;
    public GameObject gameOverMenuPanel;
    public GameObject settingsPanel;

    [Header("Main Menu")]
    public Button startButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Pause Menu")]
    public Button resumeButton;
    public Button restartButton;
    public Button pauseSettingsButton;
    public Button pauseQuitButton;

    [Header("Game Over Menu")]
    public TextMeshProUGUI gameOverScoreText;
    public TextMeshProUGUI gameOverWaveText;
    public TextMeshProUGUI gameOverKillsText;
    public Button retryButton;
    public Button mainMenuButton;
    public Button gameOverQuitButton;

    [Header("Settings")]
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public Toggle fullscreenToggle;
    public Button settingsBackButton;

    [Header("Visual")]
    public Image backgroundImage;
    public ParticleSystem menuParticles;
    public CanvasGroup mainMenuCanvasGroup;
    public CanvasGroup pauseMenuCanvasGroup;
    public CanvasGroup gameOverCanvasGroup;
    public CanvasGroup settingsCanvasGroup;

    [Header("Version")]
    public TextMeshProUGUI versionText;

    private bool isPaused = false;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (pauseSettingsButton != null) pauseSettingsButton.onClick.AddListener(OpenSettings);
        if (pauseQuitButton != null) pauseQuitButton.onClick.AddListener(QuitToMainMenu);
        if (retryButton != null) retryButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(QuitToMainMenu);
        if (gameOverQuitButton != null) gameOverQuitButton.onClick.AddListener(QuitGame);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);

        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (versionText != null) versionText.text = "v1.0.0";

        Time.timeScale = 0f;
        SetupCanvasGroups();
        ShowMainMenuInstant();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isGameOver = true;
        }

        if (menuParticles != null) menuParticles.Play();
    }

    void SetupCanvasGroups()
    {
        if (mainMenuCanvasGroup == null && mainMenuPanel != null)
            mainMenuCanvasGroup = mainMenuPanel.GetComponent<CanvasGroup>() ?? mainMenuPanel.AddComponent<CanvasGroup>();
        if (pauseMenuCanvasGroup == null && pauseMenuPanel != null)
            pauseMenuCanvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>() ?? pauseMenuPanel.AddComponent<CanvasGroup>();
        if (gameOverCanvasGroup == null && gameOverMenuPanel != null)
            gameOverCanvasGroup = gameOverMenuPanel.GetComponent<CanvasGroup>() ?? gameOverMenuPanel.AddComponent<CanvasGroup>();
        if (settingsCanvasGroup == null && settingsPanel != null)
            settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>() ?? settingsPanel.AddComponent<CanvasGroup>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;

            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
                return;
            }

            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void StartGame()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeAndStart());
    }

    IEnumerator FadeAndStart()
    {
        yield return FadeCanvasGroup(mainMenuCanvasGroup, 1f, 0f, 0.3f);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.OnGameStart();
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        if (menuParticles != null) menuParticles.Stop();
    }

    public void PauseGame()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        isPaused = true;
        Time.timeScale = 0f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(pauseMenuCanvasGroup, 0f, 1f, 0.2f));
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (menuParticles != null) menuParticles.Play();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeAndHide(pauseMenuCanvasGroup, pauseMenuPanel, 0.2f));
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (menuParticles != null) menuParticles.Stop();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetGame();
        }
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        isPaused = true;
        Time.timeScale = 0f;
        
        if (gameOverMenuPanel != null) gameOverMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(mainMenuCanvasGroup, 0f, 1f, 0.4f));
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (menuParticles != null) menuParticles.Play();
    }

    void ShowMainMenuInstant()
    {
        if (gameOverMenuPanel != null) gameOverMenuPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        
        if (mainMenuCanvasGroup != null) mainMenuCanvasGroup.alpha = 1f;
        if (pauseMenuCanvasGroup != null) pauseMenuCanvasGroup.alpha = 0f;
        if (gameOverCanvasGroup != null) gameOverCanvasGroup.alpha = 0f;
        if (settingsCanvasGroup != null) settingsCanvasGroup.alpha = 0f;
    }

    public void ShowGameOver(int score, int wave, int kills)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(ShowGameOverRoutine(score, wave, kills));
    }

    IEnumerator ShowGameOverRoutine(int score, int wave, int kills)
    {
        yield return FadeCanvasGroup(pauseMenuCanvasGroup, 1f, 0f, 0.15f);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        
        if (gameOverMenuPanel != null) gameOverMenuPanel.SetActive(true);
        if (gameOverScoreText != null) gameOverScoreText.text = $"SCORE: {score:N0}";
        if (gameOverWaveText != null) gameOverWaveText.text = $"WAVE REACHED: {wave}";
        if (gameOverKillsText != null) gameOverKillsText.text = $"KILLS: {kills}";
        
        yield return FadeCanvasGroup(gameOverCanvasGroup, 0f, 1f, 0.5f);
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        
        CanvasGroup fromGroup = mainMenuPanel?.activeSelf == true ? mainMenuCanvasGroup : pauseMenuCanvasGroup;
        GameObject fromPanel = mainMenuPanel?.activeSelf == true ? mainMenuPanel : pauseMenuPanel;
        
        fadeCoroutine = StartCoroutine(SwitchPanel(fromGroup, fromPanel, settingsCanvasGroup, settingsPanel, 0.3f));
    }

    public void CloseSettings()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        
        GameObject toPanel = isPaused ? pauseMenuPanel : mainMenuPanel;
        CanvasGroup toGroup = isPaused ? pauseMenuCanvasGroup : mainMenuCanvasGroup;
        
        fadeCoroutine = StartCoroutine(SwitchPanel(settingsCanvasGroup, settingsPanel, toGroup, toPanel, 0.3f));
    }

    IEnumerator SwitchPanel(CanvasGroup fromGroup, GameObject fromPanel, CanvasGroup toGroup, GameObject toPanel, float duration)
    {
        yield return FadeCanvasGroup(fromGroup, 1f, 0f, duration);
        if (fromPanel != null) fromPanel.SetActive(false);
        if (toPanel != null) toPanel.SetActive(true);
        yield return FadeCanvasGroup(toGroup, 0f, 1f, duration);
    }

    IEnumerator FadeAndHide(CanvasGroup group, GameObject panel, float duration)
    {
        yield return FadeCanvasGroup(group, 1f, 0f, duration);
        if (panel != null) panel.SetActive(false);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;
        
        float elapsed = 0f;
        group.alpha = from;
        group.interactable = to > 0f;
        group.blocksRaycasts = to > 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            var field = typeof(PlayerController).GetField("mouseSensitivity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(player, value);
        }
    }

    void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
    }
}