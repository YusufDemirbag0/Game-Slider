using UnityEngine;
using System;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] int startMaxHP = 100;
    public int MaxHP { get; private set; }
    public int HP    { get; private set; }

    public event Action<int,int> OnHPChanged; // (hp, max)

    [Header("Hit Sprite Overlay")]
    [SerializeField] Sprite hitSprite;
    [SerializeField] float hitDuration = 0.2f;
    [SerializeField] int sortingOrderBoost = 2;

    SpriteRenderer baseSR;
    SpriteRenderer hitSR;
    Coroutine hitCo;

    void Awake()
    {
        MaxHP = Mathf.Max(1, startMaxHP);
        HP    = MaxHP;

        baseSR = GetComponentInChildren<SpriteRenderer>();
        if (baseSR)
        {
            var go = new GameObject("HitOverlay");
            go.transform.SetParent(baseSR.transform, false);
            hitSR = go.AddComponent<SpriteRenderer>();
            hitSR.enabled = false;
            hitSR.sprite  = hitSprite ? hitSprite : baseSR.sprite;
            hitSR.sortingLayerID = baseSR.sortingLayerID;
            hitSR.sortingOrder   = baseSR.sortingOrder + sortingOrderBoost;
            hitSR.material = baseSR.sharedMaterial;
            hitSR.color = Color.white;
        }

        OnHPChanged?.Invoke(HP, MaxHP);
    }

    void LateUpdate()
    {
        if (!baseSR || !hitSR) return;
        hitSR.flipX = baseSR.flipX;
        hitSR.flipY = baseSR.flipY;
        hitSR.sortingLayerID = baseSR.sortingLayerID;
        hitSR.sortingOrder   = baseSR.sortingOrder + sortingOrderBoost;
        if (hitSprite == null) hitSR.sprite = baseSR.sprite;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        HP = Mathf.Max(HP - amount, 0);
        FlashHit();
        OnHPChanged?.Invoke(HP, MaxHP);
        if (HP <= 0) GetComponent<PlayerMovement>()?.Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        HP = Mathf.Min(HP + amount, MaxHP);
        OnHPChanged?.Invoke(HP, MaxHP);
    }

    // MaxHP’ye +X ekle (ve istersek aynı anda X kadar iyileştir)
    public void AddMaxHP(int add, bool alsoHealSame = true)
    {
        if (add <= 0) return;
        MaxHP += add;
        if (alsoHealSame) HP = Mathf.Min(HP + add, MaxHP);
        HP = Mathf.Clamp(HP, 0, MaxHP);
        OnHPChanged?.Invoke(HP, MaxHP);
    }

    void FlashHit()
    {
        if (!hitSR) return;
        if (hitCo != null) StopCoroutine(hitCo);
        hitCo = StartCoroutine(ShowHitSpriteCo());
    }

    IEnumerator ShowHitSpriteCo()
    {
        if (hitSprite) hitSR.sprite = hitSprite;
        else           hitSR.sprite = baseSR ? baseSR.sprite : hitSR.sprite;
        hitSR.enabled = true;
        yield return new WaitForSeconds(hitDuration);
        hitSR.enabled = false;
        hitCo = null;
    }
}
