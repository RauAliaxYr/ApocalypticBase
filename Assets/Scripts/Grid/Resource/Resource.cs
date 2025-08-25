using UnityEngine;


public abstract class Resource : TileBase
    {
        [Header("Resource Info")]
        public TileDefinition definition;
        
        [Header("Components")]
        public SpriteRenderer spriteRenderer;
        public Collider2D resourceCollider;
        
        private GridController gridController;
        
        
        public void Initialize(Vector2Int position, GridController controller)
        {
            base.Initialize(position, controller);
            gridController = controller;
            
            // Set visual properties if definition is already set
            if (definition != null && spriteRenderer != null && definition.sprite != null)
            {
                spriteRenderer.sprite = definition.sprite;
            }
        }
        
        public void SetDefinition(TileDefinition resourceDef)
        {
            definition = resourceDef;
            if (spriteRenderer != null && resourceDef.sprite != null)
            {
                spriteRenderer.sprite = resourceDef.sprite;
            }
        }
                
        public bool CanBeSwapped()
        {
            return definition.canBeSwapped;
        }
}