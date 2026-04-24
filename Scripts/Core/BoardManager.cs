using System;
using System.Collections.Generic;
using UnityEngine;

namespace TravelChina.Core
{
    /// <summary>
   車站類型（桃太郎電鐵）
    </summary>
    public enum StationType
    {
        Normal,       // 普通車站（無效果）
        MoneyGain,    // 加錢車站（夏天多/冬天少）
        MoneyLoss,    // 扣錢車站（冬天更慘）
        CardDraw,     // 卡片車站（免費抽卡）
        CardGood,     // 好卡片車站（更容易拿好卡）
        CardMarket,   // 卡片賣場（買賣卡片）
        Object        // 物件車站（購買特產/觀光物品）
    }

    /// <summary>
    /// 單個物件（特產/觀光資源）
    /// </summary>
    [Serializable]
    public class GameObject
    {
        public string id;          // "OBJ_001"
        public string nameCN;      // "北京烤鴨"
        public string nameEN;      // "Peking Duck"
        public string description;  // 描述
        public int Price;          // 購買價格
        public float ProfitRate;    // 利潤率（如 0.15 = 15%）
        public string stationId;    // 來自哪個車站
        public bool isMonopolyBonus; // 壟斷後是否加成
    }

    /// <summary>
    /// 車站（格子）
    /// </summary>
    [Serializable]
    public class Station
    {
        public int Id;
        public string NameCN;
        public string NameEN;
        public string Province;
        public StationType Type;
        public Vector2 MapPosition;

        // 路線順序（用於計算移動距離）
        public int RouteIndex;

        // 錢的車站專用
        public int MoneyValue;     // 基礎金額
        public bool IsSeasonal;   // 是否受季節影響

        // 物件車站專用
        public List<GameObject> Objects = new();
        public int OwnerId = -1;  // 車站擁有者（主要用於壟斷計算）
        public bool IsMonopolized = false;
        public int MonopolyOwnerId = -1;

        // UI顯示
        public bool IsDestination = false;  // 是否是當前目的地
        public bool IsVisited = false;     // 是否已被訪問
    }

