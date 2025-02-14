using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGroundMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float playerBounce = 100.0f;
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float knockback = 200f;
    [SerializeField] private float upKnockback = 80f;

    [SerializeField] private GameObject enemyHitParticles;

    private SpriteRenderer sp;
    private bool canMove = true;

    private void Start()
    {
        sp = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        if(!canMove)
        {
            return;
        }

        transform.Translate(new Vector2 (moveSpeed, 0) * Time.deltaTime);
        
        if(moveSpeed > 0)
        {
            sp.flipX = true;
        }
        if(moveSpeed < 0)
        {
            sp.flipX = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if(other.gameObject.CompareTag("EnemyBlock"))
        {
            moveSpeed = -moveSpeed;   
        }
        if(other.gameObject.CompareTag("Enemy"))
        {
            moveSpeed = -moveSpeed;
        }
        if(other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<PlayerMovement>().TakeDamage(attackDamage);
            
            if(other.transform.position.x > transform.position.x)
            {
                other.gameObject.GetComponent<PlayerMovement>().TakeKnockback(knockback, upKnockback);
            } else {
                other.gameObject.GetComponent<PlayerMovement>().TakeKnockback(-knockback, upKnockback);
            }
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {   
            other.GetComponent<Rigidbody2D>().velocity = new Vector2(other.GetComponent<Rigidbody2D>().velocity.x, 0);
            other.GetComponent<Rigidbody2D>().AddForce(new Vector2(0, playerBounce));
            other.gameObject.GetComponent<PlayerMovement>().AddExtraJump();

            GetComponent<Animator>().SetTrigger("Hit");
            GetComponent<BoxCollider2D>().enabled = false;
            GetComponent<CapsuleCollider2D>().enabled = false;
            GetComponent<Rigidbody2D>().gravityScale = 0;
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            canMove = false;
            Invoke("DeadParticles", 0.6f);
            Destroy(gameObject, 0.6f);
        }
    }

    private void DeadParticles()
    {
        Instantiate(enemyHitParticles, new Vector3(0, 0.5f, 0) + transform.position, enemyHitParticles.transform.localRotation);
    }
}
