using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SwapSystem - система свапов тайлов для игры "три в ряд"
/// 
/// Управление:
/// 1. Клик по тайлу - выбирает его (тайл увеличивается)
/// 2. Клик по соседнему тайлу - свапает их
/// 3. Клик по тому же тайлу - отменяет выбор
/// 4. Клик по пустому месту - отменяет выбор
/// 5. Клавиша Escape - отменяет выбор
/// 
/// Свап возможен только между соседними тайлами по горизонтали или вертикали
/// </summary>

public class SwapSystem : MonoBehaviour
{
    [Header("Swap Settings")]
    public float swapAnimationDuration = 0.3f;
    public AnimationCurve swapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Input")]
    public LayerMask tileLayerMask = -1;
    public float maxSwapDistance = 1.5f; // Maximum distance for adjacent tiles
    
    [Header("References")]
    public GridController gridController;
    public EconomyManager economyManager;
    
    public Camera mainCamera;
    private TileBase selectedTile;
    private Vector2Int selectedPosition;
    private bool isSwapping = false;
    
    private void Awake()
    {
        Debug.Log("SwapSystem: Awake called");
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("SwapSystem: Camera.main is null, searching for any camera");
        }
        
        if (mainCamera != null)
        {
            Debug.Log($"SwapSystem: Camera found: {mainCamera.name}");
        }
        else
        {
            Debug.LogError("SwapSystem: No camera found!");
        }
    }
    
    private void Start()
    {
        Debug.Log("SwapSystem: Start called");
        
        // Dependencies should be set in inspector
        if (gridController == null)
        {
            Debug.LogError("SwapSystem: gridController reference is not set in inspector!");
        }
        else
        {
            Debug.Log($"SwapSystem: gridController found: {gridController.name}");
        }
        
        if (economyManager == null)
        {
            Debug.LogWarning("SwapSystem: economyManager reference is not set in inspector - swaps will be unlimited for testing");
        }
        else
        {
            Debug.Log($"SwapSystem: economyManager found: {economyManager.name}");
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("SwapSystem: mainCamera is null!");
        }
        else
        {
            Debug.Log($"SwapSystem: mainCamera found: {mainCamera.name}");
        }
    }
    
    private void Update()
    {
        if (isSwapping || gridController == null || !gridController.isGameActive) 
        {
            if (gridController == null)
                Debug.LogWarning("SwapSystem: gridController is null");
            else if (!gridController.isGameActive)
                Debug.LogWarning("SwapSystem: grid is not active");
            return;
        }
        
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("SwapSystem: Mouse button down");
            SelectTile();
        }
        
        // Cancel selection with Escape key
        if (Input.GetKeyDown(KeyCode.Escape) && selectedTile != null)
        {
            Debug.Log("SwapSystem: Escape pressed, canceling selection");
            ResetTileScale(selectedTile.transform);
            selectedTile = null;
            selectedPosition = Vector2Int.zero;
        }
    }
    
    private void SelectTile()
    {
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Mathf.Infinity, tileLayerMask);
        
        Debug.Log($"SwapSystem: SelectTile - Mouse: {mousePosition}, Ray: {ray.origin} -> {ray.direction}, Hit: {hit.collider != null}");
        
        if (hit.collider != null)
        {
            TileBase tile = hit.collider.GetComponent<TileBase>();
            Debug.Log($"SwapSystem: Found component: {tile != null}, CanBeSwapped: {tile?.CanBeSwapped()}");
            
            if (tile != null && tile.CanBeSwapped())
            {
                // If we already have a selected tile, check if we can swap
                if (selectedTile != null)
                {
                    Vector2Int newPosition = tile.gridPosition;
                    Debug.Log($"SwapSystem: Second tile clicked at position {newPosition}, selected position: {selectedPosition}");
                    
                    // If clicking the same tile, deselect it
                    if (newPosition == selectedPosition)
                    {
                        Debug.Log("SwapSystem: Same tile clicked, deselecting");
                        ResetTileScale(selectedTile.transform);
                        selectedTile = null;
                        selectedPosition = Vector2Int.zero;
                        return;
                    }
                    
                    // Check if tiles are adjacent
                    if (ArePositionsAdjacent(selectedPosition, newPosition))
                    {
                        Debug.Log("SwapSystem: Tiles are adjacent, attempting swap");
                        
                        // Check if we have swaps left (optional for testing)
                        if (economyManager == null || economyManager.GetSwapsLeft() > 0)
                        {
                            if (economyManager != null)
                            {
                                Debug.Log($"SwapSystem: Swaps left: {economyManager.GetSwapsLeft()}, starting swap");
                            }
                            else
                            {
                                Debug.Log("SwapSystem: No economy manager - unlimited swaps for testing");
                            }
                            StartCoroutine(PerformSwap(selectedPosition, newPosition));
                        }
                        else
                        {
                            Debug.Log("SwapSystem: No swaps left!");
                            // Could show UI message here
                        }
                        
                        // Clear selection after swap attempt
                        ResetTileScale(selectedTile.transform);
                        selectedTile = null;
                        selectedPosition = Vector2Int.zero;
                    }
                    else
                    {
                        Debug.Log($"SwapSystem: Tiles are not adjacent! Delta: ({Mathf.Abs(selectedPosition.x - newPosition.x)}, {Mathf.Abs(selectedPosition.y - newPosition.y)})");
                        Debug.Log("SwapSystem: Replacing selection with new tile");
                        
                        // Replace selection with new tile
                        ResetTileScale(selectedTile.transform); // Reset old selection
                        selectedTile = tile;
                        selectedPosition = newPosition;
                        StartCoroutine(AnimateSelection(tile.transform));
                    }
                }
                else
                {
                    // First tile selection
                    selectedTile = tile;
                    selectedPosition = tile.gridPosition;
                    Debug.Log($"SwapSystem: First tile selected at position {selectedPosition}");
                    
                    // Visual feedback for selection
                    StartCoroutine(AnimateSelection(tile.transform));
                }
            }
        }
        else
        {
            Debug.Log("SwapSystem: No collider hit - check layer mask and colliders");
            
            // If we have a selected tile and clicked on empty space, deselect it
            if (selectedTile != null)
            {
                Debug.Log("SwapSystem: Clicked on empty space, deselecting current tile");
                ResetTileScale(selectedTile.transform);
                selectedTile = null;
                selectedPosition = Vector2Int.zero;
            }
        }
    }
    

    
    private bool ArePositionsAdjacent(Vector2Int pos1, Vector2Int pos2)
    {
        int deltaX = Mathf.Abs(pos1.x - pos2.x);
        int deltaY = Mathf.Abs(pos1.y - pos2.y);
        
        // Adjacent means exactly one step in X or Y direction (not diagonal)
        return (deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1);
    }
    
    private IEnumerator PerformSwap(Vector2Int posA, Vector2Int posB)
    {
        Debug.Log($"SwapSystem: PerformSwap started - posA: {posA}, posB: {posB}");
        
        if (isSwapping) 
        {
            Debug.Log("SwapSystem: Already swapping, aborting");
            yield break;
        }
        
        isSwapping = true;
        Debug.Log("SwapSystem: Swap in progress");
        
        // Get the tiles at both positions
        TileBase tileA = gridController.GetTileAt(posA);
        TileBase tileB = gridController.GetTileAt(posB);
        
        Debug.Log($"SwapSystem: Tiles found - tileA: {tileA != null}, tileB: {tileB != null}");
        
        if (tileA == null || tileB == null)
        {
            Debug.LogError($"SwapSystem: Cannot find tiles - tileA: {tileA != null}, tileB: {tileB != null}");
            isSwapping = false;
            yield break;
        }
        
        // Get world positions
        Vector3 worldPosA = gridController.GridToWorldPosition(posA);
        Vector3 worldPosB = gridController.GridToWorldPosition(posB);
        
        Debug.Log($"SwapSystem: World positions - posA: {worldPosA}, posB: {worldPosB}");
        
        // Animate the swap
        Debug.Log("SwapSystem: Starting swap animation");
        yield return StartCoroutine(AnimateSwap(tileA, worldPosB, tileB, worldPosA));
        
        // Update grid state
        Debug.Log("SwapSystem: Updating grid state");
        UpdateGridAfterSwap(posA, posB, tileA, tileB);
        
        // Use a swap (optional for testing)
        if (economyManager != null)
        {
            Debug.Log($"SwapSystem: Using swap, remaining: {economyManager.GetSwapsLeft() - 1}");
            economyManager.UseSwap();
        }
        else
        {
            Debug.Log("SwapSystem: No economy manager - swap used without tracking");
        }
        
        // Notify GridController about the swap
        Debug.Log("SwapSystem: Notifying GridController");
        gridController.OnPlayerSwapCompleted(posA, posB);
        
        isSwapping = false;
        Debug.Log("SwapSystem: Swap completed");
    }
    
    private IEnumerator AnimateSwap(TileBase tileA, Vector3 targetPosA, TileBase tileB, Vector3 targetPosB)
    {
        float elapsed = 0f;
        Vector3 startPosA = tileA.transform.position;
        Vector3 startPosB = tileB.transform.position;
        
        while (elapsed < swapAnimationDuration)
        {
            float t = elapsed / swapAnimationDuration;
            float curveValue = swapCurve.Evaluate(t);
            
            tileA.transform.position = Vector3.Lerp(startPosA, targetPosA, curveValue);
            tileB.transform.position = Vector3.Lerp(startPosB, targetPosB, curveValue);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final positions are exact
        tileA.transform.position = targetPosA;
        tileB.transform.position = targetPosB;
    }
    
    private void UpdateGridAfterSwap(Vector2Int posA, Vector2Int posB, TileBase tileA, TileBase tileB)
    {
        // Update tile positions
        tileA.UpdatePosition(posB);
        tileB.UpdatePosition(posA);
        
        // Update grid mapping
        gridController.AddTileToGrid(posA, tileB);
        gridController.AddTileToGrid(posB, tileA);
        
        // Update board state
        var cellA = gridController.boardState.GetTile(posA);
        var cellB = gridController.boardState.GetTile(posB);
        
        if (cellA != null && cellB != null)
        {
            // Swap the board state data
            string tempId = cellA.tileId;
            TileCategory tempCategory = cellA.category;
            int tempLevel = cellA.level;
            
            if (cellA.category == TileCategory.Resource)
            {
                gridController.boardState.RemoveResource(posA);
                gridController.boardState.AddResource(posA, cellB.tileId);
            }
            else
            {
                gridController.boardState.RemoveTower(posA);
                gridController.boardState.AddTower(posA, cellB.tileId, cellB.level);
            }
            
            if (cellB.category == TileCategory.Resource)
            {
                gridController.boardState.RemoveResource(posB);
                gridController.boardState.AddResource(posB, tempId);
            }
            else
            {
                gridController.boardState.RemoveTower(posB);
                gridController.boardState.AddTower(posB, tempId, tempLevel);
            }
        }
    }
    
    private IEnumerator AnimateSelection(Transform tileTransform)
    {
        Vector3 originalScale = tileTransform.localScale;
        Vector3 targetScale = originalScale * 1.2f; // Slightly larger for better visibility
        
        float duration = 0.1f;
        float elapsed = 0f;
        
        // Scale up to selection size
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            tileTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Keep the tile at selection size - it will be reset when deselected
        tileTransform.localScale = targetScale;
    }
    
    // Method to reset tile scale when deselected
    public void ResetTileScale(Transform tileTransform)
    {
        if (tileTransform != null)
        {
            tileTransform.localScale = Vector3.one;
        }
    }
    
    // Dependencies are set through inspector
}
