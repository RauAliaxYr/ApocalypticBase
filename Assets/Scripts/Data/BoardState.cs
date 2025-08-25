using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class BoardState
    {
        [Header("Grid Info")]
        public int width = 8;
        public int height = 8;
        
        [Header("Tiles")]
        public TileData[,] tiles;
        
        [Header("Towers")]
        public Dictionary<Vector2Int, TowerData> towers = new Dictionary<Vector2Int, TowerData>();
        
        [Header("Resources")]
        public Dictionary<Vector2Int, ResourceData> resources = new Dictionary<Vector2Int, ResourceData>();
        
        public BoardState(int width, int height)
        {
            this.width = width;
            this.height = height;
            tiles = new TileData[width, height];
            
            // Initialize empty grid
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[x, y] = new TileData();
                }
            }
        }
        
        public bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < width && 
                   position.y >= 0 && position.y < height;
        }
        
        public void SetTile(Vector2Int position, TileData tileData)
        {
            if (IsValidPosition(position))
            {
                tiles[position.x, position.y] = tileData;
            }
        }
        
        public TileData GetTile(Vector2Int position)
        {
            if (IsValidPosition(position))
            {
                return tiles[position.x, position.y];
            }
            return null;
        }
        
        public void AddTower(Vector2Int position, TowerData towerData)
        {
            if (IsValidPosition(position))
            {
                towers[position] = towerData;
                tiles[position.x, position.y].hasTower = true;
            }
        }
        
        public void RemoveTower(Vector2Int position)
        {
            if (towers.ContainsKey(position))
            {
                towers.Remove(position);
                tiles[position.x, position.y].hasTower = false;
            }
        }
        
        public void AddResource(Vector2Int position, ResourceData resourceData)
        {
            if (IsValidPosition(position))
            {
                resources[position] = resourceData;
                tiles[position.x, position.y].hasResource = true;
            }
        }
        
        public void RemoveResource(Vector2Int position)
        {
            if (resources.ContainsKey(position))
            {
                resources.Remove(position);
                tiles[position.x, position.y].hasResource = false;
            }
        }
    }
    
    [Serializable]
    public class TileData
    {
        public bool hasTower = false;
        public bool hasResource = false;
        public bool isWalkable = true;
        public bool isObstacle = false;
    }
    
    [Serializable]
    public class TowerData
    {
        public string towerId;
        public int level = 1;
        public int health = 100;
        public float lastAttackTime = 0f;
        public Vector2Int currentTarget = Vector2Int.one * -1;
    }
    
    [Serializable]
    public class ResourceData
    {
        public string resourceId;
        public int amount = 1;
        public bool isDepleted = false;
    }