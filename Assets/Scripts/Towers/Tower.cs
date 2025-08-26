using UnityEngine;


public abstract class Tower : TileBase
    {
        [Header("Tower Info")]
        public TileDefinition evolution;
        public int level = 1;
        public bool canBeSwapped = true;
        
        [Header("Components")]
        public SpriteRenderer spriteRenderer;
        public Transform firePoint;
        
        public override void Initialize(Vector2Int position, GridController controller)
        {
            base.Initialize(position, controller);
            // Definition will be applied separately by ApplyDefinition
        }

        public void ApplyEvolution(TileDefinition def, int towerLevel)
        {
            evolution = def;
            level = Mathf.Max(1, towerLevel);
            // Set sprite by level
            if (spriteRenderer != null && def != null && def.towerLevelSprites != null)
            {
                int idx = Mathf.Clamp(level - 1, 0, def.towerLevelSprites.Length - 1);
                var sprite = def.towerLevelSprites.Length > 0 ? def.towerLevelSprites[idx] : null;
                if (sprite != null)
                {
                    spriteRenderer.sprite = sprite;
                }
            }
        }

        public override bool CanBeSwapped()
        {
            return canBeSwapped;
        }
    }