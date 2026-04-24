using System;
using System.Collections.Generic;
using UnityEngine;

namespace TravelChina.AI
{
    /// <summary>
    /// AI 決策狀態
    /// </summary>
    public enum AIState
    {
        Idle,
        Rolling,
        Buying,
        Upgrading,
        UsingCard,
        Finished
    }

    /// <summary>
    /// AI 對手控制器
    /// </summary>
    public class AIGameController : MonoBehaviour
    {
        public static AIGameController Instance { get; private set; }

        private System.Random rng = new System.Random();

        // AI 行為權重（可調整）
        private const int THRESHOLD_BUY_LAND = 3_000_000;   // 低於這個餘額不買
        private const int THRESHOLD_UPGRADE = 5_000_000;    // 升級門檻
        private const int MIN_PROFIT_TO_KEEP = 500_000;      // 最低利潤門檻

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 執行 AI 回合
        /// </summary>
        public void ExecuteAITurn(int playerIndex, Action onComplete)
        {
            StartCoroutine(AITurnCoroutine(playerIndex, onComplete));
        }

        private System.Collections.IEnumerator AITurnCoroutine(int playerIndex, Action onComplete)
        {
            var pm = PlayerManager.Instance;
            var bm = BoardManager.Instance;
            var cs = CardSystem.Instance;

            var player = pm.GetPlayer(playerIndex);
            if (!player.isAI) { onComplete?.Invoke(); yield break; }

            // 1. 擲骰子
            yield return new WaitForSeconds(0.5f);
            int dice = DiceSystem.Instance.Roll();
            GameManager.Instance.UI.ShowDiceResult(dice);

            int currentPos = player.position;
            int newPos = bm.GetNextPosition(currentPos, dice);
            pm.MovePlayer(playerIndex, newPos);

            GameManager.Instance.UI.ShowAITrainMoving(playerIndex, currentPos, newPos);
            yield return new WaitForSeconds(1.5f);

            // 2. 到達地點決策
            var cell = bm.GetCellAt(newPos);

            // 2a. 如果沒人擁有，嘗試購買
            if (cell.ownerId == -1 && player.money >= cell.basePrice)
            {
                if (ShouldBuy(player, cell))
                {
                    bm.PurchaseLand(cell.id, playerIndex, cell.basePrice);
                    pm.SpendMoney(playerIndex, cell.basePrice);
                    pm.AddCellToPlayer(playerIndex, cell.id);
                    GameManager.Instance.UI.ShowAIPurchase(cell, true);
                    yield return new WaitForSeconds(0.5f);
                }
            }
            // 2b. 如果是自己已有的，考慮升級
            else if (cell.ownerId == playerIndex && player.money >= cell.upgradeHSRCost)
            {
                if (ShouldUpgrade(player, cell))
                {
                    var nextLevel = cell.level + 1;
                    int cost = cell.level == StationLevel.Land ? cell.stationCost :
                               cell.level == StationLevel.Station ? cell.upgradeHSRCost : cell.upgradeMaglevCost;
                    if (player.money >= cost)
                    {
                        bm.UpgradeStation(cell.id, nextLevel);
                        pm.SpendMoney(playerIndex, cost);
                        GameManager.Instance.UI.ShowAIPurchase(cell, false);
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
            // 2c. 如果是別人的，支付過路費
            else if (cell.ownerId >= 0 && cell.ownerId != playerIndex && cell.level > StationLevel.None)
            {
                int toll = bm.CalculateToll(cell.id);
                if (player.money >= toll)
                {
                    pm.SpendMoney(playerIndex, toll);
                    var owner = pm.GetPlayer(cell.ownerId);
                    pm.AddMoney(cell.ownerId, toll);
                    GameManager.Instance.UI.ShowAITollPaid(cell, toll);
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // 3. 考慮是否使用卡片
            if (player.handCards.Count > 0 && rng.NextDouble() > 0.6)
            {
                // 60% 機會使用手牌（簡化邏輯）
                var cardId = player.handCards[rng.Next(player.handCards.Count)];
                var card = FindCardById(cardId);
                if (card != null)
                {
                    cs.ExecuteCardEffect(card, playerIndex);
                    player.handCards.Remove(cardId);
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // 4. 結束回合
            yield return new WaitForSeconds(0.3f);
            onComplete?.Invoke();
        }

        private bool ShouldBuy(Player player, BoardCell cell)
        {
            // 評估是否應該購買
            // 簡化策略：有足夠錢 + 不是太差的位置
            if (player.money < cell.basePrice + 2_000_000) return false;
            if (cell.grade == CityGrade.B && rng.NextDouble() > 0.3) return false; // B級30%機會不買
            return true;
        }

        private bool ShouldUpgrade(Player player, BoardCell cell)
        {
            // 評估是否應該升級
            if (cell.level >= StationLevel.Maglev) return false;
            if (cell.grade == CityGrade.B && rng.NextDouble() > 0.5) return false;
            return player.money > THRESHOLD_UPGRADE;
        }

        private CardData FindCardById(string cardId)
        {
            foreach (var card in CardSystem.Instance.AllCards)
                if (card.id == cardId) return card;
            return null;
        }

        /// <summary>
        /// 獲取 AI 建議（用於提示UI）
        /// </summary>
        public string GetAIAdvice(int playerIndex)
        {
            var pm = PlayerManager.Instance;
            var bm = BoardManager.Instance;
            var player = pm.GetPlayer(playerIndex);

            // 簡單策略建議
            var unowned = new List<BoardCell>();
            foreach (var cell in bm.Cells)
                if (cell.ownerId == -1) unowned.Add(cell);

            if (unowned.Count == 0) return "無可購置土地，建議升級現有車站。";

            // 找到最值得買的
            BoardCell best = null;
            int bestScore = -1;
            foreach (var cell in unowned)
            {
                int score = cell.grade switch
                {
                    CityGrade.S => 100,
                    CityGrade.A => 60,
                    CityGrade.B => 20,
                    _ => 0
                };
                if (cell.isHeritage) score += 20;
                if (score > bestScore) { bestScore = score; best = cell; }
            }

            if (best != null && player.money >= best.basePrice)
                return $"建議購買【{best.cityName}】，¥{best.basePrice:N0}";
            else if (best != null)
                return $"【{best.cityName}】需要¥{best.basePrice:N0}，當前餘額不足";
            return "暫無建議";
        }
    }
}
