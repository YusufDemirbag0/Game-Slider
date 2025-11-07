// BallGameManager.cs
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BallGameManager : MonoBehaviour
{
    public static BallGameManager Instance { get; private set; }

    [Header("Lose Line (World Trigger)")]
    [SerializeField] BoxCollider2D worldLoseTrigger; // isTrigger=true, kutu içinde limit yüksekliğinde ince yatay çizgi
    [SerializeField] string ballTag = "Ball";
    [SerializeField] float upwardThreshold = 0.02f;   // yalnızca yukarı yönde keserse game over

    [Header("Game Over UI (DOTween)")]
    [SerializeField] CanvasGroup gameOverPanel;       // başta inactive
    [SerializeField] RectTransform popupRoot;         // panel içindeki popup kökü (scale animasyonu)
    [SerializeField] Image dimmer;                    // tam ekran siyah Image (alpha=0, Raycast OFF)
    [SerializeField] float dimAlpha = 0.55f;
    [SerializeField] float popupScaleTime = 0.45f;

    [Header("Buttons (Inspector’dan bağla)")]
    public UnityEvent OnReset;                        // sahneyi temizleme/yeniden başlatma
    public UnityEvent OnContinue;                     // kaldığı yerden devam

    [Header("Score")]
    [SerializeField] TextMeshProUGUI scoreText;       // sol üstteki TMP
    [SerializeField] int[] pointsByLevel;             // boşsa formül: 10 * 2^(level-1)
    [SerializeField] float comboWindow = 1.2f;        // şu süre içinde merge gelirse combo artar
    [SerializeField] int   maxCombo = 5;

    int score;
    int combo = 1;
    float lastMergeTime = -999f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        HideGameOverInstant();

        if (!worldLoseTrigger)
        {
            Debug.LogError("[BallGameManager] worldLoseTrigger atanmamış. Kutu içinde limit yüksekliğine ince bir BoxCollider2D (isTrigger=true) yerleştirip buraya ata.");
            return;
        }

        worldLoseTrigger.isTrigger = true;
        var relay = worldLoseTrigger.gameObject.AddComponent<TriggerRelay>();
        relay.Init(ballTag, upwardThreshold, TriggerGameOver);
    }

    // ----------------- SCORE API -----------------
    public void OnMerged(int newLevel)
    {
        // combo
        if (Time.time - lastMergeTime <= comboWindow) combo = Mathf.Min(combo + 1, maxCombo);
        else combo = 1;
        lastMergeTime = Time.time;

        // puan
        int basePts = GetPointsForLevel(newLevel);
        AddScore(basePts * combo);
    }

    public void ResetScoreHUD()
    {
        score = 0; combo = 1; lastMergeTime = -999f;
        UpdateScoreUI();
    }

    int GetPointsForLevel(int level)
    {
        if (pointsByLevel != null && pointsByLevel.Length > 0)
        {
            int idx = Mathf.Clamp(level - 1, 0, pointsByLevel.Length - 1);
            return pointsByLevel[idx];
        }
        return 10 * (int)Mathf.Pow(2, Mathf.Max(0, level - 1)); // default
    }

    void AddScore(int amount)
    {
        if (amount == 0) return;
        score += amount;
        UpdateScoreUI();
    }

    void UpdateScoreUI(){ if (scoreText) scoreText.text = $"SCORE: {score}"; }

    // ----------------- GAME OVER (FireBallManager tarzı) -----------------
    void TriggerGameOver()
    {
        Time.timeScale = 0f;

        if (dimmer) dimmer.DOFade(dimAlpha, 0.25f).SetUpdate(true);

        gameOverPanel.gameObject.SetActive(true);
        gameOverPanel.alpha = 0f;
        gameOverPanel.interactable = false;
        gameOverPanel.blocksRaycasts = false;

        if (popupRoot)
        {
            popupRoot.localScale = Vector3.zero;
            popupRoot.DOScale(2f, popupScaleTime).SetEase(Ease.OutBack).SetUpdate(true);
        }

        gameOverPanel.DOFade(2f, 0.25f).SetUpdate(true).OnComplete(() =>
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
        if (dimmer) dimmer.color = new Color(0,0,0,0);
        UpdateScoreUI();
    }

    // ----------------- BUTTON HOOKS -----------------
    public void OnResetPressed()
    {
        Time.timeScale = 1f;
        HideGameOverInstant();
        ResetScoreHUD();
        OnReset?.Invoke(); // sahneyi temizleme / restart işlemlerini Inspector’dan bağla
    }

    public void OnContinuePressed()
    {
        Time.timeScale = 1f;
        HideGameOverInstant();
        OnContinue?.Invoke(); // kaldığı yerden devam mantığını Inspector’dan bağla
    }

    // ----------------- İç sınıf: çizgi tetikleyici -----------------
    class TriggerRelay : MonoBehaviour
    {
        string ballTag; float upwardThr; System.Action onUpHit;

        public void Init(string tag, float thr, System.Action cb)
        {
            ballTag = tag; upwardThr = thr; onUpHit = cb;
        }

        // Enter + Stay: Top çizgi içinde yukarı yön başlarsa da yakalayalım
        void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
        void OnTriggerStay2D (Collider2D other) { TryHit(other); }

        void TryHit(Collider2D other)
        {
            if (!other.CompareTag(ballTag)) return;
            var rb = other.attachedRigidbody; if (!rb) return;

            // İlk düşüş (velocity.y < 0) sayılmaz; yalnızca YUKARI temas (istif yükselmesi) sayılır.
            if (rb.linearVelocity.y > upwardThr) onUpHit?.Invoke();
        }
    }
}
