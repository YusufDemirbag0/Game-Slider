using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

public class PlayerXP : MonoBehaviour
{
    // ====== XP / Level ======
    [Header("Level/XP")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNext = 10;
    public event Action<int,int,int> OnXPChanged;

    // ====== Level Up UI ======
    [Header("Level Up UI (Scene Objects)")]
    [SerializeField] GameObject    levelUpRoot;
    [SerializeField] CanvasGroup   cg;
    [SerializeField] RectTransform content;
    [SerializeField] RectTransform slotA;
    [SerializeField] RectTransform slotB;
    [SerializeField] Button[]      optionButtons = new Button[4];

    [Header("Popup Options")]
    [SerializeField] bool  pauseOnLevelUp   = true;
    [SerializeField] float showDuration     = 0.25f;
    [SerializeField] float contentShowScale = 1f;

    // ====== Apply Targets ======
    [Header("Apply Targets")]
    [SerializeField] PlayerMovement  playerMovement;
    [SerializeField] PlayerHealth    playerHealth;
    [SerializeField] PlayerLoadout   playerLoadout;        // ★ ammo → silah çoğaltma buradan
    [SerializeField] TextMeshProUGUI ammoText;
    [SerializeField] TextMeshProUGUI goldText;

    // ammo ≡ TOPLAM silah adedi
    [Header("Counts")]
    [SerializeField] int ammo = 1; // 1 silahla başla
    [SerializeField] int gold = 0;

    // Dahili
    int  pendingLevelUps = 0;
    bool panelOpen = false;

    struct ParentCache { public Transform parent; public int sibling; public Vector3 scale; public bool wasActive; }
    readonly Dictionary<Transform, ParentCache> cache = new();
    readonly List<Button> activeBtns = new(2);

    // ====== Public API ======
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;
        while (currentXP >= xpToNext)
        {
            currentXP -= xpToNext;
            LevelUp();
        }
        OnXPChanged?.Invoke(currentXP, xpToNext, level);
    }

    // ====== Unity ======
    void Start()
    {
        xpToNext = RequiredXPForLevel(level);
        if (levelUpRoot) levelUpRoot.SetActive(false);
        foreach (var b in optionButtons) if (b) b.gameObject.SetActive(false);
        OnXPChanged?.Invoke(currentXP, xpToNext, level);

        // ★ Başlangıçta toplam silah adedini ammo olarak uygula
        if (!playerLoadout) playerLoadout = FindObjectOfType<PlayerLoadout>();
        if (playerLoadout) playerLoadout.SetOrbitingCountByFirstType(Mathf.Max(1, ammo));

        RefreshUI();
    }

    void RefreshUI()
    {
        if (ammoText) ammoText.text = ammo.ToString();
        if (goldText) goldText.text = gold.ToString();
    }

    // ====== Level ======
    int RequiredXPForLevel(int lvl)
    {
        return Mathf.RoundToInt(10f + 7f*(lvl-1) + 0.5f*(lvl-1)*(lvl-1));
    }

    void LevelUp()
    {
        level++;
        xpToNext = RequiredXPForLevel(level);

        pendingLevelUps++;
        if (!panelOpen) ShowLevelUpPanel();
    }

    // ====== Panel Aç/Kapa ======
    void ShowLevelUpPanel()
    {
        if (!levelUpRoot || !cg || !content)
        {
            Debug.LogWarning("LevelUp panel referanslarını atamadın.");
            return;
        }

        cg.alpha = 0f; cg.blocksRaycasts = true; cg.interactable = true;
        levelUpRoot.SetActive(true);
        if (pauseOnLevelUp) Time.timeScale = 0f;
        panelOpen = true;

        if (optionButtons == null || optionButtons.Length < 2)
        {
            Debug.LogWarning("En az 2 sahne butonu gerekli.");
            return;
        }
        int n = optionButtons.Length;
        int iA = UnityEngine.Random.Range(0, n);
        int iB; do { iB = UnityEngine.Random.Range(0, n); } while (iB == iA);

        Button btnA = optionButtons[iA];
        Button btnB = optionButtons[iB];

        activeBtns.Clear();
        PlaceToSlot(btnA, slotA); activeBtns.Add(btnA);
        PlaceToSlot(btnB, slotB); activeBtns.Add(btnB);

        BindButton(btnA, 0);
        BindButton(btnB, 1);

        content.localScale = Vector3.one * 0.8f;
        cg.DOFade(1f, showDuration).SetUpdate(true);
        content.DOScale(contentShowScale, showDuration)
               .SetEase(Ease.OutBack)
               .SetUpdate(true);
    }

