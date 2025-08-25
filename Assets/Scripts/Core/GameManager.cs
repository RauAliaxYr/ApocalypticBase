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
            // Ensure data objects exist
            if (ProgressState == null)
            {
                ProgressState = new ProgressState();
            }
            
            // Initialize save system then load
            if (SaveLoadManager != null)
            {
                SaveLoadManager.Initialize();
                SaveLoadManager.LoadProgress();
            }
            
            // Initialize all systems
            if (GridController != null) GridController.Initialize();
            if (WaveManager != null) WaveManager.Initialize();
            if (EconomyManager != null) EconomyManager.Initialize();
        }
        
        public void StartGame()
        {
            CurrentGameState = GameState.Playing;
            GridController.StartGame();
            WaveManager.StartWaves();
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        
        public void ExitGame()
        {
            SaveLoadManager.SaveProgress();
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