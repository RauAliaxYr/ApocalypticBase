using UnityEngine;



public class Tile : MonoBehaviour
    {
        [Header("Tile Info")]
        public Vector2Int gridPosition;
        public bool canBeSwapped = true;
        public bool isSelected = false;
        
        [Header("Components")]
        public SpriteRenderer spriteRenderer;
        public Collider2D tileCollider;
        
        private GridController gridController;
        
        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
                
            if (tileCollider == null)
                tileCollider = GetComponent<Collider2D>();
        }
        
        public void Initialize(Vector2Int position, GridController controller)
        {
            gridPosition = position;
            gridController = controller;
            
            // Set position
            transform.position = gridController.GridToWorldPosition(position);
        }
        
        public void UpdatePosition(Vector2Int newPosition)
        {
            gridPosition = newPosition;
            transform.position = gridController.GridToWorldPosition(newPosition);
        }
        
        private void OnMouseDown()
        {
            if (gridController != null)
            {
                gridController.OnTileClicked(gridPosition);
            }
        }
        
        private void OnMouseEnter()
        {
            if (canBeSwapped)
            {
                // Highlight tile on hover
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.cyan;
                }
            }
        }
        
        private void OnMouseExit()
        {
            if (spriteRenderer != null && !isSelected)
            {
                spriteRenderer.color = Color.white;
            }
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = selected ? Color.yellow : Color.white;
            }
        }
        
        public void SetCanBeSwapped(bool swappable)
        {
            canBeSwapped = swappable;
            if (tileCollider != null)
            {
                tileCollider.enabled = swappable;
            }
        }
        
        public bool CanBeSwapped => canBeSwapped;
    }