using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    [Header("地图设置")]
    public Texture2D mapTexture;  // 地图图片
    public float cellSize = 1f;   // 每个像素对应的世界单位大小

    [Header("颜色定义")]
    public Color walkableColor = Color.yellow; // 可通行区域颜色（空地）
    public Color obstacleColor = Color.red;       // 障碍物颜色
    public Color dirtyWaterColor = Color.green;   // 脏水区域颜色
    public Color wallColor = Color.gray;        // 墙壁颜色
    public Color trenchColor = Color.yellow;      // 战壕颜色

    private Dictionary<Vector2Int, TerrainType> mapData;
    private Vector2Int mapSize;

    public enum TerrainType
    {
        Walkable,    // 可通行（空地）
        Obstacle,    // 障碍物
        DirtyWater,  // 脏水
        Wall,        // 墙壁
        Trench       // 战壕
    }

    void Start()
    {
        GenerateMapFromTexture();
    }

    void GenerateMapFromTexture()
    {
        if (mapTexture == null)
        {
            Debug.LogError("未分配地图纹理！");
            return;
        }

        mapData = new Dictionary<Vector2Int, TerrainType>();
        mapSize = new Vector2Int(mapTexture.width, mapTexture.height);

        for (int x = 0; x < mapTexture.width; x++)
        {
            for (int y = 0; y < mapTexture.height; y++)
            {
                Color pixelColor = mapTexture.GetPixel(x, y);
                Vector2Int gridPos = new Vector2Int(x, y);

                // 根据颜色判断地形类型
                if (IsColorSimilar(pixelColor, walkableColor))
                {
                    mapData[gridPos] = TerrainType.Walkable;
                }
                else if (IsColorSimilar(pixelColor, obstacleColor))
                {
                    mapData[gridPos] = TerrainType.Obstacle;
                }
                else if (IsColorSimilar(pixelColor, dirtyWaterColor))
                {
                    mapData[gridPos] = TerrainType.DirtyWater;
                }
                else if (IsColorSimilar(pixelColor, wallColor))
                {
                    mapData[gridPos] = TerrainType.Wall;
                }
                else if (IsColorSimilar(pixelColor, trenchColor))
                {
                    mapData[gridPos] = TerrainType.Trench;
                }
                else
                {
                    // 默认设为可通行
                    mapData[gridPos] = TerrainType.Walkable;
                }
            }
        }

        Debug.Log($"地图生成完成！大小: {mapSize.x}x{mapSize.y}");
    }

    bool IsColorSimilar(Color a, Color b, float tolerance = 0.1f)
    {
        return Vector4.Distance(a, b) < tolerance;
    }

    // 检查位置是否可通行（包括空地和战壕）
    public bool IsPositionWalkable(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);

        // 检查是否超出地图边界
        if (!IsPositionInBounds(gridPos))
        {
            return false;
        }

        if (mapData.ContainsKey(gridPos))
        {
            TerrainType terrain = mapData[gridPos];
            // 只能在地面和战壕中通行
            return terrain == TerrainType.Walkable || terrain == TerrainType.Trench;
        }

        return false;
    }

    // 检查位置是否在地图边界内
    public bool IsPositionInBounds(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < mapSize.x &&
               gridPos.y >= 0 && gridPos.y < mapSize.y;
    }

    // 检查世界坐标是否在地图边界内
    public bool IsWorldPositionInBounds(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        return IsPositionInBounds(gridPos);
    }

    // 检查位置是否是障碍（墙壁、脏水、障碍物）
    public bool IsPositionBlocked(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);

        if (!IsPositionInBounds(gridPos))
        {
            return true; // 超出边界视为障碍
        }

        if (mapData.ContainsKey(gridPos))
        {
            TerrainType terrain = mapData[gridPos];
            return terrain == TerrainType.Wall ||
                   terrain == TerrainType.DirtyWater ||
                   terrain == TerrainType.Obstacle;
        }

        return false;
    }

    public TerrainType GetTerrainAtPosition(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);

        if (mapData.ContainsKey(gridPos))
        {
            return mapData[gridPos];
        }

        return TerrainType.Obstacle; // 默认返回障碍物
    }

    Vector2Int WorldToGridPosition(Vector2 worldPos)
    {
        // 将世界坐标转换为网格坐标
        Vector2 localPos = worldPos - (Vector2)transform.position;
        int gridX = Mathf.FloorToInt(localPos.x / cellSize);
        int gridY = Mathf.FloorToInt(localPos.y / cellSize);

        return new Vector2Int(gridX, gridY);
    }

    Vector2 GridToWorldPosition(Vector2Int gridPos)
    {
        // 将网格坐标转换为世界坐标（网格单元中心）
        Vector2 worldPos = new Vector2(
            gridPos.x * cellSize + cellSize * 0.5f + transform.position.x,
            gridPos.y * cellSize + cellSize * 0.5f + transform.position.y
        );
        return worldPos;
    }

    // 在Scene视图中显示网格调试信息
    void OnDrawGizmos()
    {
        if (mapData == null) return;

        foreach (var kvp in mapData)
        {
            Vector2 worldPos = GridToWorldPosition(kvp.Key);

            switch (kvp.Value)
            {
                case TerrainType.Walkable:
                    Gizmos.color = Color.green;
                    break;
                case TerrainType.Obstacle:
                    Gizmos.color = Color.red;
                    break;
                case TerrainType.DirtyWater:
                    Gizmos.color = Color.blue;
                    break;
                case TerrainType.Wall:
                    Gizmos.color = Color.black;
                    break;
                case TerrainType.Trench:
                    Gizmos.color = Color.yellow;
                    break;
            }

            Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.8f);
        }
    }

    // 获取地图尺寸（世界单位）
    public Vector2 GetMapWorldSize()
    {
        return new Vector2(mapSize.x * cellSize, mapSize.y * cellSize);
    }

    // 获取地图边界（世界坐标）
    public Bounds GetMapBounds()
    {
        Vector2 worldSize = GetMapWorldSize();
        Vector2 center = (Vector2)transform.position + worldSize / 2f;
        return new Bounds(center, worldSize);
    }
}

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;

    private MapManager mapManager;
    private Rigidbody2D rb;
    private Vector2 movement;
    private float baseMoveSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        mapManager = FindObjectOfType<MapManager>();

        if (mapManager == null)
        {
            Debug.LogError("未找到MapManager！");
        }

        baseMoveSpeed = moveSpeed;
    }

    void Update()
    {
        // 获取输入
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // 标准化对角线移动
        if (movement.magnitude > 1)
        {
            movement.Normalize();
        }
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        if (mapManager == null) return;

        Vector2 targetPosition = rb.position + movement * moveSpeed * Time.fixedDeltaTime;

        // 检查边界
        if (!mapManager.IsWorldPositionInBounds(targetPosition))
        {
            // 超出边界，不允许移动
            return;
        }

        // 检查是否遇到障碍物（墙壁、脏水、障碍物）
        if (mapManager.IsPositionBlocked(targetPosition))
        {
            // 如果不能通行，尝试分别检查X和Y轴移动
            Vector2 horizontalMove = new Vector2(movement.x, 0) * moveSpeed * Time.fixedDeltaTime;
            Vector2 verticalMove = new Vector2(0, movement.y) * moveSpeed * Time.fixedDeltaTime;

            Vector2 horizontalTarget = rb.position + horizontalMove;
            Vector2 verticalTarget = rb.position + verticalMove;

            // 先检查水平移动
            if (movement.x != 0 &&
                mapManager.IsWorldPositionInBounds(horizontalTarget) &&
                !mapManager.IsPositionBlocked(horizontalTarget) &&
                mapManager.IsPositionWalkable(horizontalTarget))
            {
                rb.MovePosition(horizontalTarget);
                return;
            }

            // 再检查垂直移动
            if (movement.y != 0 &&
                mapManager.IsWorldPositionInBounds(verticalTarget) &&
                !mapManager.IsPositionBlocked(verticalTarget) &&
                mapManager.IsPositionWalkable(verticalTarget))
            {
                rb.MovePosition(verticalTarget);
                return;
            }

            // 如果两个方向都不能移动，则不移动
            return;
        }

        // 检查目标位置是否可通行（必须是空地或战壕）
        if (mapManager.IsPositionWalkable(targetPosition))
        {
            rb.MovePosition(targetPosition);
        }
    }
}

