using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;


public class FireBallManager : MonoBehaviour
{
    public static FireBallManager Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] Camera cam;
    [SerializeField] ArcadeBallKinematic ball;
    [SerializeField] Transform spawnPoint;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] CanvasGroup gameOverPanel;
    [SerializeField] RectTransform popupRoot;
    [SerializeField] Image dimmer;

    [Header("Popup")]
    [SerializeField] float dimAlpha = 0.55f;
    [SerializeField] float popupScaleTime = 0.45f;

    public int BounceNumber { get; private set; } = 0;
    public int ScorePoint { get; private set; } = 0;
    int TotalScore => BounceNumber + ScorePoint;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (!cam) cam = Camera.main;
    }

    void Start()
    {
        UpdateScoreUI();
        HideGameOverInstant();
        if (dimmer) dimmer.color = new Color(0, 0, 0, 0);

        ball.onPaddleBounce += OnBallBounce;
        ball.onDeath += HandleBallDeath;

        // Oyuna her zaman SpawnPoint(0,0,0) 'dan başla
        ball.ResetFromSpawnPoint(spawnPoint.position);
    }

    void OnDestroy()
    {
        if (!ball) return;
        ball.onDeath -= HandleBallDeath;
        ball.onPaddleBounce -= OnBallBounce;
    }

    // ---------- Score ----------
    void OnBallBounce()
    {
        BounceNumber++;
        UpdateScoreUI();
        CheckSpeedMilestones();
    }

    public void AddScorePoint(int amount)
    {
        ScorePoint += amount;
        UpdateScoreUI();
        CheckSpeedMilestones();
    }

    void CheckSpeedMilestones()
    {
        if (!ball) return;
        int total = BounceNumber + ScorePoint;

        // Her milestone'da seviye artışı + hızlanma + efekt
        if (total >= 60)
            ball.ApplySpeedTier(3);
        else if (total >= 40)
            ball.ApplySpeedTier(2);
        else if (total >= 20)
            ball.ApplySpeedTier(1);
        else
            ball.ApplySpeedTier(0);
    }

    void UpdateScoreUI() => scoreText.text = $"SCORE: {TotalScore}";

    // ---------- Game Over ----------
    void HandleBallDeath()
    {
        Time.timeScale = 0f;

        if (dimmer)
            dimmer.DOFade(dimAlpha, 0.25f).SetUpdate(true);

        gameOverPanel.gameObject.SetActive(true);
        gameOverPanel.alpha = 0f;
        gameOverPanel.interactable = false;
        gameOverPanel.blocksRaycasts = false;

        if (popupRoot)
        {
            popupRoot.localScale = Vector3.zero;
            popupRoot.DOScale(1f, popupScaleTime).SetEase(Ease.OutBack).SetUpdate(true);
        }

        gameOverPanel.DOFade(1f, 0.25f).SetUpdate(true).OnComplete(() =>
        {
            gameOverPanel.interactable = true;
            gameOverPanel.blocksRaycasts = true;
        });
    }

    void HideGameOverInstant()
    {
        if (gameOverPanel)
        {
            gameOverPanel.alpha = 0f;
            gameOverPanel.interactable = false;
            gameOverPanel.blocksRaycasts = false;
            gameOverPanel.gameObject.SetActive(false);
        }
        if (dimmer)
            dimmer.color = new Color(0, 0, 0, 0);
    }

    // ---------- Buttons ----------
    public void OnResetPressed()
    {
        Time.timeScale = 1f;
        HideGameOverInstant();

        // Ekranı temizle (extra script yok): Tag kullan
        KillByTag("Obstacle");
        KillByTag("Pickup");

        BounceNumber = 0;
        ScorePoint = 0;
        UpdateScoreUI();

        ball.ResetTierAndBounce();                       // tier=1, bounce=0
        ball.ResetFromSpawnPoint(spawnPoint.position);   // tamamen baştan
    }

    public void OnContinuePressed()
    {
        Time.timeScale = 1f;
        HideGameOverInstant();

        // Tier/bounce korunur; spawnPoint'ten tekrar başla
        ball.ContinueFromSpawnPoint(spawnPoint.position);
    }

    void KillByTag(string tag)
    {
        var arr = GameObject.FindGameObjectsWithTag(tag);
        for (int i = 0; i < arr.Length; i++)
            Destroy(arr[i]);
    }
}
