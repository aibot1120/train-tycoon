using System;
using System.Collections.Generic;
using UnityEngine;

namespace TravelChina.Core
{
    /// <summary>
    /// 卡片類型
    /// </summary>
    public enum CardType
    {
        Fortune,   // 命運卡（好）
        Misfortune // 厄運卡（壞）
    }

    /// <summary>
    /// 卡片效果類型
    /// </summary>
    public enum CardEffectType
    {
        MoneyGain,           // 直接獲得金錢
        MoneyLoss,           // 損失金錢
        MoveBonus,           // 骰子加成
        FreeMove,            // 免費移動到某處
        DestinationDouble,   // 目的地獎金×2
        Protect,             // 免疫厄運
        StealObject,         // 偷竊物件
        CancelCard,          // 取消對手卡片
        HealPoverty,         // 治療窮神
        SpreadPoverty,       // 傳播窮神
        DiscountBuy,         // 購買打折
        Robbery              // 搶劫
    }

    /// <summary>
    /// 卡片數據
    /// </summary>
    [Serializable]
    public class CardData
    {
        public string Id;          // "C001"
        public string NameCN;      // "春運高峰"
        public string NameEN;
        public string Description;
        public CardType Type;      // Fortune/Misfortune
        public CardEffectType Effect;
        public int Value;          // 金額 or 加成值
        public int Uses;           // 可用次數（-1 = 無限）
        public bool IsUsed = false;

        public CardData Clone() => new CardData
        {
            Id = Id, NameCN = NameCN, NameEN = NameEN,
            Description = Description, Type = Type, Effect = Effect,
            Value = Value, Uses = Uses
        };
    }

    /// <summary>
    /// 卡片系統
    /// </summary>
    public class CardSystem : MonoBehaviour
    {
        public static CardSystem Instance { get; private set; }

        [Header("狀態")]
        public List<CardData> FortuneDeck = new();   // 好運卡
        public List<CardData> MisfortuneDeck = new(); // 厄運卡
        public List<CardData> DiscardPile = new();     // 棄牌堆
        public List<CardData> AllCards = new();        // 所有卡

        private System.Random rng = new System.Random();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize()
        {
            GenerateMVPCards();
            ShuffleDeck();
            Debug.Log($"[Card] Initialized {AllCards.Count} cards");
        }

