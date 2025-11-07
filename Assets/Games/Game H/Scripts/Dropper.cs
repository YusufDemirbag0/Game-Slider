// Dropper.cs
using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class DropEvent : UnityEvent<GameObject> {} // bırakılan top

public class Dropper : MonoBehaviour
{
    [Header("Alan")]
    public float xMin = -2.4f, xMax = 2.4f, ySpawn = 4.5f;

    [Header("Cooldown")]
    public float dropCooldown = 0.35f;
    float lastDropAt = -999f;

    [Header("FX (opsiyonel)")]
    public GameObject dropFxPrefab;   // bırakma anında oynatılacak
    public AudioClip dropSfx;
    public AudioSource sfx;

    [Header("Olaylar")]
    public DropEvent OnDropped;

    GameObject heldGO;
    Ball heldBall;

    void Update()
    {
        // Yeni top yoksa oluştur ve imleci takip ettir
        if (heldGO == null)
        {
            var prefab = MergeManager.I.GetRandomDroppable();
            heldGO = Instantiate(prefab, new Vector3(0, ySpawn, 0), Quaternion.identity);
            heldBall = heldGO.GetComponent<Ball>();
            heldBall.rb.simulated = false; // havada sabit dursun
        }

        // X konumunu parmak/Mouse ile kontrol et
        float targetX = GetPointerX();
        targetX = Mathf.Clamp(targetX, xMin, xMax);
        heldGO.transform.position = new Vector3(targetX, ySpawn, 0);

        // Bırak: tık/parmak kaldır
        if (CanDrop() && (Input.GetMouseButtonUp(0) || TouchReleased()))
        {
            // Fizik aktive + uyanma vuruşu
            heldBall.rb.simulated = true;
            heldBall.rb.WakeUp();
            heldBall.rb.linearVelocity = Vector2.zero;
            heldBall.rb.AddForce(Vector2.down * 0.5f, ForceMode2D.Impulse); // minik dürtme

            // Drop FX / SFX
            if (dropFxPrefab) Instantiate(dropFxPrefab, heldGO.transform.position, Quaternion.identity);
            if (sfx && dropSfx) sfx.PlayOneShot(dropSfx, 0.6f);

            // Event
            OnDropped?.Invoke(heldGO);

            heldGO = null; heldBall = null;
            lastDropAt = Time.time;
        }
    }

    float GetPointerX()
    {
        if (Input.touchCount > 0) return Camera.main.ScreenToWorldPoint(Input.touches[0].position).x;
        return Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
    }

    bool TouchReleased()
    {
        if (Input.touchCount == 0) return false;
        var t = Input.touches[0];
        return t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
    }

    bool CanDrop() => Time.time - lastDropAt >= dropCooldown;
}
