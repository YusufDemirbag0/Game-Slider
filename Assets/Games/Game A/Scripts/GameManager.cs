using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;

    [Header("Buttons (optional)")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button homeButton;

    [Header("Yol / Hız")]
    [SerializeField] private RobustRoadScroller3D roadScroller;
    [SerializeField] private float baseSpeed = 10f;
    [SerializeField] private float accelPerSec = 0.1f;

    [Header("Skor")]
    [SerializeField] private float scorePerSpeedUnit = 1.0f;

    [Header("Game Over Popup (DOTween)")]
    [SerializeField] private CanvasGroup gameOverGroup;
    [SerializeField] private RectTransform gameOverRect;
    [SerializeField] private float popupDur = 0.35f;
    [SerializeField] private float popupScaleHidden = 0.85f;
    [SerializeField] private float popupScaleShown = 2f;
    [SerializeField] private Ease popupEaseIn = Ease.OutBack;
    [SerializeField] private Ease popupEaseOut = Ease.InBack;

    [Header("Score FX (Instant Scale)")]
    [SerializeField] private float bonusScale = 1.7f;
    [SerializeField] private float bonusScaleDuration = 0.15f;
    private Vector3 scoreDefaultScale;

    [Header("Continue (Revive)")]
    [SerializeField] private bool allowOneContinue = true;
    [SerializeField] private float reviveIFrames = 2f;
    [SerializeField] private float clearObstaclesRadius = 12f;
    [SerializeField] private Transform player;

    float score;
    bool isGameOver;
    bool usedContinue;
    float reviveSafeUntil = -1f;

    // ---- MESAFE SAATİ ----
    float distanceMeters;                                   // toplam kat edilen yol
    public float DistanceMeters => distanceMeters;          // diğerleri buradan okur

    public bool IsGameOver => isGameOver;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (roadScroller) roadScroller.SetSpeed(baseSpeed);
        score = 0f; isGameOver = false; usedContinue = false;

        if (gameOverPanel)
        {
            if (!gameOverRect)  gameOverRect = gameOverPanel.GetComponent<RectTransform>();
            if (!gameOverGroup) gameOverGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (!gameOverGroup) gameOverGroup = gameOverPanel.AddComponent<CanvasGroup>();
            HideGameOverImmediate();
        }
        if (continueButton) continueButton.onClick.AddListener(Continue);
        if (restartButton)  restartButton.onClick.AddListener(Retry);
        if (homeButton)     homeButton.onClick.AddListener(Home);

        if (scoreText) scoreDefaultScale = scoreText.transform.localScale;

        Time.timeScale = 1f;
        UpdateScoreUI();
    }

    void Update()
    {
        if (isGameOver) return;

        // Yol hızını kademeli artır
        float s = roadScroller ? roadScroller.GetSpeed() : baseSpeed;
        s += accelPerSec * Time.deltaTime;
        if (roadScroller) roadScroller.SetSpeed(s);

        // ---- MESAFE GÜNCELLE ---- (düşük FPS koruması)
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
        distanceMeters += s * dt;

        // Skor
        score += s * scorePerSpeedUnit * Time.deltaTime;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = $"Score: {Mathf.FloorToInt(score)}";
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    public float GetRoadSpeed() => roadScroller ? roadScroller.GetSpeed() : baseSpeed;

    public void GameOver()
    {
        if (Time.unscaledTime < reviveSafeUntil) return;
        if (isGameOver) return;

        isGameOver = true;

        int s = Mathf.FloorToInt(score);
        if (finalScoreText) finalScoreText.text = $"Score: {s}";

        int best = PlayerPrefs.GetInt("BestScore", 0);
        if (s > best) { best = s; PlayerPrefs.SetInt("BestScore", best); PlayerPrefs.Save(); }
        if (highScoreText) highScoreText.text = $"Best: {best}";

        if (continueButton) continueButton.gameObject.SetActive(allowOneContinue && !usedContinue);
        ShowGameOverAnimated();
    }

    public void Retry()
    {
        if (gameOverPanel && gameOverPanel.activeSelf)
            HideGameOverAnimatedAnd(() =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void Home()
    {
        if (gameOverPanel && gameOverPanel.activeSelf)
            HideGameOverAnimatedAnd(() =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("SliderScene");
            });
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("SliderScene");
        }
    }

    public void Continue()
    {
        if (usedContinue && allowOneContinue) return;

        HideGameOverAnimatedAnd(() =>
        {
            foreach (var car in FindObjectsOfType<PrefabCar>(true))
                if (car) Destroy(car.gameObject);

            isGameOver = false;
            Time.timeScale = 1f;
            reviveSafeUntil = Time.unscaledTime + reviveIFrames;

            usedContinue = true;
            if (continueButton && allowOneContinue)
                continueButton.gameObject.SetActive(false);
        });
    }

    void HideGameOverImmediate()
    {
        gameOverPanel.SetActive(false);
        gameOverGroup.alpha = 0f;
        gameOverGroup.interactable = false;
        gameOverGroup.blocksRaycasts = false;
        if (gameOverRect) gameOverRect.localScale = Vector3.one * popupScaleHidden;
    }

    void ShowGameOverAnimated()
    {
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
        gameOverGroup.alpha = 0f;
        gameOverGroup.interactable = false;
        gameOverGroup.blocksRaycasts = false;
        if (gameOverRect) gameOverRect.localScale = Vector3.one * popupScaleHidden;

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(gameOverGroup.DOFade(1f, popupDur));
        if (gameOverRect)
            seq.Join(gameOverRect.DOScale(popupScaleShown, popupDur).SetEase(popupEaseIn));
        seq.OnComplete(() =>
        {
            gameOverGroup.interactable = true;
            gameOverGroup.blocksRaycasts = true;
        });
    }

    void HideGameOverAnimatedAnd(System.Action onComplete)
    {
        gameOverGroup.interactable = false;
        gameOverGroup.blocksRaycasts = false;

        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(gameOverGroup.DOFade(0f, popupDur * 0.8f));
        if (gameOverRect)
            seq.Join(gameOverRect.DOScale(popupScaleHidden, popupDur * 0.8f).SetEase(popupEaseOut));
        seq.OnComplete(() =>
        {
            gameOverPanel.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public void AnimateBonus(int delta)
    {
        if (!scoreText) return;
        scoreText.transform.DOKill(true);
        scoreText.transform.localScale = scoreDefaultScale;
        scoreText.transform
                 .DOScale(scoreDefaultScale * bonusScale, bonusScaleDuration)
                 .SetEase(Ease.OutQuad)
                 .SetLoops(2, LoopType.Yoyo)
                 .SetUpdate(true);
    }
}
