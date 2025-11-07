using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class GridManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] int size = 5;
    [SerializeField] float cellSize = 160f;
    [SerializeField] RectTransform tileParent;   // Canvas altındaki "Tiles"
    [SerializeField] TileUI tilePrefab;          // PREFABI BURAYA ASSIGN ET

    [Header("Input")]
    [SerializeField] float swipeThreshold = 40f; // px

    [Header("Spawn")]
    [SerializeField] bool spawnFourSometimes = true; // %10 olasılıkla 4

    TileUI[,] grid;
    bool isMoving;
    Vector2 pointerDownPos, pointerUpPos;

    void Awake()
    {
        grid = new TileUI[size, size];
        Application.targetFrameRate = 120;

        if (!tileParent) Debug.LogError("Tile Parent atanmamış!");
        if (!tilePrefab) Debug.LogError("Tile Prefab atanmamış!");
    }

    void Start()
    {
        SpawnRandomTile(2);
        SpawnRandomTile(2);
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0)) pointerDownPos = Input.mousePosition;
        if (Input.GetMouseButtonUp(0))   { pointerUpPos = Input.mousePosition; TrySwipe(pointerUpPos - pointerDownPos); }
#else
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) pointerDownPos = t.position;
            else if (t.phase == TouchPhase.Ended) { pointerUpPos = t.position; TrySwipe(pointerUpPos - pointerDownPos); }
        }
#endif
    }

    void TrySwipe(Vector2 delta)
    {
        if (isMoving || delta.magnitude < swipeThreshold) return;

        Vector2 dir = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
            ? new Vector2(Mathf.Sign(delta.x), 0)   // sağ-sol
            : new Vector2(0, Mathf.Sign(delta.y));  // yukarı-aşağı (anchoredPos'ta +y yukarı)

        StartCoroutine(MoveAndSpawn(dir));
    }

    IEnumerator MoveAndSpawn(Vector2 dir)
    {
        bool moved = false, mergedThisTurn = false;
        isMoving = true;

        int startX = dir.x > 0 ? size - 1 : 0,   endX = dir.x > 0 ? -1 : size,   stepX = dir.x > 0 ? -1 : 1;
        int startY = dir.y > 0 ? size - 1 : 0,   endY = dir.y > 0 ? -1 : size,   stepY = dir.y > 0 ? -1 : 1;
        bool[,] mergedFlag = new bool[size, size];

        if (Mathf.Abs(dir.x) > 0.1f) // yatay
        {
            for (int y = 0; y < size; y++)
                for (int x = startX; x != endX; x += stepX)
                    if (grid[x, y] != null)
                    {
                        bool m;
                        moved |= SlideTile(x, y, (int)Mathf.Sign(dir.x), 0, mergedFlag, out m);
                        if (m) mergedThisTurn = true;
                    }
        }
        else // dikey
        {
            for (int x = 0; x < size; x++)
                for (int y = startY; y != endY; y += stepY)
                    if (grid[x, y] != null)
                    {
                        bool m;
                        moved |= SlideTile(x, y, 0, (int)Mathf.Sign(dir.y), mergedFlag, out m);
                        if (m) mergedThisTurn = true;
                    }
        }

        yield return new WaitForSecondsRealtime(0.09f);

        if (moved)
        {
            int spawnCount = 1 + (mergedThisTurn ? 1 : 0); // birleşme varsa bu tur 2 doğur
            for (int i = 0; i < spawnCount; i++) if (!SpawnRandomTile()) break;
            if (IsGameOver()) Debug.Log("Oyun bitti!");
        }

        isMoving = false;
    }

    bool SlideTile(int x, int y, int dx, int dy, bool[,] mergedFlag, out bool mergedOut)
    {
        mergedOut = false;
        TileUI t = grid[x, y]; if (t == null) return false;

        int nx = x, ny = y; bool moved = false;

        while (true)
        {
            int tx = nx + dx, ty = ny + dy;
            if (tx < 0 || tx >= size || ty < 0 || ty >= size) break;

            if (grid[tx, ty] == null)
            {
                grid[tx, ty] = t; grid[nx, ny] = null;
                nx = tx; ny = ty; moved = true;
            }
            else
            {
                if (!mergedFlag[tx, ty] && grid[tx, ty].Value == t.Value)
                {
                    TileUI target = grid[tx, ty];
                    int newVal = t.Value * 2;

                    // Merge: taşı hedefe "snap" et, sonra yok et; hedefi büyüt.
                    if (t.Rect) t.Rect.anchoredPosition = GetCellPos(tx, ty);
                    Destroy(t.gameObject);
                    grid[nx, ny] = null;

                    target.SetValue(newVal);
                    StartCoroutine(target.Pop());
                    mergedFlag[tx, ty] = true;

                    moved = true; mergedOut = true;
                }
                break;
            }
        }

        if (grid[nx, ny] == t && t.Rect)
            StartCoroutine(t.AnimateMove(GetCellPos(nx, ny)));

        return moved;
    }

    bool SpawnRandomTile(int forceVal = 0)
    {
        List<Vector2Int> empties = new List<Vector2Int>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (grid[x, y] == null) empties.Add(new Vector2Int(x, y));

        if (empties.Count == 0 || !tilePrefab || !tileParent) return false;

        Vector2Int pick = empties[Random.Range(0, empties.Count)];
        var tile = Instantiate(tilePrefab, tileParent);
        tile.Rect.anchoredPosition = GetCellPos(pick.x, pick.y);

        int val = (forceVal > 0) ? forceVal : ((spawnFourSometimes && Random.value < 0.10f) ? 4 : 2);
        tile.SetValue(val);
        grid[pick.x, pick.y] = tile;

        StartCoroutine(tile.Pop(1.12f, 0.07f));
        return true;
    }

    bool IsGameOver()
    {
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                if (grid[x, y] == null) return false;

        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
        {
            int v = grid[x, y].Value;
            if (x + 1 < size && grid[x + 1, y].Value == v) return false;
            if (y + 1 < size && grid[x, y + 1].Value == v) return false;
        }
        return true;
    }

    // (0,0) merkez — 160 adım: ...,-320,-160,0,160,320
    Vector2 GetCellPos(int x, int y)
    {
        float half = (size - 1) * 0.5f;
        float ox = (x - half) * cellSize;
        float oy = (y - half) * cellSize;
        return new Vector2(ox, oy);
    }
}
