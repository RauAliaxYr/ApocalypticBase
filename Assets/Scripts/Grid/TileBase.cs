using UnityEngine;



public abstract class TileBase : MonoBehaviour
    {
        [Header("Tile Base")]
        public Vector2Int gridPosition;

        public virtual void Initialize(Vector2Int position, GridController controller)
        {
            SyncGridPosition(position);
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
            SyncGridPosition(newPosition);
        }

        public virtual bool CanBeSwapped()
        {
            return true;
        }

        private void SyncGridPosition(Vector2Int pos)
        {
            gridPosition = pos;
            var res = GetComponent<Resource>();
            if (res != null && (object)res != (object)this)
            {
                res.gridPosition = pos;
            }
            var tower = GetComponent<Tower>();
            if (tower != null && (object)tower != (object)this)
            {
                tower.gridPosition = pos;
            }
        }
    }


