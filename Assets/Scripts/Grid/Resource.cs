using UnityEngine;


public class Resource : MonoBehaviour
    {
        [Header("Resource Info")]
        public Vector2Int gridPosition;
        public TileDefinition definition;
        
        [Header("Components")]
        public SpriteRenderer spriteRenderer;
        public Collider2D resourceCollider;
        
        [Header("Properties")]
        public int amount = 1;
        public bool isDepleted = false;
        
        private GridController gridController;
        
        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
                
            if (resourceCollider == null)
                resourceCollider = GetComponent<Collider2D>();
        }
        
        public void Initialize(Vector2Int position, TileDefinition resourceDef, GridController controller)
        {
            gridPosition = position;
            definition = resourceDef;
            gridController = controller;
            
            // Set visual properties
            if (spriteRenderer != null && resourceDef.sprite != null)
            {
                spriteRenderer.sprite = resourceDef.sprite;
            }
            
            // Set amount
            amount = resourceDef.baseValue;
            
            // Set position
            transform.position = gridController.GridToWorldPosition(position);
        }
        
        public void UpdatePosition(Vector2Int newPosition)
        {
            gridPosition = newPosition;
            transform.position = gridController.GridToWorldPosition(newPosition);
        }
        
        public bool CanBeSwapped()
        {
            return definition.canBeSwapped && !isDepleted;
        }
        
        public void Deplete()
        {
            isDepleted = true;
            
            // Visual feedback
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 0.5f;
                spriteRenderer.color = color;
            }
            
            // Disable collider
            if (resourceCollider != null)
            {
                resourceCollider.enabled = false;
            }
        }
        
        public void Restore()
        {
            isDepleted = false;
            
            // Restore visual
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
            
            // Enable collider
            if (resourceCollider != null)
            {
                resourceCollider.enabled = true;
            }
        }
        
        public void SetAmount(int newAmount)
        {
            amount = newAmount;
            
            if (amount <= 0)
            {
                Deplete();
            }
        }
        
        public int GetAmount()
        {
            return amount;
        }
        
        public bool IsDepleted()
        {
            return isDepleted;
        }
    }