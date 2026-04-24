using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TravelChina.UI
{
    /// <summary>
    /// 蛇形棋盘路线
    /// 100% 参考桃太郎电铁：非圆环，而是往返式路线
    /// </summary>
    [Serializable]
    public class RouteSegment
    {
        public string name;         // 路段名称，如"京沪线"
        public List<int> cityIds;  // 该路段经过的城市ID列表
        public Direction direction; // 行进方向
    }

    public enum Direction
    {
        South,   // 南下
        North,   // 北上
        East,    // 东进
        West,    // 西行
    }

    /// <summary>
    /// 蛇形地图布局数据
    /// 定义所有城市在棋盘上的视觉坐标和路线连接
    /// </summary>
    [Serializable]
    public class SnakeBoardLayout
    {
        /// <summary>
        /// 城市节点在UI上的显示位置
        /// </summary>
        public List<CityNode> nodes = new();

        /// <summary>
        /// 所有铁路连接线（用于画LineRenderer）
        /// </summary>
        public List<RailwayConnection> railways = new();
    }

    [Serializable]
    public class CityNode
    {
        public int cityId;
        public Vector2 uiPosition;    // 在棋盘UI上的像素坐标
        public string segmentName;     // 属于哪个路段
    }

    [Serializable]
    public class RailwayConnection
    {
        public int fromCityId;
        public int toCityId;
        public RailwayType type;      // 普通/高铁/磁悬浮
    }

    public enum RailwayType { None, Normal, HSR, Maglev }

    /// <summary>
    /// 棋盘路线管理器
    /// </summary>
    public class BoardRouteManager : MonoBehaviour
    {
        public static BoardRouteManager Instance { get; private set; }

        [Header("路线数据")]
        [SerializeField] private SnakeBoardLayout layout;

        [Header("视觉配置")]
        [SerializeField] private RectTransform boardCanvas;
        [SerializeField] private Vector2 startPos = new Vector2(100, 500);    // 起点
        [SerializeField] private Vector2 segmentSpacing = new Vector2(0, -60);  // 城市之间间距
        [SerializeField] private int citiesPerRow = 8;                          // 每行进方向的城市数

        /// <summary>
        /// 路线顺序列表（用于计算移动）
        /// </summary>
        public List<int> RouteOrder { get; private set; } = new();

        /// <summary>
        /// 路线段列表（用于显示）
        /// </summary>
        public List<RouteSegment> Segments { get; private set; } = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (layout == null)
                GenerateMVPLayout();
        }

        /// <summary>
        /// 生成 MVP 蛇形路线（参考桃太郎电铁风格）
        /// 模拟中国主要铁路线布局
        /// </summary>
        public void GenerateMVPLayout()
        {
            layout = new SnakeBoardLayout();
            RouteOrder.Clear();
            Segments.Clear();

            /*
             * 桃太郎电铁风格蛇形路线：
             *
             * 第一段（北→南）：北京往南经过主要城市到广州
             * 第二段（南→北）：广州往北返回
             * 第三段（东→西）：横向贯穿
             * 第四段（西→东）：返回
             * ...如此往复直到回到起点
             */

            // 36个城市，按路线顺序排列
            // 路线定义参考中国主要铁路干线

            var routeDefinition = new List<RouteDefinition>
            {
                // 起点：北京
                new RouteDefinition("北京", Direction.South, 0),

                // 第一段：京广线南下
                new RouteDefinition("石家庄", Direction.South, 1),
                new RouteDefinition("郑州", Direction.South, 2),
                new RouteDefinition("武汉", Direction.South, 3),
                new RouteDefinition("长沙", Direction.South, 4),
                new RouteDefinition("广州", Direction.South, 5),

                // 第二段：广州→深圳→折返北上（沿海）
                new RouteDefinition("深圳", Direction.North, 6),
                new RouteDefinition("南昌", Direction.North, 7),
                new RouteDefinition("杭州", Direction.North, 8),
                new RouteDefinition("南京", Direction.North, 9),
                new RouteDefinition("济南", Direction.North, 10),
                new RouteDefinition("天津", Direction.North, 11),

                // 第三段：环渤海（东→西）
                new RouteDefinition("沈阳", Direction.West, 12),
                new RouteDefinition("大连", Direction.West, 13),
                new RouteDefinition("长春", Direction.West, 14),
                new RouteDefinition("哈尔滨", Direction.West, 15),

                // 第四段：京哈线返回（东→西）
                new RouteDefinition("呼和浩特", Direction.East, 16),
                new RouteDefinition("太原", Direction.East, 17),
                new RouteDefinition("西安", Direction.East, 18),

                // 第五段：西部南下
                new RouteDefinition("成都", Direction.South, 19),
                new RouteDefinition("重庆", Direction.South, 20),
                new RouteDefinition("昆明", Direction.South, 21),
                new RouteDefinition("贵阳", Direction.South, 22),

                // 第六段：西部北上
                new RouteDefinition("兰州", Direction.North, 23),
                new RouteDefinition("敦煌", Direction.North, 24),
                new RouteDefinition("乌鲁木齐", Direction.North, 25),

                // 第七段：中部横切（东→西）
                new RouteDefinition("拉萨", Direction.West, 26),
                new RouteDefinition("南宁", Direction.West, 27),
                new RouteDefinition("海口", Direction.West, 28),

                // 第八段：沿海返回（西→东）
                new RouteDefinition("三亚", Direction.East, 29),
                new RouteDefinition("厦门", Direction.East, 30),
                new RouteDefinition("福州", Direction.East, 31),
                new RouteDefinition("青岛", Direction.East, 32),
                new RouteDefinition("合肥", Direction.East, 33),

                // 第九段：收尾回到北京
                new RouteDefinition("苏州", Direction.North, 34),
                new RouteDefinition("洛阳", Direction.North, 35),
                new RouteDefinition("北京", Direction.North, 36), // 回到北京（终点）
            };

            // 计算36个城市的UI坐标（蛇形排列）
            CalculateSnakePositions(routeDefinition);

            Debug.Log($"[BoardRouteManager] Generated {RouteOrder.Count} route nodes");
        }

        private void CalculateSnakePositions(List<RouteDefinition> route)
        {
            Vector2 currentPos = startPos;
            Direction currentDir = Direction.South;
            int col = 0;
            string currentSegment = "起点线";
            int segmentIndex = 0;

            // 根据方向确定行进的向量
            Vector2 GetStep(Direction dir)
            {
                return dir switch
                {
                    Direction.South => new Vector2(0, -70),
                    Direction.North => new Vector2(0, 70),
                    Direction.East => new Vector2(80, 0),
                    Direction.West => new Vector2(-80, 0),
                    _ => new Vector2(0, -70)
                };
            }

            for (int i = 0; i < route.Count; i++)
            {
                var def = route[i];

                // 检测方向变化 → 新路段
                if (i > 0 && def.direction != currentDir)
                {
                    segmentIndex++;
                    currentSegment = $"第{segmentIndex + 1}段";
                    col = 0;

                    // 方向变化时，位移要"拐弯"
                    currentPos = def.direction switch
                    {
                        Direction.South => new Vector2(currentPos.x + 80, startPos.y),
                        Direction.North => new Vector2(currentPos.x - 80, currentPos.y),
                        Direction.East => new Vector2(startPos.x, currentPos.y + 70),
                        Direction.West => new Vector2(startPos.x, currentPos.y - 70),
                        _ => currentPos
                    };
                    currentDir = def.direction;
                }

                // 查城市ID（从BoardManager）
                int cityId = BoardManager.Instance.Cells.FindIndex(c => c.cityName == def.cityName);
                if (cityId < 0) cityId = i % BoardManager.Instance.GetCellCount();

                // 记录路线顺序
                if (!RouteOrder.Contains(cityId))
                    RouteOrder.Add(cityId);

                // 记录节点位置
                layout.nodes.Add(new CityNode
                {
                    cityId = cityId,
                    uiPosition = currentPos,
                    segmentName = currentSegment
                });

                // 记录铁路连接（除了最后一个）
                if (i < route.Count - 1)
                {
                    int nextCityId = BoardManager.Instance.Cells.FindIndex(
                        c => c.cityName == route[i + 1].cityName);
                    if (nextCityId < 0) nextCityId = (i + 1) % BoardManager.Instance.GetCellCount();

                    layout.railways.Add(new RailwayConnection
                    {
                        fromCityId = cityId,
                        toCityId = nextCityId,
                        type = RailwayType.Normal
                    });
                }

                // 移动到下一个位置
                currentPos += GetStep(currentDir);
                col++;
            }
        }

        /// <summary>
        /// 根据路线顺序，获取下一个位置
        /// </summary>
        public int GetNextPositionOnRoute(int currentRouteIndex, int diceResult)
        {
            int newIndex = currentRouteIndex + diceResult;
            if (newIndex >= RouteOrder.Count)
            {
                // 走完一圈 = 新的一年
                newIndex = newIndex % RouteOrder.Count;
            }
            return newIndex;
        }

        /// <summary>
        /// 获取某城市在路线上的索引
        /// </summary>
        public int GetRouteIndex(int cityId)
        {
            return RouteOrder.IndexOf(cityId);
        }

        /// <summary>
        /// 获取某路线索引对应的城市ID
        /// </summary>
        public int GetCityIdAtRouteIndex(int routeIndex)
        {
            if (routeIndex < 0 || routeIndex >= RouteOrder.Count) return 0;
            return RouteOrder[routeIndex];
        }

        /// <summary>
        /// 获取两点之间的路线段数量（用于移动计算）
        /// </summary>
        public int GetDistanceOnRoute(int fromRouteIndex, int toRouteIndex)
        {
            int count = RouteOrder.Count;
            if (toRouteIndex >= fromRouteIndex)
                return toRouteIndex - fromRouteIndex;
            else
                return (count - fromRouteIndex) + toRouteIndex; // 绕了一圈
        }

        public Vector2 GetUIPosition(int cityId)
        {
            var node = layout.nodes.Find(n => n.cityId == cityId);
            return node != null ? node.uiPosition : Vector2.zero;
        }

        private class RouteDefinition
        {
            public string cityName;
            public Direction direction;
            public int order;

            public RouteDefinition(string name, Direction dir, int ord)
            {
                cityName = name;
                direction = dir;
                order = ord;
            }
        }
    }
}
