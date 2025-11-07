using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class XPBarUI : MonoBehaviour
{
    public enum FillMode { ImageFilled, ScaleWidth }

    [Header("Common")]
    [SerializeField] PlayerXP playerXP;
    [SerializeField] float smooth = 0.15f; // 0 = anında
    [SerializeField] FillMode mode = FillMode.ScaleWidth;

    [Header("ImageFilled Mode")]
    [SerializeField] Image fillImage; // Image Type = Filled (Horizontal, Origin Left)

    [Header("ScaleWidth Mode")]
    [SerializeField] RectTransform fillRect; // Sarı barın RectTransform'u
    [SerializeField] float emptyScaleX = 0f;  // Boşken X ölçek
    [SerializeField] float fullScaleX  = 2.2f; // Doluyken X ölçek (senin değerin)
    [SerializeField] bool preserveYScale = true;

    float targetT; // 0..1
    Coroutine co;

    void Awake()
    {
        if (!playerXP) playerXP = FindFirstObjectByType<PlayerXP>();
    }

    void OnEnable()
    {
        if (playerXP != null) playerXP.OnXPChanged += HandleXPChanged;
        // İlk açılışta mevcut durumu uygula
        if (playerXP != null) HandleXPChanged(playerXP.currentXP, playerXP.xpToNext, playerXP.level);
    }
    void OnDisable()
    {
        if (playerXP != null) playerXP.OnXPChanged -= HandleXPChanged;
    }

    void HandleXPChanged(int cur, int toNext, int level)
    {
        targetT = toNext > 0 ? Mathf.Clamp01((float)cur / toNext) : 1f;
        if (smooth <= 0f)
        {
            ApplyImmediate(targetT);
        }
        else
        {
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(SmoothTo(targetT));
        }
        // Debug – olayı alıyor muyuz?
        // Debug.Log($"XP UI -> {cur}/{toNext} (Lv {level})  t={targetT:F2}");
    }

    IEnumerator SmoothTo(float t)
    {
        while (true)
        {
            float cur = GetCurrentT();
            if (Mathf.Approximately(cur, t)) { ApplyImmediate(t); yield break; }
            float next = Mathf.MoveTowards(cur, t, Time.unscaledDeltaTime / Mathf.Max(0.0001f, smooth));
            ApplyImmediate(next);
            yield return null;
        }
    }

    float GetCurrentT()
    {
        if (mode == FillMode.ImageFilled && fillImage)
            return fillImage.fillAmount;

        if (mode == FillMode.ScaleWidth && fillRect)
        {
            float x = fillRect.localScale.x;
            return Mathf.InverseLerp(emptyScaleX, fullScaleX, x);
        }
        return 0f;
    }

    void ApplyImmediate(float t01)
    {
        t01 = Mathf.Clamp01(t01);

        if (mode == FillMode.ImageFilled && fillImage)
        {
            fillImage.fillAmount = t01; // 0..1
            return;
        }

        if (mode == FillMode.ScaleWidth && fillRect)
        {
            float x = Mathf.Lerp(emptyScaleX, fullScaleX, t01);
            var s = fillRect.localScale;
            fillRect.localScale = new Vector3(x, preserveYScale ? s.y : 1f, s.z);
        }
    }
}
