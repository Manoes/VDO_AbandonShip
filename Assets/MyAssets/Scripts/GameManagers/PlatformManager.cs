using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlatformManager : MonoBehaviour
{
    [Header("Tilemap Platform Output")]
    [Tooltip("Optional. If left empty, we auto-find the platform Tilemap by Tag.")]
    [SerializeField] private Tilemap platformTilemap;
    [Tooltip("Tag on the Platform Tilemap GameObject.")]
    [SerializeField] private string platformTilemapTag = "Platforms";

    [Header("Tiles")]
    [SerializeField] private TileBase platformTile;
    [SerializeField] private TileBase fallingPlatformTile; // <-- FIX #1 (this was missing)

    [Header("Falling Platforms (Prefab Tilemap Chunk)")]
    [Tooltip("Prefab root has Rigidbody2D + CompositeCollider2D + FallingTilemapPlatform. Child has Tilemap + TilemapCollider2D (Used By Composite).")]
    [SerializeField] private GameObject fallingTilemapPrefab;

    [Header("Spawn Height")]
    [SerializeField] private float spawnAhead = 14f;
    [SerializeField] private float despawnBelowDeathWall = 6f;

    [Header("Vertical Gaps (world units)")]
    [SerializeField] private float rowMinGapY = 3f;
    [SerializeField] private float rowMaxGapY = 5.5f;

    [Header("Platform Size (world units)")]
    [SerializeField] private float minWidth = 2.5f;
    [SerializeField] private float maxWidth = 7f;

    [Header("Screen Padding")]
    [SerializeField] private float edgePadding = 0.8f;

    [Header("Path Controls")]
    [SerializeField] private float maxStepX = 5.5f;
    [SerializeField] private float minStepX = 1.8f;
    [SerializeField] private bool alternateDirection = true;

    [Header("Extras")]
    [SerializeField] private int extrasPerRow = 2;
    [SerializeField] private float extraMinSeparationX = 3.5f;

    [Header("Spawn Safety")]
    [SerializeField] private float platformUnderPlayerYOffset = 1.0f;
    [SerializeField] private float minUnderPlayerWidth = 6.0f;

    [Header("Pickups: Jetpack")]
    [SerializeField] private GameObject jetpackPickupPrefab;
    [SerializeField] private float jetpackSpawnYOffset = 1.2f;

    // Runtime references
    Transform player;
    Transform deathWall;
    Camera cam;

    int nextRowCellY;
    float lastPathX;
    int dir = 1;

    public static PlatformManager Instance { get; private set; }
    void Awake() => Instance = this;

    struct PlacedSpan
    {
        public int yCell;
        public int xMin;
        public int xMax;
        public float worldY;

        public bool isFalling;
        public Transform fallingTransform; // prefab root if isFalling
    }

    readonly List<PlacedSpan> spans = new();

    float CellSizeX => platformTilemap ? platformTilemap.cellSize.x : 1f;
    float CellSizeY => platformTilemap ? platformTilemap.cellSize.y : 1f;

    bool EnsureTilemapBound()
    {
        if (platformTilemap && platformTilemap.gameObject.scene.IsValid())
            return true;

        platformTilemap = null;

        if (!string.IsNullOrWhiteSpace(platformTilemapTag))
        {
            // Works for active objects
            var taggedGO = GameObject.FindGameObjectWithTag(platformTilemapTag);
            if (taggedGO)
                platformTilemap = taggedGO.GetComponent<Tilemap>();

            // Works for inactive / disabled objects
            if (!platformTilemap)
            {
                var all = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    var tm = all[i];
                    if (!tm) continue;
                    if (tm.CompareTag(platformTilemapTag))
                    {
                        platformTilemap = tm;
                        break;
                    }
                }
            }
        }

        if (!platformTilemap)
        {
            Debug.LogError($"[PlatformManager] Could not find PLATFORM Tilemap with tag '{platformTilemapTag}'.");
            return false;
        }

        return true;
    }

    public void Init(Transform playerRef, Transform deathWallRef, Camera camRef)
    {
        player = playerRef;
        deathWall = deathWallRef;
        cam = camRef;
        EnsureTilemapBound();
    }

    void Update()
    {
        if (!player || !deathWall || !cam) return;
        if (!EnsureTilemapBound()) return;
        if (platformTile == null) return;

        // Spawn until above camera
        float camTopY = cam.transform.position.y + cam.orthographicSize;
        float targetTopY = camTopY + spawnAhead;
        int targetTopCellY = platformTilemap.WorldToCell(new Vector3(0f, targetTopY, 0f)).y;

        while (nextRowCellY < targetTopCellY)
            SpawnRow();

        // Despawn below death wall
        float killLine = deathWall.position.y - despawnBelowDeathWall;

        int removeCount = 0;
        for (int i = 0; i < spans.Count; i++)
        {
            if (spans[i].worldY >= killLine)
                break;

            var s = spans[i];

            if (s.isFalling)
            {
                if (s.fallingTransform) Destroy(s.fallingTransform.gameObject);
            }
            else
            {
                for (int x = s.xMin; x <= s.xMax; x++)
                    platformTilemap.SetTile(new Vector3Int(x, s.yCell, 0), null);
            }

            removeCount++;
        }

        if (removeCount > 0)
            spans.RemoveRange(0, removeCount);
    }

    void SpawnRow()
    {
        float score = GameManager.Instance ? GameManager.Instance.Score : 0f;

        if (DifficultyManager.Instance)
            DifficultyManager.Instance.GetGapRange(score, out rowMinGapY, out rowMaxGapY);

        int gapCells = Mathf.Max(
            1,
            Mathf.RoundToInt(Random.Range(rowMinGapY, rowMaxGapY) / Mathf.Max(0.0001f, CellSizeY))
        );
        nextRowCellY += gapCells;

        // Camera bounds (world)
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float camX = cam.transform.position.x;

        float minX = camX - halfWidth + edgePadding;
        float maxX = camX + halfWidth - edgePadding;

        // Track occupied x ranges for this row
        List<(int xMin, int xMax)> occupied = new();

        // Path platform
        float delta = Random.Range(minStepX, maxStepX) * dir;
        if (alternateDirection && Random.value < 0.55f) dir *= -1;

        float pathX = Mathf.Clamp(lastPathX + delta, minX, maxX);
        float pathWidth = Random.Range(minWidth, maxWidth);
        pathX = ClampInsideBounds(pathX, pathWidth, minX, maxX);

        PlacePlatformNoOverlap(pathX, nextRowCellY, pathWidth, occupied);
        lastPathX = pathX;

        // Extras
        for (int i = 0; i < extrasPerRow; i++)
        {
            float extraWidth = Random.Range(minWidth, maxWidth);

            bool placed = false;
            for (int tries = 0; tries < 10; tries++)
            {
                float extraX = Random.Range(minX, maxX);

                if (Mathf.Abs(extraX - pathX) < extraMinSeparationX)
                    continue;

                extraX = ClampInsideBounds(extraX, extraWidth, minX, maxX);

                if (PlacePlatformNoOverlap(extraX, nextRowCellY, extraWidth, occupied))
                {
                    placed = true;
                    break;
                }
            }

            _ = placed;
        }
    }

    bool PlacePlatformNoOverlap(float xWorld, int yCell, float widthWorld, List<(int xMin, int xMax)> occupied)
    {
        float score = GameManager.Instance ? GameManager.Instance.Score : 0f;
        float fallingChance = DifficultyManager.Instance ? DifficultyManager.Instance.GetFallingChance(score) : 0f;
        float jetpackChance = DifficultyManager.Instance ? DifficultyManager.Instance.GetJetpackChance(score) : 0f;

        // Only spawn falling if we have BOTH prefab + tile
        bool spawnFalling =
            fallingTilemapPrefab != null &&
            fallingPlatformTile != null &&
            Random.value < fallingChance;

        float tileW = Mathf.Max(0.0001f, CellSizeX);
        int tiles = Mathf.Max(1, Mathf.RoundToInt(widthWorld / tileW));

        float yWorld = platformTilemap.GetCellCenterWorld(new Vector3Int(0, yCell, 0)).y;
        int xCenter = platformTilemap.WorldToCell(new Vector3(xWorld, yWorld, 0f)).x;

        int xMin = xCenter - (tiles / 2);
        int xMax = xMin + tiles - 1;

        // overlap check
        for (int i = 0; i < occupied.Count; i++)
        {
            var o = occupied[i];
            bool overlaps = !(xMax < o.xMin || xMin > o.xMax);
            if (overlaps) return false;
        }
        occupied.Add((xMin, xMax));

        if (spawnFalling)
        {
            // spawn at the exact left-most cell position
            Vector3 worldCellOrigin = platformTilemap.GetCellCenterWorld(new Vector3Int(xMin, yCell, 0));

            GameObject go = Instantiate(fallingTilemapPrefab, worldCellOrigin, Quaternion.identity);

            // find the tilemap inside prefab
            Tilemap tm = go.GetComponentInChildren<Tilemap>(true);
            tm.ClearAllTiles();

            // paint horizontally starting at local cell (0,0)
            for (int i = 0; i < tiles; i++)
                tm.SetTile(new Vector3Int(i, 0, 0), fallingPlatformTile);

            // IMPORTANT: remove the original tiles from the main platform tilemap
            for (int x = xMin; x <= xMax; x++)
                platformTilemap.SetTile(new Vector3Int(x, yCell, 0), null);


            tm.RefreshAllTiles();
            tm.CompressBounds();

            var tmc = tm.GetComponent<TilemapCollider2D>();
            if (tmc) { tmc.enabled = false; tmc.enabled = true; }

            Physics2D.SyncTransforms();

            // reset physics script
            var falling = go.GetComponent<FallingTilemapPlatform>();
            if (falling) falling.ResetPlatform();

            spans.Add(new PlacedSpan
            {
                yCell = yCell,
                xMin = xMin,
                xMax = xMax,
                worldY = yWorld,
                isFalling = true,
                fallingTransform = go.transform
            });
        }
        else
        {
            for (int tx = xMin; tx <= xMax; tx++)
                platformTilemap.SetTile(new Vector3Int(tx, yCell, 0), platformTile);

            spans.Add(new PlacedSpan
            {
                yCell = yCell,
                xMin = xMin,
                xMax = xMax,
                worldY = yWorld,
                isFalling = false,
                fallingTransform = null
            });
        }

        // Jetpack
        if (jetpackPickupPrefab != null && Random.value < jetpackChance)
        {
            Vector3 pos = new Vector3(xWorld, yWorld + jetpackSpawnYOffset, 0f);
            Instantiate(jetpackPickupPrefab, pos, Quaternion.identity);
        }

        return true;
    }

    float ClampInsideBounds(float x, float width, float minX, float maxX)
    {
        float half = width * 0.5f;
        return Mathf.Clamp(x, minX + half, maxX - half);
    }

    void SpawnPlatformUnderPlayer()
    {
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float camX = cam.transform.position.x;

        float minX = camX - halfWidth + edgePadding;
        float maxX = camX + halfWidth - edgePadding;

        float yWorld = player.position.y - platformUnderPlayerYOffset;
        int yCell = platformTilemap.WorldToCell(new Vector3(0f, yWorld, 0f)).y;

        float width = Mathf.Max(minUnderPlayerWidth, Random.Range(minWidth, maxWidth));
        float xWorld = Mathf.Clamp(player.position.x, minX, maxX);
        xWorld = ClampInsideBounds(xWorld, width, minX, maxX);

        var occupied = new List<(int, int)>();
        PlacePlatformNoOverlap(xWorld, yCell, width, occupied);

        lastPathX = xWorld;

        float camBottomY = cam.transform.position.y - cam.orthographicSize;
        int camBottomCell = platformTilemap.WorldToCell(new Vector3(0f, camBottomY, 0f)).y;

        nextRowCellY = Mathf.Min(yCell, camBottomCell - 2);
    }

    public void ResetRun()
    {
        if (!player || !cam) return;
        if (!EnsureTilemapBound()) return;

        // clear tiles
        platformTilemap.ClearAllTiles();

        // destroy falling prefabs
        for (int i = 0; i < spans.Count; i++)
        {
            if (spans[i].isFalling && spans[i].fallingTransform)
                Destroy(spans[i].fallingTransform.gameObject);
        }

        spans.Clear();
        dir = 1;

        nextRowCellY = platformTilemap.WorldToCell(new Vector3(0f, player.position.y, 0f)).y - 10;

        SpawnPlatformUnderPlayer();

        float camTopY = cam.transform.position.y + cam.orthographicSize;
        float targetTopY = camTopY + spawnAhead;
        int targetTopCellY = platformTilemap.WorldToCell(new Vector3(0f, targetTopY, 0f)).y;

        while (nextRowCellY < targetTopCellY)
            SpawnRow();
    }
}
