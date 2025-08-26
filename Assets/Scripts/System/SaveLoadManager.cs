using UnityEngine;
using System.IO;
using System;



public class SaveLoadManager : MonoBehaviour
    {
        [Header("Save Settings")]
        public string saveFileName = "apocalyptic_base_save.json";
        public bool autoSave = true;
        public float autoSaveInterval = 60f; // seconds
        
        [Header("Save Data")]
        public GameSaveData currentSaveData;
        
        private string saveFilePath;
        private float lastAutoSaveTime;
        private bool isInitialized = false;
        
        [System.Serializable]
        public class GameSaveData
        {
            public ProgressState progressState;
            public BoardState boardState;
            public string saveDate;
            public int gameVersion = 1;
            public float totalPlayTime;
        }
        
        private void Awake()
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        }
        
        public void Initialize()
        {
            isInitialized = true;
            lastAutoSaveTime = Time.time;
            
            // Subscribe to events for auto-save triggers
            EventBus.Instance.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            
            Debug.Log($"Save file path: {saveFilePath}");
        }
        
        private void Update()
        {
            if (!isInitialized || !autoSave) return;
            
            // Check if it's time for auto-save
            if (Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                AutoSave();
            }
        }
        
        public void SaveProgress()
        {
            if (!isInitialized) return;
            
            try
            {
                // Create save data
                currentSaveData = new GameSaveData
                {
                    progressState = GameManager.Instance.ProgressState,
                    boardState = GameManager.Instance.GridController.boardState,
                    saveDate = DateTime.Now.ToString(),
                    gameVersion = 1,
                    totalPlayTime = Time.time
                };
                
                // Serialize to JSON
                string jsonData = JsonUtility.ToJson(currentSaveData, true);
                
                // Write to file
                File.WriteAllText(saveFilePath, jsonData);
                
                Debug.Log($"Game saved successfully to: {saveFilePath}");
                lastAutoSaveTime = Time.time;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
            }
        }
        
        public void LoadProgress()
        {
            if (!isInitialized) return;
            
            try
            {
                if (File.Exists(saveFilePath))
                {
                    // Read from file
                    string jsonData = File.ReadAllText(saveFilePath);
                    
                    // Deserialize from JSON
                    currentSaveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                    
                    // Apply loaded data
                    ApplyLoadedData(currentSaveData);
                    
                    Debug.Log($"Game loaded successfully from: {saveFilePath}");
                    Debug.Log($"Save date: {currentSaveData.saveDate}");
                }
                else
                {
                    Debug.Log("No save file found. Starting new game.");
                    StartNewGame();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                Debug.Log("Starting new game due to load failure.");
                StartNewGame();
            }
        }
        
        private void ApplyLoadedData(GameSaveData saveData)
        {
            if (saveData == null) return;
            
            // Apply progress state
            if (saveData.progressState != null)
            {
                GameManager.Instance.ProgressState = saveData.progressState;
            }
            
            // Apply board state
            if (saveData.boardState != null)
            {
                GameManager.Instance.GridController.boardState = saveData.boardState;
            }
            
            // Update total play time
            if (saveData.totalPlayTime > 0)
            {
                GameManager.Instance.ProgressState.totalPlayTime = saveData.totalPlayTime;
            }
        }
        
        public void StartNewGame()
        {
            // Reset progress state
            GameManager.Instance.ProgressState.ResetProgress();
            
            // Create new board state
            GameManager.Instance.GridController.boardState = new BoardState(
                GameManager.Instance.GridController.gridWidth,
                GameManager.Instance.GridController.gridHeight
            );
            
            // Clear save data
            currentSaveData = null;
            
            Debug.Log("New game started");
        }
        
        public void DeleteSave()
        {
            try
            {
                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                    Debug.Log("Save file deleted");
                }
                
                currentSaveData = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save file: {e.Message}");
            }
        }
        
        public bool HasSaveFile()
        {
            return File.Exists(saveFilePath);
        }
        
        public DateTime GetLastSaveTime()
        {
            if (currentSaveData != null && !string.IsNullOrEmpty(currentSaveData.saveDate))
            {
                if (DateTime.TryParse(currentSaveData.saveDate, out DateTime saveTime))
                {
                    return saveTime;
                }
            }
            
            return DateTime.MinValue;
        }
        
        public void AutoSave()
        {
            if (autoSave)
            {
                SaveProgress();
            }
        }
        
        public void ForceSave()
        {
            SaveProgress();
        }
        
        // Event handlers for auto-save triggers
        private void OnWaveCompleted(WaveCompletedEvent waveEvent)
        {
            // Auto-save after wave completion
            if (autoSave)
            {
                SaveProgress();
            }
        }
        
        // Public methods for manual save/load
        [ContextMenu("Save Game")]
        public void DebugSave()
        {
            SaveProgress();
        }
        
        [ContextMenu("Load Game")]
        public void DebugLoad()
        {
            LoadProgress();
        }
        
        [ContextMenu("Delete Save")]
        public void DebugDeleteSave()
        {
            DeleteSave();
        }
        
        [ContextMenu("Start New Game")]
        public void DebugNewGame()
        {
            StartNewGame();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && autoSave)
            {
                // Auto-save when game is paused
                SaveProgress();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && autoSave)
            {
                // Auto-save when game loses focus
                SaveProgress();
            }
        }
        
        private void OnApplicationQuit()
        {
            // Save on quit
            if (isInitialized)
            {
                SaveProgress();
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            EventBus.Instance.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
        }
    }