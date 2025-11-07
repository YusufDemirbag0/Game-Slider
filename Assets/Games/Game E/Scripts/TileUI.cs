using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TileUI : MonoBehaviour
{
    public RectTransform Rect;
    public TextMeshProUGUI Label;
    public Image Bg;

    [HideInInspector] public int Value;

    void Reset()
    {
        Rect = GetComponent<RectTransform>();
        Bg   = GetComponent<Image>();
        if (!Label) Label = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void SetValue(int v)
    {
        Value = v;
        if (Label) Label.text = v.ToString();
        ApplyColor(v);
        gameObject.name = "Tile_" + v;
    }

    void ApplyColor(int v)
    {
        if (!Bg) return;
        Color c = Color.white;
        if (v <= 2)       c = new Color(0.93f, 0.93f, 0.93f);
        else if (v <= 4)  c = new Color(0.90f, 0.88f, 0.76f);
        else if (v <= 8)  c = new Color(0.95f, 0.69f, 0.47f);
        else if (v <= 16) c = new Color(0.96f, 0.58f, 0.39f);
        else if (v <= 32) c = new Color(0.95f, 0.49f, 0.37f);
        else if (v <= 64) c = new Color(0.95f, 0.37f, 0.23f);
        else              c = new Color(0.23f, 0.68f, 0.38f);
        Bg.color = c;
    }

    public IEnumerator AnimateMove(Vector2 target, float dur = 0.08f)
    {
        if (!Rect) Rect = GetComponent<RectTransform>();
        Vector2 start = Rect.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            Rect.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0,1,t));
            yield return null;
        }
        Rect.anchoredPosition = target;
    }

    public IEnumerator Pop(float scale = 1.1f, float dur = 0.08f)
    {
        if (!Rect) Rect = GetComponent<RectTransform>();
        Vector3 s0 = Vector3.one, s1 = Vector3.one * scale;
        float t = 0f;
        while (t < 1f) { t += Time.unscaledDeltaTime / dur; Rect.localScale = Vector3.Lerp(s0, s1, Mathf.SmoothStep(0,1,t)); yield return null; }
        t = 0f;
        while (t < 1f) { t += Time.unscaledDeltaTime / dur; Rect.localScale = Vector3.Lerp(s1, s0, Mathf.SmoothStep(0,1,t)); yield return null; }
        Rect.localScale = s0;
    }
}
