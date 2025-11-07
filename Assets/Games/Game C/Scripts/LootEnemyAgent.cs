using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class LootEnemyAgent : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] int   maxHP       = 50;
    [SerializeField] float moveSpeed   = 0f;
    [SerializeField] float knockback   = 0.4f;

    [Header("Visuals")]
    [SerializeField] SpriteRenderer sr;
    [SerializeField] Sprite idleSprite;
    [SerializeField] Sprite hitSprite;
    [SerializeField] Sprite deadSprite;
    [SerializeField] float  hitFlashTime  = 0.1f;
    [SerializeField] float  deathShowTime = 0.6f;

    [Header("UI / Shop")]
    [Tooltip("Buraya ya bir Prefab (Instantiate edilir) ya da sahnedeki Panel GameObject'i (scene objesi) sürükle. Panel sahnede ise script paneli SetActive(true) yapar.")]
    [SerializeField] GameObject shopPopupPrefab; // prefab veya scene panel olabilir

    // Refs
    [SerializeField] Animator animator;
    Rigidbody2D rb;
    Collider2D  col;

    int hp;
    bool isDead;

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (animator) animator.enabled = false;
    }

    void OnEnable()
    {
        hp = Mathf.Max(1, maxHP);
        if (sr && idleSprite) sr.sprite = idleSprite;
        isDead = false;
        if (col) col.enabled = true;
        if (rb)  rb.simulated = true;
    }

    public void ApplyDamage(int amount, Vector2 fromPos, float extraKnockback = 0f)
    {
        if (isDead) return;

        hp -= Mathf.Max(0, amount);
        StopAllCoroutines();
        StartCoroutine(HitFlash());

        if (rb)
        {
            var dir = (Vector2)(transform.position) - fromPos;
            dir = dir.normalized * (knockback + extraKnockback);
            rb.AddForce(dir, ForceMode2D.Impulse);
        }

        if (hp <= 0) StartCoroutine(DieRoutine());
    }

    IEnumerator HitFlash()
    {
        if (sr && hitSprite)
        {
            var prev = sr.sprite;
            sr.sprite = hitSprite;
            yield return new WaitForSeconds(hitFlashTime);
            sr.sprite = idleSprite ? idleSprite : prev;
        }
        else
        {
            yield return null;
        }
    }

    IEnumerator DieRoutine()
    {
        isDead = true;
        if (col) col.enabled = false;
        if (rb)  rb.simulated = false;

        if (sr && deadSprite) sr.sprite = deadSprite;

        yield return new WaitForSeconds(deathShowTime);

        SpawnShopPopup();
        Destroy(gameObject);
    }

    void SpawnShopPopup()
    {
        if (!shopPopupPrefab) return;

        // Eğer inspector'a sahnedeki panel (scene object) bağlandıysa -> scene.IsValid() true olur
        if (shopPopupPrefab.scene.IsValid())
        {
            // Sahnedeki paneli kullan: aktif yap ve ShopPopup.Show(persistent) çalıştır
            var panel = shopPopupPrefab;
            panel.SetActive(true);

            // Konumlandır (varsa RectTransform'e ekran pozisyonu ver)
            var rt = panel.GetComponent<RectTransform>();
            if (rt && Camera.main)
            {
                Vector3 screen = Camera.main.WorldToScreenPoint(transform.position);
                rt.position = screen;
            }

            var popupComp = panel.GetComponent<ShopPopup>();
            if (popupComp == null)
            {
                // Eğer script yoksa ekle (ve persistent yap)
                popupComp = panel.AddComponent<ShopPopup>();
            }

            // Eğer mevcut component persistent değilse ayarla (sahne paneli için persistent olması mantıklı)
        }
        else
        {
            // Prefab veya asset referansı => instantiate olduğu eski davranış
            var canvas = FindObjectOfType<Canvas>();
            GameObject go;

            if (canvas)
            {
                go = Instantiate(shopPopupPrefab, canvas.transform);
                var rt = go.GetComponent<RectTransform>();
                if (rt)
                {
                    Vector3 screen = Camera.main
                        ? Camera.main.WorldToScreenPoint(transform.position)
                        : (Vector3)transform.position;
                    rt.position = screen;
                }
            }
            else
            {
                go = Instantiate(shopPopupPrefab, transform.position, Quaternion.identity);
            }

            // Üzerinde ShopPopup yoksa otomatik ekle
            var popup = go.GetComponent<ShopPopup>();
            if (!popup) popup = go.AddComponent<ShopPopup>();
            popup.Show();
        }
    }
}
