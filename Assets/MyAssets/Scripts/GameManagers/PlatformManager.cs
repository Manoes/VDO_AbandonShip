using System.Collections.Generic;
using UnityEngine;

public class PlatformManager : MonoBehaviour
{
    [SerializeField] private GameObject platformPrefab;

    [Header("Spawn Height")]
    [SerializeField] private float spawnAhead = 14f; // How Far the Platforms spawn above the Player
    [SerializeField] private float despawnBelowDeathWall = 6f;

    [Header("Vertical Gaps")]
    [SerializeField] private float rowMinGapY = 3f;
    [SerializeField] private float rowMaxGapY = 5.5f;

    [Header("Platform Size")]
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
    [SerializeField] private float extraYOffsetJitter = 0.6f;

    [Header("Spawn Safety")]
    [SerializeField] private float platformUnderPlayerYOffset = 1.0f;
    [SerializeField] private float minUnderPlayerWidth = 6.0f;

    [Header("Pickups: Jetpack")]
    [SerializeField] private GameObject jetpackPickupPrefab;
    [SerializeField] private float jetpackSpawnYOffset = 1.2f;

    readonly List<Transform> platforms = new List<Transform>();

    // References -> GameManager sets these values
    Transform player;
    Transform deathWall;
    Camera cam;

    float nextRowY;
    float lastPathX;
    int dir = 1;

    public void Init(Transform playerRef, Transform deathWallRef, Camera camRef)
    {
        player = playerRef;
        deathWall = deathWallRef;
        cam = camRef;
    }

    void Update()
    {
        if (!player || !deathWall || !cam || !platformPrefab) return;

        // Spawn Platforms as Player Clims
        float camTopY = cam.transform.position.y + cam.orthographicSize;
        float targetTop = camTopY + spawnAhead;
        while (nextRowY < targetTop)
            SpawnRow();

        // Despawn Platforms when Consumed by Exploding Ship (Death Wall)
        float killLine = deathWall.position.y - despawnBelowDeathWall;

        for (int i = platforms.Count - 1; i >= 0; i--)
        {
            Transform transform = platforms[i];
            if (transform == null)
            {
                platforms.RemoveAt(i);
                continue;
            }

            if (transform.position.y < killLine)
            {
                Destroy(transform.gameObject);
                platforms.RemoveAt(i);
            }
        }
    }

    void SpawnRow()
    {
        float score = GameManager.Instance ? GameManager.Instance.Score : 0f;

        // Update Gap Range from Difficulty
        if(DifficultyManager.Instance)
            DifficultyManager.Instance.GetGapRange(score, out rowMinGapY, out rowMaxGapY);

        nextRowY += Random.Range(rowMinGapY, rowMaxGapY);

        // Compute Camera World Bounds at this Row's Y
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float camX = cam.transform.position.x;

        float minX = camX - halfWidth + edgePadding;
        float maxX = camX + halfWidth - edgePadding;

        float delta = Random.Range(minStepX, maxStepX) * dir;

        if (alternateDirection && Random.value < 0.55f) dir *= -1;

        float pathX = Mathf.Clamp(lastPathX + delta, minX, maxX);
        float pathWidth = Random.Range(minWidth, maxWidth);
        pathX = ClampInsideBounds(pathX, pathWidth, minX, maxX);

        SpawnPlatformAt(pathX, nextRowY, pathWidth);
        lastPathX = pathX;

        for (int i = 0; i < extrasPerRow; i++)
        {
            float extraWidth = Random.Range(minWidth, maxWidth);

            // Try a few times to find a good separated X
            float extraX = pathX;
            for (int tries = 0; tries < 6; tries++)
            {
                extraX = Random.Range(minX, maxX);
                if (Mathf.Abs(extraX - pathX) >= extraMinSeparationX) break;
            }

            // Clamp to bounds with platform width considered
            extraX = ClampInsideBounds(extraX, extraWidth, minX, maxX);

            // Slight vertical jitter so it doesn't feel like “one flat row”
            float extraY = nextRowY + Random.Range(-extraYOffsetJitter, extraYOffsetJitter);

            SpawnPlatformAt(extraX, extraY, extraWidth);
        }

        nextRowY += 0.15f;
    }

    float ClampInsideBounds(float x, float width, float minX, float maxX)
    {
        float half = width * 0.5f;
        return Mathf.Clamp(x, minX + half, maxX - half);
    }

    void SpawnPlatformAt(float x, float y, float width)
    {
        GameObject platform = Instantiate(platformPrefab);
        platform.transform.position = new Vector3(x, y, 0);
        platform.transform.localScale = new Vector3(width, 1f, 1f);
        platform.layer = LayerMask.NameToLayer("Ground");
        platforms.Add(platform.transform);

        float score = GameManager.Instance ? GameManager.Instance.Score : 0f;

        float fallingChance = DifficultyManager.Instance
            ? DifficultyManager.Instance.GetFallingChance(score)
            : 0f;
        
        float jetpackChance = DifficultyManager.Instance
            ? DifficultyManager.Instance.GetJetpackChance(score)
            : 0f;

        // Enable/Disable the Fallingplatform Component if present
        if(platform.TryGetComponent<FallingPlatform>(out var fallingPlatform))
        {
            bool armed = Random.value < fallingChance;
            fallingPlatform.SetArmed(armed);
        }

        // Jetpack Spawn (Decreasing Chance with Score)
        if(jetpackPickupPrefab != null)
        {
            if(Random.value < jetpackChance)
            {
                Vector3 position = new Vector3(x, y + jetpackSpawnYOffset, 0f);
                Instantiate(jetpackPickupPrefab, position, Quaternion.identity);
            }
        }
    }

    void SpawnPlatformUnderPlayer()
    {
        // Compute Camera World Bounds at this Row's Y
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float camX = cam.transform.position.x;

        float minX = camX - halfWidth + edgePadding;
        float maxX = camX + halfWidth - edgePadding;

        float y = player.position.y - platformUnderPlayerYOffset;

        float width = Mathf.Max(minUnderPlayerWidth, Random.Range(minWidth, maxWidth));
        float x = Mathf.Clamp(player.position.x, minX, maxX);
        x = ClampInsideBounds(x, width, minX, maxX);

        SpawnPlatformAt(x, y, width);

        // Make the Path Start from here
        lastPathX = x;

        // Ensure Future Rows Spawn Above this, not Below it
        float camBottomY = cam.transform.position.y - cam.orthographicSize;
        nextRowY = Mathf.Min(y, camBottomY - 1f);
    }

    public void ResetRun()
    {

        // Destroy Existing Platforms
        for (int i = platforms.Count - 1; i >= 0; i--)
        {
            if (platforms[i] != null)
                Destroy(platforms[i].gameObject);
        }
        platforms.Clear();

        // Reset Generation State
        dir = 1;

        // Always Guarantee a Platform under Player
        SpawnPlatformUnderPlayer();

        // Spawn Initial Set
        float camTopY = cam.transform.position.y + cam.orthographicSize;
        float targetTop = camTopY + spawnAhead;
        while (nextRowY < targetTop)
            SpawnRow();
    }
}
