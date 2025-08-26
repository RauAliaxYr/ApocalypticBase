using UnityEngine;



    // Deprecated: tower-specific config moved into TileDefinition evolution
    // Keep class as placeholder to avoid compile errors during migration
    public class TowerDefinition : ScriptableObject
    {
        [Header("Deprecated - use TileDefinition evolution")]
        public string id;
        public int level = 1;
        public GameObject towerPrefab;
        public TowerDefinition nextLevelTower;
    }

