using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public int width = 8;  // 网格宽度
    public int height = 6; // 网格高度
    public float cellSize = 1f; // 每个格子的大小
    public GameObject tilePrefab;  // 格子预制件（用来显示）

    private GridTile[,] grid;  // 网格数组
    public List<GridTile> obstacles = new List<GridTile>(); // 障碍物列表
    public List<GridTile> teleporters = new List<GridTile>(); // 传送点列表
    
    // 出生点和基地位置
    // 玩家1在地图最上方（y=5），玩家2在地图最下方（y=0）
    public Vector2Int player1Spawn1 = new Vector2Int(3, 5); // 地图最上方左侧
    public Vector2Int player1Spawn2 = new Vector2Int(4, 5); // 地图最上方右侧
    public Vector2Int player2Spawn1 = new Vector2Int(3, 0); // 地图最下方左侧
    public Vector2Int player2Spawn2 = new Vector2Int(4, 0); // 地图最下方右侧
    
    public Vector2Int player1Base = new Vector2Int(3, 5); // 玩家1基地在最上方
    public Vector2Int player2Base = new Vector2Int(3, 0); // 玩家2基地在最下方

    void Awake()
    {
        // 初始化单例
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 初始化网格
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

    // 获取指定坐标的格子
    public GridTile GetTileAt(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            return grid[x, y];
        }
        return null;
    }

    // 判断格子是否可走
    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return false;
        return grid[x, y].isWalkable;
    }

    // 角色移动（只能相邻移动，不能斜线）
    public bool MoveCharacter(Character character, int targetX, int targetY)
    {
        GridTile currentTile = GetTileAt(character.gridX, character.gridY);
        GridTile targetTile = GetTileAt(targetX, targetY);

        if (targetTile == null || !targetTile.isWalkable)
            return false;

        // 检查是否是相邻移动（不能斜线）
        int dx = Mathf.Abs(targetX - character.gridX);
        int dy = Mathf.Abs(targetY - character.gridY);
        if (dx + dy != 1) // 只能移动1格，且不是斜线
        {
            // 暗夜猎手可以穿墙，但需要检查是否被障碍包围
            if (character is NightHunter && ((NightHunter)character).CanIgnoreObstacles())
            {
                // 允许穿墙移动，但距离要合理
                int distance = dx + dy;
                if (distance > character.GetEffectiveMoveDistance())
                    return false;
            }
            else
            {
                return false; // 不是相邻移动
            }
        }

        // 检查移动距离
        int moveDistance = Mathf.Abs(targetX - character.gridX) + Mathf.Abs(targetY - character.gridY);
        if (moveDistance > character.GetEffectiveMoveDistance())
        {
            Debug.Log($"Move distance exceeds limit (max: {character.GetEffectiveMoveDistance()})");
            return false;
        }

        // 检查路径是否被障碍阻挡（暗夜猎手的灵动可以忽略）
        if (!(character is NightHunter))
        {
            if (!IsPathClear(character.gridX, character.gridY, targetX, targetY))
            {
                Debug.Log("Path blocked by obstacle");
                return false;
            }
        }

        // 污染水域不能进入（策划案：无法进入）
        if (targetTile.tileType == TileType.PollutedWater)
        {
            Debug.Log("Cannot enter polluted water");
            return false;
        }

        // 执行移动（播放移动动画）
        Vector3 targetPosition = GetWorldPosition(targetX, targetY);
        
        Debug.Log($"[GridManager] Moving {character.characterName} from ({character.gridX}, {character.gridY}) to ({targetX}, {targetY})");
        
        // 面向移动方向
        if (character.visualManager != null)
        {
            character.visualManager.FaceDirection(targetPosition);
            Debug.Log($"[GridManager] Playing move animation for {character.characterName}");
            character.visualManager.PlayMove();
        }
        else
        {
            Debug.LogWarning($"[GridManager] {character.characterName} has no visualManager!");
        }
        
        character.gridX = targetX;
        character.gridY = targetY;
        
        // 平滑移动到目标位置（可选：如果需要平滑移动）
        character.transform.position = targetPosition;
        Debug.Log($"[GridManager] {character.characterName} position set to {targetPosition}");
        
        // 移动完成后播放待机动画
        if (character.visualManager != null)
        {
            // 延迟播放待机动画
            StartCoroutine(WaitAndPlayIdle(character, 0.5f));
        }
        
        Debug.Log($"[GridManager] ✅ {character.characterName} successfully moved to ({targetX}, {targetY})");
        
        // 检查是否是传送点（策划案：移动到该格后可消耗一次行动次数移动到对应的另一格）
        if (targetTile.tileType == TileType.Teleporter && targetTile.linkedTeleporter != null)
        {
            Debug.Log($"Reached teleporter, can teleport to ({targetTile.linkedTeleporter.x}, {targetTile.linkedTeleporter.y})");
        }

        return true;
    }
    
    /// <summary>
    /// 等待后播放待机动画（用于移动完成后）
    /// </summary>
    System.Collections.IEnumerator WaitAndPlayIdle(Character character, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (character != null && character.visualManager != null)
        {
            character.visualManager.PlayIdle();
        }
    }
    
    // 使用传送点（消耗一次行动次数）
    public bool UseTeleporter(Character character)
    {
        GridTile currentTile = GetTileAt(character.gridX, character.gridY);
        if (currentTile == null || currentTile.tileType != TileType.Teleporter)
        {
            Debug.Log("Not on a teleporter");
            return false;
        }
        
        if (currentTile.linkedTeleporter == null)
        {
            Debug.Log("Teleporter not paired");
            return false;
        }
        
        GridTile targetTile = currentTile.linkedTeleporter;
        
        // 检查目标传送点是否可进入
        if (!targetTile.isWalkable)
        {
            Debug.Log("Target teleporter not accessible");
            return false;
        }
        
        // 执行传送（消耗行动次数由调用者处理）
        character.gridX = targetTile.x;
        character.gridY = targetTile.y;
        character.transform.position = GetWorldPosition(targetTile.x, targetTile.y);
        Debug.Log($"{character.characterName} teleported to ({targetTile.x}, {targetTile.y})");
        return true;
    }
    
    // 检查攻击路径是否畅通（用于攻击和技能释放）
    // 障碍：无法穿过
    // 污染水域：可以穿过
    public bool IsAttackPathClear(int startX, int startY, int endX, int endY)
    {
        int dx = endX - startX;
        int dy = endY - startY;
        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

        if (steps == 0) return true;

        for (int i = 1; i < steps; i++)
        {
            int x = startX + (dx * i / steps);
            int y = startY + (dy * i / steps);
            GridTile tile = GetTileAt(x, y);
            // 障碍无法穿过，污染水域可以穿过
            if (tile != null && tile.tileType == TileType.Obstacle)
            {
                return false;
            }
        }
        return true;
    }
    
    // 检查技能释放路径是否畅通
    // 障碍：无法穿过
    // 污染水域：可以穿过
    public bool IsSkillPathClear(int startX, int startY, int endX, int endY)
    {
        return IsAttackPathClear(startX, startY, endX, endY);
    }
    
    // 检查目标是否可以被锁定（战壕效果：只能被相邻方格锁定）
    public bool CanBeTargeted(Character attacker, int targetX, int targetY)
    {
        GridTile targetTile = GetTileAt(targetX, targetY);
        if (targetTile == null) return false;
        
        // 如果目标在战壕中，只能被相邻方格锁定
        if (targetTile.CanOnlyBeTargetedByAdjacent())
        {
            int dx = Mathf.Abs(attacker.gridX - targetX);
            int dy = Mathf.Abs(attacker.gridY - targetY);
            // 必须相邻（距离为1，且不是斜线）
            if (dx + dy != 1)
            {
                Debug.Log("Targets in trench can only be locked from adjacent tiles");
                return false;
            }
        }
        
        return true;
    }

    // 检查移动路径是否畅通（用于移动）
    // 障碍：无法穿过
    public bool IsPathClear(int startX, int startY, int endX, int endY)
    {
        // 简单的直线路径检查
        int dx = endX - startX;
        int dy = endY - startY;
        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

        if (steps == 0) return true;

        for (int i = 1; i < steps; i++)
        {
            int x = startX + (dx * i / steps);
            int y = startY + (dy * i / steps);
            GridTile tile = GetTileAt(x, y);
            // 障碍无法穿过（策划案：无法穿过其进行移动）
            if (tile != null && tile.tileType == TileType.Obstacle)
            {
                return false;
            }
        }
        return true;
    }

    // 添加障碍物（策划案：位于方格边缘，无法穿过其进行攻击/移动/释放技能等操作）
    public void AddObstacle(int x, int y)
    {
        GridTile obstacleTile = GetTileAt(x, y);
        if (obstacleTile != null)
        {
            obstacleTile.isWalkable = false; // 障碍物不可进入
            obstacleTile.tileType = TileType.Obstacle;
            if (!obstacles.Contains(obstacleTile))
            {
                obstacles.Add(obstacleTile);
            }
        }
    }

    // 移除障碍物（投弹手爆炸用）
    public void RemoveObstacle(int x, int y)
    {
        GridTile tile = GetTileAt(x, y);
        if (tile != null && tile.tileType == TileType.Obstacle)
        {
            tile.isWalkable = true;
            tile.tileType = TileType.Empty;
            obstacles.Remove(tile);
        }
    }

    // 检查是否在基地
    public bool IsInBase(int x, int y, string faction)
    {
        Vector2Int basePos = GetBasePosition(faction);
        return x == basePos.x && y == basePos.y;
    }

    // 获取基地位置
    public Vector2Int GetBasePosition(string faction)
    {
        // 简化：玩家1在左侧，玩家2在右侧
        if (faction == "地球联合国" || BattleManager.Instance.playerTeam.Find(c => c.faction == faction) != null)
        {
            return player1Base;
        }
        else
        {
            return player2Base;
        }
    }

    // 判断是否在己方半场
    public bool IsInOwnHalfField(int x, int y, string faction)
    {
        // 简化：玩家1控制左半场，玩家2控制右半场
        bool isPlayer1 = faction == "地球联合国" || BattleManager.Instance.playerTeam.Find(c => c.faction == faction) != null;
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

    // 获取出生点
    public Vector2Int GetSpawnPoint(string faction)
    {
        // 简化：返回第一个出生点
        if (faction == "地球联合国" || BattleManager.Instance.playerTeam.Find(c => c.faction == faction) != null)
        {
            return player1Spawn1;
        }
        else
        {
            return player2Spawn1;
        }
    }

    // 获取世界坐标位置（给定 x 和 y 坐标）
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * cellSize, y * cellSize, 0);
    }

    // 检查角色是否在基地（胜利条件）
    public bool IsCharacterInEnemyBase(Character character, string enemyFaction)
    {
        Vector2Int enemyBase = GetBasePosition(enemyFaction);
        return character.gridX == enemyBase.x && character.gridY == enemyBase.y;
    }
}

