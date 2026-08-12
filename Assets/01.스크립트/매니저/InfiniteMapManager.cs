using UnityEngine;
using System.Linq;

public class InfiniteMapManager : MonoBehaviour
{
    [Header("기본")]
    [InspectorLabel("플레이어")]
    [SerializeField] private Transform player;

    [InspectorLabel("바닥 프리팹")]
    [SerializeField] private GameObject groundPrefab;

    [InspectorLabel("타일 크기")]
    [SerializeField] private float tileSize;

    [InspectorLabel("전체 가로 타일 개수")]
    [SerializeField] private int gridWidth;

    [InspectorLabel("전체 세로 타일 개수")]
    [SerializeField] private int gridHeight;

    [Header("활성 영역")]
    [InspectorLabel("활성 가로 타일 개수")]
    [SerializeField] private int activeWidth;

    [InspectorLabel("활성 세로 타일 개수")]
    [SerializeField] private int activeHeight;

    [Header("랜덤 타일")]
    [InspectorLabel("스프라이트 경로")]
    [SerializeField] private string groundSpritePath = "GroundTiles";

    private Sprite[] groundSprites;
    private Transform[,] tiles;
    private Vector2Int currentPlayerTile;

    private void Awake()
    {
        groundSprites = Resources.LoadAll<Sprite>(groundSpritePath)
            .Where(sprite =>
                sprite.name != "TX Tileset Grass_62" &&
                sprite.name != "TX Tileset Grass_63")
            .ToArray();
    }

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        gridWidth = Mathf.Max(3, gridWidth);
        gridHeight = Mathf.Max(3, gridHeight);
        activeWidth = Mathf.Clamp(activeWidth, 1, gridWidth);
        activeHeight = Mathf.Clamp(activeHeight, 1, gridHeight);

        if (gridWidth % 2 == 0)
            gridWidth++;

        if (gridHeight % 2 == 0)
            gridHeight++;

        if (activeWidth % 2 == 0)
            activeWidth++;

        if (activeHeight % 2 == 0)
            activeHeight++;

        activeWidth = Mathf.Min(activeWidth, gridWidth);
        activeHeight = Mathf.Min(activeHeight, gridHeight);

        currentPlayerTile = GetPlayerTile();

