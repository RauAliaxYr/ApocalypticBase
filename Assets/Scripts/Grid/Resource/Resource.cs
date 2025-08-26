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
            if (definition != null && spriteRenderer != null)
            {
                if (definition.resourceSprite != null)
                {
                    spriteRenderer.sprite = definition.resourceSprite;
                }
            }
        }
        
        public void SetDefinition(TileDefinition resourceDef)
        {
            definition = resourceDef;
            if (spriteRenderer != null)
            {
                if (resourceDef.resourceSprite != null)
                {
                    spriteRenderer.sprite = resourceDef.resourceSprite;
                }
            }
        }
                
        public bool CanBeSwapped()
        {
            return definition.canBeSwapped;
        }
}