public class MapVisualizer : MonoBehaviour
{
    [Header("可视化设置")]
    public MapManager mapManager;
    public GameObject walkableTile;
    public GameObject obstacleTile;
    public GameObject dirtyWaterTile;
    public GameObject wallTile;
    public GameObject trenchTile;

    [Header("可选设置")]
    public bool autoFindMapManager = true;
    public bool generateTilesAtStart = false;

    void Start()
    {
        if (autoFindMapManager && mapManager == null)
        {
            mapManager = FindObjectOfType<MapManager>();
        }

        if (generateTilesAtStart)
        {
            VisualizeMap();
        }
    }

    void VisualizeMap()
    {
        if (mapManager == null)
        {
            Debug.LogWarning("MapVisualizer: 未找到 MapManager！");
            return;
        }

        // 注意：这个方法需要在 MapManager 生成地图后调用
        // 如果需要在运行时可视化，需要在 MapManager 中添加公共访问方法
        Debug.Log("MapVisualizer: 可视化功能已准备就绪。如需生成瓦片，请确保 MapManager 已完成地图生成。");
    }

    // 根据地形类型获取对应的瓦片预制体
    public GameObject GetTilePrefab(MapManager.TerrainType terrainType)
    {
        switch (terrainType)
        {
            case MapManager.TerrainType.Walkable:
                return walkableTile;
            case MapManager.TerrainType.Obstacle:
                return obstacleTile;
            case MapManager.TerrainType.DirtyWater:
                return dirtyWaterTile;
            case MapManager.TerrainType.Wall:
                return wallTile;
            case MapManager.TerrainType.Trench:
                return trenchTile;
            default:
                return walkableTile;
        }
    }
}
