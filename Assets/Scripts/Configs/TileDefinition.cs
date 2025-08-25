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
                
        [Header("Properties")]
        public bool canBeSwapped = true;

        [Header("Resource → Tower Mapping")]
        public TowerDefinition producedTower; // если это ресурс, какую башню создаём при матч-3
    }
    
    public enum TileCategory
    {
        Resource,   // Дерево, камень, металл
        Tower,      // Башня
    }
