using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Empty,          // 空地：无效果
    Trench,          // 战壕：在其中只能被相邻方格中的人锁定，但不影响自身攻击距离
    Obstacle,       // 障碍：位于方格边缘，无法穿过其进行攻击/移动/释放技能等操作
    PollutedWater,  // 污染水域：无法进入，但可以越过其进行攻击/释放技能（如果距离足够）
    Teleporter,      // 传送点：移动到该格后可消耗一次行动次数移动到对应的另一格
    Base            // 基地
}

public class GridTile : MonoBehaviour
{
    public int x;
    public int y;
    public bool isWalkable = true;
    public TileType tileType = TileType.Empty;
    
    // 传送点相关
    public GridTile linkedTeleporter = null; // 对应的传送点
    
    // 基地相关
    public string baseOwner = ""; // 基地所有者阵营

    public void Init(int _x, int _y, bool _walkable, TileType _type = TileType.Empty)
    {
        x = _x;
        y = _y;
        isWalkable = _walkable;
        tileType = _type;
    }

    // 检查是否可以通过此格子进行攻击/释放技能
    // 障碍：无法穿过
    // 污染水域：可以越过攻击/释放技能
    // 其他：可以通过
    public bool CanAttackThrough()
    {
        return tileType != TileType.Obstacle; // 障碍无法穿过，污染水域和其他地形可以穿过
    }
    
    // 检查是否可以通过此格子进行移动
    // 障碍和污染水域：无法进入
    // 其他：可以通过
    public bool CanMoveThrough()
    {
        return tileType != TileType.Obstacle && tileType != TileType.PollutedWater;
    }
    
    // 检查角色在此格子是否只能被相邻方格锁定（战壕效果）
    public bool CanOnlyBeTargetedByAdjacent()
    {
        return tileType == TileType.Trench;
    }
}

