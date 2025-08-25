using UnityEngine;


public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        public float speed = 10f;
        public float lifetime = 5f;
        public int damage = 10;
        public bool isHoming = false;
        public float homingStrength = 5f;
        
        [Header("Visual Effects")]
        public GameObject hitEffect;
        public TrailRenderer trailRenderer;
        
        private Enemy target;
        private Vector3 direction;
        private float spawnTime;
        private bool hasHit = false;
        
        private void Awake()
        {
            if (trailRenderer == null)
                trailRenderer = GetComponent<TrailRenderer>();
        }
        
        public void Initialize(Enemy targetEnemy, int projectileDamage)
        {
            target = targetEnemy;
            damage = projectileDamage;
            spawnTime = Time.time;
            
            // Set initial direction
            if (target != null)
            {
                direction = (target.transform.position - transform.position).normalized;
            }
            
            // Start trail effect
            if (trailRenderer != null)
            {
                trailRenderer.enabled = true;
            }
        }
        
        private void Update()
        {
            if (hasHit) return;
            
            // Check lifetime
            if (Time.time - spawnTime > lifetime)
            {
                DestroyProjectile();
                return;
            }
            
            // Update direction if homing
            if (isHoming && target != null && target.IsAlive)
            {
                Vector3 targetDirection = (target.transform.position - transform.position).normalized;
                direction = Vector3.Lerp(direction, targetDirection, homingStrength * Time.deltaTime);
            }
            
            // Move projectile
            transform.position += direction * speed * Time.deltaTime;
            
            // Rotate to face direction
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return;
            
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null && enemy == target)
            {
                HitTarget(enemy);
            }
        }
        
        private void HitTarget(Enemy enemy)
        {
            hasHit = true;
            
            // Deal damage
            if (enemy != null && enemy.IsAlive)
            {
                enemy.TakeDamage(damage);
            }
            
            // Show hit effect
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, transform.rotation);
            }
            
            // Destroy projectile
            DestroyProjectile();
        }
        
        private void DestroyProjectile()
        {
            // Disable trail
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
            }
            
            // Destroy game object
            Destroy(gameObject);
        }
        
        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }
        
        public void SetDamage(int newDamage)
        {
            damage = newDamage;
        }
        
        public void SetHoming(bool homing, float strength = 5f)
        {
            isHoming = homing;
            homingStrength = strength;
        }
        
        public void SetLifetime(float newLifetime)
        {
            lifetime = newLifetime;
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw projectile path
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction * 2f);
            
            // Draw homing target
            if (isHoming && target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, target.transform.position);
            }
        }
    }