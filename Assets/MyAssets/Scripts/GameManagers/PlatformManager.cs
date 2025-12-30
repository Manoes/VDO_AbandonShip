using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlatformManager : MonoBehaviour
{
    [Header("Tilemaps")]
    [Tooltip("Optional. If left empty, we auto-find the platform Tilemap by Tag.")]
    [SerializeField] private Tilemap platformTilemap;
    [Tooltip("Tag on the Platform Tilemap GameObject.")]
    [SerializeField] private string platformTilemapTag = "Platforms";

    [SerializeField] private Tilemap spikesTileMap;
    [SerializeField] private string spikesTilemapTag = "Spikes";
    [SerializeField] private LayerMask hazardMask;

    [Header("Rule Tiles")]
    [SerializeField] private TileBase platformTile;
    [SerializeField] private TileBase fallingPlatformTile; 
    [SerializeField] private TileBase spikeTile;

    [Header("Falling Platforms (Prefab Tilemap Chunk)")]
    [SerializeField] private GameObject fallingTilemapPrefab;

    [Header("Spikes")]
    [Tooltip("Keep First/Last N Tiles of a Platform Spike-Free")]
    [SerializeField] private int spikesSafeEdgeTiles = 1;
    [Tooltip("Never Cover more than the Fraction of a Platform with Spikes ")]
    [Range(0f, 1f)]
    [SerializeField] private float spikesMaxCoverage = 0.6f;

    [Header("Laser")]
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private int laserMinTiles = 3;
    [SerializeField] private int laserMaxTiles = 7;
    [SerializeField] private float laserYOffset = 0.45f;

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

        public bool hasSpikes;
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

        // Spikes TileMap
        if (!spikesTileMap)
        {
            if (!string.IsNullOrWhiteSpace(spikesTilemapTag))
            {
                var taggedGO = GameObject.FindGameObjectWithTag(spikesTilemapTag);
                if(taggedGO) spikesTileMap = taggedGO.GetComponent<Tilemap>();

                if (!spikesTileMap)
                {
                    var all = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    for(int i = 0; i < all.Length; i++)
                    {
                        var tm = all[i];
                        if(!tm) continue;
                        if (tm.CompareTag(spikesTilemapTag))
                        {
                            spikesTileMap = tm;
                            break;
                        }
                    }
                }
            }
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

            if(s.hasSpikes)
                ClearSpikesSpan(s);

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

        var span = new PlacedSpan
        {
            yCell = yCell,
            xMin = xMin,
            xMax = xMax,
            worldY = yWorld,
            isFalling = false,
            fallingTransform = null,
            hasSpikes = false
        };

        if (spawnFalling)
        {
            // Spawn at the Exact Left-Most Cell Position
            Vector3 worldCellOrigin = platformTilemap.GetCellCenterWorld(new Vector3Int(xMin, yCell, 0));
            GameObject go = Instantiate(fallingTilemapPrefab, worldCellOrigin, Quaternion.identity);

            // Find the Tilemap Inside Prefab
            Tilemap tm = go.GetComponentInChildren<Tilemap>(true);
            if (!tm)
            {
                Debug.LogError("[PlatformManager] FallingTilemapPrefab needs a Tilemap in Children.");
                Destroy(go);
                return false;
            }

            tm.ClearAllTiles();

            // Paint Horizontally starting at Local Cell (0,0)
            for (int i = 0; i < tiles; i++)
                tm.SetTile(new Vector3Int(i, 0, 0), fallingPlatformTile);

            // Remove the Original Tiles from the Main Platform Tilemap
            for (int x = xMin; x <= xMax; x++)
                platformTilemap.SetTile(new Vector3Int(x, yCell, 0), null);


            tm.RefreshAllTiles();
            tm.CompressBounds();

            var tmc = tm.GetComponent<TilemapCollider2D>();
            if (tmc) { tmc.enabled = false; tmc.enabled = true; }
            Physics2D.SyncTransforms();

            // Reset Physics Script
            var falling = go.GetComponent<FallingTilemapPlatform>();
            if (falling) falling.ResetPlatform();

            span.isFalling = true;
            span.fallingTransform = go.transform;
        }
        else
        {
            for (int tx = xMin; tx <= xMax; tx++)
                platformTilemap.SetTile(new Vector3Int(tx, yCell, 0), platformTile);
        }

        // Spawn Spikes -> Only on Non-Falling Platforms
        if (!span.isFalling)
        {
            if(TryPlaceSpikesOnSpan(ref span, score))
            {
                
            }
        }

        if(!span.isFalling && !span.hasSpikes)
        {
            TrySpawnLaserOnSpan(yCell, xMin, xMax);
        }

        spans.Add(span);

        // Jetpack
        if (jetpackPickupPrefab != null && Random.value < jetpackChance)
        {
            Vector3 pos = new Vector3(xWorld, yWorld + jetpackSpawnYOffset, 0f);

            if(IsJetPackCellClear(pos))
                Instantiate(jetpackPickupPrefab, pos, Quaternion.identity);
        }

        return true;
    }

    bool IsJetPackCellClear(Vector3 worldPos)
    {
        if (spikesTileMap)
        {
            Vector3Int cell = spikesTileMap.WorldToCell(worldPos);
            if(spikesTileMap.HasTile(cell)) return false;
        }

        // Reject if Overlaps Hazards
        Collider2D hit = Physics2D.OverlapCircle(worldPos, 0.2f, hazardMask);         
        return hit == null;
    }

    bool TryPlaceSpikesOnSpan(ref PlacedSpan span, float score)
    {
        if(spikeTile == null) return false;
        if(!DifficultyManager.Instance) return false;
        if(!DifficultyManager.Instance.SpikesEnabled(score)) return false;

        float chance = DifficultyManager.Instance.GetSpikesChance(score);
        if(chance <= 0f) return false;
        if(Random.value >= chance) return false;

        int widthTiles = span.xMax - span.xMin + 1;

        // Don't Allow Spikes to be Impossible
        int safe = Mathf.Max(0, spikesSafeEdgeTiles);
        int usable = widthTiles - safe * 2;
        if(usable <= 0) return false;

        int maxCover = Mathf.Clamp(Mathf.FloorToInt(usable * Mathf.Clamp01(spikesMaxCoverage)), 1, usable); 

        // Choose a Contigunous Spike Run Lenght
        int run = Random.Range(1, maxCover+ 1);

        // Choose Start within Usable Range
        int startMin = span.xMin + safe;
        int startMax = span.xMax - safe - run + 1;
        if(startMax < startMin) return false;

        int start = Random.Range(startMin, startMax + 1);
        int end = start + run - 1;

        // Spikes are placed on top Row: yCell +1 
        int spikesY = span.yCell + 1;

        var target = spikesTileMap ? spikesTileMap : platformTilemap;

        for(int x = start; x <= end; x++)
            target.SetTile(new Vector3Int(x, spikesY, 0), spikeTile);
        
        span.hasSpikes = true;
        return true;
    }

    void ClearSpikesSpan(PlacedSpan span)
    {
        if(spikeTile == null) return;

        var target = spikesTileMap ? spikesTileMap : platformTilemap;

        int spikesY = span.yCell + 1;
        for(int x = span.xMin; x <= span.xMax; x++)
            target.SetTile(new Vector3Int(x, spikesY, 0), null);
    }

    void TrySpawnLaserOnSpan(int yCell, int xMin, int xMax)
    {
        if(!laserPrefab) return;

        float score = GameManager.Instance ? GameManager.Instance.Score : 0f;
        if(!DifficultyManager.Instance || !DifficultyManager.Instance.LaserEnabled(score)) return;

        float chance = DifficultyManager.Instance.GetLaserChance(score);
        if(chance <= 0f) return;
        if(Random.value >= chance) return;

        int usableMin = xMin + 1;
        int usableMax = xMax - 1;
        if(usableMax - usableMin + 1 < laserMinTiles) return;

        int maxLen = Mathf.Min(laserMaxTiles, usableMax - usableMin + 1);
        int lenTiles = Random.Range(laserMinTiles, maxLen + 1);

        int startX = Random.Range(usableMin, usableMax - lenTiles + 2);
        int endX = startX + lenTiles - 1;

        // World Position = Middle of Segment
        Vector3 startWorld = platformTilemap.GetCellCenterWorld(new Vector3Int(startX, yCell, 0));
        Vector3 endWorld = platformTilemap.GetCellCenterWorld(new Vector3Int(endX, yCell, 0));
        Vector3 mid = (startWorld + endWorld) * 0.5f;

        float lenghtWorld = lenTiles * CellSizeX;

        // Place slightly above Platform
        Vector3 pos = new Vector3(mid.x, startWorld.y + laserYOffset, 0f);

        GameObject go = Instantiate(laserPrefab, pos, Quaternion.identity);
        var laser = go.GetComponent<LaserHazard>();
        if(laser) laser.SetLengthWorld(lenghtWorld);
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
        if(spikesTileMap) spikesTileMap.ClearAllTiles();

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
