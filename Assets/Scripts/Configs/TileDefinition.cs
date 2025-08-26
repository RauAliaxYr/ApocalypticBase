using UnityEngine;


[CreateAssetMenu(fileName = "New Tile", menuName = "Apocalyptic Base/Configs/Tile Definition")]
public class TileDefinition : ScriptableObject
    {
        
        [Header("Category")]
        public TileCategory category;
                
        [Header("Properties")]
        public bool canBeSwapped = true;

        [Header("Evolution (Resource → Tower Levels)")]
        // Unique ids to separate matches between resource and each tower level
        public string resourceId;               // id ресурса (используется пока тайл ресурс)
        public string[] towerIds;               // id для каждого уровня башни (index = level-1)
        public int maxTowerLevel = 3;           // максимальный уровень башни

        [Header("Visuals")]
        public Sprite resourceSprite;           // спрайт ресурса
        public Sprite[] towerLevelSprites;      // спрайты для уровней башни (index = level-1)

    }
    
    public enum TileCategory
    {
        Resource,   // Дерево, камень, металл
        Tower,      // Башня
    }
