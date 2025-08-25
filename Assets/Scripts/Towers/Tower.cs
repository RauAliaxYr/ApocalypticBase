using UnityEngine;


public abstract class Tower : TileBase
    {
        [Header("Tower Info")]
        public TowerDefinition definition;
        public bool canBeSwapped = true;
        
        [Header("Components")]
        public SpriteRenderer spriteRenderer;
        public Transform firePoint;
        
        public virtual void Initialize(Vector2Int position, GridController controller)
        {
            base.Initialize(position, controller);
            // Definition will be applied separately by ApplyDefinition
        }

        public void ApplyDefinition(TowerDefinition towerDef)
        {
            definition = towerDef;
            // Visual comes from prefab; keep spriteRenderer as-is or let child override
        }

        public override bool CanBeSwapped()
        {
            return canBeSwapped;
        }
    }