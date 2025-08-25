using UnityEngine;


public class Enemy : MonoBehaviour
    {
        [Header("Enemy Info")]
        public EnemyDefinition definition;
        public int currentHealth;
        public bool isAlive = true;
        
        [Header("Movement")]
        public Vector2Int[] path;
        public int currentPathIndex = 0;
        public float moveSpeed = 2f;
        
        [Header("Components")]
        public SpriteRenderer spriteRenderer;
        public Collider2D enemyCollider;
        public Animator animator;
        
        [Header("State")]
        public bool isMoving = true;
        public bool isBoss = false;
        
        private Vector3 targetPosition;
        private float lastAttackTime;
        private bool hasReachedBase = false;
        
        public bool IsAlive => isAlive;
        
        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
                
            if (enemyCollider == null)
                enemyCollider = GetComponent<Collider2D>();
                
            if (animator == null)
                animator = GetComponent<Animator>();
        }
        
        public void Initialize(EnemyDefinition enemyDef, Vector2Int[] enemyPath)
        {
            definition = enemyDef;
            path = enemyPath;
            currentHealth = enemyDef.health;
            moveSpeed = enemyDef.moveSpeed;
            isBoss = enemyDef.isBoss;
            
            // Set visual properties
            if (spriteRenderer != null && enemyDef.sprite != null)
            {
                spriteRenderer.sprite = enemyDef.sprite;
                spriteRenderer.color = enemyDef.enemyColor;
            }
            
            transform.localScale = Vector3.one * enemyDef.scale;
            
            // Set initial position
            if (path.Length > 0)
            {
                transform.position = GameManager.Instance.GridController.GridToWorldPosition(path[0]);
                targetPosition = transform.position;
            }
            
            // Publish spawn event
            EventBus.Instance.Publish(new EnemySpawnedEvent
            {
                Enemy = this,
                SpawnPosition = path[0]
            });
        }
        
        private void Update()
        {
            if (!isAlive || !GameManager.Instance.isGameActive) return;
            
            if (isMoving && !hasReachedBase)
            {
                MoveAlongPath();
            }
        }
        
        private void MoveAlongPath()
        {
            if (currentPathIndex >= path.Length)
            {
                ReachedBase();
                return;
            }
            
            // Move towards current target
            Vector3 currentTarget = GameManager.Instance.GridController.GridToWorldPosition(path[currentPathIndex]);
            Vector3 direction = (currentTarget - transform.position).normalized;
            
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Check if we reached the current waypoint
            if (Vector3.Distance(transform.position, currentTarget) < 0.1f)
            {
                currentPathIndex++;
                
                // Check if we reached the base
                if (currentPathIndex >= path.Length)
                {
                    ReachedBase();
                }
            }
            
            // Update animation
            if (animator != null)
            {
                animator.SetBool("IsMoving", true);
                animator.SetFloat("MoveX", direction.x);
                animator.SetFloat("MoveY", direction.y);
            }
        }
        
        private void ReachedBase()
        {
            hasReachedBase = true;
            isMoving = false;
            
            // Damage the base
            GameManager.Instance.ProgressState.TakeDamage(definition.damage);
            
            // Publish event
            EventBus.Instance.Publish(new EnemyReachedBaseEvent
            {
                Enemy = this,
                Damage = definition.damage
            });
            
            // Destroy enemy
            Die();
        }
        
        public void TakeDamage(int damage)
        {
            if (!isAlive) return;
            
            currentHealth -= damage;
            
            // Visual feedback
            StartCoroutine(DamageFlash());
            
            // Check if dead
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        private System.Collections.IEnumerator DamageFlash()
        {
            if (spriteRenderer != null)
            {
                Color originalColor = spriteRenderer.color;
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = originalColor;
            }
        }
        
        private void Die()
        {
            if (!isAlive) return;
            
            isAlive = false;
            isMoving = false;
            
            // Publish death event
            EventBus.Instance.Publish(new EnemyDiedEvent
            {
                Enemy = this,
                GoldReward = definition.goldReward
            });
            
            // Add gold reward
            GameManager.Instance.EconomyManager.AddGold(definition.goldReward);
            
            // Play death animation
            if (animator != null)
            {
                animator.SetTrigger("Die");
                
                // Wait for animation to finish
                StartCoroutine(DestroyAfterAnimation());
            }
            else
            {
                // Destroy immediately if no animator
                Destroy(gameObject);
            }
        }
        
        private System.Collections.IEnumerator DestroyAfterAnimation()
        {
            // Wait for death animation to complete
            yield return new WaitForSeconds(1f);
            Destroy(gameObject);
        }
        
        public void SetPath(Vector2Int[] newPath)
        {
            path = newPath;
            currentPathIndex = 0;
            
            if (path.Length > 0)
            {
                transform.position = GameManager.Instance.GridController.GridToWorldPosition(path[0]);
            }
        }
        
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }
        
        public void StopMovement()
        {
            isMoving = false;
            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
            }
        }
        
        public void ResumeMovement()
        {
            isMoving = true;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (path != null && path.Length > 0)
            {
                Gizmos.color = Color.red;
                
                for (int i = 0; i < path.Length - 1; i++)
                {
                    Vector3 from = GameManager.Instance != null && GameManager.Instance.GridController != null ? 
                        GameManager.Instance.GridController.GridToWorldPosition(path[i]) : 
                        new Vector3(path[i].x, path[i].y, 0);
                        
                    Vector3 to = GameManager.Instance != null && GameManager.Instance.GridController != null ? 
                        GameManager.Instance.GridController.GridToWorldPosition(path[i + 1]) : 
                        new Vector3(path[i + 1].x, path[i + 1].y, 0);
                        
                    Gizmos.DrawLine(from, to);
                }
            }
        }
    }
    
    // Additional event for when enemy reaches base
    public class EnemyReachedBaseEvent
    {
        public Enemy Enemy;
        public int Damage;
    }