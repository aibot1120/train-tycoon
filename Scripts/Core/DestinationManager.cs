using System;
using System.Collections.Generic;
using UnityEngine;

namespace TravelChina.Core
{
    /// <summary>
    /// 目的地系統
    /// 桃太郎電鐵核心：誰先抵達目的地 → 拿獎金 + 連續獎勵
    /// </summary>
    public class DestinationManager : MonoBehaviour
    {
        public static DestinationManager Instance { get; private set; }

        [Header("目的地")]
        public int CurrentDestinationId { get; private set; } = -1;  // 當前目的地城市ID
        public int CurrentDestinationIndex { get; private set; } = -1; // 在路線上的順序

        [Header("獎金池")]
        public long BaseReward = 5_000_000;     // 基本獎金
        public long ConsecutiveBonus = 2_000_000; // 連續獎勵遞增

        [Header("狀態")]
        public Dictionary<int, int> PlayerArrivalCount { get; private set; } = new(); // 玩家搶先次數
        public int ArrivalOrder { get; private set; } = 0;  // 抵達順序

        // 事件
        public event Action<int, int, int> OnDestinationReached; // playerIndex, destinationId, bonus
        public event Action<int> OnNewDestinationSet;            // newDestinationId

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 設置新的目的地
        /// </summary>
        public void SetNewDestination()
        {
            var board = GameManager.Instance.Board;
            int count = board.RouteCount;

            // 隨機選擇一個新的目的地（避開當前位置附近的）
            int newDest;
            do
            {
                newDest = UnityEngine.Random.Range(0, count);
            }
            while (newDest == CurrentDestinationId && count > 1);

            CurrentDestinationId = board.GetCityIdAtRouteIndex(newDest);
            CurrentDestinationIndex = newDest;
            ArrivalOrder = 0;

            // 重置抵達記錄
            PlayerArrivalCount.Clear();

            OnNewDestinationSet?.Invoke(CurrentDestinationId);
            Debug.Log($"[Destination] New destination: {board.GetCityName(CurrentDestinationId)} (route #{newDest})");

            GameManager.Instance.UI?.ShowNewDestination(CurrentDestinationId);
        }

        /// <summary>
        /// 檢查是否為目的地
        /// </summary>
        public bool IsDestination(int position)
        {
            var board = GameManager.Instance.Board;
            int routeIndex = board.GetRouteIndex(position);
            return routeIndex == CurrentDestinationIndex;
        }

        /// <summary>
        /// 玩家抵達目的地
        /// </summary>
        public void PlayerArrived(int playerIndex)
        {
            ArrivalOrder++;

            if (!PlayerArrivalCount.ContainsKey(playerIndex))
                PlayerArrivalCount[playerIndex] = 0;

            // 計算獎金
            long bonus = CalculateBonus(playerIndex);

            // 發放獎金
            var player = GameManager.Instance.Players.GetPlayer(playerIndex);
            player.Money += bonus;
            PlayerArrivalCount[playerIndex]++;

            // 破產還債
            if (player.Money < 0)
            {
                player.Money = 0;
            }

            // 發送事件
            OnDestinationReached?.Invoke(playerIndex, CurrentDestinationId, (int)bonus);
            GameManager.Instance.UI?.ShowDestinationArrival(playerIndex, bonus, ArrivalOrder);

            Debug.Log($"[Destination] Player {playerIndex} arrived! Bonus: ¥{bonus:N0} (arrival #{ArrivalOrder})");
        }

        /// <summary>
        /// 計算獎金（基本 + 連續搶先加成）
        /// </summary>
        private long CalculateBonus(int playerIndex)
        {
            int consecutive = PlayerArrivalCount[playerIndex];
            return BaseReward + (consecutive * ConsecutiveBonus);
        }

        /// <summary>
        /// 獲取某玩家剩餘連續獎勵次數
        /// </summary>
        public int GetConsecutiveCount(int playerIndex)
        {
            return PlayerArrivalCount.ContainsKey(playerIndex) ? PlayerArrivalCount[playerIndex] : 0;
        }

        /// <summary>
        /// 獲取抵達目的地描述
        /// </summary>
        public string GetDestinationInfo()
        {
            var board = GameManager.Instance.Board;
            return $"目的地：{board.GetCityName(CurrentDestinationId)}";
        }
    }
}
