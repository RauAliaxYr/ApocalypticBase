using UnityEngine;



public abstract class TileBase : MonoBehaviour
    {
        [Header("Tile Base")]
        public Vector2Int gridPosition;

        public virtual void Initialize(Vector2Int position, GridController controller)
        {
            gridPosition = position;
            transform.position = controller.GridToWorldPosition(position);
            
            // Ensure we have a collider for mouse interaction
            if (GetComponent<Collider2D>() == null)
            {
                BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one; // Adjust size as needed
            }
        }

        public virtual void UpdatePosition(Vector2Int newPosition)
        {
            gridPosition = newPosition;
        }

        public virtual bool CanBeSwapped()
        {
            return true;
        }
    }


