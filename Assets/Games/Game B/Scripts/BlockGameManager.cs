using UnityEngine;
using TMPro;
using DG.Tweening;

public class BlockGameManager : MonoBehaviour
{
    [Header("Bloklar")]
    public GameObject[] blockPrefabs;        // farklı blok görselleri/prefabları
    public Transform spawnPoint;
    public Transform leftPoint;
    public Transform rightPoint;

    [Header("Kamera")]
    public Transform mainCameraTransform;

    [Header("Hareket")]
    public float baseMoveSpeed = 3.0f;
    public float speedIncreaseAmount = 0.2f;

    [Header("Puan")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestScoreText;
    private int score = 0;
    private int bestScore = 0;

    [Header("Yükseliş")]
    [Tooltip("Her kaç blokta bir oyun yukarı kayar?")]
    public int blocksToMove = 3;
    public float moveDuration = 0.5f;
    public float moveAmount = 3f;

    [Header("Perfect Ayarları")]
    [Tooltip("Perfect sayılması için X farkının, blok genişliğinin bu oranından küçük olması gerekir.")]
    [Range(0.0f, 0.5f)] public float perfectThresholdRatio = 0.12f;
    [Tooltip("Perfect için temel bonus.")]
    public int perfectBonus = 5;
    [Tooltip("Her ardışık perfect'te eklenen bonus artışı.")]
    public int perfectComboStep = 2;
    [Tooltip("Perfect olduğunda üstteki bloğu alttakine hafifçe 'snap' et.")]
    public bool snapOnPerfect = true;
    [Tooltip("Perfect inişte uygulanacak küçük ölçek pulse değeri.")]
    public float perfectPulse = 0.15f;
    public float pulseTime = 0.15f;

    [Header("Efekt Prefabları (opsiyonel)")]
    public ParticleSystem perfectEffectPrefab;   // küçük bir pırıltı/partikül
    public TextMeshPro floatingTextPrefab;       // “+5” gibi baloncuk

    [Header("UI – Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalBestScoreText;
    public Vector2 targetPosition;

    private GameObject currentBlock;
    public GameObject lastLandedBlock { get; private set; }
    private bool isDropping = false;
    private int perfectCombo = 0;
    private int landedCount = 0;

    // === EKLENDİ: Kamera–spawn sabit ofseti ===
    private Vector3 camSpawnOffset;

    void Start()
    {
        DOTween.SetTweensCapacity(500, 50);

        if (gameOverPanel != null)
        {
            gameOverPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 1000);
            gameOverPanel.SetActive(false);
        }

        bestScore = PlayerPrefs.GetInt("BT_BEST", 0);
        if (bestScoreText) bestScoreText.text = $"Best: {bestScore}";

        UpdateScore(0);

        // === EKLENDİ: Başlangıçta ofseti kilitle ===
        if (mainCameraTransform != null && spawnPoint != null)
        {
            camSpawnOffset = mainCameraTransform.position - spawnPoint.position;
        }

        SpawnNewBlock();
    }

    void Update()
    {
        if (isDropping) return;

        // Mobile için tap = mouse down zaten çalışır
        if (Input.GetMouseButtonDown(0))
        {
            if (currentBlock != null)
            {
                DropBlock();
                isDropping = true;
            }
        }

        if (currentBlock != null)
        {
            float pingPongValue = Mathf.PingPong(Time.time * baseMoveSpeed, 1);
            currentBlock.transform.position = Vector3.Lerp(leftPoint.position, rightPoint.position, pingPongValue);
        }
    }

    private void DropBlock()
    {
        var rb = currentBlock.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1.0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // İnişi kontrol et
        Invoke(nameof(CheckLanded), 0.35f);
    }

    private void CheckLanded()
    {
        if (currentBlock == null) return;

        var rb = currentBlock.GetComponent<Rigidbody2D>();
        // === DÜZELTME: linearVelocity yerine velocity ===
        if (rb != null && rb.linearVelocity.magnitude > 0.08f)
        {
            Invoke(nameof(CheckLanded), 0.08f);
            return;
        }

        // Doğru hedefe iniş mi?
        var col = currentBlock.GetComponent<BoxCollider2D>();
        if (col == null) col = currentBlock.AddComponent<BoxCollider2D>();
        var hits = Physics2D.OverlapBoxAll((Vector2)currentBlock.transform.position + col.offset, col.size, 0f);

        bool landedOk = false;
        bool landedOnGround = false;

        foreach (var h in hits)
        {
            if (h.gameObject == currentBlock) continue;

            if (lastLandedBlock == null && h.CompareTag("Ground"))
            {
                landedOk = true; landedOnGround = true; break;
            }
            else if (lastLandedBlock != null && h.gameObject == lastLandedBlock)
            {
                // Yukarıdan gelmiş olmalı
                if (currentBlock.transform.position.y > lastLandedBlock.transform.position.y)
                {
                    landedOk = true; break;
                }
            }
            else if (lastLandedBlock != null && h.CompareTag("Ground"))
            {
                // Yanlışlıkla zemine değerse oyun biter
                EndGame(score);
                return;
            }
        }

        if (!landedOk)
        {
            EndGame(score);
            return;
        }

        // Sabitle
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeAll;

        // PERFECT kontrolü (altta başka blok varsa)
        bool isPerfect = false;
        if (!landedOnGround && lastLandedBlock != null)
        {
            float width = col.size.x * Mathf.Abs(currentBlock.transform.lossyScale.x);
            float xDiff = Mathf.Abs(currentBlock.transform.position.x - lastLandedBlock.transform.position.x);
            isPerfect = xDiff <= width * perfectThresholdRatio;

            if (isPerfect)
            {
                perfectCombo++;
                int bonus = perfectBonus + (perfectCombo - 1) * perfectComboStep;
                AddScore(1 + bonus); // normal + bonus
                ShowFloatingText("+" + bonus.ToString(), currentBlock.transform.position + Vector3.up * 0.6f);
                PlayPerfectFx(currentBlock.transform.position);

                if (snapOnPerfect)
                {
                    // X’i alttaki ile hizala (küçük bir snap)
                    currentBlock.transform.DOMoveX(lastLandedBlock.transform.position.x, 0.08f);
                }

                // Pulse
                currentBlock.transform.DOPunchScale(Vector3.one * perfectPulse, pulseTime, 6, 0.5f);
            }
            else
            {
                perfectCombo = 0;
                AddScore(1); // sadece normal puan
            }
        }
        else
        {
            // İlk blok zemine indikten sonra normal puan
            perfectCombo = 0;
            AddScore(1);
        }

        lastLandedBlock = currentBlock;
        landedCount++;

        // Her inişte küçük bir zıplama hissi
        lastLandedBlock.transform.DOPunchPosition(Vector3.down * 0.05f, 0.12f, 4, 0.5f);

        // Zorluk artışı
        baseMoveSpeed += speedIncreaseAmount;

        // Yeni blok
        SpawnNewBlock();

        // Yukarı kaydır
        if (landedCount % blocksToMove == 0)
        {
            MoveGameElementsUp();
        }
    }

    private void SpawnNewBlock()
    {
        isDropping = false;

        if (blockPrefabs == null || blockPrefabs.Length == 0)
        {
            Debug.LogError("Blok prefab'ı eklenmedi.");
            return;
        }

        int randomIndex = Random.Range(0, blockPrefabs.Length);
        GameObject prefab = blockPrefabs[randomIndex];
        currentBlock = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        // İnen bloğun z'ine kilitlenmesin diye current biraz daha önde olabilir (2D ortamlarda gerekmez ama istersen kullan)
        // currentBlock.transform.position += Vector3.forward * 0.01f;
    }

    private void MoveGameElementsUp()
    {
        Vector3 moveVector = new Vector3(0, moveAmount, 0);

        // Spawn & sınırlar: eskisi gibi göreceli taşı
        Vector3 nextSpawnPos = spawnPoint.position + moveVector;
        spawnPoint.DOMove(nextSpawnPos, moveDuration).SetEase(Ease.OutSine);
        leftPoint.DOMove(leftPoint.position + moveVector, moveDuration).SetEase(Ease.OutSine);
        rightPoint.DOMove(rightPoint.position + moveVector, moveDuration).SetEase(Ease.OutSine);

        // === DEĞİŞTİ: Kamera hedefi, her zaman "spawn hedefi + sabit ofset" ===
        if (mainCameraTransform != null)
        {
            Vector3 camTarget = nextSpawnPos + camSpawnOffset;
            camTarget.x = mainCameraTransform.position.x; // X aynı
            camTarget.z = mainCameraTransform.position.z; // Z aynı (2D’de genelde -10)

            mainCameraTransform
                .DOMove(camTarget, moveDuration)
                .SetEase(Ease.OutSine);
        }
    }

    private void AddScore(int add)
    {
        UpdateScore(score + add);
    }

    private void UpdateScore(int newScore)
    {
        score = newScore;
        if (scoreText) scoreText.text = score.ToString();
    }

    private void ShowFloatingText(string txt, Vector3 worldPos)
    {
        if (!floatingTextPrefab) return;

        var t = Instantiate(floatingTextPrefab, worldPos, Quaternion.identity);
        t.text = txt;
        t.transform.localScale = Vector3.one * 0.8f;

        // Canvas yoksa “World Space” TMP kullanın veya küçük bir Canvas/ScreenSpace-Camera kurun
        t.transform.DOMoveY(worldPos.y + 0.9f, 0.6f).SetEase(Ease.OutQuad);
        t.DOFade(0, 0.6f).OnComplete(() => Destroy(t.gameObject));
    }

    private void PlayPerfectFx(Vector3 pos)
    {
        if (!perfectEffectPrefab) return;
        var fx = Instantiate(perfectEffectPrefab, pos, Quaternion.identity);
        Destroy(fx.gameObject, 2f);
    }

    public void EndGame(int finalScore)
    {
        Time.timeScale = 0;

        // Best Score
        if (finalScore > bestScore)
        {
            bestScore = finalScore;
            PlayerPrefs.SetInt("BT_BEST", bestScore);
        }

        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            if (finalScoreText) finalScoreText.text = "Score: " + finalScore;
            if (finalBestScoreText) finalBestScoreText.text = "Best: " + bestScore;

            gameOverPanel.GetComponent<RectTransform>()
                .DOAnchorPos(targetPosition, 0.5f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }

    public void RetryGame()
    {
        Time.timeScale = 1;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ContinueGame()
    {
        if (currentBlock) Destroy(currentBlock);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        Time.timeScale = 1;
        SpawnNewBlock();
    }
}
