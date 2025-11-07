using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class CardGameSetup : MonoBehaviour
{
    [Header("Kart Prefabları (en az rows*columns/2 farklı)")]
    public GameObject[] cardPrefabs;

    [Header("Grid")]
    public int rows = 3;
    public int columns = 2;

    [Header("Otomatik Yerleşim")]
    [Range(0f, 0.25f)]
    public float outerPaddingPercent = 0.06f;

    [Header("UI")]
    public TMP_Text timerText;
    public TMP_Text levelText; // sadece tek sayı (1..5)

    [Header("Game Over")]
    public RectTransform gameOverRoot;
    public CanvasGroup gameOverCg;
    public TMP_Text gameOverTitle;

    [Header("Level/Timer")]
    public int[] levelCardCounts = new int[] { 4, 6, 8, 10, 12 };
    public int[] levelTimesSec   = new int[] { 20, 30, 40, 50, 60 };
    public int continueBonusSec  = 15;
    public float revealTime      = 1.2f;

    [Header("Görsel Geçişler (opsiyonel)")]
    public CanvasGroup screenFlash;     // tam ekran beyaz imaj + CanvasGroup (alpha 0→1→0)
    public float screenFlashTime = 0.22f;

    [Header("Timer Uyarı (opsiyonel)")]
    public int lowTimeThreshold = 5;
    public Color lowTimeColor = Color.red;
    private Color timerBaseColor = Color.white;
    private bool lowWarnActive = false;
    private Tween timerPulseTw;

    [Header("Global Sesler (opsiyonel)")]
    public AudioClip sfxLevelUp;
    public AudioClip sfxGameOver;

    private Camera cam;

    // seçim
    private Card firstSelected, secondSelected;

    // sadece mismatch kapanırken kısa kilit
    private bool inputLocked = false;

    // timer / level
    private bool timerRunning = false;
    private bool inGameOver = false;
    private int currentLevelIndex = 0;
    private float timeLeft = 0f;

    // stabilite: eşleşen kartlar arkada yok olurken oyun devam edebilsin
    private int remainingPairs = 0;
    private int activeVanishCount = 0;     // şu an animasyonla yok olan kart sayısı
    private bool pendingLevelAdvance = false;

    void Start()
    {
        cam = Camera.main;
        InstantHideGameOver();

        if (timerText) timerBaseColor = timerText.color;

        StartLevel(currentLevelIndex);
    }

    void Update()
    {
        if (!timerRunning || inGameOver) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft < 0f) timeLeft = 0f;
        UpdateTimerUI();
        if (timeLeft <= 0f) TimerFailed();
    }

    // ---------------- Level Akışı ----------------

    void StartLevel(int levelIdx)
    {
        inGameOver = false;
        InstantHideGameOver();

        int totalCards = levelCardCounts[Mathf.Clamp(levelIdx, 0, levelCardCounts.Length - 1)];

        // GRID KONFİG (kullanıcı isteğine göre)
        switch (totalCards)
        {
            case 4:  rows = 2; columns = 2; break; // 4 kart
            case 6:  rows = 3; columns = 2; break; // 6 kart
            case 8:  rows = 4; columns = 2; break; // 8 kart
            case 10: rows = 5; columns = 2; break; // 10 kart
            case 12: rows = 4; columns = 3; break; // 12 kart
            default:
                columns = 2;
                rows = Mathf.CeilToInt(totalCards / (float)columns);
                break;
        }

        remainingPairs = totalCards / 2;
        activeVanishCount = 0;
        pendingLevelAdvance = false;

        timeLeft = levelTimesSec[Mathf.Clamp(levelIdx, 0, levelTimesSec.Length - 1)];
        ResetLowTimeUiState();
        UpdateTimerUI();
        UpdateLevelUI(levelIdx);

        StartCoroutine(SetupRoutine(totalCards));
    }

    IEnumerator SetupRoutine(int totalCards)
    {
        timerRunning = false;
        inputLocked = true;

        // temizle
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        // deste hazırla
        int unique = totalCards / 2;
        var pool = new List<int>();
        for (int i = 0; i < cardPrefabs.Length; i++) pool.Add(i);
        Shuffle(pool);

        var deck = new List<(GameObject prefab, int pairId)>();
        for (int i = 0; i < unique; i++)
        {
            deck.Add((cardPrefabs[pool[i]], i));
            deck.Add((cardPrefabs[pool[i]], i));
        }
        Shuffle(deck);

        // spawn
        SpawnCards(deck, out float usedScale);

        // Reveal → bekle → kapat
        yield return FlipAllToFront(0.35f);
        yield return new WaitForSeconds(revealTime);
        yield return FlipAllToBack(0.45f);

        foreach (Transform t in transform)
        {
            var c = t.GetComponent<Card>();
            if (c) c.SetInteractable(true);
        }

        timerRunning = true;
        inputLocked = false;
    }

    void UpdateLevelUI(int levelIdx)
    {
        if (!levelText) return;
        // küçük seviye bounce
        levelText.text = (levelIdx + 1).ToString();
        levelText.transform.DOKill();
        levelText.transform.localScale = Vector3.one;
        levelText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 6, 0.8f);
    }

    void UpdateTimerUI()
    {
        if (!timerText) return;
        int t = Mathf.CeilToInt(timeLeft);
        int m = t / 60;
        int s = t % 60;
        timerText.text = $"{m:00}:{s:00}";

        // Low time uyarı girişi/çıkışı
        if (t <= lowTimeThreshold && !lowWarnActive)
        {
            lowWarnActive = true;
            timerText.DOColor(lowTimeColor, 0.2f);
            timerPulseTw?.Kill();
            timerPulseTw = timerText.transform.DOPunchScale(Vector3.one * 0.25f, 0.4f, 8, 0.9f).SetLoops(-1, LoopType.Restart);
        }
        else if (t > lowTimeThreshold && lowWarnActive)
        {
            ResetLowTimeUiState();
        }
    }

    void ResetLowTimeUiState()
    {
        if (!timerText) return;
        lowWarnActive = false;
        timerPulseTw?.Kill();
        timerText.color = timerBaseColor;
        timerText.transform.localScale = Vector3.one;
    }

    // --------------- Game Over ----------------

    void TimerFailed()
    {
        timerRunning = false;
        inputLocked = true;

        if (sfxGameOver) AudioSource.PlayClipAtPoint(sfxGameOver, Vector3.zero);
        ShowGameOverAnimated();
    }

    void UpdateGameOverTitle()
    {
        int reachedLevel = Mathf.Clamp(currentLevelIndex + 1, 1, levelCardCounts.Length);
        if (gameOverTitle) gameOverTitle.text = $"Your Score: Level {reachedLevel}";
    }

    void InstantHideGameOver()
    {
        if (gameOverRoot) gameOverRoot.gameObject.SetActive(false);
        if (gameOverCg)
        {
            gameOverCg.alpha = 0f;
            gameOverCg.blocksRaycasts = false;
            gameOverCg.interactable = false;
        }
        inGameOver = false;
    }

    void ShowGameOverAnimated()
    {
        inGameOver = true;
        UpdateGameOverTitle();
        if (!gameOverRoot || !gameOverCg) return;

        gameOverRoot.gameObject.SetActive(true);
        gameOverRoot.localScale = Vector3.one * 0.85f;
        gameOverCg.alpha = 0f;
        gameOverCg.blocksRaycasts = true;
        gameOverCg.interactable = true;

        DOTween.Sequence()
            .Append(gameOverCg.DOFade(1f, 0.18f))
            .Join(gameOverRoot.DOScale(1f, 0.32f).SetEase(Ease.OutBack));
    }

    IEnumerator HideGameOverAnimated()
    {
        if (!gameOverRoot || !gameOverCg) yield break;

        var seq = DOTween.Sequence();
        seq.Append(gameOverCg.DOFade(0f, 0.18f));
        seq.Join(gameOverRoot.DOScale(0.92f, 0.22f).SetEase(Ease.InBack));
        yield return seq.WaitForCompletion();

        gameOverCg.blocksRaycasts = false;
        gameOverCg.interactable = false;
        gameOverRoot.gameObject.SetActive(false);
        inGameOver = false;
    }

    // UI Butonları
    public void ContinueGame() { if (!inGameOver) return; StartCoroutine(CoContinue()); }
    IEnumerator CoContinue()
    {
        yield return HideGameOverAnimated();
        timeLeft += continueBonusSec;
        UpdateTimerUI();
        timerRunning = true;
        inputLocked = false;
    }

    public void RetryGame() { StartCoroutine(CoRetry()); }
    IEnumerator CoRetry()
    {
        if (inGameOver) yield return HideGameOverAnimated();
        currentLevelIndex = 0;
        StartLevel(currentLevelIndex);
    }

    // --------------- Spawn / Yerleşim ---------------

    void SpawnCards(List<(GameObject prefab, int pairId)> cards, out float outScale)
    {
        outScale = 1f;
        if (cards == null || cards.Count == 0) return;

        // referans boyut
        float refW = 1f, refH = 1f;
        var sr0 = cards[0].prefab.GetComponentInChildren<SpriteRenderer>();
        if (sr0 && sr0.sprite)
        { var b = sr0.sprite.bounds.size; refW = b.x; refH = b.y; }
        else
        {
            var bc = cards[0].prefab.GetComponent<BoxCollider2D>();
            if (bc) { refW = bc.size.x; refH = bc.size.y; }
        }
        float refSize = Mathf.Max(refW, refH);
        refW = refH = refSize;

        // kamera alanı
        float camH = Camera.main.orthographicSize * 2f;
        float camW = camH * Camera.main.aspect;

        // dış boşluk
        float padX = camW * Mathf.Clamp01(outerPaddingPercent);
        float padY = camH * Mathf.Clamp01(outerPaddingPercent);

        // kart boyutu
        float maxSizeX = (camW - 2f * padX) / Mathf.Max(1, columns);
        float maxSizeY = (camH - 2f * padY) / Mathf.Max(1, rows);
        float cardSize = Mathf.Max(0.0001f, Mathf.Min(maxSizeX, maxSizeY));
        float scale = cardSize / refSize;
        outScale = scale;

        // iç boşluk
        float usedW = columns * cardSize;
        float usedH = rows * cardSize;
        float remainW = Mathf.Max(0f, (camW - 2f * padX) - usedW);
        float remainH = Mathf.Max(0f, (camH - 2f * padY) - usedH);
        float innerX = (columns > 1) ? remainW / (columns - 1) : 0f;
        float innerY = (rows > 1) ? remainH / (rows - 1) : 0f;

        float gridW = usedW + innerX * (columns - 1);
        float gridH = usedH + innerY * (rows - 1);
        float startX = -gridW / 2f + cardSize / 2f;
        float startY =  gridH / 2f - cardSize / 2f;

        int idx = 0; float appearDur = 0.35f;

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < columns; c++)
        {
            float x = startX + c * (cardSize + innerX);
            float y = startY - r * (cardSize + innerY);
            Vector3 pos = new Vector3(x, y, 0);

            var spec = cards[idx++];
            var go = Instantiate(spec.prefab, transform);
            go.transform.position = pos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            foreach (var r2 in go.GetComponentsInChildren<SpriteRenderer>())
            {
                r2.flipX = false; r2.flipY = false;
            }

            var card = go.GetComponent<Card>();
            if (!card) card = go.AddComponent<Card>();
            card.Init(spec.pairId, OnCardClicked);
            card.SetInteractable(false);

            float delay = (r * columns + c) * 0.035f;
            card.Appear(scale, appearDur, delay);
        }
    }

    IEnumerator FlipAllToFront(float duration)
    {
        var tws = new List<Tween>();
        foreach (Transform t in transform)
        {
            var c = t.GetComponent<Card>();
            if (c) { var tw = c.FlipToFront(duration); if (tw != null) tws.Add(tw); }
        }
        foreach (var tw in tws) if (tw != null) yield return tw.WaitForCompletion();
    }

    IEnumerator FlipAllToBack(float duration)
    {
        var tws = new List<Tween>();
        foreach (Transform t in transform)
        {
            var c = t.GetComponent<Card>();
            if (c) { var tw = c.FlipToBack(duration); if (tw != null) tws.Add(tw); }
        }
        foreach (var tw in tws) if (tw != null) yield return tw.WaitForCompletion();
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    // --------------- Tıklama & Eşleştirme ---------------

    void OnCardClicked(Card card)
    {
        if (inGameOver) return;
        if (card.IsMatched) return;
        if (inputLocked) return; // sadece mismatch kapanırken

        StartCoroutine(CoHandleClick(card));
    }

    IEnumerator CoHandleClick(Card card)
    {
        // 1. seçim
        if (firstSelected == null)
        {
            firstSelected = card;
            yield break;
        }

        if (card == firstSelected) yield break;

        // 2. seçim
        if (secondSelected == null)
        {
            secondSelected = card;

            // iki kartın flip'inin bittiğini bekle
            yield return new WaitUntil(() => !firstSelected.IsFlipping && !secondSelected.IsFlipping);

            yield return EvaluatePair();
        }
    }

    IEnumerator EvaluatePair()
    {
        bool isMatch = firstSelected.PairId == secondSelected.PairId;

        if (isMatch)
        {
            // küçük sevinç
            var j1 = firstSelected.transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 6, 0.8f);
            var j2 = secondSelected.transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 6, 0.8f);
            yield return DOTween.Sequence().Join(j1).Join(j2).WaitForCompletion();

            // seçimleri bırak → kartları düşür
            var a = firstSelected; var b = secondSelected;
            firstSelected = null; secondSelected = null;

            a.MarkMatched(); b.MarkMatched();
            activeVanishCount += 2;
            StartCoroutine(CoVanish(a));
            StartCoroutine(CoVanish(b));

            remainingPairs--;
            if (remainingPairs <= 0)
            {
                pendingLevelAdvance = true;
                StartCoroutine(CoAdvanceWhenAllVanishDone());
            }
        }
        else
        {
            // yanlış: kısa kilit
            inputLocked = true;

            // geri bildirim (shake + kırmızı flash + ses)
            var f1 = StartCoroutine(firstSelected.MismatchFeedback());
            var f2 = StartCoroutine(secondSelected.MismatchFeedback());
            firstSelected.PlayMismatchSfx();
            secondSelected.PlayMismatchSfx();

            yield return f1; yield return f2;

            // paralel, deterministik kapanış
            var c1 = StartCoroutine(firstSelected.CloseSafely(0.28f));
            var c2 = StartCoroutine(secondSelected.CloseSafely(0.28f));
            yield return c1; yield return c2;

            firstSelected = null;
            secondSelected = null;
            inputLocked = false;
        }
    }

    IEnumerator CoVanish(Card c)
    {
        yield return c.MatchVanish(0.45f);
        activeVanishCount = Mathf.Max(0, activeVanishCount - 1);
    }

    IEnumerator CoAdvanceWhenAllVanishDone()
    {
        while (activeVanishCount > 0) yield return null;

        timerRunning = false;
        yield return new WaitForSeconds(0.25f);

        if (!pendingLevelAdvance) yield break;

        // Ekran flaşı (opsiyonel)
        if (screenFlash)
        {
            screenFlash.gameObject.SetActive(true);
            screenFlash.alpha = 0f;
            yield return screenFlash.DOFade(1f, screenFlashTime * 0.5f).WaitForCompletion();
            yield return screenFlash.DOFade(0f, screenFlashTime * 0.5f).WaitForCompletion();
            screenFlash.gameObject.SetActive(false);
        }

        if (sfxLevelUp) AudioSource.PlayClipAtPoint(sfxLevelUp, Vector3.zero);

        if (currentLevelIndex < levelCardCounts.Length - 1)
        {
            currentLevelIndex++;
            StartLevel(currentLevelIndex);
        }
        else
        {
            ShowGameOverAnimated();
        }
    }
}
