using System;
using System.Collections.Generic;
using UnityEngine;

namespace TravelChina.Core
{
    /// <summary>
    /// 窮神系統
    /// 桃太郎電鐵核心：有人抵達目的地時，距離最遠的玩家被附身
    /// 窮神會不斷襲擊該玩家，傳染給別人
    /// </summary>
    public class PovertyGodManager : MonoBehaviour
    {
        public static PovertyGodManager Instance { get; private set; }

        /// <summary>
        /// 窮神等級
        /// </summary>
        public enum PovertyLevel
        {
            None,       // 無
            Baby,       // 窮神寶寶
            Normal,     // 普通窮神
            Great,      // 大窮神
            Demon,      // 魔王窮神
            Destroyer   // 破壞號（魔王最終形態）
        }

        [Header("當前狀態")]
        public int CursedPlayerId = -1;           // 被附身玩家
        public PovertyLevel CurrentLevel = PovertyLevel.None;

        [Header("窮神配置")]
        public int[] LevelUpgradeThreshold = { 3, 5, 8, 12 }; // 升級所需的抵達次數
        public long[] MoneyPenalty = { 100_000, 300_000, 500_000, 1_000_000 }; // 各級罰款

        private int curseAccumulatedDamage = 0;    // 累計傷害
        private int turnsWithoutDestination = 0;  // 沒抵達目的地的回合數

        // 事件
        public event Action<int, PovertyLevel> OnPlayerCursed;
        public event Action<int> OnPlayerCurseLifted;
        public event Action<int, long, PovertyLevel> OnCurseDamage;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize()
        {
            CursedPlayerId = -1;
            CurrentLevel = PovertyLevel.None;
            curseAccumulatedDamage = 0;
        }

        /// <summary>
        /// 有人抵達目的地時調用
        /// </summary>
        public void OnDestinationReached(int playerIndex)
        {
            // 找出距離最遠的玩家
            int farthestPlayer = FindFarthestPlayerFromDestination(playerIndex);

            // 如果最遠的玩家是自己，不會被附身
            if (farthestPlayer == playerIndex) return;

            // 被附身玩家切換
            if (CursedPlayerId != -1 && CursedPlayerId != farthestPlayer)
            {
                // 舊的被附身者有機會擺脫
                LiftCurseFromPlayer(CursedPlayerId);
            }

            CursedPlayerId = farthestPlayer;
            turnsWithoutDestination = 0;

            OnPlayerCursed?.Invoke(farthestPlayer, CurrentLevel);
            Debug.Log($"[PovertyGod] Player {farthestPlayer} is now cursed!");
        }

        /// <summary>
        /// 每回合檢查並應用窮神效果
        /// </summary>
        public void CheckAndApplyPovertyGod(int currentPlayer, int position)
        {
            if (CursedPlayerId == -1) return;
            if (CursedPlayerId != currentPlayer) return;

            // 執行窮神效果
            long damage = MoneyPenalty[(int)CurrentLevel - 1];
            var player = GameManager.Instance.Players.GetPlayer(CursedPlayerId);
            player.Money -= damage;
            curseAccumulatedDamage += (int)damage;

            OnCurseDamage?.Invoke(CursedPlayerId, damage, CurrentLevel);

            // 嘗試傳染給鄰近玩家
            TrySpreadToAdjacentPlayer(position);

            // 升級檢查
            CheckLevelUp();

            Debug.Log($"[PovertyGod] Cursed player {CursedPlayerId} took ¥{damage:N0} damage. Total: ¥{curseAccumulatedDamage:N0}");
        }

