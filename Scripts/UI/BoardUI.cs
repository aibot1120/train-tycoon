using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TravelChina.UI
{
    /// <summary>
    /// 車站按鈕組件
    /// </summary>
    public class CityNodeUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI 引用")]
        public Image backgroundImage;
        public Image typeIcon;
        public Text cityNameText;
        public Image destinationIndicator;
        public Image povertyIndicator;
        public Image playerTokenSlot;

        [Header("狀態")]
        public int StationId { get; private set; }
        public bool IsDestination { get; private set; }
        public bool IsPoverty { get; private set; }

        private Action<int> onClickCallback;

        public void Initialize(int stationId, string cityName, string province, string typeIconSprite, Color bgColor, Action<int> callback)
        {
            StationId = stationId;
            onClickCallback = callback;

            cityNameText.text = cityName;
            cityNameText.fontSize = 10;

            // 根據省份設定顏色（視覺區分）
            backgroundImage.color = bgColor;

            // 設定車站類型圖標
            if (!string.IsNullOrEmpty(typeIconSprite))
            {
                typeIcon.gameObject.SetActive(true);
                // typeIcon.sprite = Resources.Load<Sprite>(typeIconSprite);
            }
            else
            {
                typeIcon.gameObject.SetActive(false);
            }

            // 初始化狀態
            SetDestination(false);
            SetPoverty(false);
            ClearPlayers();
        }

        public void SetDestination(bool isDest)
        {
            IsDestination = isDest;
            destinationIndicator.gameObject.SetActive(isDest);

            if (isDest)
            {
                // 目的地：金色光環
                destinationIndicator.color = new Color(1f, 0.84f, 0f, 1f);
            }
        }

        public void SetPoverty(bool isPoverty)
        {
            IsPoverty = isPoverty;
            povertyIndicator.gameObject.SetActive(isPoverty);
        }

        /// <summary>
        /// 顯示玩家 Token
        /// </summary>
        public void ShowPlayerToken(int playerIndex, Color playerColor)
        {
            playerTokenSlot.gameObject.SetActive(true);
            playerTokenSlot.color = playerColor;
        }

        public void ClearPlayers()
        {
            playerTokenSlot.gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            onClickCallback?.Invoke(StationId);
        }

        /// <summary>
        /// 高亮顯示（鼠標懸停）
        /// </summary>
        public void SetHighlight(bool highlight)
        {
            if (highlight)
            {
                transform.localScale = Vector3.one * 1.15f;
                backgroundImage.color = Color.yellow;
            }
            else
            {
                transform.localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// 路線連接線
    /// </summary>
    public class RouteLine : MonoBehaviour
    {
        public LineRenderer lineRenderer;
        public int FromStationId { get; set; }
        public int ToStationId { get; set; }
    }

    /// <summary>
    /// 玩家 Token
    /// </summary>
    public class PlayerToken : MonoBehaviour
    {
        public int PlayerIndex { get; set; }
        public Image tokenImage;
        public Text playerLabel;

        public void Setup(int index, Color color, string label)
        {
            PlayerIndex = index;
            tokenImage.color = color;
            playerLabel.text = label;
            playerLabel.color = Color.white;
        }
    }

    /// <summary>
    /// 蛇形地圖 UI 管理器
    /// 參考桃太郎電鐵：46城市蛇形路線
    /// </summary>
    public class BoardUI : MonoBehaviour
    {
        public static BoardUI Instance { get; private set; }

        [Header("UI 預制體")]
        [SerializeField] private GameObject cityNodePrefab;
        [SerializeField] private GameObject routeLinePrefab;
        [SerializeField] private GameObject playerTokenPrefab;
        [SerializeField] private GameObject pointMarkerPrefab;

        [Header("父對象")]
        [SerializeField] private Transform cityContainer;
        [SerializeField] private Transform lineContainer;
        [SerializeField] private Transform tokenContainer;
        [SerializeField] private Transform markerContainer;

        [Header("佈局配置")]
        [SerializeField] private RectTransform boardCanvas;
        [SerializeField] private Vector2 startPosition = new Vector2(80f, 500f);
        [SerializeField] private float cellWidth = 110f;
        [SerializeField] private float cellHeight = 75f;
        [SerializeField] private int citiesPerRow = 8;

        [Header("顏色配置")]
        public Color normalCityColor = new Color(0.9f, 0.9f, 0.9f);
        public Color moneyGainColor = new Color(0.3f, 0.6f, 1f);       // 藍
        public Color moneyLossColor = new Color(1f, 0.3f, 0.3f);       // 紅
        public Color cardColor = new Color(1f, 0.9f, 0.3f);             // 黃
        public Color objectCityColor = new Color(0.4f, 0.9f, 0.4f);      // 綠
        public Color destinationColor = new Color(1f, 0.84f, 0f);       // 金

        // 狀態
        private Dictionary<int, CityNodeUI> cityNodes = new();
        private List<RouteLine> routeLines = new();
        private List<PlayerToken> playerTokens = new();

        // 顏色列表（8個玩家）
        private Color[] playerColors = new[]
        {
            new Color(1f, 0.2f, 0.2f),   // 紅
            new Color(0.2f, 0.6f, 1f),   // 藍
            new Color(0.2f, 0.8f, 0.2f), // 綠
            new Color(1f, 0.8f, 0.2f),   // 黃
            new Color(0.8f, 0.2f, 1f),   // 紫
            new Color(0.2f, 1f, 0.8f),   // 青
            new Color(1f, 0.4f, 0.6f),   // 粉
            new Color(0.6f, 0.4f, 0.2f), // 棕
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 初始化地圖 UI
        /// </summary>
        public void Initialize()
        {
            ClearBoard();
            GenerateCityNodes();
            GenerateRouteLines();
            GeneratePlayerTokens();

            // 訂閱事件
            GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
            DestinationManager.Instance.OnNewDestinationSet += OnDestinationChanged;
            PovertyGodManager.Instance.OnPlayerCursed += OnPovertyCursed;
            GameManager.Instance.OnActivePlayerChanged += OnActivePlayerChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
                GameManager.Instance.OnActivePlayerChanged -= OnActivePlayerChanged;
            }
            if (DestinationManager.Instance != null)
                DestinationManager.Instance.OnNewDestinationSet -= OnDestinationChanged;
            if (PovertyGodManager.Instance != null)
                PovertyGodManager.Instance.OnPlayerCursed -= OnPovertyCursed;
        }

        /// <summary>
        /// 清空面板
        /// </summary>
        private void ClearBoard()
        {
            foreach (var node in cityNodes.Values)
                if (node != null) Destroy(node.gameObject);
            cityNodes.Clear();

            foreach (var line in routeLines)
                if (line != null) Destroy(line.gameObject);
            routeLines.Clear();

            foreach (var token in playerTokens)
                if (token != null) Destroy(token.gameObject);
            playerTokens.Clear();
        }

        /// <summary>
        /// 生成所有城市節點
        /// </summary>
        private void GenerateCityNodes()
        {
            var board = GameManager.Instance.Board;

            for (int i = 0; i < board.GetStationCount(); i++)
            {
                var station = board.GetStation(i);
                Vector2 pos = CalculateCirclePosition(i);

                var nodeObj = Instantiate(cityNodePrefab, cityContainer);
                var rect = nodeObj.GetComponent<RectTransform>();
                rect.anchoredPosition = pos;

                var cityUI = nodeObj.GetComponent<CityNodeUI>();
                Color bgColor = GetCityColor(station.Type);
                string iconName = GetTypeIconName(station.Type);

                cityUI.Initialize(
                    station.Id,
                    station.NameCN,
                    station.Province,
                    iconName,
                    bgColor,
                    OnCityClicked
                );

                cityNodes[i] = cityUI;
            }
        }

        /// <summary>
        /// 計算閉環圓形座標
        /// </summary>
        private Vector2 CalculateCirclePosition(int index)
        {
            int count = board.GetStationCount();
            float angleStep = 360f / count;
            float angle = (angleStep * index - 90f) * Mathf.Deg2Rad; // -90 让第一个城市在顶部

            float radius = 320f; // 圓環半徑
            float centerX = 450f;
            float centerY = 400f;

            float x = centerX + radius * Mathf.Cos(angle);
            float y = centerY + radius * Mathf.Sin(angle);

            return new Vector2(x, y);
        }

        /// <summary>
        /// 根據車站類型獲取顏色
        /// </summary>
        private Color GetCityColor(StationType type)
        {
            return type switch
            {
                StationType.MoneyGain => moneyGainColor,
                StationType.MoneyLoss => moneyLossColor,
                StationType.CardDraw or StationType.CardGood or StationType.CardMarket => cardColor,
                StationType.Object => objectCityColor,
                _ => normalCityColor
            };
        }

        /// <summary>
        /// 獲取類型圖標名稱
        /// </summary>
        private string GetTypeIconName(StationType type)
        {
            return type switch
            {
                StationType.MoneyGain => "icon_money_plus",
                StationType.MoneyLoss => "icon_money_minus",
                StationType.CardDraw => "icon_card",
                StationType.CardGood => "icon_card_good",
                StationType.CardMarket => "icon_card_market",
                StationType.Object => "icon_object",
                _ => ""
            };
        }

        /// <summary>
        /// 生成路線連接線（閉環）
        /// </summary>
        private void GenerateRouteLines()
        {
            var board = GameManager.Instance.Board;
            int count = board.RouteCount;

            for (int i = 0; i < count; i++)
            {
                int fromId = i;
                int toId = (i + 1) % count; // 閉環：最後一個連到第一個

                var fromPos = cityNodes[fromId].GetComponent<RectTransform>().anchoredPosition;
                var toPos = cityNodes[toId].GetComponent<RectTransform>().anchoredPosition;

                var lineObj = Instantiate(routeLinePrefab, lineContainer);
                var lr = lineObj.GetComponent<LineRenderer>();

                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(fromPos.x, fromPos.y, 0));
                lr.SetPosition(1, new Vector3(toPos.x, toPos.y, 0));

                lr.startWidth = 3f;
                lr.endWidth = 3f;
                lr.material.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

                var routeLine = lineObj.AddComponent<RouteLine>();
                routeLine.FromStationId = fromId;
                routeLine.ToStationId = toId;

                routeLines.Add(routeLine);
            }
        }

        /// <summary>
        /// 生成玩家 Token
        /// </summary>
        private void GeneratePlayerTokens()
        {
            var players = GameManager.Instance.Players;

            for (int i = 0; i < players.Count; i++)
            {
                var tokenObj = Instantiate(playerTokenPrefab, tokenContainer);
                var token = tokenObj.GetComponent<PlayerToken>();
                token.Setup(i, playerColors[i % playerColors.Length], $"P{i + 1}");
                token.gameObject.SetActive(false);
                playerTokens.Add(token);
            }
        }

        /// <summary>
        /// 更新所有玩家位置顯示
        /// </summary>
        public void UpdatePlayerPositions()
        {
            // 清除所有 Token
            foreach (var token in playerTokens)
                token.gameObject.SetActive(false);

            var players = GameManager.Instance.Players;
            var board = GameManager.Instance.Board;

            // 每個城市有多少玩家
            var cityPlayerCounts = new Dictionary<int, List<int>>();
            for (int i = 0; i < players.Count; i++)
            {
                int pos = players.GetPlayer(i).Position;
                if (!cityPlayerCounts.ContainsKey(pos))
                    cityPlayerCounts[pos] = new List<int>();
                cityPlayerCounts[pos].Add(i);
            }

            // 顯示 Token
            foreach (var kvp in cityPlayerCounts)
            {
                int cityId = kvp.Key;
                var playerList = kvp.Value;

                if (!cityNodes.ContainsKey(cityId)) continue;

                // 如果只有一個玩家，直接放中間
                if (playerList.Count == 1)
                {
                    var nodeRect = cityNodes[cityId].GetComponent<RectTransform>();
                    var token = playerTokens[playerList[0]];
                    token.GetComponent<RectTransform>().anchoredPosition = nodeRect.anchoredPosition + new Vector2(0, 20);
                    token.gameObject.SetActive(true);
                }
                else
                {
                    // 多個玩家：扇形排列
                    float angleStep = 30f;
                    float startAngle = -(playerList.Count - 1) * angleStep / 2f;

                    for (int j = 0; j < playerList.Count; j++)
                    {
                        var nodeRect = cityNodes[cityId].GetComponent<RectTransform>();
                        float angle = (startAngle + j * angleStep) * Mathf.Deg2Rad;
                        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 25f;

                        var token = playerTokens[playerList[j]];
                        token.GetComponent<RectTransform>().anchoredPosition = nodeRect.anchoredPosition + offset + new Vector2(0, 20);
                        token.gameObject.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// 播放玩家移動動畫
        /// </summary>
        public void AnimatePlayerMove(int playerIndex, int fromCityId, int toCityId, int steps, Action onComplete)
        {
            if (!cityNodes.ContainsKey(fromCityId) || !cityNodes.ContainsKey(toCityId))
            {
                onComplete?.Invoke();
                return;
            }

            var token = playerTokens[playerIndex];
            var fromRect = cityNodes[fromCityId].GetComponent<RectTransform>();
            var toRect = cityNodes[toCityId].GetComponent<RectTransform>();

            token.gameObject.SetActive(true);

            // 動畫：沿著路線一步步移動
            StartCoroutine(AnimateMoveCoroutine(token, fromRect, toRect, steps, onComplete));
        }

        private System.Collections.IEnumerator AnimateMoveCoroutine(PlayerToken token, RectTransform from, RectTransform to, int steps, Action onComplete)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector2 startPos = from.anchoredPosition + new Vector2(0, 20);
            Vector2 endPos = to.anchoredPosition + new Vector2(0, 20);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float eased = Mathf.SmoothStep(0, 1, t);

                token.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
                elapsed += Time.deltaTime;
                yield return null;
            }

            token.GetComponent<RectTransform>().anchoredPosition = endPos;
            onComplete?.Invoke();
        }

        #region 事件處理

        private void OnPhaseChanged(GamePhase phase)
        {
            // 季節變化時更新顏色
        }

        private void OnDestinationChanged(int destinationId)
        {
            // 清除舊的目的地標記
            foreach (var node in cityNodes.Values)
                node.SetDestination(false);

            // 設置新的目的地
            if (cityNodes.ContainsKey(destinationId))
                cityNodes[destinationId].SetDestination(true);
        }

        private void OnPovertyCursed(int playerId, PovertyGodManager.PovertyLevel level)
        {
            // 更新窮神標記
            foreach (var node in cityNodes.Values)
                node.SetPoverty(false);

            if (PovertyGodManager.Instance.CursedPlayerId >= 0)
            {
                int pos = GameManager.Instance.Players.GetPlayer(PovertyGodManager.Instance.CursedPlayerId).Position;
                if (cityNodes.ContainsKey(pos))
                    cityNodes[pos].SetPoverty(true);
            }
        }

        private void OnActivePlayerChanged(int playerIndex)
        {
            UpdatePlayerPositions();
        }

        private void OnCityClicked(int cityId)
        {
            Debug.Log($"[BoardUI] City clicked: {cityId}");

            var station = GameManager.Instance.Board.GetStation(cityId);
            UIManager.Instance.ShowStationInfoPanel(station);
        }

        #endregion

        /// <summary>
        /// 獲取城市 UI 節點
        /// </summary>
        public CityNodeUI GetCityNode(int cityId)
        {
            return cityNodes.ContainsKey(cityId) ? cityNodes[cityId] : null;
        }

        /// <summary>
        /// 獲取玩家 Token
        /// </summary>
        public PlayerToken GetPlayerToken(int playerIndex)
        {
            return playerIndex < playerTokens.Count ? playerTokens[playerIndex] : null;
        }

        /// <summary>
        /// 設置面板縮放（Scroll View zoom）
        /// </summary>
        public void SetZoom(float zoomFactor)
        {
            boardCanvas.localScale = Vector3.one * Mathf.Clamp(zoomFactor, 0.5f, 2f);
        }
    }
}
