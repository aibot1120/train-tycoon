using System;
using System.Collections.Generic;
using UnityEngine;

namespace TravelChina.Core
{
    /// <summary>
    /// 遊戲狀態
    /// </summary>
    public enum GamePhase
    {
        April, May, June, July, August, September,
        October, November, December, January, February, March
    }

    public enum Season
    {
        Spring,  // 4-5月
        Summer,  // 6-8月（加錢站獲利多）
        Autumn,  // 9-11月
        Winter   // 12-3月（扣錢站重傷）
    }

    /// <summary>
    /// 全局遊戲管理器
    /// 參考桃太郎電鐵：4月開始 → 3月結束，12個月為一年
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("遊戲配置")]
        [SerializeField] private int totalYears = 10;      // 遊戲多少年
        [SerializeField] private int startingMoney = 10_000_000; // 起始資金

        [Header("當前狀態")]
        public GamePhase CurrentPhase { get; private set; } = GamePhase.April;
        public int CurrentYear { get; private set; } = 1;
        public int TotalRoundsPlayed { get; private set; } = 0;
        public int ActivePlayerIndex { get; private set; } = 0;

        [Header("系統引用")]
        public BoardManager Board;
        public PlayerManager Players;
        public DestinationManager Destinations;
        public PovertyGodManager PovertyGod;
        public CardSystem Cards;
        public SettlementManager Settlement;
        public UIManager UI;

        // 事件
        public event Action<GamePhase> OnPhaseChanged;
        public event Action<int> OnYearChanged;
        public event Action<int> OnActivePlayerChanged;
        public event Action OnMarchSettlement;  // 年度結算

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeSystems();
        }

        private void InitializeSystems()
        {
            Board = GetComponent<BoardManager>() ?? gameObject.AddComponent<BoardManager>();
            Players = GetComponent<PlayerManager>() ?? gameObject.AddComponent<PlayerManager>();
            Destinations = GetComponent<DestinationManager>() ?? gameObject.AddComponent<DestinationManager>();
            PovertyGod = GetComponent<PovertyGodManager>() ?? gameObject.AddComponent<PovertyGodManager>();
            Cards = GetComponent<CardSystem>() ?? gameObject.AddComponent<CardSystem>();
            Settlement = GetComponent<SettlementManager>() ?? gameObject.AddComponent<SettlementManager>();
            UI = FindObjectOfType<UIManager>();
        }

        #region 遊戲流程

        public void StartNewGame(int playerCount)
        {
            CurrentYear = 1;
            CurrentPhase = GamePhase.April;
            TotalRoundsPlayed = 0;
            ActivePlayerIndex = 0;

            Players.InitializePlayers(playerCount, startingMoney);
            Board.InitializeBoard();
            Destinations.SetNewDestination();
            PovertyGod.Initialize();
            Cards.ShuffleDeck();

            UI?.ShowGameScreen();
            UI?.UpdatePhaseDisplay(CurrentPhase);
            UI?.UpdateYearDisplay(CurrentYear);

            Debug.Log($"[Game] New game started - Year {CurrentYear}, {CurrentPhase}");
        }

        /// <summary>
        /// 玩家擲骰子
        /// </summary>
        public void PlayerRollDice()
        {
            int dice = DiceSystem.Instance.Roll();
            int currentPos = Players.GetPlayer(ActivePlayerIndex).Position;
            int newPos = Board.GetNextPosition(currentPos, dice);

            Players.MovePlayer(ActivePlayerIndex, newPos);
            UI?.ShowDiceResult(dice);

            // 檢查到達目的地
            CheckDestination(newPos);

            // 執行車站效果
            ExecuteStationEffect(newPos);

            // 檢查窮神
            PovertyGod.CheckAndApplyPovertyGod(ActivePlayerIndex, newPos);

            UI?.UpdatePlayerMoney(ActivePlayerIndex, Players.GetPlayer(ActivePlayerIndex).Money);
        }

        /// <summary>
        /// 檢查是否到達目的地
        /// </summary>
        private void CheckDestination(int position)
        {
            if (Destinations.IsDestination(position))
            {
                Destinations.PlayerArrived(ActivePlayerIndex);
                PovertyGod.OnDestinationReached(ActivePlayerIndex);
            }
        }

        /// <summary>
        /// 執行車站效果
        /// </summary>
        private void ExecuteStationEffect(int position)
        {
            var station = Board.GetStation(position);
            var player = Players.GetPlayer(ActivePlayerIndex);

            switch (station.Type)
            {
                case StationType.MoneyGain:
                    // 夏天獲利多，冬天獲利少
                    int gain = CalculateSeasonalMoney(station.MoneyValue, true);
                    player.Money += gain;
                    UI?.ShowMoneyPopup(ActivePlayerIndex, gain, true);
                    break;

                case StationType.MoneyLoss:
                    int loss = CalculateSeasonalMoney(station.MoneyValue, false);
                    player.Money -= loss;
                    UI?.ShowMoneyPopup(ActivePlayerIndex, loss, false);
                    break;

                case StationType.CardDraw:
                    var card = Cards.DrawCard();
                    if (card != null) player.AddCard(card);
                    UI?.ShowCard(card);
                    break;

                case StationType.CardMarket:
                    UI?.ShowCardMarket(ActivePlayerIndex);
                    break;

                case StationType.Object:
                    UI?.ShowObjectPurchase(ActivePlayerIndex, station);
                    break;
            }
        }

        private int CalculateSeasonalMoney(int baseValue, bool isGain)
        {
            float seasonMod = CurrentPhase switch
            {
                GamePhase.June or GamePhase.July or GamePhase.August => isGain ? 1.5f : 0.5f,
                GamePhase.December or GamePhase.January or GamePhase.February => isGain ? 0.5f : 2.0f,
                _ => 1.0f
            };
            return (int)(baseValue * seasonMod);
        }

        /// <summary>
        /// 玩家結束行動，結束回合
        /// </summary>
        public void EndTurn()
        {
            ActivePlayerIndex = (ActivePlayerIndex + 1) % Players.Count;

            // 如果回到玩家1，表示所有玩家都走完 = 這個月結束
            if (ActivePlayerIndex == 0)
            {
                AdvancePhase();
            }

            OnActivePlayerChanged?.Invoke(ActivePlayerIndex);
        }

        private void AdvancePhase()
        {
            TotalRoundsPlayed++;

            // 切換到下一個月
            CurrentPhase = (GamePhase)(((int)CurrentPhase % 12) + 1);
            OnPhaseChanged?.Invoke(CurrentPhase);
            UI?.UpdatePhaseDisplay(CurrentPhase);

            // 12月結束後 → 1月（新年）→ 進入結算
            if (CurrentPhase == GamePhase.January && TotalRoundsPlayed > 0)
            {
                // 12月結束，進3月結算
            }

            // 3月結束 → 新的一年
            if (CurrentPhase == GamePhase.April)
            {
                StartNewYear();
            }
            else if (CurrentPhase == GamePhase.March)
            {
                // 3月結束觸發年度結算
                PerformMarchSettlement();
            }

            // 每個新月份設置新目的地（偶爾）
            if (TotalRoundsPlayed % 3 == 0)
            {
                Destinations.SetNewDestination();
            }
        }

        private void StartNewYear()
        {
            CurrentYear++;
            OnYearChanged?.Invoke(CurrentYear);
            UI?.UpdateYearDisplay(CurrentYear);

            if (CurrentYear > totalYears)
            {
                EndGame();
            }
        }

        private void PerformMarchSettlement()
        {
            // 年度結算：所有物件產生利潤
            Settlement.PerformSettlement(Players, Board);
            OnMarchSettlement?.Invoke();
            UI?.ShowSettlementResults(Players, Settlement);
        }

        private void EndGame()
        {
            var winner = Players.DetermineWinner();
            UI?.ShowGameOverScreen(winner);
            Debug.Log($"[Game] Game Over! Winner: Player {winner.Id}");
        }

        #endregion

        #region 玩家互動

        /// <summary>
        /// 購買物件
        /// </summary>
        public void PurchaseObject(int playerIndex, int stationId, int objectIndex)
        {
            var station = Board.GetStation(stationId);
            var obj = station.Objects[objectIndex];

            if (station.OwnerId != -1 && station.OwnerId != playerIndex)
            {
                Debug.Log($"[Game] Station {stationId} is owned by player {station.OwnerId}");
                return;
            }

            var player = Players.GetPlayer(playerIndex);
            if (player.Money < obj.Price)
            {
                UI?.ShowError("金錢不足！");
                return;
            }

            player.Money -= obj.Price;
            player.OwnedObjects.Add(new OwnedObject { StationId = stationId, ObjectIndex = objectIndex });

            // 如果是車站主人，檢查壟斷
            if (station.OwnerId == -1)
            {
                station.OwnerId = playerIndex;
            }

            CheckMonopoly(stationId, playerIndex);
            UI?.UpdatePlayerMoney(playerIndex, player.Money);
        }

        /// <summary>
        /// 檢查壟斷（車站所有物件都屬於同一玩家）
        /// </summary>
        private void CheckMonopoly(int stationId, int playerIndex)
        {
            var station = Board.GetStation(stationId);
            bool allOwned = true;

            foreach (var obj in station.Objects)
            {
                bool found = false;
                foreach (var owned in Players.GetPlayer(playerIndex).OwnedObjects)
                {
                    if (owned.StationId == stationId && owned.ObjectIndex == station.Objects.IndexOf(obj))
                        found = true;
                }
                if (!found) allOwned = false;
            }

            if (allOwned && station.Objects.Count > 0)
            {
                station.IsMonopolized = true;
                station.MonopolyOwnerId = playerIndex;
                UI?.ShowMonopolyAlert(stationId, playerIndex);
            }
        }

        #endregion

        public Player GetActivePlayer() => Players.GetPlayer(ActivePlayerIndex);
        public Season GetCurrentSeason() => CurrentPhase switch
        {
            GamePhase.April or GamePhase.May => Season.Spring,
            GamePhase.June or GamePhase.July or GamePhase.August => Season.Summer,
            GamePhase.September or GamePhase.October or GamePhase.November => Season.Autumn,
            _ => Season.Winter
        };
    }
}
