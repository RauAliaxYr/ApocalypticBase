using UnityEngine;



public abstract class TileBase : MonoBehaviour
    {
        [Header("Tile Base")]
        public Vector2Int gridPosition;

        public virtual void Initialize(Vector2Int position, GridController controller)
        {
            gridPosition = position;
            transform.position = controller.GridToWorldPosition(position);
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