    void CloseLevelUpPanel()
    {
        foreach (var b in activeBtns) Restore(b ? b.transform : null);
        activeBtns.Clear();

        if (pauseOnLevelUp) Time.timeScale = 1f;
        cg.blocksRaycasts = false; cg.interactable = false;
        levelUpRoot.SetActive(false);
        panelOpen = false;

        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);
        if (pendingLevelUps > 0) ShowLevelUpPanel();
    }

    // ====== Seçim & Upgrade Uygulama ======
    public void ChooseActiveOption(int which) // 0=A, 1=B
    {
        var chosen = (which == 0) ? activeBtns[0] : activeBtns[1];
        if (!chosen) { CloseLevelUpPanel(); return; }

        // ★ BUTON TARAFINA DOKUNMUYORUM: Mevcut sistemin nasıl okuyorsa öyle kalsın
        // Eğer butonda UpgradeOption (MonoBehaviour) varsa:
        var opt = chosen.GetComponent<UpgradeOption>();
        if (!opt)
        {
            Debug.LogWarning("Seçilen butonda UpgradeOption yok (MonoBehaviour). Mevcut okuma yöntemine göre ayarla.");
            CloseLevelUpPanel();
            return;
        }

        ApplyUpgrade(opt);
        CloseLevelUpPanel();
    }

    void ApplyUpgrade(UpgradeOption opt)
    {
        switch (opt.type)
        {
            case UpgradeOption.Type.Speed:
                if (playerMovement)
                {
                    if (opt.isPercent) playerMovement.AddSpeedPercent(Mathf.Max(0f, opt.value));
                    else               playerMovement.AddSpeed(opt.value);
                }
                break;

            case UpgradeOption.Type.MaxHPAdd:
                if (playerHealth) playerHealth.AddMaxHP(Mathf.RoundToInt(opt.value), true);
                break;

            case UpgradeOption.Type.AmmoAdd:
            {
                // ★ SADECE BİR KEZ ARTIR + HEMEN UYGULA
                ammo += Mathf.RoundToInt(opt.value);   // ammo ≡ toplam silah
                if (!playerLoadout) playerLoadout = FindObjectOfType<PlayerLoadout>();
                if (playerLoadout)  playerLoadout.SetOrbitingCountByFirstType(Mathf.Max(1, ammo));
                RefreshUI();
                break;
            }

            case UpgradeOption.Type.GoldAdd:
                gold += Mathf.RoundToInt(opt.value);
                RefreshUI();
                break;
        }
    }

    // ====== Yardımcılar ======
    void PlaceToSlot(Button btn, RectTransform slot)
    {
        if (!btn || !slot) return;

        var t = btn.transform;
        if (!cache.ContainsKey(t))
        {
            cache[t] = new ParentCache {
                parent    = t.parent,
                sibling   = t.GetSiblingIndex(),
                scale     = t.localScale,
                wasActive = t.gameObject.activeSelf
            };
        }

        t.SetParent(slot, false);
        if (t is RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
        t.localScale = Vector3.one;
        t.gameObject.SetActive(true);
    }

    void Restore(Transform t)
    {
        if (!t) return;
        if (cache.TryGetValue(t, out var pc))
        {
            t.SetParent(pc.parent, false);
            t.SetSiblingIndex(pc.sibling);
            t.localScale = pc.scale;
            t.gameObject.SetActive(pc.wasActive);
        }
        else
        {
            t.gameObject.SetActive(false);
        }
    }

    void BindButton(Button btn, int index)
    {
        if (!btn) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => ChooseActiveOption(index));
    }

    public void AddGold(int amount)
    {
        gold += Mathf.Max(0, amount);
        if (goldText) goldText.text = gold.ToString();
    }

}
