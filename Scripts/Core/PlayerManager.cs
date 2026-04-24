using System;
using System.Collections.Generic;
using UnityEngine;

namespace TravelChina.Core
{
    /// <summary>
    /// 玩家擁有的物件記錄
    /// </summary>
    [Serializable]
    public class OwnedObject
    {
        public int StationId;
        public int ObjectIndex;
    }

    /// <summary>
    /// 玩家數據
    /// </summary>
    [Serializable]
    public class Player
    {
        public int Id;
        public string Name;
        public CharacterType Character;
        public long Money;

        // 路線相關
        public int Position;          // 當前所在車站ID
        public int RouteIndex;        // 在路線上的順序

        // 持有的物件
        public List<OwnedObject> OwnedObjects = new();

        // 持有的卡片
        public List<CardData> HandCards = new();

        // 臨時狀態
        public int TempDiceBonus = 0;     // 臨時骰子加成
        public bool DestinationDouble = false; // 目的地獎金×2
        public bool IsProtected = false;   // 免疫厄運
        public bool PovertyHealed = false; // 窮神已康復

        // 統計
        public int DestinationArrivals = 0;    // 抵達目的地次數
        public int ConsecutiveArrivals = 0;     // 連續搶先次數
        public long TotalProfit = 0;            // 總利潤

        public long TotalAssets => Money + CalculatePropertyValue();

        private long CalculatePropertyValue()
        {
            long val = 0;
            var board = GameManager.Instance.Board;
            foreach (var owned in OwnedObjects)
            {
                var station = board.GetStation(owned.StationId);
                if (owned.ObjectIndex < station.Objects.Count)
                {
                    val += station.Objects[owned.ObjectIndex].Price;
                }
            }
            return val;
        }
    }

    public enum CharacterType
    {
        SunWukong,   // 筋斗雲 - 每3回合免費飛行
        ZhugeLiang,  // 智慧 - 建設成本-15%
        Panda,       // 竹子 - 每主場回合+500K
        Confucius,   // 門徒 - 每月減免1次罰款
        GuanYu,      // 赤兔 - 移動骰子+1
        JackieChan,  // 功夫 - 可抵消1次厄運
        WongFeiHung, // 佛山無影腳 - 可連續移動2格
        Robot        // 機械狗 - 自動偵查相鄰城市繁榮度
    }

    /// <summary>
    /// 玩家管理器
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }

        public List<Player> Players { get; private set; } = new();
        public int Count => Players.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void InitializePlayers(int count, long startingMoney)
        {
            Players.Clear();

            var chars = new[]
            {
                CharacterType.SunWukong, CharacterType.ZhugeLiang,
                CharacterType.Panda, CharacterType.Confucius,
                CharacterType.GuanYu, CharacterType.JackieChan,
                CharacterType.WongFeiHung, CharacterType.Robot
            };

            for (int i = 0; i < count; i++)
            {
                Players.Add(new Player
                {
                    Id = i,
                    Name = i == 0 ? "玩家" : $"AI-{i}",
                    Character = chars[i % chars.Length],
                    Money = startingMoney,
                    Position = 0,
                    RouteIndex = 0
                });
            }
            Debug.Log($"[PlayerManager] Initialized {count} players with ¥{startingMoney:N0}");
        }

        public Player GetPlayer(int index) => Players[index % Players.Count];

        public void MovePlayer(int playerIndex, int newStationId)
        {
            if (playerIndex < 0 || playerIndex >= Players.Count) return;
            Players[playerIndex].Position = newStationId;
            Players[playerIndex].RouteIndex = GameManager.Instance.Board.GetRouteIndex(newStationId);
        }

        public void AddMoney(int playerIndex, long amount)
        {
            if (playerIndex < 0 || playerIndex >= Players.Count) return;
            Players[playerIndex].Money += amount;
        }

        public bool SpendMoney(int playerIndex, long amount)
        {
            if (playerIndex < 0 || playerIndex >= Players.Count) return false;
            if (Players[playerIndex].Money < amount) return false;
            Players[playerIndex].Money -= amount;
            return true;
        }

        /// <summary>
        /// 判定勝利者（資產最高）
        /// </summary>
        public Player DetermineWinner()
        {
            Player winner = Players[0];
            foreach (var p in Players)
            {
                if (p.TotalAssets > winner.TotalAssets)
                    winner = p;
            }
            return winner;
        }

        /// <summary>
        /// 獲取玩家持有物件的總利潤率
        /// </summary>
        public float GetAverageProfitRate(int playerIndex)
        {
            var player = GetPlayer(playerIndex);
            if (player.OwnedObjects.Count == 0) return 0;

            float totalRate = 0;
            var board = GameManager.Instance.Board;

            foreach (var owned in player.OwnedObjects)
            {
                var station = board.GetStation(owned.StationId);
                if (owned.ObjectIndex < station.Objects.Count)
                {
                    totalRate += station.Objects[owned.ObjectIndex].ProfitRate;
                }
            }

            return totalRate / player.OwnedObjects.Count;
        }

        public void RestorePlayers(List<PlayerSaveData> data)
        {
            for (int i = 0; i < data.Count && i < Players.Count; i++)
            {
                Players[i].Money = data[i].money;
                Players[i].Position = data[i].position;
                Players[i].OwnedObjects = new List<OwnedObject>(data[i].ownedObjects);
            }
        }
    }

    #region 存檔

    [Serializable]
    public class PlayerSaveData
    {
        public long money;
        public int position;
        public List<OwnedObject> ownedObjects;
    }

    #endregion
}
