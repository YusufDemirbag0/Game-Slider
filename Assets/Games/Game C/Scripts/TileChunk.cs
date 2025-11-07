using UnityEngine;
using UnityEngine.Tilemaps;

public class TileChunk : MonoBehaviour
{
    [Header("Grid/Tilemap")]
    public Tilemap tilemap;

    [Header("Boyut")]
    [Min(1)] public int tilesPerSide = 30;   // 30x30

    [Header("Kaynak")]
    public WeightedTileSet weightedSet;

    public void BuildChunk(int chunkX, int chunkY)
    {
        if (!tilemap || !weightedSet) return;

        // 1) Önce eski hücreleri temizle (yeniden kullanıldığında artefakt olmasın)
        tilemap.ClearAllTiles();

        // 2) Chunk’ın dünya grid koordinat aralığı
        int startX = chunkX * tilesPerSide;
        int startY = chunkY * tilesPerSide;

        // 3) Hücreleri doldur
        for (int lx = 0; lx < tilesPerSide; lx++)
        for (int ly = 0; ly < tilesPerSide; ly++)
        {
            int wx = startX + lx;
            int wy = startY + ly;

            // Deterministic pseudo-random: her dünya hücresi hep aynı tile'ı alır
            float r = Hash01(wx, wy, weightedSet.seed);
            TileBase t = weightedSet.Pick(r);

            tilemap.SetTile(new Vector3Int(lx, ly, 0), t);
        }

        // 4) Görsel dikişleri azaltmak için refresh
        tilemap.RefreshAllTiles();
    }

    // Basit deterministic hash → 0..1
    static float Hash01(int x, int y, int seed)
    {
        unchecked
        {
            uint h = 2166136261u;
            h = (h ^ (uint)x) * 16777619u;
            h = (h ^ (uint)y) * 16777619u;
            h = (h ^ (uint)seed) * 16777619u;
            // 0..1
            return (h & 0x00FFFFFF) / 16777215f;
        }
    }
}
