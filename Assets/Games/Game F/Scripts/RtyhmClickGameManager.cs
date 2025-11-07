using UnityEngine;
using TMPro;

public class RhythmClickGameManager : MonoBehaviour
{
    public static RhythmClickGameManager I;

    [Header("UI (opsiyonel)")]
    [SerializeField] TMP_Text scoreText;
    [SerializeField] CanvasGroup gameOverGroup;
    [SerializeField] TMP_Text finalScoreText;

    [Header("Core")]
    [SerializeField] BubbleSpawner bubbleSpawner;

    [Header("Prefabs (WORLD)")]
    public ClickBubble bubblePrefab;
    public UIRipple ripplePrefab;

    [Header("Continue")]
    [SerializeField] bool allowOneContinue = true;   // tek hak
    [SerializeField] float continueDelay   = 0.6f;   // devamdan önce ufak nefes
    bool usedContinue;

    int score;
    bool gameOver;

    void Awake()
    {
        I = this;
        Time.timeScale = 1f;
        score = 0;
        usedContinue = false;
        SetGameOver(false);
        UpdateScore();
    }

    void Start() => bubbleSpawner.Begin();

    public void OnBubbleClicked(Vector3 worldPos)
    {
        if (gameOver) return;
        score++;
        UpdateScore();
        bubbleSpawner.OnSuccessfulHit();
        SpawnRipple(worldPos);
    }

    public void OnBubbleMissed()
    {
        if (gameOver) return;
        gameOver = true;

        // spawner'ı durdur (progresyon korunur)
        bubbleSpawner.StopSpawning();

        if (finalScoreText) finalScoreText.text = $"Score: {score}";
        SetGameOver(true);
    }

    // UI -> Retry Button
    public void Retry()
    {
        gameOver = false;
        usedContinue = false;
        score = 0;
        UpdateScore();
        SetGameOver(false);

        bubbleSpawner.ResetSpawner();   // baştan başla
        bubbleSpawner.Begin();
    }

    // UI -> Continue Button
    public void Continue()
    {
        if (!allowOneContinue || usedContinue || !gameOver) return;

        usedContinue = true;
        gameOver = false;
        SetGameOver(false);

        // kaldığı hız/ömür/ölçekle devam
        bubbleSpawner.ContinueFromPause(continueDelay);
    }

    void SpawnRipple(Vector3 worldPos)
    {
        var r = Instantiate(ripplePrefab, worldPos, Quaternion.identity);
        r.Play();
    }

    void UpdateScore(){ if (scoreText) scoreText.text = $"SCORE: {score}"; }

    void SetGameOver(bool show)
    {
        if (!gameOverGroup) return;
        gameOverGroup.alpha = show ? 1f : 0f;
        gameOverGroup.interactable = show;
        gameOverGroup.blocksRaycasts = show;
    }
}
