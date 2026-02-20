using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
    Empty,          // 空地
    Trench,          // 战壕
    Obstacle,       // 障碍
    PollutedWater,  // 污染水域
    Teleporter,      // 传送点
    Base            // 基地
}

public class GridTile : MonoBehaviour
{
    public int x;
    public int y;
    public bool isWalkable = true;
    public TileType tileType = TileType.Empty;
        
    // 传送点相关
    public GridTile linkedTeleporter = null; // 对应的传送点（保持兼容性）
    public int teleporterGroupId = -1; // 传送点组ID（-1表示不是传送点）
        
    // 基地相关
    public string baseOwner = ""; // 基地所有者阵营

    public void Init(int _x, int _y, bool _walkable, TileType _type = TileType.Empty)
    {
        x = _x;
        y = _y;
        isWalkable = _walkable;
        tileType = _type;
    }

    // 检查是否可以通过此格子进行攻击（污染水域可以越过）
    public bool CanAttackThrough()
    {
        return tileType != TileType.Obstacle; // 只有障碍不能越过攻击
    }
}