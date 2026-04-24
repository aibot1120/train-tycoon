using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TravelChina.Core;

namespace TravelChina.UI
{
    /// <summary>
    /// UI 管理器
    /// 統一管理所有 UI 面板和交互
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("面板引用")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject stationInfoPanel;
        [SerializeField] private GameObject cardPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("遊戲 HUD")]
        [SerializeField] private TextMeshProUGUI monthText;
        [SerializeField] private TextMeshProUGUI yearText;
        [SerializeField] private TextMeshProUGUI seasonText;
        [SerializeField] private TextMeshProUGUI destinationText;
        [SerializeField] private TextMeshProUGUI currentPlayerText;
        [SerializeField] private TextMeshProUGUI[] playerMoneyTexts;

        [Header("按鈕")]
        [SerializeField] private Button rollDiceButton;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Button menuButton;

        [Header("骰子顯示")]
        [SerializeField] private Image diceImage;
        [SerializeField] private TextMeshProUGUI diceText;

        [Header("彈出面板")]
        [SerializeField] private GameObject popupCanvas;
        [SerializeField] private TextMeshProUGUI popupText;
        [SerializeField] private Image popupIcon;

        [Header("車站信息面板")]
        [SerializeField] private TextMeshProUGUI stationNameText;
        [SerializeField] private TextMeshProUGUI stationTypeText;
        [SerializeField] private TextMeshProUGUI stationDescText;
        [SerializeField] private Transform objectListContainer;
        [SerializeField] private GameObject objectItemPrefab;
        [SerializeField] private Button purchaseButton;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SetupButtonListeners();
            ShowMainMenu();
        }

        private void SetupButtonListeners()
        {
            rollDiceButton?.GetComponent<Button>().onClick.AddListener(OnRollDiceClicked);
            endTurnButton?.GetComponent<Button>().onClick.AddListener(OnEndTurnClicked);
            menuButton?.GetComponent<Button>().onClick.AddListener(OnMenuClicked);
        }

        #region 面板切換

        public void ShowMainMenu()
        {
            HideAllPanels();
            mainMenuPanel?.SetActive(true);
        }

        public void ShowSetupPanel()
        {
            HideAllPanels();
            setupPanel?.SetActive(true);
        }

        public void ShowGameScreen()
        {
            HideAllPanels();
            gamePanel?.SetActive(true);

            // 初始化 BoardUI
            BoardUI.Instance?.Initialize();
            BoardUI.Instance?.UpdatePlayerPositions();

            // 設置按鈕狀態
            rollDiceButton.interactable = true;
            endTurnButton.interactable = false;
        }

        public void HideAllPanels()
        {
            mainMenuPanel?.SetActive(false);
            setupPanel?.SetActive(false);
            gamePanel?.SetActive(false);
            stationInfoPanel?.SetActive(false);
            cardPanel?.SetActive(false);
            gameOverPanel?.SetActive(false);
        }

        #endregion

        #region HUD 更新

        public void UpdatePhaseDisplay(GamePhase phase)
        {
            string[] monthNames = { "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月", "1月", "2月", "3月" };
            monthText.text = monthNames[(int)phase];

            // 季節
            string season = (phase) switch
            {
                GamePhase.April or GamePhase.May => "🌸 春季",
                GamePhase.June or GamePhase.July or GamePhase.August => "☀️ 夏季",
                GamePhase.September or GamePhase.October or GamePhase.November => "🍂 秋季",
                _ => "❄️ 冬季"
            };
            seasonText.text = season;
        }

        public void UpdateYearDisplay(int year)
        {
            yearText.text = $"第 {year} 年";
        }

        public void UpdateDestinationDisplay(string destName)
        {
            destinationText.text = $"📍 目的地：{destName}";
        }

        public void UpdateActivePlayer(int playerIndex)
        {
            currentPlayerText.text = $"當前：玩家 {playerIndex + 1}";
        }

        public void UpdatePlayerMoney(int playerIndex, long money)
        {
            if (playerIndex < playerMoneyTexts.Length)
                playerMoneyTexts[playerIndex].text = $"¥{money:N0}";
        }

        public void UpdateAllPlayerMoney()
        {
            var players = GameManager.Instance.Players;
            for (int i = 0; i < players.Count && i < playerMoneyTexts.Length; i++)
            {
                playerMoneyTexts[i].text = $"¥{players.GetPlayer(i).Money:N0}";
            }
        }

        #endregion

        #region 按鈕事件

        private void OnRollDiceClicked()
        {
            rollDiceButton.interactable = false;
            endTurnButton.interactable = false;

            int dice = DiceSystem.Instance.Roll();
            ShowDiceResult(dice);

            // 延遲後處理移動
            StartCoroutine(ProcessDiceRollCoroutine(dice));
        }

        private System.Collections.IEnumerator ProcessDiceRollCoroutine(int dice)
        {
            yield return new WaitForSeconds(1f);

            var gm = GameManager.Instance;
            var player = gm.GetActivePlayer();
            int oldPos = player.Position;
            int newPos = gm.Board.GetNextPosition(player.RouteIndex, dice);

            gm.Players.MovePlayer(gm.ActivePlayerIndex, newPos);

            // 播放移動動畫
            BoardUI.Instance?.AnimatePlayerMove(gm.ActivePlayerIndex, oldPos, newPos, dice, () =>
            {
                // 動畫完成後處理車站效果
                gm.PlayerRollDice();

                // 顯示車站效果
                var station = gm.Board.GetStation(newPos);
                ShowStationEffect(station);

                endTurnButton.interactable = true;
            });
        }

        private void OnEndTurnClicked()
        {
            GameManager.Instance.EndTurn();
            rollDiceButton.interactable = true;
            endTurnButton.interactable = false;
            UpdateAllPlayerMoney();
        }

        private void OnMenuClicked()
        {
            ShowMainMenu();
        }

        #endregion

        #region 骰子顯示

        public void ShowDiceResult(int dice)
        {
            diceText.text = dice.ToString();
            // 播放骰子動畫
            StartCoroutine(DiceRollAnimation(dice));
        }

        private System.Collections.IEnumerator DiceRollAnimation(int finalValue)
        {
            float duration = 0.5f;
            float elapsed = 0;
            int displayValue = 1;

            while (elapsed < duration)
            {
                displayValue = UnityEngine.Random.Range(1, 7);
                diceText.text = displayValue.ToString();
                elapsed += 0.05f;
                yield return new WaitForSeconds(0.05f);
            }

            diceText.text = finalValue.ToString();
        }

        #endregion

        #region 車站面板

        public void ShowStationInfoPanel(Station station)
        {
            stationInfoPanel?.SetActive(true);

            stationNameText.text = station.NameCN;
            stationTypeText.text = GetStationTypeName(station.Type);
            stationDescText.text = GetStationDesc(station);

            // 清空並填充物件列表
            foreach (Transform child in objectListContainer)
                Destroy(child.gameObject);

            foreach (var obj in station.Objects)
            {
                var item = Instantiate(objectItemPrefab, objectListContainer);
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = obj.nameCN;
                    texts[1].text = $"¥{obj.Price:N0} (利潤{obj.ProfitRate:P0})";
                }

                var btn = item.GetComponent<Button>();
                int objIndex = station.Objects.IndexOf(obj);
                btn?.onClick.AddListener(() => OnPurchaseObject(station.Id, objIndex));
            }

            purchaseButton?.GetComponent<Button>().onClick.AddListener(() =>
            {
                // 購買第一個可用物件
                if (station.Objects.Count > 0)
                    OnPurchaseObject(station.Id, 0);
            });
        }

        private string GetStationTypeName(StationType type)
        {
            return type switch
            {
                StationType.MoneyGain => "💰 加錢站",
                StationType.MoneyLoss => "💸 扣錢站",
                StationType.CardDraw => "🃏 抽卡站",
                StationType.CardGood => "🃏 好卡站",
                StationType.CardMarket => "🏪 卡片賣場",
                StationType.Object => "🎁 物件站",
                _ => "🚉 普通站"
            };
        }

        private string GetStationDesc(Station station)
        {
            return station.Type switch
            {
                StationType.MoneyGain => $"每次經過獲得 ¥{station.MoneyValue:N0}（夏季更佳）",
                StationType.MoneyLoss => $"每次經過損失 ¥{station.MoneyValue:N0}（冬季更慘）",
                StationType.CardDraw => "免費抽取1張隨機卡片",
                StationType.CardGood => "較高機會抽到好卡",
                StationType.CardMarket => "可購買或出售卡片",
                StationType.Object => $"擁有 {station.Objects.Count} 種當地特產",
                _ => "普通車站，無特殊效果"
            };
        }

        private void OnPurchaseObject(int stationId, int objectIndex)
        {
            var player = GameManager.Instance.GetActivePlayer();
            var station = GameManager.Instance.Board.GetStation(stationId);
            var obj = station.Objects[objectIndex];

            if (player.Money < obj.Price)
            {
                ShowError("金錢不足！");
                return;
            }

            GameManager.Instance.PurchaseObject(GameManager.Instance.ActivePlayerIndex, stationId, objectIndex);
            stationInfoPanel?.SetActive(false);
            UpdateAllPlayerMoney();
        }

        public void ShowStationEffect(Station station)
        {
            string effectMsg = station.Type switch
            {
                StationType.MoneyGain => $"💰 獲得 ¥{station.MoneyValue:N0}",
                StationType.MoneyLoss => $"💸 損失 ¥{station.MoneyValue:N0}",
                StationType.CardDraw => "🃏 抽取卡片！",
                StationType.CardGood => "🃏 好卡站！",
                StationType.CardMarket => "🏪 卡片市場",
                StationType.Object => $"🎁 {station.NameCN}特產",
                _ => ""
            };

            if (!string.IsNullOrEmpty(effectMsg))
                ShowPopup(effectMsg, station.Type == StationType.MoneyGain ? Color.cyan : Color.red);
        }

        #endregion

        #region 彈出消息

        public void ShowPopup(string message, Color color)
        {
            popupText.text = message;
            popupText.color = color;
            popupCanvas?.SetActive(true);

            CancelInvoke(nameof(HidePopup));
            Invoke(nameof(HidePopup), 2f);
        }

        private void HidePopup()
        {
            popupCanvas?.SetActive(false);
        }

        public void ShowError(string message)
        {
            ShowPopup(message, Color.red);
        }

        #endregion

        #region 遊戲結束

        public void ShowGameOverScreen(Player winner)
        {
            gameOverPanel?.SetActive(true);

            var winnerText = gameOverPanel.transform.Find("WinnerText")?.GetComponent<TextMeshProUGUI>();
            if (winnerText != null)
            {
                winnerText.text = $"🏆 冠軍：{winner.Name}\n資產：¥{winner.TotalAssets:N0}";
            }
        }

        #endregion

        #region 特殊事件顯示

        public void ShowNewDestination(int cityId)
        {
            string cityName = GameManager.Instance.Board.GetCityName(cityId);
            UpdateDestinationDisplay(cityName);
            ShowPopup($"📍 新目的地：{cityName}", Color.yellow);
        }

        public void ShowDestinationArrival(int playerIndex, long bonus, int arrivalOrder)
        {
            string msg = arrivalOrder == 1
                ? $"🎉 玩家 {playerIndex + 1} 搶先抵達！+¥{bonus:N0}"
                : $"🏃 玩家 {playerIndex + 1} 抵達目的地 +¥{bonus:N0}";
            ShowPopup(msg, Color.green);
        }

        public void ShowMonopolyAlert(int stationId, int playerIndex)
        {
            var station = GameManager.Instance.Board.GetStation(stationId);
            ShowPopup($"🎊 玩家 {playerIndex + 1} 壟斷 {station.NameCN}！利潤×2！", Color.magenta);
        }

        public void ShowPovertyGodLevelUp(PovertyGodManager.PovertyLevel level)
        {
            string name = level switch
            {
                PovertyGodManager.PovertyLevel.Baby => "🐣 窮神寶寶",
                PovertyGodManager.PovertyLevel.Normal => "👹 窮神",
                PovertyGodManager.PovertyLevel.Great => "👹👹 大窮神",
                PovertyGodManager.PovertyLevel.Demon => "👹👹👹 魔王",
                PovertyGodManager.PovertyLevel.Destroyer => "💀 破壞號",
                _ => "窮神"
            };
            ShowPopup($"{name} 出現！", new Color(0.5f, 0f, 0.5f));
        }

        public void ShowSettlementResults(PlayerManager players, SettlementManager settlement)
        {
            var history = settlement.GetHistory();
            if (history.Count == 0) return;

            var lastSettlement = history[history.Count - 1];
            string msg = "📊 年度結算\n";

            foreach (var ps in lastSettlement.PlayerSettlements)
            {
                msg += $"玩家 {ps.PlayerIndex + 1}：+¥{ps.TotalProfit:N0}\n";
            }

            ShowPopup(msg, Color.cyan);
        }

        public void ShowCardEffect(CardData card, string effectText)
        {
            string msg = $"🃏 {card.NameCN}\n{effectText}";
            Color color = card.Type == CardType.Fortune ? Color.green : Color.red;
            ShowPopup(msg, color);
        }

        public void ShowMoneyPopup(int playerIndex, int amount, bool isGain)
        {
            string msg = isGain ? $"💰 +¥{amount:N0}" : $"💸 -¥{amount:N0}";
            Color color = isGain ? Color.green : Color.red;
            ShowPopup(msg, color);
        }

        #endregion

        #region 卡片市場

        public void ShowCardMarket(int playerIndex)
        {
            cardPanel?.SetActive(true);

            // 顯示可用卡片購買
        }

        public void OnBuyCard(string cardId, int price)
        {
            var player = GameManager.Instance.GetActivePlayer();
            if (player.Money < price)
            {
                ShowError("金錢不足！");
                return;
            }

            var card = CardSystem.Instance.DrawFortuneCard();
            if (card != null)
            {
                player.Money -= price;
                player.HandCards.Add(card);
                ShowPopup($"🃏 購買成功：{card.NameCN}", Color.white);
            }

            cardPanel?.SetActive(false);
        }

        #endregion
    }
}
