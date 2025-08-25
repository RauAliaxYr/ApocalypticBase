using UnityEngine;



    [CreateAssetMenu(fileName = "New Tower", menuName = "Apocalyptic Base/Configs/Tower Definition")]
    public class TowerDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string id;
        public string displayName;
        public Sprite sprite;
        public GameObject towerPrefab;
        
        [Header("Level")]
        public int level = 1;
        public int maxLevel = 3;
        
        [Header("Combat Stats")]
        public int damage = 10;
        public float attackInterval = 1f;
        public float range = 3f;
        public int cost = 100;
        
        [Header("Attack Pattern")]
        public AttackPattern attackPattern;
        public GameObject projectilePrefab;
        
        [Header("Upgrade")]
        public TowerDefinition nextLevelTower;
        public int upgradeCost;
    }
    
    [System.Serializable]
    public class AttackPattern
    {
        public Vector2Int[] attackCells;
        public bool isCircular = false;
        public float radius = 1f;
        
        public bool ContainsCell(Vector2Int cell)
        {
            if (isCircular)
            {
                float distance = Vector2.Distance(Vector2.zero, cell);
                return distance <= radius;
            }
            
            foreach (Vector2Int attackCell in attackCells)
            {
                if (attackCell == cell) return true;
            }
            return false;
        }
    }

