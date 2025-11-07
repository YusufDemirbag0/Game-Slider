using UnityEngine;
using System.Collections;

public class SideObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs (KIRILMAZ ENGEL)")]
    [SerializeField] GameObject[] obstaclePrefabs; // Tag = "Obstacle"

    [Header("Sıklık (sn)")]
    [SerializeField] float spawnEveryMin = 4f;
    [SerializeField] float spawnEveryMax = 8f;

    [Header("Hız")]
    [SerializeField] float moveSpeedMin = 2.5f;
    [SerializeField] float moveSpeedMax = 4.5f;

    [Header("Boyut / Y Konumu")]
    [SerializeField] Vector2 scaleRange = new Vector2(0.9f, 1.3f);
    [SerializeField] Vector2 yPadding = new Vector2(.5f, 1.5f);
    [SerializeField, Range(0f, 0.8f)] float bottomClip = 0.22f;

    Camera cam;

    void Awake() => cam = Camera.main;
    void OnEnable() => StartCoroutine(Loop());

    IEnumerator Loop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(spawnEveryMin, spawnEveryMax));
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;

        float minY = c.y - halfH + yPadding.x + (halfH * bottomClip);
        float maxY = c.y + halfH - yPadding.y;
        if (minY > maxY) minY = maxY - 0.1f;

        bool fromLeft = Random.value < 0.5f;
        float y = Random.Range(minY, maxY);
        float x = fromLeft ? c.x - halfW - 1.2f : c.x + halfW + 1.2f;

        var prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        var go = Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity);
        go.tag = "Obstacle";

        float s = Random.Range(scaleRange.x, scaleRange.y);
        go.transform.localScale = Vector3.one * s;

        var rb = go.GetComponent<Rigidbody2D>(); if (!rb) rb = go.AddComponent<Rigidbody2D>();
        rb.isKinematic = true; rb.gravityScale = 0f;

        var col = go.GetComponent<Collider2D>(); if (!col) col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;
        col.sharedMaterial = MakeBouncy();

        float spd = Random.Range(moveSpeedMin, moveSpeedMax);
        float dir = fromLeft ? 1f : -1f;

        StartCoroutine(MoveAndCull(go, new Vector2(spd * dir, 0f), halfW, c));
    }

    IEnumerator MoveAndCull(GameObject go, Vector2 vel, float halfW, Vector3 camCenter)
    {
        while (go)
        {
            go.transform.position += (Vector3)(vel * Time.deltaTime);
            if (Mathf.Abs(go.transform.position.x - camCenter.x) > halfW + 2.5f)
                Destroy(go);
            yield return null;
        }
    }

    PhysicsMaterial2D MakeBouncy()
    {
        var pm = new PhysicsMaterial2D("ObstacleMat");
        pm.friction = 0f; pm.bounciness = 1f;
        return pm;
    }
}
