using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Geliştirilmiş ShopPopup:
/// - Prefab olarak instantiate edilebilen eski davranışı korur.
/// - Sahnedeki (scene) UI panel referansını destekler: bu durumda sadece SetActive(true) olur, oyun durur (Time.timeScale = 0)
/// - Persistent (panel) modunda Close() çağrılana kadar açık kalır. Close() çağrılınca oyun devam eder.
public class ShopPopup : MonoBehaviour
{
    [Header("Optional UI")]
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] string defaultText = "Shop!";

    [Header("Lifetime (prefab-mode)")]
    [SerializeField] float showDuration = 1.2f;

    // Eğer panel sahnede ise persistent mode kullan. (Inspector'dan set edilebilir.)
    [Header("Mode")]
    [Tooltip("Eğer true ise bu obje sahnedeki panel gibi davranır: SetActive(true) -> bekle -> Close() ile kapatılır. Prefab-mode'da false kalabilir.")]
    [SerializeField] bool persistentPanel = false;

    CanvasGroup cg;
    RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        if (titleText && !string.IsNullOrEmpty(defaultText))
            titleText.text = defaultText;

        // Eğer sahnedeki panel ise başlangıçta inaktif kaldıysa (Inspector ayarlanmış olabilir)
        // not: panelin aktifliği sahneye bağlıdır; script burada aktif olunca zaten görünür.
    }

    /// Kullan: prefab ile oluşturulduysa veya sahne paneli modunda çağrılabilir.
    public void Show(string text = null)
    {
        if (titleText && !string.IsNullOrEmpty(text))
            titleText.text = text;

        if (persistentPanel)
        {
            // Sahnedeki panel modu: sadece aktif yap, açma efekti uygula (unscaled) ve oyunu durdur.
            StartCoroutine(ShowPersistentRoutine());
        }
        else
        {
            // Eski prefab-mode davranışı: fade-in, bekle, fade-out, Destroy
            StopAllCoroutines();
            StartCoroutine(ShowRoutine());
        }
    }

    IEnumerator ShowRoutine()
    {
        float t = 0f;
        Vector3 start = Vector3.one * 0.8f;
        Vector3 end   = Vector3.one;
        cg.alpha = 0f;
        rt.localScale = start;

        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.15f;
            cg.alpha = Mathf.SmoothStep(0f, 1f, k);
            rt.localScale = Vector3.Lerp(start, end, k);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(showDuration);

        t = 0f;
        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.15f;
            cg.alpha = Mathf.SmoothStep(1f, 0f, k);
            yield return null;
        }

        Destroy(gameObject);
    }

    IEnumerator ShowPersistentRoutine()
    {
        // Açma efekti (unscaled) ve oyunu dondur
        float t = 0f;
        Vector3 start = Vector3.one * 0.9f;
        Vector3 end   = Vector3.one;
        cg.alpha = 0f;
        rt.localScale = start;
        gameObject.SetActive(true);

        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.15f;
            cg.alpha = Mathf.SmoothStep(0f, 1f, k);
            rt.localScale = Vector3.Lerp(start, end, k);
            yield return null;
        }

        // Oyun durur (timeScale = 0)
        Time.timeScale = 0f;
        yield break;
    }

    /// Kapama çağrısı: persistent panel ise sadece gizle ve zamanı geri aç. Prefab-mode'da da çağrulursa Destroy eder.
    public void Close()
    {
        // Eğer prefab-mode ise oyun zaten akışında devam ediyor; yine de güvenli olsun:
        if (persistentPanel)
        {
            StartCoroutine(ClosePersistent());
        }
        else
        {
            Destroy(gameObject);
            Time.timeScale = 1f;
        }
    }

    IEnumerator ClosePersistent()
    {
        // fade-out unscaled
        float t = 0f;
        float duration = 0.12f;
        float startAlpha = cg.alpha;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            cg.alpha = Mathf.SmoothStep(startAlpha, 0f, k);
            yield return null;
        }

        gameObject.SetActive(false);
        Time.timeScale = 1f;
        yield break;
    }
}
