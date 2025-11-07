using System.Collections;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class Card : MonoBehaviour
{
    [Header("Kart Görselleri")]
    public Sprite frontSprite;   // Ön yüz
    public Sprite backSprite;    // Arka yüz

    [Header("Efekt Prefabları (opsiyonel)")]
    public ParticleSystem matchParticles;     // Eşleşme anı parıltısı
    public ParticleSystem flipShineParticles; // Flip ortasında kısa parlama

    [Header("Sesler (opsiyonel)")]
    public AudioClip sfxFlip;
    public AudioClip sfxMatch;
    public AudioClip sfxMismatch;

    [Header("Geri Bildirim Ayarları")]
    [Range(0.05f, 0.4f)] public float flipDuration = 0.25f;
    [Range(0.0f, 0.3f)]  public float mismatchShakeTime = 0.18f;
    [Range(0.0f, 0.3f)]  public float mismatchFlashTime = 0.10f;
    public float appearRotateJitter = 10f; // spawn sırasında ufak z rotasyonu

    // Kimlik ve tıklama callback
    public int PairId { get; private set; }
    private System.Action<Card> onClicked;

    private SpriteRenderer sr;
    private BoxCollider2D col;

    // Klasik akış: kart kendi flip'ini yapar
    private bool isFront = false;
    private bool interactable = true;

    // Stabilite: flip çakışmalarını izole etmek için sadece flip tween referansı
    private Tween flipTween;
    public bool IsFlipping => flipTween != null && flipTween.IsActive();

    // Eşleşmiş kart oyundan düşer (tıklanamaz)
    public bool IsMatched { get; private set; } = false;

    // Renk geri dönüşü için
    private Color baseColor = Color.white;

    void Awake()
    {
        sr  = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();

        transform.localEulerAngles = Vector3.zero;
        transform.localScale = Vector3.one;

        sr.sprite  = backSprite;
        baseColor  = sr.color;
        isFront    = false;
    }

    public void Init(int pairId, System.Action<Card> onClick)
    {
        PairId = pairId;
        onClicked = onClick;
    }

    public void SetInteractable(bool canClick)
    {
        interactable = canClick;
        if (col) col.enabled = canClick && !IsMatched;
    }

    void OnMouseDown()
    {
        if (!interactable || IsMatched) return;

        // Tıklanınca öne çevir (arkadaysa)
        if (!isFront) FlipToFront(flipDuration);

        onClicked?.Invoke(this);
    }

    public bool IsFront() => isFront;

    public void ShowFront() { sr.sprite = frontSprite; isFront = true; }
    public void ShowBack()  { sr.sprite = backSprite;  isFront = false; }

    // ---------------- Güvenli Flip ----------------

    public Tween FlipToFront(float duration = 0.35f)
    {
        if (isFront || IsMatched) return null;

        if (flipTween != null && flipTween.IsActive()) flipTween.Kill(false);

        var seq = DOTween.Sequence();
        seq.Append(transform.DOLocalRotate(new Vector3(0, 90, 0), duration / 2f).SetEase(Ease.InQuad));
        seq.AppendCallback(() =>
        {
            ShowFront();
            // Flip ortası parıltı
            if (flipShineParticles)
                Instantiate(flipShineParticles, transform.position, Quaternion.identity);
            // Ses
            if (sfxFlip) AudioSource.PlayClipAtPoint(sfxFlip, transform.position);
        });
        seq.Append(transform.DOLocalRotate(Vector3.zero, duration / 2f).SetEase(Ease.OutQuad));
        flipTween = seq;
        return seq;
    }

    public Tween FlipToBack(float duration = 0.35f)
    {
        if (!isFront || IsMatched) return null;

        if (flipTween != null && flipTween.IsActive()) flipTween.Kill(false);

        var seq = DOTween.Sequence();
        seq.Append(transform.DOLocalRotate(new Vector3(0, 90, 0), duration / 2f).SetEase(Ease.InQuad));
        seq.AppendCallback(ShowBack);
        seq.Append(transform.DOLocalRotate(Vector3.zero, duration / 2f).SetEase(Ease.OutQuad));
        flipTween = seq;
        return seq;
    }

    // Yanlış eşleşmede görsel geri bildirim (shake + kırmızı flash)
    public IEnumerator MismatchFeedback()
    {
        if (IsMatched) yield break;

        // Shake (pozisyonu yerinde tutmak istersen relative true)
        var shake = transform.DOShakePosition(mismatchShakeTime, 0.12f, 12, 90, false, true);

        // Kırmızıya hızlı geç, sonra geri dön
        var c1 = sr.DOColor(Color.red, mismatchFlashTime);
        yield return c1.WaitForCompletion();

        var c2 = sr.DOColor(baseColor, mismatchFlashTime);
        yield return DOTween.Sequence().Join(shake).Join(c2).WaitForCompletion();
    }

    // Yanlış eşleşmede taş gibi kapanış: aktif flip varsa bekler, sonra kapatır
    public IEnumerator CloseSafely(float duration = 0.28f)
    {
        if (IsMatched) yield break;

        if (flipTween != null && flipTween.IsActive())
            yield return flipTween.WaitForCompletion();

        if (isFront)
        {
            var t = FlipToBack(duration);
            if (t != null) yield return t.WaitForCompletion();
        }

        if (isFront)
        {
            if (flipTween != null && flipTween.IsActive()) flipTween.Kill(false);

            ShowBack();
            transform.localEulerAngles = Vector3.zero;
        }
    }

    // Doğru eşleşmede oyundan düşür (hemen tıklanamaz)
    public void MarkMatched()
    {
        IsMatched = true;
        interactable = false;
        if (col) col.enabled = false;

        if (flipTween != null && flipTween.IsActive()) flipTween.Kill(false);
    }

    // Görsel yok olma + eşleşme parçacığı + ses
    public IEnumerator MatchVanish(float total = 0.45f)
    {
        // Parçacık
        if (matchParticles)
            Instantiate(matchParticles, transform.position, Quaternion.identity);

        // Ses
        if (sfxMatch)
            AudioSource.PlayClipAtPoint(sfxMatch, transform.position);

        float half = total * 0.5f;
        var seq = DOTween.Sequence();
        seq.Join(transform.DOPunchScale(Vector3.one * 0.15f, half, 8, 0.8f));
        seq.Append(sr.DOFade(0f, half));
        seq.Join(transform.DOScale(0f, half).SetEase(Ease.InBack));
        yield return seq.WaitForCompletion();
        Destroy(gameObject);
    }

    // Spawn giriş animasyonu (scale+fade) + hafif z-jitter
    public void Appear(float targetScale, float duration, float delay)
    {
        var c = sr.color; c.a = 0f; sr.color = c;
        transform.localScale = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-appearRotateJitter, appearRotateJitter));

        DOTween.Sequence()
            .SetDelay(delay)
            .Append(sr.DOFade(1f, duration * 0.5f))
            .Join(transform.DOScale(targetScale * 1.08f, duration * 0.6f).SetEase(Ease.OutBack))
            .Append(transform.DOScale(targetScale, duration * 0.15f).SetEase(Ease.OutQuad))
            .Append(transform.DORotate(Vector3.zero, 0.12f)); // z-jitter sıfırlama
    }

    // Dışarıdan çağrılabilsin diye
    public void PlayMismatchSfx()
    {
        if (sfxMismatch) AudioSource.PlayClipAtPoint(sfxMismatch, transform.position);
    }
}
