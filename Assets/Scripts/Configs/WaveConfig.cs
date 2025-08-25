using UnityEngine;


[CreateAssetMenu(fileName = "New Wave", menuName = "Apocalyptic Base/Configs/Wave Config")]
public class WaveConfig : ScriptableObject
    {
        [Header("Wave Info")]
        public int dayIndex;
        public string waveName;
        
        [Header("Enemy Packs")]
        public EnemyPack[] enemyPacks;
        
        [Header("Boss")]
        public bool hasBoss;
        public BossPath bossPath;
        
        [Header("Rewards")]
        public int goldReward = 50;
        public int experienceReward = 10;
    }
    
    [System.Serializable]
    public class EnemyPack
    {
        public EnemyDefinition enemyType;
        public int count = 5;
        public float spawnInterval = 1f;
        public float delayBeforePack = 0f;
    }
    
    [System.Serializable]
    public class BossPath
    {
        public EnemyDefinition enemyType;
        public Vector2Int[] pathCells;
        public float moveSpeed = 2f;
        public int health = 100;
        public int damage = 20;
        
        public Vector2Int[] GetHintPath()
        {
            // Возвращает путь для подсказки игроку
            return pathCells;
        }
    }