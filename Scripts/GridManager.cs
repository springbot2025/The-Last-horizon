using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance; // 添加Instance单例

    public int width = 8;  // 网格宽度（列数）
    public int height = 6; // 网格高度（行数，规则要求6*8）
    public float cellSize = 1f; // 每个格子的大小
    public GameObject tilePrefab;  // 格子预制件（用来显示）

    private GridTile[,] grid;  // 网格数组
    
    // 障碍系统：障碍位于格子之间的线上（边）
    // 使用Dictionary存储边：Key为"(x1,y1)-(x2,y2)"，Value为true表示有障碍
    private Dictionary<string, bool> obstacleEdges = new Dictionary<string, bool>();
    
    // 传送点分组系统：传送格分为若干组，每组两个，一一对应
    // Key为组ID，Value为该组的两个传送点
    private Dictionary<int, List<GridTile>> teleporterGroups = new Dictionary<int, List<GridTile>>();
    
    // 出生点和基地位置（6行8列地图，索引0-7为x，0-5为y）
    public Vector2Int player1Spawn1 = new Vector2Int(0, 0);
    public Vector2Int player1Spawn2 = new Vector2Int(0, 1);
    public Vector2Int player2Spawn1 = new Vector2Int(7, 5);
    public Vector2Int player2Spawn2 = new Vector2Int(7, 4);
    
    public Vector2Int player1Base = new Vector2Int(0, 2);
    public Vector2Int player2Base = new Vector2Int(7, 3);

    void Awake()
    {
        // 初始化单例
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // 如果已有Instance，则销毁当前对象
        }
    }

    // 🟢 初始化网格
    public void GenerateGrid()
    {
        grid = new GridTile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(x * cellSize, y * cellSize, 0), Quaternion.identity);
                tile.name = $"Tile_{x}_{y}";
                GridTile tileComp = tile.GetComponent<GridTile>();
                tileComp.Init(x, y, true); // 默认可走
                grid[x, y] = tileComp;
            }
        }
    }

    // 🔄 获取指定坐标的格子
    public GridTile GetTileAt(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            return grid[x, y];
        }
        return null;
    }

    // 🟢 判断格子是否可走
    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return false;  // 防止越界
        return grid[x, y].isWalkable;
    }

    // 🧍 角色移动（只能相邻移动，不能斜线）
    public bool MoveCharacter(Character character, int targetX, int targetY)
    {
        GridTile currentTile = GetTileAt(character.gridX, character.gridY);
        GridTile targetTile = GetTileAt(targetX, targetY);

        if (targetTile == null || !targetTile.isWalkable)
            return false;

        // 规则：每次移动只能移动1格（相邻方格），不能走斜线
        int dx = Mathf.Abs(targetX - character.gridX);
        int dy = Mathf.Abs(targetY - character.gridY);
        if (dx + dy != 1) // 只能移动1格，且不是斜线（dx+dy=1确保是上下左右）
        {
            Debug.Log("移动距离超出限制：每次只能移动1格");
            return false;
        }

        // 规则：污染水域不能进入（水域占用整个格子）
        if (targetTile.tileType == TileType.PollutedWater)
        {
            Debug.Log("污染水域无法进入");
            return false;
        }

        // 规则：障碍检查（障碍位于格子之间的线上）
        // 暗夜猎手的灵动技能：在移动时可以无视障碍，但无法无视水域
        bool canIgnoreObstacles = (character is NightHunter);
        
        // 检查两个格子之间的边是否有障碍
        if (!canIgnoreObstacles)
        {
            if (HasObstacleBetween(character.gridX, character.gridY, targetX, targetY))
            {
                Debug.Log("路径被障碍阻挡（两个格子之间的线上有障碍）");
                return false;
            }
        }
        else
        {
            // 暗夜猎手的灵动：可以无视障碍，但仍然不能进入水域
            Debug.Log($"{character.characterName} 使用灵动技能，无视障碍");
        }

        // 执行移动
        character.gridX = targetX;
        character.gridY = targetY;
        character.transform.position = GetWorldPosition(targetX, targetY);
        Debug.Log($"{character.characterName} 成功移动到 ({targetX}, {targetY})");

        return true;
    }

    /// <summary>
    /// 检查两个相邻格子之间的边是否有障碍
    /// </summary>
    private bool HasObstacleBetween(int x1, int y1, int x2, int y2)
    {
        // 确保是相邻格子
        if (Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) != 1)
            return false;

        // 生成边的键（确保顺序一致）
        string edgeKey = GetEdgeKey(x1, y1, x2, y2);
        
        // 检查是否有障碍
        return obstacleEdges.ContainsKey(edgeKey) && obstacleEdges[edgeKey];
    }

    /// <summary>
    /// 获取边的唯一键（确保较小的坐标在前）
    /// </summary>
    private string GetEdgeKey(int x1, int y1, int x2, int y2)
    {
        // 确保边的表示顺序一致（较小的坐标在前）
        if (x1 < x2 || (x1 == x2 && y1 < y2))
        {
            return $"{x1},{y1}-{x2},{y2}";
        }
        else
        {
            return $"{x2},{y2}-{x1},{y1}";
        }
    }

    // 传送功能：从当前位置传送到同一组的对应传送点
    // 规则：进入传送格后，选择移动时可以选择传送到对应传送点，也可以选择移动到相邻格子
    public bool TeleportCharacter(Character character, int targetX, int targetY)
    {
        GridTile currentTile = GetTileAt(character.gridX, character.gridY);
        
        if (currentTile == null || currentTile.tileType != TileType.Teleporter)
        {
            Debug.Log($"{character.characterName} 不在传送点上");
            return false;
        }
        
        GridTile targetTile = GetTileAt(targetX, targetY);
        if (targetTile == null || targetTile.tileType != TileType.Teleporter)
        {
            Debug.Log("目标位置不是传送点");
            return false;
        }
        
        // 检查是否属于同一组
        if (currentTile.teleporterGroupId == -1 || targetTile.teleporterGroupId == -1)
        {
            Debug.Log("传送点未分组");
            return false;
        }
        
        if (currentTile.teleporterGroupId != targetTile.teleporterGroupId)
        {
            Debug.Log("两个传送点不属于同一组，无法传送");
            return false;
        }
        
        // 检查是否是同一组的另一个传送点（不能传送到自己）
        if (currentTile == targetTile)
        {
            Debug.Log("不能传送到同一个传送点");
            return false;
        }
        
        // 检查目标位置是否有其他角色
        if (IsOccupied(targetX, targetY, character))
        {
            Debug.Log("目标传送点位置已被占用");
            return false;
        }
        
        // 执行传送
        character.gridX = targetX;
        character.gridY = targetY;
        character.transform.position = GetWorldPosition(targetX, targetY);
        Debug.Log($"{character.characterName} 从组 {currentTile.teleporterGroupId} 传送到 ({targetX}, {targetY})");
        return true;
    }
    
    /// <summary>
    /// 获取角色所在传送点的同一组对应传送点
    /// </summary>
    public GridTile GetLinkedTeleporter(Character character)
    {
        GridTile currentTile = GetTileAt(character.gridX, character.gridY);
        
        if (currentTile == null || currentTile.tileType != TileType.Teleporter)
            return null;
        
        if (currentTile.teleporterGroupId == -1)
            return null;
        
        // 在同一组中查找另一个传送点
        if (teleporterGroups.ContainsKey(currentTile.teleporterGroupId))
        {
            foreach (var tile in teleporterGroups[currentTile.teleporterGroupId])
            {
                if (tile != currentTile)
                {
                    return tile;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 创建传送点组（每组两个传送点，一一对应）
    /// </summary>
    public void CreateTeleporterGroup(int groupId, int x1, int y1, int x2, int y2)
    {
        GridTile tile1 = GetTileAt(x1, y1);
        GridTile tile2 = GetTileAt(x2, y2);
        
        if (tile1 == null || tile2 == null)
        {
            Debug.LogError($"无法创建传送点组：格子不存在 ({x1},{y1}) 或 ({x2},{y2})");
            return;
        }
        
        // 设置为传送点
        tile1.tileType = TileType.Teleporter;
        tile2.tileType = TileType.Teleporter;
        tile1.teleporterGroupId = groupId;
        tile2.teleporterGroupId = groupId;
        
        // 保持兼容性：设置linkedTeleporter
        tile1.linkedTeleporter = tile2;
        tile2.linkedTeleporter = tile1;
        
        // 添加到组
        if (!teleporterGroups.ContainsKey(groupId))
        {
            teleporterGroups[groupId] = new List<GridTile>();
        }
        
        teleporterGroups[groupId].Add(tile1);
        teleporterGroups[groupId].Add(tile2);
        
        Debug.Log($"创建传送点组 {groupId}：({x1},{y1}) <-> ({x2},{y2})");
    }

    // 检查指定位置是否被其他角色占用
    private bool IsOccupied(int x, int y, Character excludeCharacter)
    {
        if (BattleManager.Instance == null) return false;
        
        List<Character> allCharacters = new List<Character>();
        allCharacters.AddRange(BattleManager.Instance.player1Team);
        allCharacters.AddRange(BattleManager.Instance.player2Team);
        
        foreach (var character in allCharacters)
        {
            if (character != excludeCharacter && character.isAlive && 
                character.gridX == x && character.gridY == y)
            {
                return true;
            }
        }
        return false;
    }

    // 检查角色是否在传送点上（用于UI显示传送选项）
    public bool IsCharacterOnTeleporter(Character character)
    {
        GridTile tile = GetTileAt(character.gridX, character.gridY);
        if (tile == null || tile.tileType != TileType.Teleporter)
            return false;
        
        // 检查是否有对应的传送点（同一组的另一个传送点）
        return GetLinkedTeleporter(character) != null;
    }

    // 检查路径是否畅通（用于攻击和技能）
    // 参数allowPollutedWater：是否允许路径中有污染水域（攻击/技能可以，移动不行）
    // 注意：移动使用HasObstacleBetween检查障碍（线），攻击和技能使用此方法检查障碍（路径上的格子）
    public bool IsPathClear(int startX, int startY, int endX, int endY, bool allowPollutedWater = false)
    {
        // 简单的直线路径检查
        int dx = endX - startX;
        int dy = endY - startY;
        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

        if (steps == 0) return true;

        // 检查路径上的每个格子之间的边（障碍位于边上）
        int prevX = startX;
        int prevY = startY;
        
        for (int i = 1; i <= steps; i++)
        {
            int x = startX + (dx * i / steps);
            int y = startY + (dy * i / steps);
            
            // 检查前一个格子到这个格子之间的边是否有障碍
            if (i > 0)
            {
                if (HasObstacleBetween(prevX, prevY, x, y))
                {
                    return false; // 路径被障碍阻挡
                }
            }
            
            // 检查路径上的格子（用于水域检查）
            GridTile tile = GetTileAt(x, y);
            if (tile != null)
            {
                // 规则：污染水域可以越过进行攻击/释放技能，但不能进入
                if (tile.tileType == TileType.PollutedWater && !allowPollutedWater)
                {
                    return false;
                }
            }
            
            prevX = x;
            prevY = y;
        }
        return true;
    }

    // 🧱 添加障碍（在两个格子之间的边上）
    // 障碍位于格子之间的线上，不是格子本身
    public void AddObstacle(int x1, int y1, int x2, int y2)
    {
        // 确保是相邻格子
        if (Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) != 1)
        {
            Debug.Log("错误：障碍只能在相邻格子之间的边上添加");
            return;
        }
        
        string edgeKey = GetEdgeKey(x1, y1, x2, y2);
        obstacleEdges[edgeKey] = true;
        Debug.Log($"在边 ({x1},{y1})-({x2},{y2}) 上添加障碍");
    }

    // 🧱 移除障碍（投弹手爆炸用）
    public void RemoveObstacle(int x1, int y1, int x2, int y2)
    {
        string edgeKey = GetEdgeKey(x1, y1, x2, y2);
        if (obstacleEdges.ContainsKey(edgeKey))
        {
            obstacleEdges[edgeKey] = false;
            Debug.Log($"移除边 ({x1},{y1})-({x2},{y2}) 上的障碍");
        }
    }
    
    // 🧱 移除指定格子周围的所有障碍（投弹手爆炸用）
    public void RemoveObstaclesAround(int centerX, int centerY)
    {
        // 移除上下左右四个方向的障碍
        int[] directions = { 0, 1, 0, -1, 1, 0, -1, 0 }; // 上下右左
        
        for (int i = 0; i < 4; i++)
        {
            int nx = centerX + directions[i * 2];
            int ny = centerY + directions[i * 2 + 1];
            
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                RemoveObstacle(centerX, centerY, nx, ny);
            }
        }
    }

    // 🏠 检查是否在基地
    public bool IsInBase(int x, int y, string faction)
    {
        Vector2Int basePos = GetBasePosition(faction);
        return x == basePos.x && y == basePos.y;
    }

    // 🏠 获取基地位置
    public Vector2Int GetBasePosition(string faction)
    {
        // 检查该阵营属于哪个玩家
        if (BattleManager.Instance != null)
        {
            bool isPlayer1 = BattleManager.Instance.player1Team.Find(c => c.faction == faction) != null;
            if (isPlayer1)
            {
                return player1Base;
            }
            else
            {
                return player2Base;
            }
        }
        return player1Base; // 默认返回玩家1基地
    }

    // 📍 判断是否在己方半场
    public bool IsInOwnHalfField(int x, int y, string faction)
    {
        // 玩家1控制左半场，玩家2控制右半场
        bool isPlayer1 = false;
        if (BattleManager.Instance != null)
        {
            isPlayer1 = BattleManager.Instance.player1Team.Find(c => c.faction == faction) != null;
        }
        int midX = width / 2;
        
        if (isPlayer1)
        {
            return x < midX; // 左半场
        }
        else
        {
            return x >= midX; // 右半场
        }
    }

    // 📍 获取出生点
    public Vector2Int GetSpawnPoint(string faction)
    {
        // 检查该阵营属于哪个玩家，返回对应的第一个出生点
        if (BattleManager.Instance != null)
        {
            bool isPlayer1 = BattleManager.Instance.player1Team.Find(c => c.faction == faction) != null;
            if (isPlayer1)
            {
                return player1Spawn1;
            }
            else
            {
                return player2Spawn1;
            }
        }
        return player1Spawn1; // 默认返回玩家1出生点
    }

    // 🔲 获取世界坐标位置（给定 x 和 y 坐标）
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * cellSize, y * cellSize, 0);
    }

    // 📡 检查角色是否在基地（胜利条件）
    public bool IsCharacterInEnemyBase(Character character, string enemyFaction)
    {
        Vector2Int enemyBase = GetBasePosition(enemyFaction);
        return character.gridX == enemyBase.x && character.gridY == enemyBase.y;
    }
}
