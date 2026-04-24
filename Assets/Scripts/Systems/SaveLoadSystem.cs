using System;
using System.IO;
using UnityEngine;

namespace TravelChina.Systems
{
    /// <summary>
    /// 存檔/讀檔系統
    /// </summary>
    public class SaveLoadSystem : MonoBehaviour
    {
        public static SaveLoadSystem Instance { get; private set; }

        private string SaveDirectory => Path.Combine(Application.dataPath, "..", "Saves");
        private const string SAVE_FILE_PREFIX = "TravelChina_Save_";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (!Directory.Exists(SaveDirectory))
                Directory.CreateDirectory(SaveDirectory);
        }

        /// <summary>
        /// 保存當前遊戲狀態
        /// </summary>
        public void SaveGame(GameManager gm, PlayerManager pm, BoardManager bm, int month, int round)
        {
            var saveData = new GameSaveData
            {
                version = "1.0",
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                currentMonth = month,
                currentRound = round,
                playerData = new System.Collections.Generic.List<PlayerSaveData>(),
                boardData = new BoardSaveData { stations = new System.Collections.Generic.List<StationSaveData>() }
            };

            // 玩家數據
            for (int i = 0; i < pm.PlayerCount; i++)
            {
                var p = pm.GetPlayer(i);
                saveData.playerData.Add(new PlayerSaveData
                {
                    id = p.Id,
                    money = p.Money,
                    position = p.Position,
                    routeIndex = p.RouteIndex
                });
            }

            // 板塊數據
            for (int i = 0; i < bm.GetStationCount(); i++)
            {
                var station = bm.GetStation(i);
                saveData.boardData.stations.Add(new StationSaveData
                {
                    id = station.Id,
                    ownerId = station.OwnerId,
                    isMonopolized = station.IsMonopolized,
                    monopolyOwnerId = station.MonopolyOwnerId
                });
            }

            string json = JsonUtility.ToJson(saveData, true);
            string fileName = $"{SAVE_FILE_PREFIX}{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string fullPath = Path.Combine(SaveDirectory, fileName);

            File.WriteAllText(fullPath, json);
            Debug.Log($"[SaveLoad] Game saved to {fullPath}");
        }

        /// <summary>
        /// 讀取存檔
        /// </summary>
        public GameSaveData LoadGame(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[SaveLoad] Save file not found: {path}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<GameSaveData>(json);
                Debug.Log($"[SaveLoad] Game loaded from {path}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveLoad] Failed to load: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 獲取所有存檔文件列表
        /// </summary>
        public string[] GetSaveFileList()
        {
            if (!Directory.Exists(SaveDirectory))
                return Array.Empty<string>();

            return Directory.GetFiles(SaveDirectory, "*.json");
        }

        /// <summary>
        /// 刪除存檔
        /// </summary>
        public void DeleteSave(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveLoad] Deleted: {path}");
            }
        }
    }

    #region 存檔數據結構

    [Serializable]
    public class GameSaveData
    {
        public string version;
        public string timestamp;
        public int currentMonth;
        public int currentRound;
        public System.Collections.Generic.List<PlayerSaveData> playerData;
        public BoardSaveData boardData;
    }

    [Serializable]
    public class PlayerSaveData
    {
        public int id;
        public long money;
        public int position;
        public int routeIndex;
    }

    [Serializable]
    public class BoardSaveData
    {
        public System.Collections.Generic.List<StationSaveData> stations;
    }

    [Serializable]
    public class StationSaveData
    {
        public int id;
        public int ownerId;
        public bool isMonopolized;
        public int monopolyOwnerId;
    }

    #endregion
}