        CreateTiles();
        PlaceAllTiles();
        RefreshTileActiveStates();
    }

    private void Update()
    {
        if (player == null || tiles == null)
            return;

        Vector2Int playerTile = GetPlayerTile();
        Vector2Int delta = playerTile - currentPlayerTile;

        if (delta == Vector2Int.zero)
            return;

        while (delta.x > 0)
        {
            MoveLeftColumnToRight();
            currentPlayerTile.x++;
            delta.x--;
        }

        while (delta.x < 0)
        {
            MoveRightColumnToLeft();
            currentPlayerTile.x--;
            delta.x++;
        }

        while (delta.y > 0)
        {
            MoveBottomRowToTop();
            currentPlayerTile.y++;
            delta.y--;
        }

        while (delta.y < 0)
        {
            MoveTopRowToBottom();
            currentPlayerTile.y--;
            delta.y++;
        }

        RefreshTileActiveStates();
    }

    private void CreateTiles()
    {
        if (groundPrefab == null)
            return;

        tiles = new Transform[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                GameObject tile = Instantiate(groundPrefab, transform);
                tile.name = $"Ground Tile {x}_{z}";

                RandomizeTileSprite(tile.transform);

                tiles[x, z] = tile.transform;
            }
        }
    }

    private void PlaceAllTiles()
    {
        int halfWidth = gridWidth / 2;
        int halfHeight = gridHeight / 2;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                int tileX = currentPlayerTile.x + x - halfWidth;
                int tileZ = currentPlayerTile.y + z - halfHeight;

                tiles[x, z].position = GetWorldPosition(tileX, tileZ);
            }
        }
    }

    private void MoveLeftColumnToRight()
    {
        int lastX = gridWidth - 1;
        int halfWidth = gridWidth / 2;
        int halfHeight = gridHeight / 2;

        for (int z = 0; z < gridHeight; z++)
        {
            Transform movedTile = tiles[0, z];

            for (int x = 0; x < lastX; x++)
                tiles[x, z] = tiles[x + 1, z];

            tiles[lastX, z] = movedTile;

            int tileX = currentPlayerTile.x + halfWidth + 1;
            int tileZ = currentPlayerTile.y + z - halfHeight;

            movedTile.position = GetWorldPosition(tileX, tileZ);
            RandomizeTileSprite(movedTile);
        }
    }

    private void MoveRightColumnToLeft()
    {
        int lastX = gridWidth - 1;
        int halfWidth = gridWidth / 2;
        int halfHeight = gridHeight / 2;

        for (int z = 0; z < gridHeight; z++)
        {
            Transform movedTile = tiles[lastX, z];

            for (int x = lastX; x > 0; x--)
                tiles[x, z] = tiles[x - 1, z];

            tiles[0, z] = movedTile;

            int tileX = currentPlayerTile.x - halfWidth - 1;
            int tileZ = currentPlayerTile.y + z - halfHeight;

            movedTile.position = GetWorldPosition(tileX, tileZ);
            RandomizeTileSprite(movedTile);
        }
    }

    private void MoveBottomRowToTop()
    {
        int lastZ = gridHeight - 1;
        int halfWidth = gridWidth / 2;
        int halfHeight = gridHeight / 2;

        for (int x = 0; x < gridWidth; x++)
        {
            Transform movedTile = tiles[x, 0];

            for (int z = 0; z < lastZ; z++)
                tiles[x, z] = tiles[x, z + 1];

            tiles[x, lastZ] = movedTile;

            int tileX = currentPlayerTile.x + x - halfWidth;
            int tileZ = currentPlayerTile.y + halfHeight + 1;

            movedTile.position = GetWorldPosition(tileX, tileZ);
            RandomizeTileSprite(movedTile);
        }
    }

    private void MoveTopRowToBottom()
    {
        int lastZ = gridHeight - 1;
        int halfWidth = gridWidth / 2;
        int halfHeight = gridHeight / 2;

        for (int x = 0; x < gridWidth; x++)
        {
            Transform movedTile = tiles[x, lastZ];

            for (int z = lastZ; z > 0; z--)
                tiles[x, z] = tiles[x, z - 1];

            tiles[x, 0] = movedTile;

            int tileX = currentPlayerTile.x + x - halfWidth;
            int tileZ = currentPlayerTile.y - halfHeight - 1;

            movedTile.position = GetWorldPosition(tileX, tileZ);
            RandomizeTileSprite(movedTile);
        }
    }

    private void RefreshTileActiveStates()
    {
        int centerX = gridWidth / 2;
        int centerZ = gridHeight / 2;

        int activeHalfWidth = activeWidth / 2;
        int activeHalfHeight = activeHeight / 2;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                bool isActive =
                    x >= centerX - activeHalfWidth &&
                    x <= centerX + activeHalfWidth &&
                    z >= centerZ - activeHalfHeight &&
                    z <= centerZ + activeHalfHeight;

                tiles[x, z].gameObject.SetActive(isActive);
            }
        }
    }

    private void RandomizeTileSprite(Transform tile)
    {
        if (groundSprites == null || groundSprites.Length == 0)
            return;

        SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite =
            groundSprites[Random.Range(0, groundSprites.Length)];
    }

    private Vector3 GetWorldPosition(int tileX, int tileZ)
    {
        return new Vector3(
            tileX * tileSize,
            0f,
            tileZ * tileSize);
    }

    private Vector2Int GetPlayerTile()
    {
        int x = Mathf.FloorToInt((player.position.x + tileSize * 0.5f) / tileSize);
        int z = Mathf.FloorToInt((player.position.z + tileSize * 0.5f) / tileSize);

        return new Vector2Int(x, z);
    }
}