using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class BoardState
    {
        [Header("Grid Info")]
        public int width = 5;
        public int height = 5;
        
        [Header("Tiles")]
        public Dictionary<Vector2Int, CellData> cells = new Dictionary<Vector2Int, CellData>();
                
        public BoardState(int width, int height)
        {
            this.width = width;
            this.height = height;
            // cells will be populated as tiles are placed
        }
        
        public bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < width && 
                   position.y >= 0 && position.y < height;
        }
        
        public void SetTile(Vector2Int position, CellData tileData)
        {
            if (IsValidPosition(position))
            {
                cells[position] = tileData;
            }
        }
        
        public CellData GetTile(Vector2Int position)
        {
            return cells.ContainsKey(position) ? cells[position] : null;
        }
        
        public void AddTower(Vector2Int position, string towerId, int level)
        {
            if (IsValidPosition(position))
            {
                cells[position] = new CellData
                {
                    tileId = towerId,
                    category = TileCategory.Tower,
                    level = level
                };
            }
        }
        
        public void RemoveTower(Vector2Int position)
        {
            if (cells.ContainsKey(position) && cells[position].category == TileCategory.Tower)
            {
                cells.Remove(position);
            }
        }
        
        public void AddResource(Vector2Int position, string resourceId)
        {
            if (IsValidPosition(position))
            {
                cells[position] = new CellData
                {
                    tileId = resourceId,
                    category = TileCategory.Resource,
                    level = 1
                };
            }
        }
        
        public void RemoveResource(Vector2Int position)
        {
            if (cells.ContainsKey(position) && cells[position].category == TileCategory.Resource)
            {
                cells.Remove(position);
            }
        }
    }

[Serializable]
public class CellData
    {
        public string tileId;
        public TileCategory category;
        public int level = 1; // для башен уровни, для ресурсов можно оставить 1
    }