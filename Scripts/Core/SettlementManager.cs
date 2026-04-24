using System;
using System.Collections.Generic;
using UnityEngine;

namespace TravelChina.Core
{
    /// <summary>
    /// 年度結算系統
    /// 桃太郎電鐵：每年3月結算，所有物件產生利潤
    /// </summary>
    public class SettlementManager : MonoBehaviour
    {
        public static SettlementManager Instance { get; private set; }

        [Header("結算歷史")]
        public List<SettlementRecord> History = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 執行年度結算（3月）
        /// </summary>
        public void PerformSettlement(PlayerManager players, BoardManager board)
        {
            var record = new SettlementRecord
            {
                Year = GameManager.Instance.CurrentYear,
                Month = (int)GamePhase.March,
                PlayerSettlements = new List<PlayerSettlement>()
            };

            for (int i = 0; i < players.Count; i++)
            {
                var player = players.GetPlayer(i);
                long totalProfit = 0;

                foreach (var owned in player.OwnedObjects)
                {
                    var station = board.GetStation(owned.StationId);
                    if (owned.ObjectIndex < station.Objects.Count)
                    {
                        var obj = station.Objects[owned.ObjectIndex];
                        long profit = (long)(obj.Price * obj.ProfitRate);

                        // 壟斷加成
                        if (station.IsMonopolized && station.MonopolyOwnerId == i)
                        {
                            profit *= 2;
                        }

                        totalProfit += profit;
                    }
                }

                player.Money += totalProfit;
                player.TotalProfit += totalProfit;

                record.PlayerSettlements.Add(new PlayerSettlement
                {
                    PlayerIndex = i,
                    TotalProfit = totalProfit,
                    ObjectCount = player.OwnedObjects.Count
                });

                Debug.Log($"[Settlement] Player {i}: ¥{totalProfit:N0} profit from {player.OwnedObjects.Count} objects");
            }

            History.Add(record);
        }

        /// <summary>
        /// 計算某玩家如果現在結算能獲得多少
        /// </summary>
        public long CalculatePotentialProfit(int playerIndex)
        {
            var player = PlayerManager.Instance.GetPlayer(playerIndex);
            var board = GameManager.Instance.Board;
            long total = 0;

            foreach (var owned in player.OwnedObjects)
            {
                var station = board.GetStation(owned.StationId);
                if (owned.ObjectIndex < station.Objects.Count)
                {
                    var obj = station.Objects[owned.ObjectIndex];
                    long profit = (long)(obj.Price * obj.ProfitRate);
                    if (station.IsMonopolized && station.MonopolyOwnerId == playerIndex)
                        profit *= 2;
                    total += profit;
                }
            }

            return total;
        }

        public List<SettlementRecord> GetHistory() => History;
    }

    #region 數據結構

    [Serializable]
    public class SettlementRecord
    {
        public int Year;
        public int Month;
        public List<PlayerSettlement> PlayerSettlements;
    }

    [Serializable]
    public class PlayerSettlement
    {
        public int PlayerIndex;
        public long TotalProfit;
        public int ObjectCount;
    }

    #endregion
}
