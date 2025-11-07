using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName="WeightedTileSet", menuName="Tiles/Weighted Tile Set")]
public class WeightedTileSet : ScriptableObject
{
    [System.Serializable]
    public struct Entry { public TileBase tile; [Min(0f)] public float weight; }

    public Entry[] entries;
    [Tooltip("Aynı dağılımın her chunk/koordinatta tekrarlanması için seed.")]
    public int seed = 12345;

    // 0..1 arası bir değeri ağırlıklara göre tile'a map eder
    public TileBase Pick(float r01)
    {
        if (entries == null || entries.Length == 0) return null;
        float sum = 0f; foreach (var e in entries) sum += Mathf.Max(0f, e.weight);
        if (sum <= 0f) return entries[0].tile;
        float t = r01 * sum;
        foreach (var e in entries)
        {
            float w = Mathf.Max(0f, e.weight);
            if (t <= w) return e.tile;
            t -= w;
        }
        return entries[entries.Length - 1].tile;
    }
}
