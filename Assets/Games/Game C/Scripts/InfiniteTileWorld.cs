using UnityEngine;

public class InfiniteTileWorld : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public TileChunk chunkPrefab;      // TileChunk içeren prefab
    public WeightedTileSet weightedSet;// Aynı seti chunkPrefab'a da atayabilirsin

    [Header("Grid")]
    public int gridSize = 3;           // 3x3
    public int tilesPerSide = 30;      // chunk genişliği (TileChunk ile aynı olmalı)

    private TileChunk[,] chunks;
    private Vector2Int currentCenterChunk; // oyuncunun bulunduğu chunk (grid koordinatı)

    void Start()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;

        chunks = new TileChunk[gridSize, gridSize];
        int half = gridSize / 2;

        for (int gx = 0; gx < gridSize; gx++)
        for (int gy = 0; gy < gridSize; gy++)
        {
            var c = Instantiate(chunkPrefab, transform);
            c.tilesPerSide = tilesPerSide;
            if (weightedSet) c.weightedSet = weightedSet;

            // Yerleşim (chunk yerel hücreleri 0..N-1 olduğu için pozisyonu 0’a koyuyoruz)
            c.transform.position = Vector3.zero;
            chunks[gx, gy] = c;
        }

        // İlk kez merkez hesapla ve diz
        ForceRebuildAll();
    }

    void Update()
    {
        if (!player) return;

        Vector2 playerCell = new Vector2(Mathf.Floor(player.position.x), Mathf.Floor(player.position.y));
        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(playerCell.x / tilesPerSide),
            Mathf.FloorToInt(playerCell.y / tilesPerSide)
        );

        if (playerChunk != currentCenterChunk)
        {
            currentCenterChunk = playerChunk;
            RepositionAndRebuild();
        }
    }

    void ForceRebuildAll()
    {
        currentCenterChunk = GetPlayerChunk();
        RepositionAndRebuild();
    }

    Vector2Int GetPlayerChunk()
    {
        Vector2 playerCell = new Vector2(Mathf.Floor(player.position.x), Mathf.Floor(player.position.y));
        return new Vector2Int(
            Mathf.FloorToInt(playerCell.x / tilesPerSide),
            Mathf.FloorToInt(playerCell.y / tilesPerSide)
        );
    }

    void RepositionAndRebuild()
    {
        int half = gridSize / 2;

        for (int gx = 0; gx < gridSize; gx++)
        for (int gy = 0; gy < gridSize; gy++)
        {
            // Bu chunk'ın dünya chunk koordinatı
            int cx = currentCenterChunk.x + (gx - half);
            int cy = currentCenterChunk.y + (gy - half);

            var chunk = chunks[gx, gy];

            // Bu chunk, dünya koordinatında (cx,cy) aralığını temsil ediyor.
            // Tilemap hücreleri yerel 0..N-1 olduğundan, objeyi dünya pozisyonuna taşıyoruz:
            chunk.transform.position = new Vector3(cx * tilesPerSide, cy * tilesPerSide, 0f);

            // İçeriği oluştur (deterministic → aynı yer hep aynı pattern)
            chunk.BuildChunk(cx, cy);
        }
    }
}
