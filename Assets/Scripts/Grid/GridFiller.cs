using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridFiller : MonoBehaviour
{
    [Header("Animation Settings")]
    public float fallSpeed = 10f;
    public float fallDelay = 0.1f;
    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("References")]
    public GridController gridController;
    
    private bool isFilling = false;
    
    public void Initialize(GridController controller)
    {
        gridController = controller;
    }
    
    // Main method to fill empty cells with falling animation
    public void FillEmptyCellsWithAnimation()
    {
        if (isFilling) return;
        
        StartCoroutine(FillEmptyCellsCoroutine());
    }
    
    // Fill empty cells starting from a specific position, expanding to whole board
    public void FillEmptyCellsWithAnimationFrom(Vector2Int start)
    {
        if (isFilling) return;
        
        StartCoroutine(FillEmptyCellsCoroutine(start));
    }
    
    // Fill specific positions (useful for after matches)
    public void FillPositionsWithAnimation(List<Vector2Int> positions)
    {
        if (isFilling) return;
        
        StartCoroutine(FillPositionsCoroutine(positions));
    }
    
    // Fill all empty cells from top to bottom
    private IEnumerator FillEmptyCellsCoroutine()
    {
        isFilling = true;
        
        // Get all empty positions
        List<Vector2Int> emptyPositions = GetEmptyPositions();
        
        if (emptyPositions.Count == 0)
        {
            isFilling = false;
            yield break;
        }
        
        // Sort by Y position (top to bottom)
        emptyPositions.Sort((a, b) => b.y.CompareTo(a.y));
        
        // Fill using round-robin per column, bottom-up
        yield return StartCoroutine(FillPositionsCoroutine(emptyPositions));
        
        isFilling = false;
    }
    
    private IEnumerator FillEmptyCellsCoroutine(Vector2Int start)
    {
        isFilling = true;
        
        // Get all empty positions ordered by distance to start (wavefront)
        List<Vector2Int> emptyPositions = GetEmptyPositions();
        if (emptyPositions.Count == 0)
        {
            isFilling = false;
            yield break;
        }
        
        // Group by Manhattan distance from start
        SortedDictionary<int, List<Vector2Int>> byDistance = GroupPositionsByDistance(emptyPositions, start);
        
        foreach (var kv in byDistance)
        {
            // For each distance ring, fill round-robin per column bottom-up
            yield return StartCoroutine(FillPositionsCoroutine(kv.Value));
        }
        
        isFilling = false;
    }
    
    // Fill specific positions with falling animation (round-robin per column, bottom-up)
    private IEnumerator FillPositionsCoroutine(List<Vector2Int> positions)
    {
        if (positions.Count == 0) yield break;
        
        // Group positions by column (X coordinate)
        Dictionary<int, List<Vector2Int>> columns = new Dictionary<int, List<Vector2Int>>();
        foreach (var pos in positions)
        {
            if (!columns.ContainsKey(pos.x))
                columns[pos.x] = new List<Vector2Int>();
            columns[pos.x].Add(pos);
        }
        
        // Sort each column bottom-to-top (ascending Y)
        foreach (var kvp in columns)
        {
            kvp.Value.Sort((a, b) => a.y.CompareTo(b.y));
        }
        
        // Track next index per column
        Dictionary<int, int> nextIndexPerColumn = new Dictionary<int, int>();
        foreach (var x in columns.Keys)
        {
            nextIndexPerColumn[x] = 0;
        }
        
        // Round-robin: one tile per column per wave, starting from bottom
        while (true)
        {
            bool anyScheduled = false;
            int pendingThisWave = 0;
            
            foreach (var kvp in columns)
            {
                int x = kvp.Key;
                List<Vector2Int> col = kvp.Value;
                int idx = nextIndexPerColumn[x];
                if (idx < col.Count)
                {
                    Vector2Int pos = col[idx];
                    nextIndexPerColumn[x] = idx + 1;
                    anyScheduled = true;
                    pendingThisWave++;
                    StartCoroutine(SpawnAndFallAt(pos, () => { pendingThisWave--; }));
                }
            }
            
            if (!anyScheduled)
                break;
            
            // Wait until all tiles of this wave reach their targets
            while (pendingThisWave > 0)
            {
                yield return null;
            }
            
            // Small delay between waves for readability
            if (fallDelay > 0f)
            {
                yield return new WaitForSeconds(fallDelay);
            }
        }
    }

    private IEnumerator SpawnAndFallAt(Vector2Int pos, System.Action onComplete)
    {
        // Create tile above the grid
        Vector3 startPosition = GetStartPositionAboveGrid(pos);
        Vector3 targetPosition = gridController.GridToWorldPosition(pos);
        
        // Create the tile
        GameObject tilePrefab = GetRandomResourcePrefab();
        if (tilePrefab != null)
        {
            GameObject tile = Instantiate(tilePrefab, startPosition, Quaternion.identity, gridController.gridContainer);
            
            // Get the tile component
            Resource resourceComponent = tile.GetComponent<Resource>();
            if (resourceComponent != null)
            {
                // Prepare definition before animation
                TileDefinition def = resourceComponent.definition;
                if (def != null)
                {
                    resourceComponent.SetDefinition(def);
                }
                
                // Animate falling first
                yield return StartCoroutine(AnimateTileFall(tile, targetPosition));
                
                // Finalize initialization after animation
                resourceComponent.Initialize(pos, gridController);
                
                // Register into board and grid maps
                if (def != null)
                {
                    gridController.boardState.AddResource(pos, def.id);
                }
                gridController.AddTileToGrid(pos, resourceComponent);
            }
            else
            {
                Debug.LogError($"GridFiller: Tile prefab {tilePrefab.name} has no Resource component!");
                Destroy(tile);
            }
        }
        
        onComplete?.Invoke();
    }

    private SortedDictionary<int, List<Vector2Int>> GroupPositionsByDistance(List<Vector2Int> positions, Vector2Int start)
    {
        SortedDictionary<int, List<Vector2Int>> result = new SortedDictionary<int, List<Vector2Int>>();
        foreach (var p in positions)
        {
            int d = Mathf.Abs(p.x - start.x) + Mathf.Abs(p.y - start.y);
            if (!result.ContainsKey(d)) result[d] = new List<Vector2Int>();
            result[d].Add(p);
        }
        return result;
    }
    
    // Animate a tile falling from start to target position
    private IEnumerator AnimateTileFall(GameObject tile, Vector3 targetPosition)
    {
        Vector3 startPosition = tile.transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float fallTime = distance / fallSpeed;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < fallTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fallTime;
            float curveValue = fallCurve.Evaluate(progress);
            
            tile.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            yield return null;
        }
        
        // Ensure final position is exact
        tile.transform.position = targetPosition;
    }
    
    // Get start position above the grid for falling tiles
    private Vector3 GetStartPositionAboveGrid(Vector2Int gridPosition)
    {
        Vector3 targetPos = gridController.GridToWorldPosition(gridPosition);
        
        // Calculate how high above the grid to start
        float gridHeight = gridController.gridHeight * gridController.cellHeight;
        float startHeight = targetPos.y + gridHeight + 2f; // Start 2 units above the top of the grid
        
        return new Vector3(targetPos.x, startHeight, targetPos.z);
    }
    
    // Get all empty positions in the grid
    private List<Vector2Int> GetEmptyPositions()
    {
        List<Vector2Int> emptyPositions = new List<Vector2Int>();
        
        for (int x = 0; x < gridController.gridWidth; x++)
        {
            for (int y = 0; y < gridController.gridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!gridController.IsPositionOccupied(pos))
                {
                    emptyPositions.Add(pos);
                }
            }
        }
        
        return emptyPositions;
    }
    
    // Get random resource prefab
    private GameObject GetRandomResourcePrefab()
    {
        if (gridController.allowedResourcePrefabs != null && gridController.allowedResourcePrefabs.Length > 0)
        {
            int idx = Random.Range(0, gridController.allowedResourcePrefabs.Length);
            return gridController.allowedResourcePrefabs[idx];
        }
        return null;
    }
    
    // Check if currently filling
    public bool IsFilling => isFilling;
    
    // Force stop filling (useful for game over scenarios)
    public void StopFilling()
    {
        StopAllCoroutines();
        isFilling = false;
    }
}
