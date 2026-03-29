# The Last Horizon (最后的地平线)

Unity回合制战棋游戏项目

# 项目停摆中，愿意帮助的可以联系我们 -2026.3.29

## 项目结构

```
The Last horizon/
├── Scripts/              # 游戏脚本代码
│   ├── battlemanager.cs      # 战斗管理器
│   ├── character list.cs     # 角色列表（8个角色类）
│   ├── Gamemanager.cs        # 游戏管理器
│   ├── GridManager.cs        # 网格/地图管理器
│   ├── GridTile.cs           # 地图格子类
│   ├── listscript.cs         # Character基类
│   └── UImanager.cs          # UI管理器
├── Assets/               # Unity资源文件
├── Documentation/        # 项目文档
│   └── 策划案.md            # 游戏策划案
├── animation/           # 动画资源
├── ProjectSettings/     # Unity项目设置
└── Packages/            # Unity包管理

```

## 核心系统

### 战斗系统
- **BattleManager**: 管理回合制战斗流程、行动顺序、胜负判定
- 行动顺序：先手1号 → 后手1号 → 后手2号 → 先手2号
- 胜利条件：进入对手基地 或 全灭对手

### 角色系统
- **Character**: 角色基类，定义基础属性和方法
- **8个角色类**：
  - 钢腕（地球联合国）
  - 半魔游侠（无阵营）
  - 弓箭手（避风港）
  - 暗夜猎手（避风港）
  - 工程师（寰宇联合国）
  - 投弹手（地球联合国）
  - 圣光卫士（地球联合国）
  - 至忠圣卫（寰宇联合国）

### 地图系统
- **GridManager**: 8x6网格地图，支持多种地形
- **GridTile**: 地图格子，支持空地、战壕、障碍、污染水域、传送点、基地

## 开发说明

所有游戏逻辑脚本位于 `Scripts/` 文件夹中，便于管理和维护。

## 参考文档

详细游戏规则和角色设定请查看 `Documentation/策划案.md`

