using UnityEngine;


    [CreateAssetMenu(fileName = "New Enemy", menuName = "Apocalyptic Base/Configs/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string id;
        public string displayName;
        public Sprite sprite;
        public GameObject enemyPrefab;
        
        [Header("Stats")]
        public int health = 50;
        public float moveSpeed = 2f;
        public int damage = 10;
        public int goldReward = 5;
        
        [Header("Movement")]
        public EnemyType enemyType;
        public bool canFly = false;
        public bool isBoss = false;
        
        [Header("Visual")]
        public Color enemyColor = Color.red;
        public float scale = 1f;
    }
    
    public enum EnemyType
    {
        Normal,     // Обычный враг
        Fast,       // Быстрый враг
        Armored,    // Бронированный враг
        Boss        // Босс
    }

