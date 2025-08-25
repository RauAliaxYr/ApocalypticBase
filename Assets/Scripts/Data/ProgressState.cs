using System;
using UnityEngine;


    [Serializable]
    public class ProgressState
    {
        [Header("Game Progress")]
        public int currentDay = 1;
        public int baseHealth = 100;
        public int maxBaseHealth = 100;
        public int swapsLeft = 3;
        public int maxSwapsPerDay = 3;
        
        [Header("Economy")]
        public int gold = 100;
        public int totalGoldEarned = 0;
        public int totalGoldSpent = 0;
        
        [Header("Statistics")]
        public int enemiesKilled = 0;
        public int towersBuilt = 0;
        public int towersUpgraded = 0;
        public int wavesCompleted = 0;
        
        [Header("Settings")]
        public bool soundEnabled = true;
        public bool musicEnabled = true;
        public float masterVolume = 1f;
        
        [Header("Meta")]
        public string saveDate;
        public float totalPlayTime = 0f;
        public int gameVersion = 1;
        
        public ProgressState()
        {
            saveDate = System.DateTime.Now.ToString();
        }
        
        public void StartNewDay()
        {
            currentDay++;
            swapsLeft = maxSwapsPerDay;
            baseHealth = Mathf.Min(baseHealth + 10, maxBaseHealth); // Восстановление здоровья базы
        }
        
        public void UseSwap()
        {
            if (swapsLeft > 0)
            {
                swapsLeft--;
            }
        }
        
        public void AddGold(int amount)
        {
            gold += amount;
            totalGoldEarned += amount;
        }
        
        public bool SpendGold(int amount)
        {
            if (gold >= amount)
            {
                gold -= amount;
                totalGoldSpent += amount;
                return true;
            }
            return false;
        }
        
        public void TakeDamage(int damage)
        {
            baseHealth = Mathf.Max(0, baseHealth - damage);
        }
        
        public bool IsGameOver()
        {
            return baseHealth <= 0;
        }
        
        public void ResetProgress()
        {
            currentDay = 1;
            baseHealth = maxBaseHealth;
            swapsLeft = maxSwapsPerDay;
            gold = 100;
            enemiesKilled = 0;
            towersBuilt = 0;
            towersUpgraded = 0;
            wavesCompleted = 0;
            totalGoldEarned = 0;
            totalGoldSpent = 0;
            totalPlayTime = 0f;
        }
    }