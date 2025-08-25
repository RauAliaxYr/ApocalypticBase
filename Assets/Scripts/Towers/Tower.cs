using UnityEngine;


public class Tower : MonoBehaviour
    {
        [Header("Tower Info")]
        public Vector2Int gridPosition;
        public TowerDefinition definition;
        public TowerData data;
        
        [Header("Components")]
        public SpriteRenderer spriteRenderer;
        public Transform firePoint;
        public GameObject projectilePrefab;
        
        [Header("Combat")]
        public float attackRange = 3f;
        public LayerMask enemyLayerMask;
        
        private GridController gridController;
        private Enemy currentTarget;
        private float lastAttackTime;
        private bool isInitialized = false;
        
        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
                
            if (firePoint == null)
                firePoint = transform;
        }
        
        public void Initialize(Vector2Int position, TowerDefinition towerDef, GridController controller)
        {
            gridPosition = position;
            definition = towerDef;
            gridController = controller;
            
            // Initialize tower data
            data = new TowerData
            {
                towerId = towerDef.id,
                level = towerDef.level,
                health = 100
            };
            
            // Set visual properties
            if (spriteRenderer != null && towerDef.sprite != null)
            {
                spriteRenderer.sprite = towerDef.sprite;
            }
            
            // Set attack properties
            attackRange = towerDef.range;
            projectilePrefab = towerDef.projectilePrefab;
            
            isInitialized = true;
        }
        
        private void Update()
        {
            if (!isInitialized || !GameManager.Instance.isGameActive) return;
            
            // Check if we need a new target
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                currentTarget = FindTarget();
            }
            
            // Attack if we can
            if (CanAttack())
            {
                Attack();
            }
        }
        
        private bool IsTargetValid(Enemy enemy)
        {
            if (enemy == null || !enemy.IsAlive)
                return false;
                
            // Check if enemy is in attack range
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance > attackRange)
                return false;
                
            // Check if enemy is in attack pattern
            Vector2Int enemyGridPos = gridController.WorldToGridPosition(enemy.transform.position);
            if (!definition.attackPattern.ContainsCell(enemyGridPos - gridPosition))
                return false;
                
            return true;
        }
        
        private Enemy FindTarget()
        {
            // Find enemies in range
            Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(transform.position, attackRange, enemyLayerMask);
            
            Enemy closestEnemy = null;
            float closestDistance = float.MaxValue;
            
            foreach (Collider2D enemyCollider in enemiesInRange)
            {
                Enemy enemy = enemyCollider.GetComponent<Enemy>();
                if (enemy != null && enemy.IsAlive)
                {
                    float distance = Vector2.Distance(transform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy;
                    }
                }
            }
            
            return closestEnemy;
        }
        
        private bool CanAttack()
        {
            if (currentTarget == null) return false;
            
            return Time.time >= lastAttackTime + definition.attackInterval;
        }
        
        private void Attack()
        {
            if (currentTarget == null) return;
            
            // Create projectile
            if (projectilePrefab != null)
            {
                Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
                GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
                
                Projectile projectileComponent = projectile.GetComponent<Projectile>();
                if (projectileComponent != null)
                {
                    projectileComponent.Initialize(currentTarget, definition.damage);
                }
            }
            
            // Update attack time
            lastAttackTime = Time.time;
            
            // Update tower data
            data.lastAttackTime = lastAttackTime;
            data.currentTarget = gridController.WorldToGridPosition(currentTarget.transform.position);
        }
        
        public void Upgrade(TowerDefinition newDefinition)
        {
            definition = newDefinition;
            data.level = newDefinition.level;
            
            // Update visual properties
            if (spriteRenderer != null && newDefinition.sprite != null)
            {
                spriteRenderer.sprite = newDefinition.sprite;
            }
            
            // Update attack properties
            attackRange = newDefinition.range;
            projectilePrefab = newDefinition.projectilePrefab;
            
            // Publish upgrade event
            EventBus.Instance.Publish(new TowerUpgradedEvent
            {
                Position = gridPosition,
                OldTower = definition,
                NewTower = newDefinition
            });
        }
        
        public void TakeDamage(int damage)
        {
            data.health -= damage;
            
            if (data.health <= 0)
            {
                DestroyTower();
            }
        }
        
        private void DestroyTower()
        {
            // Remove from grid
            if (gridController != null)
            {
                gridController.boardState.RemoveTower(gridPosition);
            }
            
            // Destroy game object
            Destroy(gameObject);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw attack pattern
            if (definition != null && definition.attackPattern != null)
            {
                Gizmos.color = Color.yellow;
                foreach (Vector2Int cell in definition.attackPattern.attackCells)
                {
                    Vector3 worldPos = gridController != null ? 
                        gridController.GridToWorldPosition(gridPosition + cell) : 
                        transform.position + new Vector3(cell.x, cell.y, 0);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
                }
            }
        }
    }