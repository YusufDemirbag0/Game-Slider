using UnityEngine;
using System.Collections;

public class SidePointSpawner : MonoBehaviour
{
    [Header("Prefab (PUAN NESNESİ)")]
    [SerializeField] GameObject pickupPrefab; // ScorePickup + CircleCollider2D (isTrigger) + Rigidbody2D(Kinematic) + Tag="Pickup"

    [Header("Sıklık (sn)")]
    [SerializeField] float spawnEveryMin = 2.5f;
    [SerializeField] float spawnEveryMax = 5f;

    [Header("Hız")]
    [SerializeField] float moveSpeedMin = 2.5f;
    [SerializeField] float moveSpeedMax = 4.5f;

    [Header("Y Konumu")]
    [SerializeField] Vector2 yPadding = new(.6f, 1.0f);
    [SerializeField, Range(0f, 0.8f)] float bottomClip = 0.15f;

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
        if (!pickupPrefab) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;

        float minY = c.y - halfH + yPadding.x + (halfH * bottomClip);
        float maxY = c.y + halfH - yPadding.y;
        if (minY > maxY) minY = maxY - 0.1f;

        bool fromLeft = Random.value < 0.5f;
        float y = Random.Range(minY, maxY);
        float x = fromLeft ? c.x - halfW - 1.0f : c.x + halfW + 1.0f;

        var go = Instantiate(pickupPrefab, new Vector3(x, y, 0), Quaternion.identity);
        go.tag = "Pickup";

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
}
