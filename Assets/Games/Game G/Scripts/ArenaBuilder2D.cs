using UnityEngine;

public class ArenaBuilder2D : MonoBehaviour
{
    [Header("Duvar Kalınlığı")]
    [SerializeField] float wallThickness = 1f;

    [Header("Alt çizgi (ölüm) için ekstra boşluk")]
    [SerializeField] float bottomGap = 2f;

    void Start()
    {
        var cam = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;

        // Top
        CreateWall(new Vector2(c.x, c.y + halfH + wallThickness*0.5f), new Vector2(halfW*2f + wallThickness*2f, wallThickness));
        // Left
        CreateWall(new Vector2(c.x - halfW - wallThickness*0.5f, c.y), new Vector2(wallThickness, halfH*2f + wallThickness*2f));
        // Right
        CreateWall(new Vector2(c.x + halfW + wallThickness*0.5f, c.y), new Vector2(wallThickness, halfH*2f + wallThickness*2f));
        // Bottom (görünmez referans istiyorsan kapat): burada açık bırakıyoruz, sadece istersen çiz
        // CreateWall(new Vector2(c.x, c.y - halfH - wallThickness*0.5f), new Vector2(halfW*2f, wallThickness));
    }

    void CreateWall(Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Wall");
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.sharedMaterial = MakeBouncy(); // sekme için
    }

    PhysicsMaterial2D MakeBouncy()
    {
        var pm = new PhysicsMaterial2D("WallMat");
        pm.friction = 0f;
        pm.bounciness = 1f;
        return pm;
    }
}
