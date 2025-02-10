using System.Collections;
using System.Collections.Generic;
//using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerMovement : MonoBehaviour
{

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float jumpForce = 300f;
    [SerializeField] private Transform leftFoot, rightFoot;
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private AudioClip jumpSound, pickupSoundHealth, pickupSoundFruit, damageSound;
    [SerializeField] private GameObject pickupEffect, dustParticles;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text fruitsCollectedText;

    [SerializeField] private int doubleJump = 1;
    private float horizontalValue; // för att röra sig vänster/höger
    private bool isGrounded;
    private bool canMove;
    private float rayDistance = 0.15f;
    private int jumpsRemaining;
    private int startingHealth = 5;
    private int currentHealth = 0;
    private int respawns = 3;
    public int fruitsCollected = 0;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private AudioSource audioSource;

    public PauseMenuController gameOverScreen;

    void Start()
    {
        canMove = true;
        currentHealth = startingHealth;
        fruitsCollectedText.text = "" + fruitsCollected;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        horizontalValue = Input.GetAxis("Horizontal");

        if (horizontalValue < 0)
        {
            FlipSprite(true);
        }
        if (horizontalValue > 0)
        {
            FlipSprite(false);
        }

        CheckIfGrounded();

        if(Input.GetButtonDown("Jump") && jumpsRemaining > 0)
        {
            Jump();
            jumpsRemaining--;
        }
        if (CheckIfGrounded()) {
             jumpsRemaining = doubleJump;
        }

        anim.SetFloat("MoveSpeed", Mathf.Abs(rb.velocity.x));
        anim.SetFloat("VerticalSpeed", rb.velocity.y);
        anim.SetBool("IsGrounded", CheckIfGrounded());
    }

    private void FixedUpdate()
    {
        if(!canMove)
        {
            return;
        }
        rb.velocity = new Vector2(horizontalValue * moveSpeed * Time.deltaTime, rb.velocity.y);
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Fruit_1P"))
        {
            Destroy(other.gameObject);
            fruitsCollected++;
            fruitsCollectedText.text = "" + fruitsCollected;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(pickupSoundFruit, 0.5f);
            Instantiate(pickupEffect, other.transform.position, Quaternion.identity);
        }
        if(other.CompareTag("Fruit_3P"))
        {
            Destroy(other.gameObject);
            fruitsCollected++;
            fruitsCollectedText.text = "" + fruitsCollected;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(pickupSoundFruit, 0.5f);
            Instantiate(pickupEffect, other.transform.position, Quaternion.identity);
        }
        if(other.CompareTag("Fruit_5P"))
        {
            Destroy(other.gameObject);
            fruitsCollected++;
            fruitsCollectedText.text = "" + fruitsCollected;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(pickupSoundFruit, 0.5f);
            Instantiate(pickupEffect, other.transform.position, Quaternion.identity);
        }
        if(other.CompareTag("HealthFruit"))
        {
            RestoreHealth(other.gameObject);
        }
    }

    private void FlipSprite(bool flip)
    {
        sr.flipX = flip;
    }

    private void Jump()
    {
        if(!CheckIfGrounded()) {
            anim.SetTrigger("DoubleJump");
        }

        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(new Vector2(0, jumpForce));
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(jumpSound, 0.3f);

        if(CheckIfGrounded()){
            Instantiate(dustParticles, transform.position, dustParticles.transform.localRotation);
        }
    }

    public void ExtraJump() {
        if(jumpsRemaining == 0) {
            jumpsRemaining++;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        UpdateHealthBar();

        if(currentHealth <= 0) {
            if(respawns <= 0) {
                gameOverScreen.GameOver();
            }
            Respawn();
        }
    }

    public void TakeKnockback(float knockbackForce, float up) {
        canMove = false;
        rb.velocity = Vector2.zero;
        rb.AddForce(new Vector2(knockbackForce, up));
        Invoke("CanMoveAgain", 0.25f);
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(damageSound, 0.5f);
    }

    private void CanMoveAgain() {
        canMove = true;
    }

    private void Respawn() {   
        currentHealth = startingHealth;
        UpdateHealthBar();
        transform.position = spawnPosition.position;
        rb.velocity = Vector2.zero;
        respawns--;
    }

    private void UpdateHealthBar() {
        healthSlider.value = currentHealth;
    }

    private void RestoreHealth(GameObject healthFruit) {
        if(currentHealth >= startingHealth) {
            return;
        } else {
            currentHealth += healthFruit.GetComponent<HealthPickup>().healingAmount;
            if(currentHealth > startingHealth)
            {
                currentHealth = startingHealth;
            }
            audioSource.PlayOneShot(pickupSoundHealth, 0.5f);
            UpdateHealthBar();
            Destroy(healthFruit);
        }
    }

    private bool CheckIfGrounded()
    {
        RaycastHit2D leftHit = Physics2D.Raycast(leftFoot.position, Vector2.down, rayDistance, whatIsGround);
        RaycastHit2D rightHit = Physics2D.Raycast(rightFoot.position, Vector2.down, rayDistance, whatIsGround);

        if(leftHit.collider != null && leftHit.collider.CompareTag("Ground") || rightHit.collider != null && rightHit.collider.CompareTag("Ground")) {
            return true;
        } else {
            return false;
        }
    }
}
