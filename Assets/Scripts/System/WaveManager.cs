using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WaveManager : MonoBehaviour
    {
        [Header("Wave Settings")]
        public WaveConfig[] waveConfigs;
        public float timeBetweenWaves = 5f;
        public Transform enemyContainer;
        
        [Header("Current Wave")]
        public int currentWaveIndex = 0;
        public WaveConfig currentWave;
        public bool isWaveActive = false;
        public int enemiesRemaining = 0;
        
        [Header("Spawn Settings")]
        public Vector2Int[] spawnPoints;
        public Vector2Int[] basePositions;
        
        [Header("References")]
        public GameObject enemyPrefab;
        
        private List<Enemy> activeEnemies = new List<Enemy>();
        private Coroutine currentWaveCoroutine;
        private bool isInitialized = false;
        
        public void Initialize()
        {
            if (waveConfigs.Length == 0)
            {
                Debug.LogWarning("No wave configs assigned to WaveManager!");
                return;
            }
            
            // Set default spawn and base points if not set
            if (spawnPoints.Length == 0)
            {
                spawnPoints = new Vector2Int[] { new Vector2Int(0, 7), new Vector2Int(7, 7) };
            }
            
            if (basePositions.Length == 0)
            {
                basePositions = new Vector2Int[] { new Vector2Int(3, 0), new Vector2Int(4, 0) };
            }
            
            isInitialized = true;
        }
        
        public void StartWaves()
        {
            if (!isInitialized) return;
            
            currentWaveIndex = 0;
            StartNextWave();
        }
        
        public void StartNextWave()
        {
            if (currentWaveIndex >= waveConfigs.Length)
            {
                // All waves completed
                Debug.Log("All waves completed!");
                return;
            }
            
            currentWave = waveConfigs[currentWaveIndex];
            isWaveActive = true;
            
            Debug.Log($"Starting Wave {currentWaveIndex + 1}: {currentWave.waveName}");
            
            // Start wave coroutine
            if (currentWaveCoroutine != null)
            {
                StopCoroutine(currentWaveCoroutine);
            }
            
            currentWaveCoroutine = StartCoroutine(RunWave(currentWave));
        }
        
        private IEnumerator RunWave(WaveConfig wave)
        {
            enemiesRemaining = 0;
            
            // Calculate total enemies
            foreach (EnemyPack pack in wave.enemyPacks)
            {
                enemiesRemaining += pack.count;
            }
            
            if (wave.hasBoss)
            {
                enemiesRemaining++;
            }
            
            // Spawn enemy packs
            foreach (EnemyPack pack in wave.enemyPacks)
            {
                yield return StartCoroutine(SpawnEnemyPack(pack));
            }
            
            // Spawn boss if wave has one
            if (wave.hasBoss && wave.bossPath != null)
            {
                yield return new WaitForSeconds(2f); // Delay before boss
                SpawnBoss(wave.bossPath);
            }
            
            // Wait for all enemies to be defeated or reach base
            while (enemiesRemaining > 0 && isWaveActive)
            {
                yield return null;
            }
            
            // Wave completed
            CompleteWave(wave);
        }
        
        private IEnumerator SpawnEnemyPack(EnemyPack pack)
        {
            for (int i = 0; i < pack.count; i++)
            {
                SpawnEnemy(pack.enemyType, GetRandomSpawnPoint());
                
                // Wait between spawns
                yield return new WaitForSeconds(pack.spawnInterval);
            }
            
            // Wait before next pack
            yield return new WaitForSeconds(pack.delayBeforePack);
        }
        
        private void SpawnEnemy(EnemyDefinition enemyDef, Vector2Int spawnPoint)
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("Enemy prefab not assigned to WaveManager!");
                return;
            }
            
            // Create enemy
            Vector3 spawnPosition = GameManager.Instance.GridController.GridToWorldPosition(spawnPoint);
            GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemyContainer);
            
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Set enemy path
                Vector2Int[] path = GeneratePathToBase(spawnPoint);
                enemy.Initialize(enemyDef, path);
                
                // Add to active enemies
                activeEnemies.Add(enemy);
                
                // Subscribe to enemy events
                EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
                EventBus.Instance.Subscribe<EnemyReachedBaseEvent>(OnEnemyReachedBase);
            }
        }
        
        private void SpawnBoss(BossPath bossPath)
        {
            // Create boss enemy
            Vector2Int spawnPoint = GetRandomSpawnPoint();
            Vector3 spawnPosition = GameManager.Instance.GridController.GridToWorldPosition(spawnPoint);
            
            GameObject bossObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemyContainer);
            
            Enemy boss = bossObj.GetComponent<Enemy>();
            if (boss != null)
            {
                // Use boss path from config
                boss.Initialize(bossPath.enemyType, bossPath.pathCells);
                boss.isBoss = true;
                
                // Add to active enemies
                activeEnemies.Add(boss);
                
                // Subscribe to boss events
                EventBus.Instance.Subscribe<EnemyDiedEvent>(OnEnemyDied);
                EventBus.Instance.Subscribe<EnemyReachedBaseEvent>(OnEnemyReachedBase);
                
                // Show boss hint every 5 days
                if (GameManager.Instance.ProgressState.currentDay % 5 == 0)
                {
                    EventBus.Instance.Publish(new BossHintEvent
                    {
                        BossPath = bossPath.GetHintPath(),
                        DayIndex = GameManager.Instance.ProgressState.currentDay
                    });
                }
            }
        }
        
        private Vector2Int[] GeneratePathToBase(Vector2Int startPoint)
        {
            // Simple path generation - could be improved with A* pathfinding
            List<Vector2Int> path = new List<Vector2Int>();
            path.Add(startPoint);
            
            Vector2Int current = startPoint;
            Vector2Int target = basePositions[Random.Range(0, basePositions.Length)];
            
            while (current != target)
            {
                // Move towards target
                if (current.x < target.x) current.x++;
                else if (current.x > target.x) current.x--;
                
                if (current.y < target.y) current.y++;
                else if (current.y > target.y) current.y--;
                
                path.Add(current);
            }
            
            return path.ToArray();
        }
        
        private Vector2Int GetRandomSpawnPoint()
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }
        
        private void OnEnemyDied(EnemyDiedEvent enemyEvent)
        {
            // Remove from active enemies
            if (activeEnemies.Contains(enemyEvent.Enemy))
            {
                activeEnemies.Remove(enemyEvent.Enemy);
                enemiesRemaining--;
            }
        }
        
        private void OnEnemyReachedBase(EnemyReachedBaseEvent enemyEvent)
        {
            // Remove from active enemies
            if (activeEnemies.Contains(enemyEvent.Enemy))
            {
                activeEnemies.Remove(enemyEvent.Enemy);
                enemiesRemaining--;
            }
            
            // Check if base is destroyed
            if (GameManager.Instance.ProgressState.IsGameOver())
            {
                GameOver();
            }
        }
        
        private void CompleteWave(WaveConfig wave)
        {
            isWaveActive = false;
            currentWaveIndex++;
            
            Debug.Log($"Wave {wave.waveName} completed!");
            
            // Add rewards
            GameManager.Instance.EconomyManager.AddGold(wave.goldReward);
            
            // Publish wave completed event
            EventBus.Instance.Publish(new WaveCompletedEvent
            {
                DayIndex = wave.dayIndex,
                GoldReward = wave.goldReward
            });
            
            // Start next wave after delay
            StartCoroutine(StartNextWaveAfterDelay());
        }
        
        private IEnumerator StartNextWaveAfterDelay()
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            StartNextWave();
        }
        
        private void GameOver()
        {
            isWaveActive = false;
            Debug.Log("Game Over! Base destroyed!");
            
            // Stop all enemies
            foreach (Enemy enemy in activeEnemies)
            {
                enemy.StopMovement();
            }
            
            // Publish game over event
            EventBus.Instance.Publish(new GameOverEvent());
        }
        
        public void PauseWaves()
        {
            isWaveActive = false;
            
            foreach (Enemy enemy in activeEnemies)
            {
                enemy.StopMovement();
            }
        }
        
        public void ResumeWaves()
        {
            isWaveActive = true;
            
            foreach (Enemy enemy in activeEnemies)
            {
                enemy.ResumeMovement();
            }
        }
        
        public void StopWaves()
        {
            isWaveActive = false;
            
            if (currentWaveCoroutine != null)
            {
                StopCoroutine(currentWaveCoroutine);
            }
            
            // Destroy all enemies
            foreach (Enemy enemy in activeEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            
            activeEnemies.Clear();
            enemiesRemaining = 0;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            EventBus.Instance.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Instance.Unsubscribe<EnemyReachedBaseEvent>(OnEnemyReachedBase);
        }
    }