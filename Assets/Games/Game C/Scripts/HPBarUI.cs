// HPBarUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HPBarUI : MonoBehaviour
{
    [SerializeField] PlayerHealth health;
    [SerializeField] Image fill;      // Type=Filled, Horizontal
    [SerializeField] float smooth = 0.12f; // 0 = anında

    float target;
    Coroutine co;

    void Awake()
    {
        if (!health) health = FindFirstObjectByType<PlayerHealth>();
    }
    void OnEnable()
    {
        if (health) health.OnHPChanged += HandleHP;
        if (health) HandleHP(health.HP, health.MaxHP);
    }
    void OnDisable()
    {
        if (health) health.OnHPChanged -= HandleHP;
    }

    void HandleHP(int hp, int max)
    {
        target = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
        if (!fill) return;

        if (smooth <= 0f) fill.fillAmount = target;
        else
        {
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(Smooth());
        }
    }

    IEnumerator Smooth()
    {
        while (!Mathf.Approximately(fill.fillAmount, target))
        {
            fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, Time.unscaledDeltaTime / smooth);
            yield return null;
        }
    }
}
