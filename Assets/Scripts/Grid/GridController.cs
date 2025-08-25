using System.Collections.Generic;
using UnityEngine;


public class GridController : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int gridWidth = 8;
        public int gridHeight = 8;
        public float cellSize = 1f;
        public Vector3 gridOrigin = Vector3.zero;
        
        [Header("Prefabs")]
        public GameObject tilePrefab;
        public GameObject resourcePrefab;
        public GameObject towerPrefab;
        
        [Header("References")]
        public Transform gridContainer;
        public MatchSystem matchSystem;
        
        [Header("State")]
        public BoardState boardState;
        public bool isGameActive = false;
        
        private Dictionary<Vector2Int, GameObject> tileObjects = new Dictionary<Vector2Int, GameObject>();
        private Dictionary<Vector2Int, GameObject> resourceObjects = new Dictionary<Vector2Int, GameObject>();
        private Dictionary<Vector2Int, GameObject> towerObjects = new Dictionary<Vector2Int, GameObject>();
        
        private Vector2Int selectedTile = Vector2Int.one * -1;
        private bool isDragging = false;
        
        private void Awake()
        {
            boardState = new BoardState(gridWidth, gridHeight);
        }
        
        public void Initialize()
        {
            CreateGrid();
            PopulateGrid();
            matchSystem.Initialize(this);
        }
        
        public void StartGame()
        {
            isGameActive = true;
        }
        
        private void CreateGrid()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 worldPosition = GridToWorldPosition(new Vector2Int(x, y));
                    GameObject tile = Instantiate(tilePrefab, worldPosition, Quaternion.identity, gridContainer);
                    tile.name = $"Tile_{x}_{y}";
                    
                    tileObjects[new Vector2Int(x, y)] = tile;
                    
                    // Add tile component
                    Tile tileComponent = tile.GetComponent<Tile>();
                    if (tileComponent != null)
                    {
                        tileComponent.Initialize(new Vector2Int(x, y), this);
                    }
                }
            }
        }
        
        private void PopulateGrid()
        {
            // Populate with initial resources and towers
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    
                    // Random resource placement
                    if (Random.Range(0f, 1f) < 0.3f)
                    {
                        CreateResource(position, GetRandomResourceDefinition());
                    }
                    
                    // Random tower placement
                    if (Random.Range(0f, 1f) < 0.1f)
                    {
                        CreateTower(position, GetRandomTowerDefinition());
                    }
                }
            }
        }
        
        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return gridOrigin + new Vector3(
                gridPosition.x * cellSize,
                gridPosition.y * cellSize,
                0f
            );
        }
        
        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            Vector3 localPosition = worldPosition - gridOrigin;
            return new Vector2Int(
                Mathf.RoundToInt(localPosition.x / cellSize),
                Mathf.RoundToInt(localPosition.y / cellSize)
            );
        }
        
        public void CreateResource(Vector2Int position, TileDefinition resourceDef)
        {
            if (!boardState.IsValidPosition(position) || boardState.GetTile(position).hasResource)
                return;
                
            Vector3 worldPosition = GridToWorldPosition(position);
            GameObject resource = Instantiate(resourcePrefab, worldPosition, Quaternion.identity, gridContainer);
            
            Resource resourceComponent = resource.GetComponent<Resource>();
            if (resourceComponent != null)
            {
                resourceComponent.Initialize(position, resourceDef, this);
            }
            
            resourceObjects[position] = resource;
            
            ResourceData resourceData = new ResourceData
            {
                resourceId = resourceDef.id,
                amount = resourceDef.baseValue
            };
            
            boardState.AddResource(position, resourceData);
        }
        
        public void CreateTower(Vector2Int position, TowerDefinition towerDef)
        {
            if (!boardState.IsValidPosition(position) || boardState.GetTile(position).hasTower)
                return;
                
            Vector3 worldPosition = GridToWorldPosition(position);
            GameObject tower = Instantiate(towerPrefab, worldPosition, Quaternion.identity, gridContainer);
            
            Tower towerComponent = tower.GetComponent<Tower>();
            if (towerComponent != null)
            {
                towerComponent.Initialize(position, towerDef, this);
            }
            
            towerObjects[position] = tower;
            
            TowerData towerData = new TowerData
            {
                towerId = towerDef.id,
                level = towerDef.level
            };
            
            boardState.AddTower(position, towerData);
        }
        
        public bool TrySwapTiles(Vector2Int from, Vector2Int to)
        {
            if (!boardState.IsValidPosition(from) || !boardState.IsValidPosition(to))
                return false;
                
            if (from == to)
                return false;
                
            // Check if tiles can be swapped
            if (!CanSwapTiles(from, to))
                return false;
                
            // Perform swap
            SwapTiles(from, to);
            
            // Check for matches
            matchSystem.CheckMatches();
            
            // Publish event
            EventBus.Instance.Publish(new TileSwappedEvent
            {
                FromPosition = from,
                ToPosition = to,
                FromTile = GetTileAt(from),
                ToTile = GetTileAt(to)
            });
            
            return true;
        }
        
        private bool CanSwapTiles(Vector2Int from, Vector2Int to)
        {
            // Check if both positions have swappable tiles
            Tile fromTile = GetTileAt(from);
            Tile toTile = GetTileAt(to);
            
            if (fromTile == null || toTile == null)
                return false;
                
            if (!fromTile.CanBeSwapped || !toTile.CanBeSwapped)
                return false;
                
            return true;
        }
        
        private void SwapTiles(Vector2Int from, Vector2Int to)
        {
            // Swap tile objects
            GameObject fromObj = GetTileObjectAt(from);
            GameObject toObj = GetTileObjectAt(to);
            
            if (fromObj != null && toObj != null)
            {
                Vector3 fromPos = fromObj.transform.position;
                Vector3 toPos = toObj.transform.position;
                
                fromObj.transform.position = toPos;
                toObj.transform.position = fromPos;
                
                // Update tile positions
                Tile fromTile = fromObj.GetComponent<Tile>();
                Tile toTile = toObj.GetComponent<Tile>();
                
                if (fromTile != null) fromTile.UpdatePosition(to);
                if (toTile != null) toTile.UpdatePosition(from);
            }
        }
        
        public Tile GetTileAt(Vector2Int position)
        {
            if (tileObjects.ContainsKey(position))
            {
                return tileObjects[position].GetComponent<Tile>();
            }
            return null;
        }
        
        public GameObject GetTileObjectAt(Vector2Int position)
        {
            if (tileObjects.ContainsKey(position))
            {
                return tileObjects[position];
            }
            return null;
        }
        
        public bool IsPositionOccupied(Vector2Int position)
        {
            return boardState.GetTile(position).hasTower || 
                   boardState.GetTile(position).hasResource;
        }
        
        private TileDefinition GetRandomResourceDefinition()
        {
            // This would be populated from ScriptableObjects
            // For now, return a default
            return ScriptableObject.CreateInstance<TileDefinition>();
        }
        
        private TowerDefinition GetRandomTowerDefinition()
        {
            // This would be populated from ScriptableObjects
            // For now, return a default
            return ScriptableObject.CreateInstance<TowerDefinition>();
        }

        // Layout API
        public void ApplyLayoutFromAnchors(Transform topLeft, Transform bottomRight)
        {
            if (topLeft == null || bottomRight == null)
            {
                return;
            }

            Vector3 topLeftPos = topLeft.position;
            Vector3 bottomRightPos = bottomRight.position;

            float rectWidth = Mathf.Abs(bottomRightPos.x - topLeftPos.x);
            float rectHeight = Mathf.Abs(topLeftPos.y - bottomRightPos.y);

            if (rectWidth <= 0.0001f || rectHeight <= 0.0001f)
            {
                return;
            }

            float newCellSize = Mathf.Min(rectWidth / gridWidth, rectHeight / gridHeight);

            // bottom-left corner from given anchors
            Vector3 bottomLeft = new Vector3(topLeftPos.x, bottomRightPos.y, topLeftPos.z);
            Vector3 newOrigin = bottomLeft + new Vector3(newCellSize * 0.5f, newCellSize * 0.5f, 0f);

            cellSize = newCellSize;
            gridOrigin = newOrigin;

            RepositionAll();
        }

        public void RepositionAll()
        {
            // Reposition base tiles
            foreach (var kv in tileObjects)
            {
                Tile tile = kv.Value != null ? kv.Value.GetComponent<Tile>() : null;
                if (tile != null)
                {
                    tile.UpdatePosition(tile.gridPosition);
                }
            }

            // Reposition resources
            foreach (var kv in resourceObjects)
            {
                if (kv.Value != null)
                {
                    kv.Value.transform.position = GridToWorldPosition(kv.Key);
                }
            }

            // Reposition towers
            foreach (var kv in towerObjects)
            {
                if (kv.Value != null)
                {
                    kv.Value.transform.position = GridToWorldPosition(kv.Key);
                }
            }
        }
        
        public void OnTileClicked(Vector2Int position)
        {
            if (!isGameActive) return;
            
            if (selectedTile == Vector2Int.one * -1)
            {
                // First tile selected
                selectedTile = position;
                HighlightTile(position, true);
            }
            else
            {
                // Second tile selected - try to swap
                if (selectedTile != position)
                {
                    if (TrySwapTiles(selectedTile, position))
                    {
                        // Swap successful
                        GameManager.Instance.EconomyManager.UseSwap();
                    }
                }
                
                // Clear selection
                HighlightTile(selectedTile, false);
                selectedTile = Vector2Int.one * -1;
            }
        }
        
        private void HighlightTile(Vector2Int position, bool highlight)
        {
            if (tileObjects.ContainsKey(position))
            {
                // Add highlighting logic here
                SpriteRenderer renderer = tileObjects[position].GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = highlight ? Color.yellow : Color.white;
                }
            }
        }
    }