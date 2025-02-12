using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerMovement : MonoBehaviour
{

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float jumpForce = 380f; //1 ruta height lite drygt för 2 och inte 3 i dubbel
    [SerializeField] private Transform leftFoot, rightFoot;
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private AudioClip jumpSound, pickupSoundHealth, pickupSoundFruit, damageSound;
    [SerializeField] private GameObject pickupEffect, dustParticles;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text fruitsCollectedText;
    [SerializeField] private TMP_Text respawnsText;

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


    //------------[ nya för SpelDesign ]------------//
    // Constants
    [SerializeField] private float normGravityScale = -9.81f;
    [SerializeField] private float maxFallSpeed = 50;
    [SerializeField] private float jumpHangTimeThreshold = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;

    // States
    public bool isFacingRight = true;
    public bool isJumping = false;
    public bool isWallJumping = false;
    public bool isDashing = false;
    public bool isSliding = false;

    // Timers
    public float lastOnGroundTime = 0f;
    public float lastOnWallTime = 0f;
    public float lastOnWallRightTime = 0f;
    public float lastOnWallLeftTime = 0f;

    // Jump
    private bool _isJumpCut;
    private bool _isJumpFalling = false;
    private float jumpInputBufferTime = 0.1f;

    // Wall Jump
    private float _wallJumpStartTime = 0f;
    private int _lastWallJumpDir;
    private float wallJumpTime = 0.1f;

    // Dash
    private int _dashesLeft;
    private bool _dashRefilling;
    private int _lastDashDir;
    
    // Input
    public float lastPressedJumpTime;
    public float lastPressedDashTime;

    // Check
    [SerializeField] private Transform _frontWallCheckPoint;
    [SerializeField] private Transform _backWallCheckPoint;
    [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);
    //----------------[ END ]----------------//


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
        respawnsText.text = "x " + respawns;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        //_______________TIMERS_______________
        lastOnGroundTime -= Time.deltaTime;
        lastOnWallTime -= Time.deltaTime;
        lastOnWallRightTime -= Time.deltaTime;
        lastOnWallLeftTime -= Time.deltaTime;

        lastPressedJumpTime -= Time.deltaTime;
        lastPressedDashTime -= Time.deltaTime;
        //____________________________________

        horizontalValue = Input.GetAxis("Horizontal");
        if(Input.GetButtonDown("Jump") && jumpsRemaining > 0) {
            OnJumpInput();
            OnJumpUpInput(); // kolla vad gör
        }

        //______________CHECKS____________
        CheckHorizontalValue();
        if(!isJumping) {
            CheckIfGrounded();
            RightWallCheck();
            LeftWallCheck();
            lastOnWallTime = Mathf.Max(lastOnWallLeftTime, lastOnWallRightTime); //checkPoints kommer att vända när spelaren vänder, därav båda checksen
        }
        if(isJumping && rb.velocity.y < 0) {
            isJumping = false;

            if(!isWallJumping) {
                _isJumpFalling = true;
            }
        }
        if(isWallJumping && Time.time - _wallJumpStartTime > wallJumpTime) {
            isWallJumping = false;
        }
        if(lastOnGroundTime > 0 && !isJumping && !isWallJumping) {
            _isJumpCut = false;

            if(!isJumping) {
                _isJumpFalling = false;
            }
        }
        if(lastOnGroundTime > 0 && !isJumping && lastPressedJumpTime > 0) {
            isJumping = true;
			isWallJumping = false;
			_isJumpCut = false;
			_isJumpFalling = false;
			Jump();
        } else if (CanWallJump() && lastPressedJumpTime > 0)
		{
			isWallJumping = true;
			isJumping = false;
			_isJumpCut = false;
			_isJumpFalling = false;
			_wallJumpStartTime = Time.time;
			_lastWallJumpDir = (lastOnWallRightTime > 0) ? -1 : 1;
			
			WallJump(_lastWallJumpDir);
		}
        //________________________________
        /*
        if(Input.GetButtonDown("Jump") && jumpsRemaining > 0) {
            Jump();
            jumpsRemaining--;
        }
        if (CheckIfGrounded()) {
             jumpsRemaining = doubleJump;
        }

        if(Input.GetKeyDown(KeyCode.X) && _dashesLeft > 0) {
            Dash();
            _dashesLeft--;
        }
        */
        //___________________GRAVITY___________________
        if(isSliding) { // ingen gravitation när wall slidar
            rb.gravityScale = 0f;
        } else if(rb.velocity.y < 0) { // ökad gravitation när man faller
            rb.gravityScale = normGravityScale * 1.5f;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, maxFallSpeed));
        } else if((isJumping || isWallJumping || _isJumpFalling) && Mathf.Abs(rb.velocity.y) < jumpHangTimeThreshold) { // mindre gravitation vid toppen av ett hopp
            rb.gravityScale = normGravityScale * 0.8f;
        }
        //______________________________________________

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

    private void CheckHorizontalValue() {
        if (horizontalValue < 0)
        {
            FlipSprite(true);
            isFacingRight = false;
        }
        if (horizontalValue > 0)
        {
            FlipSprite(false);
            isFacingRight = true;
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

    private void WallJump(int dir) {
        lastPressedJumpTime = 0;
		lastOnGroundTime = 0;
		lastOnWallRightTime = 0;
		lastOnWallLeftTime = 0;
    }

    private void Dash() {
        //mhm
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
        if(respawns > 0) {
            respawnsText.fontSize = 50;
            respawnsText.text = "x " + respawns;
        } else {
            respawnsText.fontSize = 36;
            respawnsText.text = "Last";
        }
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

        if(leftHit.collider != null && leftHit.collider.CompareTag("Ground") || rightHit.collider != null && rightHit.collider.CompareTag("Ground") && !isJumping) {
            lastOnGroundTime = coyoteTime;
            return true;
        } else {
            return false;
        }
    }

    private void RightWallCheck() {
        if(((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, whatIsGround) && isFacingRight) || (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, whatIsGround) && !isFacingRight)) && !isWallJumping) {
            lastOnWallRightTime = coyoteTime;
        }
    }

    private void LeftWallCheck() {
        if(((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, whatIsGround) && !isFacingRight) || (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, whatIsGround) && isFacingRight)) && !isWallJumping) {
            lastOnWallLeftTime = coyoteTime;
        }
    }

    private void OnJumpInput() {
        lastPressedJumpTime = jumpInputBufferTime;
    }

    private void OnJumpUpInput() {
        if ((isJumping && rb.velocity.y > 0) || (isWallJumping && rb.velocity.y > 0)) {
			_isJumpCut = true;
        }
    }

    private bool CanWallJump() {
        return lastPressedJumpTime > 0 && lastOnWallTime > 0 && lastOnGroundTime <= 0 && (!isWallJumping || (lastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (lastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
    }
}


// https://github.com/DawnosaurDev/platformer-movement/tree/main (WALLJUMP SKRIVER RN)