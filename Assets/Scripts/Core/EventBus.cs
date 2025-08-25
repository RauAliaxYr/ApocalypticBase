using System;
using System.Collections.Generic;
using UnityEngine;


public class EventBus : MonoBehaviour
    {
        public static EventBus Instance { get; private set; }
        
        private Dictionary<Type, List<Delegate>> eventDictionary = new Dictionary<Type, List<Delegate>>();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void Subscribe<T>(Action<T> listener)
        {
            Type eventType = typeof(T);
            
            if (!eventDictionary.ContainsKey(eventType))
            {
                eventDictionary[eventType] = new List<Delegate>();
            }
            
            eventDictionary[eventType].Add(listener);
        }
        
        public void Unsubscribe<T>(Action<T> listener)
        {
            Type eventType = typeof(T);
            
            if (eventDictionary.ContainsKey(eventType))
            {
                eventDictionary[eventType].Remove(listener);
            }
        }
        
        public void Publish<T>(T eventData)
        {
            Type eventType = typeof(T);
            
            if (eventDictionary.ContainsKey(eventType))
            {
                foreach (Delegate listener in eventDictionary[eventType])
                {
                    if (listener is Action<T> action)
                    {
                        action.Invoke(eventData);
                    }
                }
            }
        }
        
        private void OnDestroy()
        {
            eventDictionary.Clear();
        }
    }
    
    // Event definitions
    public class TileSwappedEvent
    {
        public Vector2Int FromPosition;
        public Vector2Int ToPosition;
        public TileBase FromTile;
        public TileBase ToTile;
    }
    
    public class MatchFoundEvent
    {
        public List<Vector2Int> MatchedPositions;
        public string TileId;
        public int MatchCount;
    }
    
    public class TowerUpgradedEvent
    {
        public Vector2Int Position;
        public TowerDefinition OldTower;
        public TowerDefinition NewTower;
    }
    
    public class EnemySpawnedEvent
    {
        public Enemy Enemy;
        public Vector2Int SpawnPosition;
    }
    
    public class EnemyDiedEvent
    {
        public Enemy Enemy;
        public int GoldReward;
    }
    
    public class WaveCompletedEvent
    {
        public int DayIndex;
        public int GoldReward;
    }
    
    public class BossHintEvent
    {
        public Vector2Int[] BossPath;
        public int DayIndex;
    }
    
    public class GameOverEvent { }