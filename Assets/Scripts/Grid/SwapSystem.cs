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
    
    [Header("Debug")]
    public bool logClicks = true;
    
    private void Awake()
    {
        mainCamera = Camera.main;
    }
    
    private void Start() { }
    
    private void Update()
    {
        if (isSwapping || gridController == null || !gridController.isGameActive || 
            (gridController.gridFiller != null && gridController.gridFiller.IsFilling)) 
        {
            return;
        }
        
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
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
        
        if (hit.collider != null)
        {
            TileBase tile = hit.collider.GetComponent<TileBase>();
            
            if (tile != null && tile.CanBeSwapped())
            {
                if (logClicks)
                {
                    Debug.Log($"Click Debug: Tile at grid {tile.gridPosition}, world {tile.transform.position}");
                }
                // If we already have a selected tile, check if we can swap
                if (selectedTile != null)
                {
                    Vector2Int newPosition = tile.gridPosition;
                    
                    // If clicking the same tile, deselect it
                    if (newPosition == selectedPosition)
                    {
                        ResetTileScale(selectedTile.transform);
                        selectedTile = null;
                        selectedPosition = Vector2Int.zero;
                        return;
                    }
                    
                    // Check if tiles are adjacent
                    if (ArePositionsAdjacent(selectedPosition, newPosition))
                    {
                        // Check if we have swaps left (optional for testing)
                        if (economyManager == null || economyManager.GetSwapsLeft() > 0)
                        {
                            StartCoroutine(PerformSwap(selectedPosition, newPosition));
                        }
                        
                        // Clear selection after swap attempt
                        ResetTileScale(selectedTile.transform);
                        selectedTile = null;
                        selectedPosition = Vector2Int.zero;
                    }
                    else
                    {
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
                    
                    // Visual feedback for selection
                    StartCoroutine(AnimateSelection(tile.transform));
                }
            }
        }
        else
        {
            // If we have a selected tile and clicked on empty space, deselect it
            if (selectedTile != null)
            {
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
        if (isSwapping) 
        {
            yield break;
        }
        
        isSwapping = true;
        
        // Get the tiles at both positions
        TileBase tileA = gridController.GetTileAt(posA);
        TileBase tileB = gridController.GetTileAt(posB);
        
        if (tileA == null || tileB == null)
        {
            isSwapping = false;
            yield break;
        }
        
        // Get world positions
        Vector3 worldPosA = gridController.GridToWorldPosition(posA);
        Vector3 worldPosB = gridController.GridToWorldPosition(posB);
        
        // Animate the swap (temporarily disable colliders to prevent extra hits)
        var colA = tileA.GetComponent<Collider2D>();
        var colB = tileB.GetComponent<Collider2D>();
        bool colAE = colA != null && colA.enabled;
        bool colBE = colB != null && colB.enabled;
        if (colA != null) colA.enabled = false;
        if (colB != null) colB.enabled = false;
        
        // Animate the swap
        yield return StartCoroutine(AnimateSwap(tileA, worldPosB, tileB, worldPosA));
        
        // Update grid state
        UpdateGridAfterSwap(posA, posB, tileA, tileB);
        
        // Use a swap (optional for testing)
        if (economyManager != null)
        {
            economyManager.UseSwap();
        }
        
        // Notify GridController about the swap
        gridController.OnPlayerSwapCompleted(posA, posB);
        
        if (colA != null) colA.enabled = colAE;
        if (colB != null) colB.enabled = colBE;
        isSwapping = false;
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