        /// <summary>
        /// 計算並找出離目的地最遠的玩家
        /// </summary>
        private int FindFarthestPlayerFromDestination(int arrivedPlayer)
        {
            var board = GameManager.Instance.Board;
            int destRouteIndex = DestinationManager.Instance.CurrentDestinationIndex;

            int farthest = -1;
            int maxDistance = -1;

            for (int i = 0; i < GameManager.Instance.Players.Count; i++)
            {
                if (i == arrivedPlayer) continue;

                var player = GameManager.Instance.Players.GetPlayer(i);
                int playerRouteIndex = board.GetRouteIndex(player.Position);
                int distance = board.GetDistanceOnRoute(playerRouteIndex, destRouteIndex);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthest = i;
                }
            }

            return farthest;
        }

        /// <summary>
        /// 嘗試傳染給相鄰玩家
        /// </summary>
        private void TrySpreadToAdjacentPlayer(int position)
        {
            if (UnityEngine.Random.value > 0.15f) return; // 15% 傳染機會

            var board = GameManager.Instance.Board;
            var adjacent = board.GetAdjacentPlayers(position);

            foreach (var adjacentPlayer in adjacent)
            {
                if (adjacentPlayer != CursedPlayerId)
                {
                    // 傳染
                    LiftCurseFromPlayer(CursedPlayerId);
                    CursedPlayerId = adjacentPlayer;
                    turnsWithoutDestination = 0;
                    OnPlayerCursed?.Invoke(adjacentPlayer, CurrentLevel);
                    Debug.Log($"[PovertyGod] Curse spread to player {adjacentPlayer}!");
                    break;
                }
            }
        }

        /// <summary>
        /// 解除玩家身上的窮神
        /// </summary>
        private void LiftCurseFromPlayer(int playerIndex)
        {
            OnPlayerCurseLifted?.Invoke(playerIndex);
            Debug.Log($"[PovertyGod] Player {playerIndex} is freed from curse!");
        }

        /// <summary>
        /// 等級升級檢查
        /// </summary>
        private void CheckLevelUp()
        {
            if (CursedPlayerId == -1) return;

            int totalDamage = curseAccumulatedDamage;
            PovertyLevel newLevel = PovertyLevel.Normal;

            if (totalDamage >= 5_000_000) newLevel = PovertyLevel.Destroyer;
            else if (totalDamage >= 2_000_000) newLevel = PovertyLevel.Demon;
            else if (totalDamage >= 1_000_000) newLevel = PovertyLevel.Great;
            else if (totalDamage >= 500_000) newLevel = PovertyLevel.Baby;

            if (newLevel > CurrentLevel)
            {
                CurrentLevel = newLevel;
                GameManager.Instance.UI?.ShowPovertyGodLevelUp(newLevel);
                Debug.Log($"[PovertyGod] Level up! Now: {CurrentLevel}");
            }
        }

        /// <summary>
        /// 被附身玩家如果連續3回合沒抵達目的地，自動傳染
        /// </summary>
        public void OnTurnEnd()
        {
            if (CursedPlayerId == -1) return;

            // 如果被附身玩家在本回合抵達目的地，解脫
            // 這個邏輯在 DestinationManager.PlayerArrived 已經處理

            // 沒抵達，累積
            turnsWithoutDestination++;

            // 3回合沒抵達，強制傳染
            if (turnsWithoutDestination >= 3)
            {
                SpreadCurseRandomly();
            }
        }

        private void SpreadCurseRandomly()
        {
            var players = GameManager.Instance.Players;
            var candidates = new List<int>();

            for (int i = 0; i < players.Count; i++)
            {
                if (i != CursedPlayerId) candidates.Add(i);
            }

            if (candidates.Count > 0)
            {
                int newCursed = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                LiftCurseFromPlayer(CursedPlayerId);
                CursedPlayerId = newCursed;
                turnsWithoutDestination = 0;
                CurrentLevel = PovertyLevel.Baby;
                OnPlayerCursed?.Invoke(newCursed, CurrentLevel);
            }
        }

        public bool IsCursed(int playerIndex) => CursedPlayerId == playerIndex;
        public PovertyLevel GetPovertyLevel() => CurrentLevel;
    }
}
