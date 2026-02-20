using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 从图片导入地图，自动识别方格和边界线
/// Unity 2022.3.62
/// </summary>
public class MapImageLoader : MonoBehaviour
{
    [Header("地图图片设置")]
    [Tooltip("拖入您的地图图片（PNG/JPG格式）")]
    public Texture2D mapImage;
    
    [Header("网格识别设置")]
    [Tooltip("每个方格的大小（像素）。如果设置为0，将自动检测")]
    public int cellSizeInPixels = 0;
    
    [Tooltip("边界线颜色阈值（0-1），用于识别网格线")]
    [Range(0f, 1f)]
    public float borderColorThreshold = 0.3f;
    
    [Tooltip("边界线宽度（像素），通常为1-3像素")]
    [Range(1, 5)]
    public int borderLineWidth = 2;
    
    [Header("地图尺寸设置")]
    [Tooltip("地图的列数（X方向方格数）。如果设置为0，将自动检测")]
    public int mapWidth = 0;
    
    [Tooltip("地图的行数（Y方向方格数）。如果设置为0，将自动检测")]
    public int mapHeight = 0;
    
    [Header("调试选项")]
    [Tooltip("显示检测到的网格线")]
    public bool showGridLines = false;
    
    [Tooltip("在Console中输出详细信息")]
    public bool verboseLogging = true;
    
    // 检测到的网格信息
    private List<Vector2Int> detectedGridLines = new List<Vector2Int>();
    private int detectedCellSize;
    private int detectedMapWidth;
    private int detectedMapHeight;
    
    // 地图数据
    private MapManager.TerrainType[,] loadedMapData;
    
    /// <summary>
    /// 从图片加载地图
    /// </summary>
    [ContextMenu("从图片加载地图")]
    public void LoadMapFromImage()
    {
        if (mapImage == null)
        {
            Debug.LogError("[MapImageLoader] 错误：请先分配地图图片！");
            return;
        }
        
        Debug.Log($"[MapImageLoader] 开始加载地图图片：{mapImage.name}");
        Debug.Log($"[MapImageLoader] 图片尺寸：{mapImage.width}x{mapImage.height} 像素");
        
        // 步骤1：检测网格线
        DetectGridLines();
        
        // 步骤2：计算网格尺寸
        CalculateGridSize();
        
        // 步骤3：解析每个方格
        ParseMapCells();
        
        // 步骤4：应用地图数据
        ApplyMapData();
        
        Debug.Log("[MapImageLoader] 地图加载完成！");
    }
    
    /// <summary>
    /// 检测图片中的网格线（边界线）
    /// </summary>
    private void DetectGridLines()
    {
        if (verboseLogging)
            Debug.Log("[MapImageLoader] 步骤1：检测网格线...");
        
        detectedGridLines.Clear();
        
        // 读取图片像素数据
        Color[] pixels = mapImage.GetPixels();
        int width = mapImage.width;
        int height = mapImage.height;
        
        // 检测垂直网格线（从左到右）
        for (int x = 0; x < width; x++)
        {
            bool isGridLine = true;
            
            // 检查这一列是否主要是边界颜色（深色）
            int darkPixelCount = 0;
            for (int y = 0; y < height; y++)
            {
                Color pixel = pixels[y * width + x];
                float brightness = (pixel.r + pixel.g + pixel.b) / 3f;
                
                // 如果是深色（可能是边界线）
                if (brightness < borderColorThreshold)
                {
                    darkPixelCount++;
                }
            }
            
            // 如果这一列中有足够多的深色像素，认为是网格线
            if (darkPixelCount > height * 0.5f)
            {
                detectedGridLines.Add(new Vector2Int(x, 0)); // 0表示垂直线
            }
        }
        
        // 检测水平网格线（从上到下）
        for (int y = 0; y < height; y++)
        {
            bool isGridLine = true;
            
            // 检查这一行是否主要是边界颜色
            int darkPixelCount = 0;
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[y * width + x];
                float brightness = (pixel.r + pixel.g + pixel.b) / 3f;
                
                if (brightness < borderColorThreshold)
                {
                    darkPixelCount++;
                }
            }
            
            // 如果这一行中有足够多的深色像素，认为是网格线
            if (darkPixelCount > width * 0.5f)
            {
                detectedGridLines.Add(new Vector2Int(0, y)); // 0表示水平线
            }
        }
        