        private void GenerateMVPCards()
        {
            AllCards.Clear();
            FortuneDeck.Clear();
            MisfortuneDeck.Clear();

            // MVP 50張卡片
            var cards = new List<CardData>
            {
                // === 命運卡（好）===
                new CardData { Id="F001", NameCN="春節紅包", NameEN="Red Packet", Description="獲得壓歲錢", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=1_000_000 },
                new CardData { Id="F002", NameCN="黃金周", NameEN="Golden Week", Description="旅遊旺季收益+50%", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=2_000_000 },
                new CardData { Id="F003", NameCN="高鐵開通", NameEN="HSR Launch", Description="任意車站來回免費", Type=CardType.Fortune, Effect=CardEffectType.FreeMove, Value=0 },
                new CardData { Id="F004", NameCN="賽馬獲勝", NameEN="Horse Race Win", Description="獲得獎金", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=3_000_000 },
                new CardData { Id="F005", NameCN="股票大漲", NameEN="Stock Surge", Description="投資回報", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=2_500_000 },
                new CardData { Id="F006", NameCN="專利授權", NameEN="Patent Royalties", Description="技術專利收入", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=1_500_000 },
                new CardData { Id="F007", NameCN="颱風好運", NameEN="Typhoon Luck", Description="對手破產，獲得其一半財產", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=0 },
                new CardData { Id="F008", NameCN="貴人相助", NameEN="Benefactor Help", Description="指定玩家減半財產", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=0 },
                new CardData { Id="F009", NameCN="連續獲勝", NameEN="Winning Streak", Description="目的地獎金×2", Type=CardType.Fortune, Effect=CardEffectType.DestinationDouble, Value=2 },
                new CardData { Id="F010", NameCN="平安符", NameEN="Lucky Charm", Description="免疫1次厄運卡", Type=CardType.Fortune, Effect=CardEffectType.Protect, Value=1 },
                new CardData { Id="F011", NameCN="好天氣", NameEN="Good Weather", Description="骰子+2", Type=CardType.Fortune, Effect=CardEffectType.MoveBonus, Value=2 },
                new CardData { Id="F012", NameCN="快捷列車", NameEN="Express Train", Description="前進5步", Type=CardType.Fortune, Effect=CardEffectType.FreeMove, Value=5 },
                new CardData { Id="F013", NameCN="溫泉之旅", NameEN="Hot Spring Trip", Description="後退3步", Type=CardType.Fortune, Effect=CardEffectType.FreeMove, Value=-3 },
                new CardData { Id="F014", NameCN="溫暖康復", NameEN="Warm Recovery", Description="解除身上窮神", Type=CardType.Fortune, Effect=CardEffectType.HealPoverty, Value=0 },
                new CardData { Id="F015", NameCN="幸運硬幣", NameEN="Lucky Coin", Description="下回合骰子×2", Type=CardType.Fortune, Effect=CardEffectType.MoveBonus, Value=6 },
                new CardData { Id="F016", NameCN="禮物盒", NameEN="Gift Box", Description="隨機獲得¥500K-3M", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=0 },
                new CardData { Id="F017", NameCN="拍賣特惠", NameEN="Auction Deal", Description="下個物件8折", Type=CardType.Fortune, Effect=CardEffectType.DiscountBuy, Value=20 },
                new CardData { Id="F018", NameCN="獎學金", NameEN="Scholarship", Description="獲得獎學金", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=800_000 },
                new CardData { Id="F019", NameCN="股息分紅", NameEN="Dividend", Description="投資分紅", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=1_200_000 },
                new CardData { Id="F020", NameCN="彩票中獎", NameEN="Lottery Win", Description="小試手氣", Type=CardType.Fortune, Effect=CardEffectType.MoneyGain, Value=500_000 },

                // === 厄運卡（壞）===
                new CardData { Id="M001", NameCN="設備故障", NameEN="Equipment Failure", Description="維修費用", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=500_000 },
                new CardData { Id="M002", NameCN="員工罷工", NameEN="Staff Strike", Description="停工損失", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=800_000 },
                new CardData { Id="M003", NameCN="對手挑釁", NameEN="Rival Provocation", Description="對手獲得¥1M", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=1_000_000 },
                new CardData { Id="M004", NameCN="自然災害", NameEN="Natural Disaster", Description="隨機破壞1個物件", Type=CardType.Misfortune, Effect=CardEffectType.StealObject, Value=1 },
                new CardData { Id="M005", NameCN="經濟危機", NameEN="Economic Crisis", Description="所有收益-50%", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=1_500_000 },
                new CardData { Id="M006", NameCN="競爭對手", NameEN="Competitor Arrives", Description="對手收益×2", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=0 },
                new CardData { Id="M007", NameCN="罰款", NameEN="Fine", Description="違反規定被罰", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=600_000 },
                new CardData { Id="M008", NameCN="交通事故", NameEN="Traffic Accident", Description="事故賠償", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=1_000_000 },
                new CardData { Id="M009", NameCN="傳染病", NameEN="Epidemic", Description="被窮神附身", Type=CardType.Misfortune, Effect=CardEffectType.SpreadPoverty, Value=0 },
                new CardData { Id="M010", NameCN="火災", NameEN="Fire", Description="燒毀1個物件", Type=CardType.Misfortune, Effect=CardEffectType.StealObject, Value=1 },
                new CardData { Id="M011", NameCN="水災", NameEN="Flood", Description="損失¥2M", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=2_000_000 },
                new CardData { Id="M012", NameCN="貪污", NameEN="Corruption", Description="被罰¥1.5M", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=1_500_000 },
                new CardData { Id="M013", NameCN="被盜竊", NameEN="Theft", Description="被盜¥1M", Type=CardType.Misfortune, Effect=CardEffectType.Robbery, Value=1_000_000 },
                new CardData { Id="M014", NameCN="訴訟", NameEN="Lawsuit", Description="律師費", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=800_000 },
                new CardData { Id="M015", NameCN="交通事故", NameEN="Traffic Jam", Description="被移動到隨機位置", Type=CardType.Misfortune, Effect=CardEffectType.FreeMove, Value=0 },
                new CardData { Id="M016", NameCN="產品召回", NameEN="Product Recall", Description="召回費用", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=1_200_000 },
                new CardData { Id="M017", NameCN="匯率損失", NameEN="Exchange Loss", Description="外幣虧損", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=700_000 },
                new CardData { Id="M018", NameCN="顧客投訴", NameEN="Customer Complaint", Description="賠償", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=300_000 },
                new CardData { Id="M019", NameCN="疫情爆發", NameEN="Outbreak", Description="停產損失", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=2_000_000 },
                new CardData { Id="M020", NameCN="鐵路事故", NameEN="Railway Accident", Description="嚴重事故", Type=CardType.Misfortune, Effect=CardEffectType.MoneyLoss, Value=3_000_000 },
            };

            AllCards = cards;
            foreach (var c in cards)
                if (c.Type == CardType.Fortune) FortuneDeck.Add(c);
                else MisfortuneDeck.Add(c);
        }

