using System.Collections.Generic;
using UnityEngine;


public class GridController : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int gridWidth = 5;
        public int gridHeight = 5;
        public float cellSize = 1f; // Legacy - kept for compatibility
        public float cellWidth = 1f; // Width of each cell
        public float cellHeight = 1f; // Height of each cell
        public Vector3 gridOrigin = Vector3.zero;
        
        [Header("Prefabs")]
        public GameObject[] allowedResourcePrefabs;
        
        [Header("References")]
        public Transform gridContainer;
        public MatchSystem matchSystem;
        public GridFiller gridFiller;
        
        [Header("State")]
        public BoardState boardState;
        public bool isGameActive = false;
        public bool isGridPositioned = false; // New property to track if grid has been positioned
        
        // Last player swap tracking (used by MatchSystem to place towers)
        public Vector2Int? lastSwapA = null;
        public Vector2Int? lastSwapB = null;
        
        private Dictionary<Vector2Int, TileBase> gridObjects = new Dictionary<Vector2Int, TileBase>();
            
        public void Initialize()
        {
            // Create BoardState here when gridWidth and gridHeight are properly set
            boardState = new BoardState(gridWidth, gridHeight);
            
            // Check if required components are set
            if (allowedResourcePrefabs == null || allowedResourcePrefabs.Length == 0)
            {
                Debug.LogError("GridController: allowedResourcePrefabs is not set!");
                return;
            }
            
            if (gridContainer == null)
            {
                Debug.LogError("GridController: gridContainer is not set!");
                return;
            }
            
            if (matchSystem == null)
            {
                Debug.LogError("GridController: matchSystem is not set!");
                return;
            }
            
            if (gridFiller == null)
            {
                Debug.LogError("GridController: gridFiller is not set!");
                return;
            }
            
            // Initialize systems
            matchSystem.Initialize(this);
            gridFiller.Initialize(this);
            
            // Don't create grid here - wait for layout to be applied first
            // Grid will be created when ApplyLayoutFromAnchors is called
        }
        
        // New method to create the grid after layout is applied
        public void CreateGridAfterLayout()
        {
            if (boardState == null) return;
            
            // Fill grid with falling animation (initial)
            gridFiller.FillEmptyCellsWithAnimation();
        }
        
        public void StartGame()
        {
            Debug.Log("GridController: StartGame called, setting isGameActive = true");
            isGameActive = true;
        }
        
        // Fill empty cells after tiles are removed (e.g., after matches)
        public void FillAfterRemoval()
        {
            if (gridFiller != null && !gridFiller.IsFilling)
            {
                // Find empty positions and fill only them to mark as AfterRemoval
                List<Vector2Int> empties = new List<Vector2Int>();
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int y = 0; y < gridHeight; y++)
                    {
                        Vector2Int p = new Vector2Int(x, y);
                        if (!IsPositionOccupied(p))
                        {
                            empties.Add(p);
                        }
                    }
                }
                if (empties.Count > 0)
                {
                    gridFiller.FillPositionsWithAnimation(empties);
                }
            }
        }
        
        // Fill specific positions after removal
        public void FillPositionsAfterRemoval(List<Vector2Int> positions)
        {
            if (gridFiller != null && !gridFiller.IsFilling)
            {
                gridFiller.CollapseAndFillAfterRemoval(positions);
            }
        }

        // Call this after a successful swap is performed by input system
        public void OnPlayerSwapCompleted(Vector2Int a, Vector2Int b)
        {
            lastSwapA = a;
            lastSwapB = b;
            if (matchSystem != null)
            {
                matchSystem.CheckMatches();
            }
        }
        
        public void ClearLastSwap()
        {
            lastSwapA = null;
            lastSwapB = null;
        }
        
        public void FillEmptyCells()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2Int p = new Vector2Int(x, y);
                    if (!gridObjects.ContainsKey(p))
                    {
                        var prefab = GetRandomResourcePrefab();
                        if (prefab != null)
                        {
                            CreateResourceFromPrefab(p, prefab);
                        }
                    }
                }
            }
        }
        
        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return gridOrigin + new Vector3(
                gridPosition.x * cellWidth,
                gridPosition.y * cellHeight,
                0f
            );
        }
        
        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            Vector3 localPosition = worldPosition - gridOrigin;
            return new Vector2Int(
                Mathf.RoundToInt(localPosition.x / cellWidth),
                Mathf.RoundToInt(localPosition.y / cellHeight)
            );
        }
        
        public void CreateResourceFromPrefab(Vector2Int position, GameObject resourcePrefabOverride)
        {
            if (!boardState.IsValidPosition(position) || boardState.GetTile(position) != null)
            {
                Debug.LogWarning($"GridController: Cannot create resource at {position} - invalid position or already occupied");
                return;
            }

            Vector3 worldPosition = GridToWorldPosition(position);
            GameObject resource = Instantiate(resourcePrefabOverride, worldPosition, Quaternion.identity, gridContainer);

            Resource resourceComponent = resource.GetComponent<Resource>();
            if (resourceComponent != null)
            {
                // Use definition from prefab component
                TileDefinition def = resourceComponent.definition;
                resourceComponent.Initialize(position, this);
                
                // Set definition after initialize
                if (def != null)
                {
                    resourceComponent.SetDefinition(def);
                    string resId = !string.IsNullOrEmpty(def.resourceId) ? def.resourceId : null;
                    if (!string.IsNullOrEmpty(resId))
                    {
                        boardState.AddResource(position, resId);
                    }
                }

                gridObjects[position] = resourceComponent;
            }
            else
            {
                Debug.LogError($"GridController: Resource prefab {resourcePrefabOverride.name} has no Resource component!");
                Destroy(resource);
            }
        }
        
        // CreateTower method removed - towers are now created by transforming existing resources
        
        // Swapping and matching handled by external systems
        
        // Base tile accessors removed; using resource/tower maps
        
        public bool IsPositionOccupied(Vector2Int position)
        {
            return gridObjects.ContainsKey(position);
        }
        
        public TileBase GetTileAt(Vector2Int position)
        {
            if (gridObjects.ContainsKey(position))
            {
                return gridObjects[position];
            }
            return null;
        }
        
        public void AddTileToGrid(Vector2Int position, TileBase tile)
        {
            gridObjects[position] = tile;
        }
        
        // Move mapping and board state for a tile without destroying it (used by gravity)
        public void MoveTileMapping(Vector2Int from, Vector2Int to, TileBase tile, string tileId, TileCategory category, int level)
        {
            // Update board state
            if (category == TileCategory.Resource)
            {
                boardState.RemoveResource(from);
                boardState.AddResource(to, tileId);
            }
            else if (category == TileCategory.Tower)
            {
                boardState.RemoveTower(from);
                boardState.AddTower(to, tileId, level);
            }
            
            // Update visual mapping
            if (gridObjects.ContainsKey(from) && gridObjects[from] == tile)
            {
                gridObjects.Remove(from);
                gridObjects[to] = tile;
            }
        }
              
        public void RemoveTile(Vector2Int position)
        {
            if (gridObjects.ContainsKey(position))
            {
                Destroy(gridObjects[position].gameObject);
                gridObjects.Remove(position);
                // Remove from boardState as well
                boardState.RemoveResource(position); // Will remove if it's a resource
                boardState.RemoveTower(position);    // Will remove if it's a tower
            }
        }
        
        private GameObject GetRandomResourcePrefab()
        {
            if (allowedResourcePrefabs != null && allowedResourcePrefabs.Length > 0)
            {
                int idx = Random.Range(0, allowedResourcePrefabs.Length);
                return allowedResourcePrefabs[idx];
            }
            return null;
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

            // Calculate separate cell dimensions to fill the entire rectangle
            float newCellWidth = rectWidth / gridWidth;
            float newCellHeight = rectHeight / gridHeight;
            
            // Keep cellSize updated for compatibility
            cellSize = Mathf.Min(newCellWidth, newCellHeight);

            // bottom-left corner from given anchors
            Vector3 bottomLeft = new Vector3(topLeftPos.x, bottomRightPos.y, topLeftPos.z);
            Vector3 newOrigin = bottomLeft + new Vector3(newCellWidth * 0.5f, newCellHeight * 0.5f, 0f);

            cellWidth = newCellWidth;
            cellHeight = newCellHeight;
            gridOrigin = newOrigin;

            // Create grid immediately after layout is applied
            CreateGridAfterLayout();
            
            // Mark grid as positioned
            isGridPositioned = true;
        }

        public void RepositionAll()
        {
            // Reposition grid objects
            foreach (var kv in gridObjects)
            {
                if (kv.Value != null)
                {
                    kv.Value.transform.position = GridToWorldPosition(kv.Key);
                }
            }
        }
    }