    /// <summary>
    /// 地圖板管理
    /// 參考桃太郎電鐵：真實鐵路順序，非閉環
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }

        [Header("地圖配置")]
        [SerializeField] private int totalStations = 46;  // MVP 精簡版

        [Header("當前狀態")]
        public List<Station> Stations { get; private set; } = new();
        public List<int> RouteOrder { get; private set; } = new(); // 路線順序

        public int RouteCount => RouteOrder.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void InitializeBoard()
        {
            GenerateMVPRoute();
            Debug.Log($"[Board] Initialized with {Stations.Count} stations");
        }

        /// <summary>
        /// 生成 MVP 路線（參考桃太郎電鐵日本47都道府縣）
        /// 中國版：我們用主要城市鐵路線
        /// </summary>
        private void GenerateMVPRoute()
        {
            // MVP 46個車站，模擬中國主要鐵路樞紐
            // 路線順序參考真實鐵路走向（非閉環）
            var routeData = new[]
            {
                // 格式：(城市名, 省份, 車站類型, 金額/物件)
                ("北京", "北京", StationType.Object, 0),
                ("天津", "天津", StationType.MoneyGain, 500_000),
                ("唐山", "河北", StationType.Normal, 0),
                ("秦皇岛", "河北", StationType.CardGood, 0),
                ("沈阳", "辽宁", StationType.Object, 0),
                ("大连", "辽宁", StationType.MoneyGain, 800_000),
                ("长春", "吉林", StationType.Normal, 0),
                ("哈尔滨", "黑龙江", StationType.Object, 0),
                ("齐齐哈尔", "黑龙江", StationType.MoneyLoss, 300_000),
                ("牡丹江", "黑龙江", StationType.CardDraw, 0),
                ("吉林", "吉林", StationType.Normal, 0),
                ("石家庄", "河北", StationType.Object, 0),
                ("太原", "山西", StationType.CardMarket, 0),
                ("呼和浩特", "内蒙古", StationType.MoneyLoss, 200_000),
                ("郑州", "河南", StationType.Object, 0),
                ("武汉", "湖北", StationType.CardGood, 0),
                ("长沙", "湖南", StationType.MoneyGain, 600_000),
                ("广州", "广东", StationType.Object, 0),
                ("深圳", "广东", StationType.MoneyGain, 1_000_000),
                ("佛山", "广东", StationType.CardDraw, 0),
                ("南宁", "广西", StationType.Normal, 0),
                ("桂林", "广西", StationType.MoneyGain, 400_000),
                ("海口", "海南", StationType.Object, 0),
                ("三亚", "海南", StationType.CardGood, 0),
                ("重庆", "重庆", StationType.Object, 0),
                ("成都", "四川", StationType.CardMarket, 0),
                ("贵阳", "贵州", StationType.Normal, 0),
                ("昆明", "云南", StationType.MoneyGain, 500_000),
                ("拉萨", "西藏", StationType.MoneyLoss, 800_000),
                ("西安", "陕西", StationType.Object, 0),
                ("兰州", "甘肃", StationType.CardDraw, 0),
                ("敦煌", "甘肃", StationType.CardGood, 0),
                ("乌鲁木齐", "新疆", StationType.Object, 0),
                ("吐鲁番", "新疆", StationType.MoneyLoss, 300_000),
                ("西宁", "青海", StationType.Normal, 0),
                ("济南", "山东", StationType.Object, 0),
                ("青岛", "山东", StationType.MoneyGain, 700_000),
                ("烟台", "山东", StationType.CardDraw, 0),
                ("南京", "江苏", StationType.Object, 0),
                ("苏州", "江苏", StationType.CardGood, 0),
                ("上海", "上海", StationType.MoneyGain, 1_200_000),
                ("杭州", "浙江", StationType.Object, 0),
                ("宁波", "浙江", StationType.Normal, 0),
                ("福州", "福建", StationType.MoneyGain, 500_000),
                ("厦门", "福建", StationType.CardMarket, 0),
            };

            Stations.Clear();
            RouteOrder.Clear();

            for (int i = 0; i < routeData.Length; i++)
            {
                var (name, province, type, money) = routeData[i];
                var station = new Station
                {
                    Id = i,
                    NameCN = name,
                    Province = province,
                    Type = type,
                    MoneyValue = money,
                    IsSeasonal = (type == StationType.MoneyGain || type == StationType.MoneyLoss),
                    RouteIndex = i,
                    MapPosition = CalculateMapPosition(i, routeData.Length)
                };

                // 為物件車站添加物件
                if (type == StationType.Object)
                {
                    station.Objects = GenerateObjectsForStation(name, province);
                }

                Stations.Add(station);
                RouteOrder.Add(i);
            }
        }

        private Vector2 CalculateMapPosition(int index, int total)
        {
            // 蛇形路線佈局
            // 每8個站為一段，改變方向
            const int ROW_LENGTH = 8;
            const float CELL_WIDTH = 120f;
            const float CELL_HEIGHT = 80f;

            int row = index / ROW_LENGTH;
            int col = index % ROW_LENGTH;
            bool reverse = (row % 2 == 1); // 奇數行反向

            float x = reverse ? (ROW_LENGTH - 1 - col) * CELL_WIDTH : col * CELL_WIDTH;
            float y = (30 - row) * CELL_HEIGHT; // 從上往下

            return new Vector2(x, y);
        }

        /// <summary>
        /// 為物件車站生成特色物件
        /// </summary>
        private List<GameObject> GenerateObjectsForStation(string cityName, string province)
        {
            var objects = new List<GameObject>();
            int basePrice = UnityEngine.Random.Range(500_000, 2_000_000);

            objects.Add(new GameObject
            {
                id = $"OBJ_{cityName}_A",
                nameCN = $"{cityName}特產",
                nameEN = $"{cityName} Specialty",
                description = $"來自{cityName}的特色產品",
                Price = basePrice,
                ProfitRate = 0.10f + UnityEngine.Random.Range(0f, 0.15f),
                stationId = cityName
            });

            objects.Add(new GameObject
            {
                id = $"OBJ_{cityName}_B",
                nameCN = $"{cityName}觀光",
                nameEN = $"{cityName} Tourism",
                description = $"{cityName}著名景點門票收入",
                Price = (int)(basePrice * 0.7f),
                ProfitRate = 0.08f + UnityEngine.Random.Range(0f, 0.10f),
                stationId = cityName
            });

            return objects;
        }

        #region 路線移動（閉環）

        /// <summary>
        /// 獲取下一個位置（閉環：繞圈）
        /// </summary>
        public int GetNextPosition(int currentRouteIndex, int diceResult)
        {
            // 閉環：永遠在繞圈
            return (currentRouteIndex + diceResult) % RouteCount;
        }

        /// <summary>
        /// 計算行進距離（繞圈）
        /// </summary>
        public int GetDistanceOnRoute(int fromRouteIndex, int toRouteIndex)
        {
            // 計算從 from 到 to 需要走多少步（不考慮繞圈方向，永遠正向）
            if (toRouteIndex >= fromRouteIndex)
                return toRouteIndex - fromRouteIndex;
            else
                return (RouteCount - fromRouteIndex) + toRouteIndex;
        }

        /// <summary>
        /// 繞一圈後回到起點（用於計算年份）
        /// </summary>
        public bool CheckLapCompleted(int oldRouteIndex, int newRouteIndex, int diceResult)
        {
            // 如果 newRouteIndex < oldRouteIndex，表示剛才繞了一圈
            return newRouteIndex < oldRouteIndex;
        }

        /// <summary>
        /// 獲取相鄰的玩家
        /// </summary>
        public List<int> GetAdjacentPlayers(int stationId)
        {
            var adjacent = new List<int>();
            var players = GameManager.Instance.Players;

            for (int i = 0; i < players.Count; i++)
            {
                if (i == GameManager.Instance.ActivePlayerIndex) continue;
                if (players.GetPlayer(i).Position == stationId)
                    adjacent.Add(i);
            }
            return adjacent;
        }

        #endregion

        public Station GetStation(int id) => Stations[id % Stations.Count];
        public int GetStationCount() => Stations.Count;
        public string GetCityName(int id) => Stations[id % Stations.Count].NameCN;

        public List<Station> GetStationsByType(StationType type)
        {
            var result = new List<Station>();
            foreach (var s in Stations)
                if (s.Type == type) result.Add(s);
            return result;
        }
    }
}
