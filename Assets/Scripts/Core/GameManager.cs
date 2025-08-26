using UnityEngine;
using UnityEngine.SceneManagement;



public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public bool isGameActive => CurrentGameState == GameState.Playing;
        
        [Header("Game State")]
        public GameState CurrentGameState = GameState.Welcome;
        
        [Header("Systems")]
        public GridController GridController;
        public WaveManager WaveManager;
        public EconomyManager EconomyManager;
        public SaveLoadManager SaveLoadManager;
        public SwapSystem SwapSystem;
        
        [Header("Data")]
        public ProgressState ProgressState;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeGame()
        {
            Debug.Log("GameManager.InitializeGame() called");
            
            // Ensure data objects exist
            if (ProgressState == null)
            {
                ProgressState = new ProgressState();
                Debug.Log("GameManager: Created new ProgressState");
            }
            
            // Initialize save system then load
            if (SaveLoadManager != null)
            {
                SaveLoadManager.Initialize();
                SaveLoadManager.LoadProgress();
                Debug.Log("GameManager: SaveLoadManager initialized");
            }
            else
            {
                Debug.LogWarning("GameManager: SaveLoadManager is null");
            }
            
            // Initialize all systems
            if (GridController != null)
            {
                Debug.Log("GameManager: Initializing GridController");
                GridController.Initialize();
            }
            else
            {
                Debug.LogError("GameManager: GridController is null!");
            }
            
            if (WaveManager != null)
            {
                Debug.Log("GameManager: Initializing WaveManager");
                WaveManager.Initialize();
            }
            else
            {
                Debug.LogWarning("GameManager: WaveManager is null");
            }
            
            if (EconomyManager != null)
            {
                Debug.Log("GameManager: Initializing EconomyManager");
                EconomyManager.Initialize();
            }
            else
            {
                Debug.LogWarning("GameManager: EconomyManager is null");
            }
            
            // SwapSystem dependencies should be set in inspector
            if (SwapSystem == null)
            {
                Debug.LogWarning("GameManager: SwapSystem is null");
            }
            
            Debug.Log("GameManager.InitializeGame() completed");
            
            // Auto-start game for testing (can be removed later)
            StartGame();
        }
        
        public void StartGame()
        {
            Debug.Log("GameManager: StartGame called");
            CurrentGameState = GameState.Playing;
            
            if (GridController != null)
            {
                GridController.StartGame();
            }
            else
            {
                Debug.LogError("GameManager: Cannot start game - GridController is null!");
                return;
            }
            
            if (WaveManager != null)
            {
                WaveManager.StartWaves();
            }
            else
            {
                Debug.LogWarning("GameManager: WaveManager is null - waves will not start");
            }
            
            Debug.Log("GameManager: Game started successfully");
        }
        
        public void PauseGame()
        {
            CurrentGameState = GameState.Paused;
            Time.timeScale = 0f;
        }
        
        public void ResumeGame()
        {
            CurrentGameState = GameState.Playing;
            Time.timeScale = 1f;
        }
        
        public void RestartGame()
        {
            if (SaveLoadManager != null)
            {
                SaveLoadManager.SaveProgress();
            }
            else
            {
                Debug.LogWarning("GameManager: SaveLoadManager is null - progress will not be saved before restart");
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        
        public void ExitGame()
        {
            if (SaveLoadManager != null)
            {
                SaveLoadManager.SaveProgress();
            }
            else
            {
                Debug.LogWarning("GameManager: SaveLoadManager is null - progress will not be saved");
            }
            Application.Quit();
        }
    }
    
    public enum GameState
    {
        Welcome,
        Playing,
        Paused,
        GameOver
    }