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
        UsingCard,
        Finished
    }

    /// <summary>
    /// AI 對手控制器
    /// MVP 版本：適配當前 BoardManager/Station/Player 結構
    /// </summary>
    public class AIGameController : MonoBehaviour
    {
        public static AIGameController Instance { get; private set; }

        private System.Random rng = new System.Random();

        // AI 行為權重（可調整）
        private const long THRESHOLD_BUY = 3_000_000;   // 低於這個餘額不買物件
        private const long THRESHOLD_USE_CARD = 5_000_000; // 有多少錢時使用卡片

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

            // 1. 擲骰子
            yield return new WaitForSeconds(0.5f);
            int dice = DiceSystem.Instance.Roll();
            GameManager.Instance.UI.ShowDiceResult(dice);

            int currentRouteIndex = player.RouteIndex;
            int newRouteIndex = bm.GetNextPosition(currentRouteIndex, dice);
            pm.MovePlayer(playerIndex, newRouteIndex);

            GameManager.Instance.UI.ShowAITrainMoving(playerIndex, currentRouteIndex, newRouteIndex);
            yield return new WaitForSeconds(1.5f);

            // 2. 到達車站處理
            var station = bm.GetStation(newRouteIndex);

            // 根據車站類型處理
            switch (station.Type)
            {
                case StationType.Object:
                    // 嘗試購買物件
                    if (station.Objects.Count > 0 && player.Money >= THRESHOLD_BUY)
                    {
                        var obj = station.Objects[0]; // 買第一個物件
                        if (player.Money >= obj.Price)
                        {
                            // 簡化：AI 30% 機會購買
                            if (rng.NextDouble() > 0.7)
                            {
                                GameManager.Instance.PurchaseObject(playerIndex, 0);
                                yield return new WaitForSeconds(0.5f);
                            }
                        }
                    }
                    break;

                case StationType.CardDraw:
                case StationType.CardGood:
                    // 抽卡片
                    var card = cs.DrawCard();
                    if (card != null)
                    {
                        cs.ExecuteCard(card, playerIndex);
                    }
                    break;
            }

            // 3. 考慮是否使用手牌
            if (player.HandCards.Count > 0 && player.Money >= THRESHOLD_USE_CARD)
            {
                if (rng.NextDouble() > 0.5) // 50% 機會使用手牌
                {
                    var cardToUse = player.HandCards[0];
                    cs.ExecuteCard(cardToUse, playerIndex);
                    // 從手牌移除（CardSystem需要有對應方法）
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // 4. 結束回合
            yield return new WaitForSeconds(0.3f);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 獲取 AI 建議（用於提示UI）
        /// </summary>
        public string GetAIAdvice(int playerIndex)
        {
            var pm = PlayerManager.Instance;
            var bm = BoardManager.Instance;
            var player = pm.GetPlayer(playerIndex);

            // 找到還沒有人擁有物件的車站
            var unowned = new List<Station>();
            foreach (var station in bm.Stations)
            {
                if (station.Type == StationType.Object && station.OwnerId == -1)
                    unowned.Add(station);
            }

            if (unowned.Count == 0) return "暫無建議";

            // 找到最值得買的（隨機選擇一個）
            if (unowned.Count > 0 && player.Money >= THRESHOLD_BUY)
            {
                var pick = unowned[rng.Next(unowned.Count)];
                return $"建議購買【{pick.NameCN}】的{pick.Objects.Count}個物件";
            }

            return "資金不足，建議等待";
        }
    }
}