        public void ShuffleDeck()
        {
            FortuneDeck.Shuffle(rng);
            MisfortuneDeck.Shuffle(rng);
            DiscardPile.Clear();
        }

        /// <summary>
        /// 抽取一張隨機卡片
        /// </summary>
        public CardData DrawCard()
        {
            bool isGood = rng.NextDouble() > 0.4; // 60% 好卡
            var deck = isGood ? FortuneDeck : MisfortuneDeck;

            if (deck.Count == 0) RecycleDeck(isGood ? CardType.Fortune : CardType.Misfortune);
            if (deck.Count == 0) return null;

            var card = deck[0];
            deck.RemoveAt(0);
            return card.Clone();
        }

        /// <summary>
        /// 抽取命運卡
        /// </summary>
        public CardData DrawFortuneCard()
        {
            if (FortuneDeck.Count == 0) RecycleDeck(CardType.Fortune);
            if (FortuneDeck.Count == 0) return null;

            var card = FortuneDeck[0];
            FortuneDeck.RemoveAt(0);
            return card.Clone();
        }

        /// <summary>
        /// 抽取厄運卡
        /// </summary>
        public CardData DrawMisfortuneCard()
        {
            if (MisfortuneDeck.Count == 0) RecycleDeck(CardType.Misfortune);
            if (MisfortuneDeck.Count == 0) return null;

            var card = MisfortuneDeck[0];
            MisfortuneDeck.RemoveAt(0);
            return card.Clone();
        }

        private void RecycleDeck(CardType type)
        {
            var source = type == CardType.Fortune ? FortuneDeck : MisfortuneDeck;
            foreach (var c in DiscardPile)
                if (c.Type == type) source.Add(c);
            DiscardPile.RemoveAll(c => c.Type == type);
            source.Shuffle(rng);
        }

        public void DiscardCard(CardData card)
        {
            DiscardPile.Add(card);
        }

        /// <summary>
        /// 執行卡片效果
        /// </summary>
        public void ExecuteCard(CardData card, int playerIndex, int targetPlayer = -1)
        {
            var player = PlayerManager.Instance.GetPlayer(playerIndex);
            var gm = GameManager.Instance;

            switch (card.Effect)
            {
                case CardEffectType.MoneyGain:
                    long gain = card.Value > 0 ? card.Value : rng.Next(500_000, 3_000_000);
                    player.Money += gain;
                    gm.UI.ShowCardEffect(card, $"+¥{gain:N0}");
                    break;

                case CardEffectType.MoneyLoss:
                    player.Money -= card.Value;
                    gm.UI.ShowCardEffect(card, $"-¥{card.Value:N0}");
                    break;

                case CardEffectType.MoveBonus:
                    // 緩存到玩家狀態，下次骰子時加成
                    player.TempDiceBonus = card.Value;
                    gm.UI.ShowCardEffect(card, $"骰子+{card.Value}");
                    break;

                case CardEffectType.FreeMove:
                    // 移動處理在 UI 層
                    gm.UI.ShowCardEffect(card, $"移動{card.Value}步");
                    break;

                case CardEffectType.DestinationDouble:
                    player.DestinationDouble = true;
                    gm.UI.ShowCardEffect(card, "目的地獎金×2");
                    break;

                case CardEffectType.HealPoverty:
                    if (PovertyGodManager.Instance.IsCursed(playerIndex))
                    {
                        PovertyGodManager.Instance.CursedPlayerId = -1;
                        gm.UI.ShowCardEffect(card, "解除窮神！");
                    }
                    break;

                case CardEffectType.SpreadPoverty:
                    if (targetPlayer >= 0)
                    {
                        PovertyGodManager.Instance.CursedPlayerId = targetPlayer;
                        gm.UI.ShowCardEffect(card, $"窮神傳染給玩家{targetPlayer}");
                    }
                    break;

                default:
                    Debug.Log($"[Card] Unhandled effect: {card.Effect}");
                    break;
            }
        }
    }

    #region 擴展

    public static class ListExt
    {
        public static void Shuffle<T>(this List<T> list, System.Random rng)
        {
            for (int n = list.Count - 1; n > 0; n--)
            {
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
    }

    #endregion
}
