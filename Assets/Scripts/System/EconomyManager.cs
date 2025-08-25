using UnityEngine;



public class EconomyManager : MonoBehaviour
    {
        [Header("Economy Settings")]
        public int startingGold = 100;
        public int goldPerEnemyKill = 5;
        public int goldPerWaveCompletion = 50;
        public int goldPerDay = 25;
        
        [Header("Shop Settings")]
        public int resourceTileCost = 50;
        public int towerTileCost = 100;
        public int upgradeCost = 75;
        
        private ProgressState progressState;
        private bool isInitialized = false;
        
        public void Initialize()
        {
            progressState = GameManager.Instance.ProgressState;
            
            // Set initial gold if starting new game
            if (progressState.gold == 0)
            {
                progressState.gold = startingGold;
            }
            
            isInitialized = true;
            
            // Subscribe to events
            EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Instance.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        }
        
        public void AddGold(int amount)
        {
            if (!isInitialized) return;
            
            progressState.AddGold(amount);
            
            // Show gold gain animation
            ShowGoldGainAnimation(amount);
            
            Debug.Log($"Gold gained: +{amount} (Total: {progressState.gold})");
        }
        
        public bool SpendGold(int amount)
        {
            if (!isInitialized) return false;
            
            bool success = progressState.SpendGold(amount);
            
            if (success)
            {
                Debug.Log($"Gold spent: -{amount} (Total: {progressState.gold})");
            }
            else
            {
                Debug.LogWarning($"Not enough gold! Required: {amount}, Available: {progressState.gold}");
            }
            
            return success;
        }
        
        public void UseSwap()
        {
            if (!isInitialized) return;
            
            progressState.UseSwap();
            
            // Check if out of swaps
            if (progressState.swapsLeft <= 0)
            {
                StartWave();
            }
        }
        
        public void StartWave()
        {
            // Start enemy wave when swaps are depleted
            GameManager.Instance.WaveManager.StartNextWave();
        }
        
        public void CompleteDay()
        {
            if (!isInitialized) return;
            
            // Add daily gold bonus
            AddGold(goldPerDay);
            
            // Start new day
            progressState.StartNewDay();
            
            Debug.Log($"Day {progressState.currentDay} completed! Gold bonus: +{goldPerDay}");
        }
        
        // Shop methods
        public bool BuyResourceTile()
        {
            return SpendGold(resourceTileCost);
        }
        
        public bool BuyTowerTile()
        {
            return SpendGold(towerTileCost);
        }
        
        public bool BuyUpgrade()
        {
            return SpendGold(upgradeCost);
        }
        
        public bool CanAfford(int cost)
        {
            return progressState.gold >= cost;
        }
        
        public int GetGold()
        {
            return progressState.gold;
        }
        
        public int GetSwapsLeft()
        {
            return progressState.swapsLeft;
        }
        
        public int GetCurrentDay()
        {
            return progressState.currentDay;
        }
        
        // Event handlers
        private void OnEnemyDied(EnemyDiedEvent enemyEvent)
        {
            // Add gold for enemy kill
            AddGold(enemyEvent.GoldReward);
            
            // Update statistics
            progressState.enemiesKilled++;
        }
        
        private void OnWaveCompleted(WaveCompletedEvent waveEvent)
        {
            // Add gold for wave completion
            AddGold(waveEvent.GoldReward);
            
            // Update statistics
            progressState.wavesCompleted++;
            
            // Complete day
            CompleteDay();
        }
        
        private void ShowGoldGainAnimation(int amount)
        {
            // This would show a visual animation for gold gain
            // Could be implemented with UI animations or particle effects
            
            // For now, just log it
            Debug.Log($"Gold gain animation: +{amount}");
        }
        
        // Debug methods
        [ContextMenu("Add 100 Gold")]
        public void DebugAddGold()
        {
            AddGold(100);
        }
        
        [ContextMenu("Reset Economy")]
        public void DebugResetEconomy()
        {
            progressState.gold = startingGold;
            progressState.swapsLeft = progressState.maxSwapsPerDay;
            Debug.Log("Economy reset to starting values");
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            EventBus.Instance.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Instance.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
        }
    }