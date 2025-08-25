using UnityEngine;


    [CreateAssetMenu(fileName = "New Tile", menuName = "Apocalyptic Base/Configs/Tile Definition")]
    public class TileDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public string id;
        public string displayName;
        public Sprite sprite;
        
        [Header("Category")]
        public TileCategory category;
        
        [Header("Match Result")]
        public MatchResult matchResult;
        
        [Header("Properties")]
        public int baseValue = 1;
        public bool isWalkable = true;
        public bool canBeSwapped = true;
    }
    
    public enum TileCategory
    {
        Resource,   // Дерево, камень, металл
        Tower,      // Башня
        Bonus,      // Бонусные тайлы
        Obstacle    // Препятствия
    }
    
    public enum MatchResult
    {
        None,           // Ничего не происходит
        BuildTower,     // Строится башня
        UpgradeTower,   // Улучшается башня
        BonusEffect,    // Бонусный эффект
        ResourceGain    // Получение ресурсов
    }