        if (verboseLogging)
            Debug.Log($"[MapImageLoader] 检测到 {detectedGridLines.Count} 条网格线");
    }
    
    /// <summary>
    /// 计算网格尺寸（方格大小和地图尺寸）
    /// </summary>
    private void CalculateGridSize()
    {
        if (verboseLogging)
            Debug.Log("[MapImageLoader] 步骤2：计算网格尺寸...");
        
        // 如果用户指定了尺寸，使用用户指定的
        if (cellSizeInPixels > 0)
        {
            detectedCellSize = cellSizeInPixels;
        }
        else
        {
            // 自动检测：找到最频繁出现的网格线间距
            List<int> verticalDistances = new List<int>();
            List<int> horizontalDistances = new List<int>();
            
            List<int> verticalLines = detectedGridLines.Where(g => g.x > 0 && g.y == 0).Select(g => g.x).OrderBy(x => x).ToList();
            List<int> horizontalLines = detectedGridLines.Where(g => g.x == 0 && g.y > 0).Select(g => g.y).OrderBy(y => y).ToList();
            
            // 计算垂直线的间距
            for (int i = 1; i < verticalLines.Count; i++)
            {
                int distance = verticalLines[i] - verticalLines[i - 1];
                verticalDistances.Add(distance);
            }
            
            // 计算水平线的间距
            for (int i = 1; i < horizontalLines.Count; i++)
            {
                int distance = horizontalLines[i] - horizontalLines[i - 1];
                horizontalDistances.Add(distance);
            }
            
            // 使用最常见的间距作为单元格大小
            if (verticalDistances.Count > 0)
            {
                detectedCellSize = verticalDistances.GroupBy(d => d).OrderByDescending(g => g.Count()).First().Key;
            }
            else if (horizontalDistances.Count > 0)
            {
                detectedCellSize = horizontalDistances.GroupBy(d => d).OrderByDescending(g => g.Count()).First().Key;
            }
            else
            {
                // 如果没有检测到网格线，使用默认值
                detectedCellSize = 96; // 您提到的96像素
                Debug.LogWarning("[MapImageLoader] 无法自动检测网格大小，使用默认值96像素");
            }
        }
        
        // 计算地图尺寸
        if (mapWidth > 0 && mapHeight > 0)
        {
            detectedMapWidth = mapWidth;
            detectedMapHeight = mapHeight;
        }
        else
        {
            // 自动计算：根据图片大小和单元格大小
            detectedMapWidth = mapImage.width / detectedCellSize;
            detectedMapHeight = mapImage.height / detectedCellSize;
        }
        
        if (verboseLogging)
        {
            Debug.Log($"[MapImageLoader] 检测到的单元格大小：{detectedCellSize} 像素");
            Debug.Log($"[MapImageLoader] 检测到的地图尺寸：{detectedMapWidth}x{detectedMapHeight}");
        }
    }
    
    /// <summary>
    /// 解析每个方格的内容
    /// </summary>
    private void ParseMapCells()
    {
        if (verboseLogging)
            Debug.Log("[MapImageLoader] 步骤3：解析地图方格...");
        
        loadedMapData = new MapManager.TerrainType[detectedMapWidth, detectedMapHeight];
        Color[] pixels = mapImage.GetPixels();
        int imageWidth = mapImage.width;
        
        for (int x = 0; x < detectedMapWidth; x++)
        {
            for (int y = 0; y < detectedMapHeight; y++)
            {
                // 计算方格在图片中的位置
                int pixelX = x * detectedCellSize + detectedCellSize / 2;
                int pixelY = y * detectedCellSize + detectedCellSize / 2;
                
                // 确保不越界
                pixelX = Mathf.Clamp(pixelX, 0, imageWidth - 1);
                pixelY = Mathf.Clamp(pixelY, 0, mapImage.height - 1);
                
                // 获取方格中心点的颜色
                Color cellColor = pixels[pixelY * imageWidth + pixelX];
                
                // 或者获取方格区域的平均颜色（更准确）
                Color avgColor = GetAverageColorInCell(x, y);
                
                // 根据颜色判断地形类型
                MapManager.TerrainType terrainType = IdentifyTerrainFromColor(avgColor);
                loadedMapData[x, y] = terrainType;
            }
        }
        
        if (verboseLogging)
            Debug.Log($"[MapImageLoader] 已解析 {detectedMapWidth * detectedMapHeight} 个方格");
    }
    
    /// <summary>
    /// 获取方格区域的平均颜色
    /// </summary>
    private Color GetAverageColorInCell(int cellX, int cellY)
    {
        Color[] pixels = mapImage.GetPixels();
        int imageWidth = mapImage.width;
        
        float r = 0, g = 0, b = 0;
        int sampleCount = 0;
        
        // 在方格内采样多个点（避开边界）
        int startX = cellX * detectedCellSize + borderLineWidth;
        int endX = (cellX + 1) * detectedCellSize - borderLineWidth;
        int startY = cellY * detectedCellSize + borderLineWidth;
        int endY = (cellY + 1) * detectedCellSize - borderLineWidth;
        
        // 采样间距
        int sampleStep = Mathf.Max(1, detectedCellSize / 5);
        
        for (int x = startX; x < endX; x += sampleStep)
        {
            for (int y = startY; y < endY; y += sampleStep)
            {
                if (x >= 0 && x < imageWidth && y >= 0 && y < mapImage.height)
                {
                    Color pixel = pixels[y * imageWidth + x];
                    r += pixel.r;
                    g += pixel.g;
                    b += pixel.b;
                    sampleCount++;
                }
            }
        }
        
        if (sampleCount > 0)
        {
            return new Color(r / sampleCount, g / sampleCount, b / sampleCount);
        }
        
        return Color.white;
    }
    
    /// <summary>
    /// 根据颜色识别地形类型
    /// </summary>
    private MapManager.TerrainType IdentifyTerrainFromColor(Color color)
    {
        float r = color.r;
        float g = color.g;
        float b = color.b;
        float brightness = (r + g + b) / 3f;
        
        // 简单的颜色识别逻辑
        // 您可以根据实际地图图片调整这些阈值
        
        // 深色区域（可能是障碍、边界等）
        if (brightness < 0.3f)
        {
            return MapManager.TerrainType.Obstacle; // 障碍
        }
        
        // 蓝色区域（水域）
        if (b > r + 0.2f && b > g + 0.2f)
        {
            if (brightness < 0.5f)
                return MapManager.TerrainType.DirtyWater; // 脏水
            else
                return MapManager.TerrainType.Walkable; // 清水（视为可通行）
        }
        
        // 绿色区域（草地）
        if (g > r + 0.1f && g > b + 0.1f)
        {
            return MapManager.TerrainType.Walkable; // 空地/草地
        }
        
        // 灰色/棕色区域（废墟、战壕等）
        if (Mathf.Abs(r - g) < 0.1f && Mathf.Abs(g - b) < 0.1f)
        {
            if (brightness < 0.4f)
                return MapManager.TerrainType.Trench; // 战壕
            else
                return MapManager.TerrainType.Walkable; // 废墟（视为可通行）
        }
        
        // 黄色/棕色区域（城壁）
        if (r > g + 0.1f && g > b + 0.1f && brightness > 0.5f)
        {
            return MapManager.TerrainType.Wall; // 城壁
        }
        
        // 默认：空地
        return MapManager.TerrainType.Walkable;
    }
    
    /// <summary>
    /// 将加载的地图数据应用到Map脚本
    /// </summary>
    private void ApplyMapData()
    {
        if (verboseLogging)
            Debug.Log("[MapImageLoader] 步骤4：应用地图数据...");
        
        // 地图数据已保存在loadedMapData中
        // Map脚本会在Start()方法中自动读取
        Debug.Log("[MapImageLoader] 地图数据已准备就绪，Map脚本将在运行时自动加载");
    }
    
    /// <summary>
    /// 手动设置地图数据（供外部调用）
    /// </summary>
    public MapManager.TerrainType[,] GetLoadedMapData()
    {
        return loadedMapData;
    }
    
    public int GetDetectedMapWidth() => detectedMapWidth;
    public int GetDetectedMapHeight() => detectedMapHeight;
    public int GetDetectedCellSize() => detectedCellSize;
    
    /// <summary>
    /// 可视化检测到的网格线（调试用）
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showGridLines || mapImage == null || detectedGridLines.Count == 0)
            return;
        
        Gizmos.color = Color.red;
        
        foreach (var line in detectedGridLines)
        {
            if (line.x > 0 && line.y == 0) // 垂直线
            {
                Vector3 start = new Vector3(line.x, 0, 0);
                Vector3 end = new Vector3(line.x, 0, mapImage.height);
                Gizmos.DrawLine(start, end);
            }
            else if (line.x == 0 && line.y > 0) // 水平线
            {
                Vector3 start = new Vector3(0, 0, line.y);
                Vector3 end = new Vector3(mapImage.width, 0, line.y);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}

