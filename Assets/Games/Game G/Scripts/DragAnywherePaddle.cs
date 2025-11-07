using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DragAnywherePaddle : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] float moveLerp = 20f;     // daha “takipçi” his için
    [SerializeField] float padding = 0.5f;     // duvara yapışmasın

    Camera cam;
    float minX, maxX; // dünya biriminde sınırlar
    float targetX;

    void Awake()
    {
        cam = Camera.main;
        ComputeBounds();
        targetX = transform.position.x;
    }

    void Update()
    {
        // Dokunma + mouse (editor) desteği
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            var w = cam.ScreenToWorldPoint(new Vector3(t.position.x, 0, 0));
            targetX = Mathf.Clamp(w.x, minX + padding, maxX - padding);
        }
        else if (Input.GetMouseButton(0))
        {
            var w = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, 0, 0));
            targetX = Mathf.Clamp(w.x, minX + padding, maxX - padding);
        }

        var pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetX, moveLerp * Time.deltaTime);
        transform.position = pos;
    }

    void ComputeBounds()
    {
        // Kamera ortografik kabulü
        float halfW = cam.orthographicSize * cam.aspect;
        minX = cam.transform.position.x - halfW;
        maxX = cam.transform.position.x + halfW;
    }

    // Ekran dönerse vs. (istersen)
    void OnDrawGizmosSelected()
    {
        if (!Camera.main) return;
        float halfW = Camera.main.orthographicSize * Camera.main.aspect;
        Vector3 c = Camera.main.transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(c.x - halfW, transform.position.y, 0), new Vector3(c.x + halfW, transform.position.y, 0));
    }
}
