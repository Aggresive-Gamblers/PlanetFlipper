using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SnakeBehaviour : MonoBehaviour, IDamageable
{
    [Header("R�f�rences")]
    public Animator animator;
    public HealthBar healthBar; 

    [Header("Stats d'attaque")]
    public float attackDamage = 5f;
    public float attackCooldown = 2f;
    public float attackRange = 3f;

    public GameObject Anchor;
    
    private GameObject player;
    private HealthBar playerHealthBar;
    private bool isDead = false;
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    public bool isStuck = false;

    private bool useRLMidBite = true;
    
    public float CumulativeDamage = 0f;

    public GameObject impatcpos;
    
    public void DoDamage(float damage)
    {
        //Physics.BoxCast(impatcpos.transform.position, impatcpos.transform.localScale * 4, -impatcpos.transform.right, out var hit,impatcpos.transform.rotation,2);
        
        impatcpos.SetActive(true);
        isStuck = true;

    }
    public void StopDamage()
    {
        impatcpos.SetActive(false);
    }
    
    public void Release()
    {
        isStuck = false;

        CumulativeDamage = 0;
    }
    
    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (healthBar == null)
        {
            healthBar = GetComponent<HealthBar>();
            Debug.LogWarning("[SnakeBehaviour] HealthBar non assign�, recherche automatique");
        }

        impatcpos.SetActive(false);
        CumulativeDamage = 0;


        FindPlayer();

 
        StartCoroutine(AutoAttackLoop());
    }

    void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("[SnakeBehaviour] Player non trouv� ! Tag 'Player' requis.");
        }
        else
        {
            playerHealthBar = player.GetComponent<HealthBar>();
            if (playerHealthBar == null)
            {
                Debug.LogWarning("[SnakeBehaviour] Le player n'a pas de script HealthBar !");
            }
            else
            {
                Debug.Log("[SnakeBehaviour] Player trouv� et cibl� !");
            }
        }
    }

    void FixedUpdate()
    {
        if (player == null && !isDead)
        {
            FindPlayer();
        }

        if (healthBar != null && healthBar.currentHealth <= 0 && !isDead)
        {
            Die();
        }

        if (player != null && !isDead && !isStuck)
        {
            Vector3 direction = (Anchor.transform.position - player.transform.position).normalized;
            if (direction != Vector3.zero)  
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                Anchor.transform.rotation = Quaternion.Slerp(Anchor.transform.rotation, lookRotation, 0.005f);
            }
        }
    }

    IEnumerator AutoAttackLoop()
    {
        yield return new WaitForSeconds(5f);

        while (!isDead)
        {
            if (player == null)
            {
                yield return new WaitForSeconds(0.5f);
                FindPlayer();
                continue;
            }

            
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                yield return StartCoroutine(AttackPlayer());
            }

            yield return null;
        }
    }

    IEnumerator AttackPlayer()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        if (isStuck)
        {
            //Bite-To-Bite -> Retreat-To-Standing -> Standing
            animator.SetTrigger("BiteToBite");
            
            yield return new WaitForSeconds(0.5f);
            
           
        }
        else
        {
            //R-L-Mid-Bite -> Retreat-To-Standing -> Standing
            animator.SetTrigger("BiteRL");

            yield return new WaitForSeconds(0.5f);
        }

        //useRLMidBite = !useRLMidBite;
        isAttacking = false;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        if (healthBar != null)
        {
            healthBar.TakeDamage(damage);

            CumulativeDamage  += damage;
            if (CumulativeDamage >= 4)
                animator.SetTrigger("Hit");

            Debug.Log($"[SnakeBehaviour] Sant�: {healthBar.currentHealth}/{healthBar.maxHealth}");
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        animator.SetTrigger("Dead");

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        Invoke("EndScene", 7f);
    }

    public void EndScene()
    {
        SceneManager.LoadScene("EndScene");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        DrawBoxCastGizmo(impatcpos.transform.position, impatcpos.transform.localScale * 4, impatcpos.transform.rotation , 
            -impatcpos.transform.right,2, new Color(255, 0, 0), new Color(0, 255, 0));
    }
    
    void DrawBoxCastGizmo(Vector3 origin, Vector3 halfExtents, Quaternion rotation, Vector3 direction, float distance, Color colorStart, Color colorEnd)
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;

        // --- Box de départ ---
        Gizmos.color = colorStart;
        Gizmos.matrix = Matrix4x4.TRS(origin, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);

        // --- Box de fin ---
        Vector3 endPos = origin + direction.normalized * distance;
        Gizmos.color = colorEnd;
        Gizmos.matrix = Matrix4x4.TRS(endPos, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);

        Gizmos.matrix = oldMatrix;
    }
}