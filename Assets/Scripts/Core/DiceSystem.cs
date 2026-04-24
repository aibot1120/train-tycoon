using System;
using UnityEngine;

namespace TravelChina.Core
{
    /// <summary>
    /// 骰子系统
    /// </summary>
    public class DiceSystem : MonoBehaviour
    {
        public static DiceSystem Instance { get; private set; }

        public event Action<int> OnDiceRolled;

        [SerializeField] private int minValue = 1;
        [SerializeField] private int maxValue = 6;

        private System.Random rng = new System.Random();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 掷骰子
        /// </summary>
        public int Roll()
        {
            int result = rng.Next(minValue, maxValue + 1);

            // 如果玩家有骰子加成
            var player = GameManager.Instance?.GetActivePlayer();
            if (player != null && player.TempDiceBonus > 0)
            {
                result += player.TempDiceBonus;
                player.TempDiceBonus = 0; // 用完清除
            }

            OnDiceRolled?.Invoke(result);
            return result;
        }

        /// <summary>
        /// 可选：双骰子（桃太郎风格）
        /// </summary>
        public (int d1, int d2, int total) RollDouble()
        {
            int d1 = rng.Next(minValue, maxValue + 1);
            int d2 = rng.Next(minValue, maxValue + 1);

            var player = GameManager.Instance?.GetActivePlayer();
            int bonus = 0;
            if (player != null && player.TempDiceBonus > 0)
            {
                bonus = player.TempDiceBonus;
                player.TempDiceBonus = 0;
            }

            return (d1, d2, d1 + d2 + bonus);
        }

        /// <summary>
        /// 选择骰子（特殊卡片效果）
        /// </summary>
        public int RollChoose()
        {
            // 返回可选择的值（1-6）
            return rng.Next(minValue, maxValue + 1);
        }
    }
}
