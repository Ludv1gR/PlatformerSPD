using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float jumpForce = 8.2f; //1 ruta height lite drygt för 2 och inte 3 i dubbel
    [SerializeField] private Transform leftFoot, rightFoot;
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private AudioClip jumpSound, pickupSoundHealth, pickupSoundFruit, damageSound;
    [SerializeField] private GameObject pickupEffect, dustParticles;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text fruitsCollectedText;
    [SerializeField] private TMP_Text respawnsText;

    private float rayDistance = 0.15f;
    private int startingHealth = 5;
    private int currentHealth = 0;
    private int respawns = 3;
    public int fruitsCollected = 0;


    //------------[ nya för SpelDesign ]------------//
    // Constants
    [SerializeField] private float normGravityScale = 2.2f; //-9.81f;
    [SerializeField] private float maxFallSpeed = 50;
    [SerializeField] private float jumpHangTimeThreshold = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float runMaxSpeed = 3.5f;
    [SerializeField] private bool doConserveMomentum = true; // om man behåller fart när man fortsatt vill gå år det hållet

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
    private float wallJumpTime = 0.2f;
    private Vector2 wallJumpForce = new Vector2(4f, 8f);

    // Double Jump
    [SerializeField]private int extraJumpAmount = 1;
    private int _extraJumpsLeft;

    // Slide
    private float slideSpeed = -1.8f;
    private float slideAccel = 20.0f;

    // Dash
    private int dashAmount = 1;
    private int _dashesLeft;
    private bool _dashRefilling;
    private int _lastDashDir;
    private bool _isDashAttacking;
    private float dashSleepTime = 0.05f;
    private float dashInputBufferTime = 0.1f;
    private float dashAttackTime = 0.15f;
    private float dashSpeed = 14f;
    private float dashEndSpeed = 6f;
    private float dashEndTime = 0.05f; // kanske 0.15?
    private float dashRefillTime = 0.3f;

    // GravityMultipliers
    private float fastFallGravityMult = 1.4f;
    private float jumpCutGravityMult = 1.4f;
    private float jumpHangGravityMult = 0.7f;
    private float fallGravityMult = 1.1f;

    // Lerps
    private float wallJumpRunLerp = 0.2f;
    private float dashEndRunLerp = 0.6f;

    // Acceleration
    private float runAccelAmount = 16f;
    private float runDeccelAmount = 10f;
    private float accelInAir = 0.6f;
    private float deccelInAir = 0.4f;
    private float jumpHangAccelerationMult = 1; // vill inte ha speed boost vid apex om inte väldigt lite
    private float jumpHangMaxSpeedMult = 1; // tillhör den övre här
    
    // Input
    private Vector2 _moveInput;

    public float lastPressedJumpTime;
    public float lastPressedDashTime;

    // Check
    [SerializeField] private Transform _frontWallCheckPoint;
    [SerializeField] private Transform _backWallCheckPoint;
    [SerializeField] private Vector2 _wallCheckSize = new Vector2(0.1f, 0.8f);
    //----------------[ END ]----------------//


    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;
    private AudioSource audioSource;

    public PauseMenuController gameOverScreen;

    #region START
    void Start()
    {
        currentHealth = startingHealth;
        fruitsCollectedText.text = "" + fruitsCollected;
        respawnsText.text = "x " + respawns;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        isFacingRight = true;
        rb.gravityScale = normGravityScale;
    }
    #endregion

    #region UPDATE
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

        _moveInput.x = Input.GetAxisRaw("Horizontal");
		_moveInput.y = Input.GetAxisRaw("Vertical");

		if (_moveInput.x != 0) {
			CheckDirectionToFace(_moveInput.x > 0);
        }

		if(Input.GetKeyDown(KeyCode.Space)) { // Trycka new space
			OnJumpInput();
        }

		if (Input.GetKeyUp(KeyCode.Space)) { // Släppa space
			OnJumpUpInput();
		}

		if (Input.GetKeyDown(KeyCode.LeftShift)) { // Shift to Dash
			OnDashInput();
		}

        //______________JUMP CHECKS____________
        if(!isDashing && !isJumping) {
            CheckIfGrounded();
            FrontWallCheck();
            FrontTwoWallCheck();

            lastOnWallTime = Mathf.Max(lastOnWallLeftTime, lastOnWallRightTime); //checkPoints kommer att vända när spelaren vänder, därav båda checksen
        }

        if(isJumping && rb.velocity.y < 0) {
            isJumping = false;
            _isJumpFalling = true;
        }

        if(isWallJumping && Time.time - _wallJumpStartTime > wallJumpTime) {
            isWallJumping = false;
        }

        if(lastOnGroundTime > 0 && !isJumping && !isWallJumping) {
            _isJumpCut = false;
            _isJumpFalling = false;
            _extraJumpsLeft = extraJumpAmount;
        }

        if(!isDashing) {
            if(CanJump() && lastPressedJumpTime > 0 && !isSliding) {
                isJumping = true;
                isWallJumping = false;
                _isJumpCut = false;
                _isJumpFalling = false;
                Jump();

            } else if (CanWallJump() && lastPressedJumpTime > 0) {
                isWallJumping = true;
                isJumping = false;
                _isJumpCut = false;
                _isJumpFalling = false;
                
                _wallJumpStartTime = Time.time;
                _lastWallJumpDir = (lastOnWallRightTime > 0) ? -1 : 1;
                
                WallJump(_lastWallJumpDir);
            }
        }
        //_____________________________________

        //______________DASH CHECKS____________
        if (CanDash() && lastPressedDashTime > 0) {

            Sleep(dashSleepTime);

            _lastDashDir = isFacingRight ? 1 : -1; // Kolla om den kanske inte fixar isFacingRight efter input?

            isDashing = true;
            isJumping = false;
            isWallJumping = false;
            _isJumpCut = false;

            StartCoroutine(nameof(Dash), _lastDashDir);
        }
        //_____________________________________

        //______________SLIDE CHECKS____________
        if(CanSlide() && ((lastOnWallLeftTime > 0 && _moveInput.x < 0) || (lastOnWallRightTime > 0 && _moveInput.x > 0))) {
            isSliding = true;
            anim.SetBool("IsSliding", true);
        } else {
            isSliding = false;
            anim.SetBool("IsSliding", false);
        }
        //______________________________________

        //___________________GRAVITY___________________
        if(!_isDashAttacking) {
            if(isSliding) { // ingen gravitation när wall slidar
                rb.gravityScale = 0f;
            } else if(rb.velocity.y < 0 && _moveInput.y < 0) { // ökad gravitation när man faller
                rb.gravityScale = normGravityScale * fastFallGravityMult;
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed));
            } else if (_isJumpCut) {
				rb.gravityScale = normGravityScale * jumpCutGravityMult;
				rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed));
			} else if((isJumping || isWallJumping || _isJumpFalling) && Mathf.Abs(rb.velocity.y) < jumpHangTimeThreshold) { // mindre gravitation vid toppen av ett hopp
                rb.gravityScale = normGravityScale * jumpHangGravityMult;
            } else if(rb.velocity.y < 0) {
                rb.gravityScale = normGravityScale * fallGravityMult;
                rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed));
            } else {
                rb.gravityScale = normGravityScale;
            }
        } else {
            rb.gravityScale = 0;
        }
        //______________________________________________

        anim.SetFloat("MoveSpeed", Mathf.Abs(rb.velocity.x));
        anim.SetFloat("VerticalSpeed", rb.velocity.y);
        anim.SetBool("IsGrounded", CheckIfGrounded());
    }
    #endregion

    #region FIXED UPDATE
    private void FixedUpdate() {
        if(!isDashing) {
            if(isWallJumping) {
                Run(wallJumpRunLerp);
            } else {
                Run(1);
            }
        } else if(_isDashAttacking) {
            Run(dashEndRunLerp);
        }

        if(isSliding) {
            Slide();
        }
    }
    #endregion

    #region INPUT METHODS
    private void OnJumpInput() {
        lastPressedJumpTime = jumpInputBufferTime;
    }

    private void OnJumpUpInput() {
        if (CanJumpCut() || CanWallJumpCut()) {
			_isJumpCut = true;
        }
    }

    private void OnDashInput() {
        lastPressedDashTime = dashInputBufferTime;
    }
    #endregion

    #region RUN METHODS
    private  void Run(float lerpAmount) {
        float targetSpeed = _moveInput.x * runMaxSpeed;
        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

        float accelRate;

        if(lastOnGroundTime > 0) {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? runAccelAmount : runDeccelAmount;
        } else {
            accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? runAccelAmount * accelInAir : runDeccelAmount * deccelInAir;
        }

        if ((isJumping || isWallJumping || _isJumpFalling) && Mathf.Abs(rb.velocity.y) < jumpHangTimeThreshold)
		{
			accelRate *= jumpHangAccelerationMult;
			targetSpeed *= jumpHangMaxSpeedMult;
		}

        if(doConserveMomentum && Mathf.Abs(rb.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(rb.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && lastOnGroundTime < 0) {
            accelRate = 0;
        }

        float speedDif = targetSpeed - rb.velocity.x;
        float movement = speedDif * accelRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    private void Turn() {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        isFacingRight = !isFacingRight;
    }
    #endregion

    #region JUMP METHODS
    private void Jump()
    {
        float force = jumpForce;

        if(rb.velocity.y < 0) {
            force -= rb.velocity.y;
        }

        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);

        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(jumpSound, 0.3f);

        if(lastOnGroundTime > -coyoteTime && lastOnGroundTime < 0 && CheckIfGrounded()){
            // dont produce particles when jumping during coyote time (måste göra coyote time check innan CheckIfGrounded())
        } else if(CheckIfGrounded()) {
            Instantiate(dustParticles, transform.position, dustParticles.transform.localRotation);
        }

        if (lastOnGroundTime > 0) {
            lastOnGroundTime = 0; // Reset ground time on normal jump
            _extraJumpsLeft = extraJumpAmount;
        } else {
            _extraJumpsLeft--; // Consume an extra jump
            if(_extraJumpsLeft < extraJumpAmount) {
                anim.SetTrigger("DoubleJump");
            }
        }
        lastPressedJumpTime = 0;
    }

    private void WallJump(int dir) {
        lastPressedJumpTime = 0;
		lastOnGroundTime = 0;
		lastOnWallRightTime = 0;
		lastOnWallLeftTime = 0;

        Vector2 force = new Vector2(wallJumpForce.x, wallJumpForce.y);
        force.x *= dir;

        if(Mathf.Sign(rb.velocity.x) != Mathf.Sign(force.x)) {
            force.x -= rb.velocity.x;
        }
        if(rb.velocity.y < 0) {
            rb.velocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    public void AddExtraJump() {
        if(_extraJumpsLeft == 0) {
            _extraJumpsLeft++;
        }
    }

    public void JumpCooldown(float time) {
        if(!isJumping) {
            StartCoroutine(JumpCooldownCoroutine(time));
        }
    }

    private IEnumerator JumpCooldownCoroutine(float time) {
        isJumping = true;
        yield return new WaitForSeconds(time);
        isJumping = false;
    }
    #endregion

    #region DASH METHODS
    private IEnumerator Dash(int dir) {
        lastOnGroundTime = 0;
        lastPressedDashTime = 0;

        float startTime = Time.time;

        _dashesLeft--;
        _isDashAttacking = true;

        rb.gravityScale = 0;

        while(Time.time -startTime <= dashAttackTime) {
            rb.velocity = new Vector2(dir * dashSpeed, 0f);

            yield return null;
        }

        startTime = Time.time;

        _isDashAttacking = false;

        rb.gravityScale = normGravityScale;
        rb.velocity = new Vector2(dir * dashEndSpeed, 0f);

        while(Time.time - startTime <= dashEndTime) {
            yield return null;
        }

        // Dashen är över
        isDashing = false;
    }

    private IEnumerator RefillDash(int amount) {
        _dashRefilling = true;
        yield return new WaitForSeconds(dashRefillTime);
        _dashRefilling = false;
        _dashesLeft = Mathf.Min(dashAmount, _dashesLeft + 1);
    }

    private void InstaRefillDash() {
        _dashesLeft = Mathf.Min(dashAmount, _dashesLeft + 1);
    }
    #endregion

    #region OTHER MOVEMENT METHODS
    private void Slide() {
        if(rb.velocity.y > 0) {
            rb.AddForce(-rb.velocity.y * Vector2.up, ForceMode2D.Impulse);
        }

        float speedDif = slideSpeed - rb.velocity.y;	
		float movement = speedDif * slideAccel;

        movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif)  * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));

		rb.AddForce(movement * Vector2.up);
    }

    public void TakeKnockback(float knockbackForce, float up) {
        rb.velocity = Vector2.zero;
        rb.AddForce(new Vector2(knockbackForce, up));
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(damageSound, 0.5f);
    }
    #endregion

    #region HEALTH METHODS
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
            Instantiate(pickupEffect, healthFruit.transform.position, Quaternion.identity); // kanske ändra när det health pickup
            Destroy(healthFruit);
        }
    }
    #endregion

    #region PICKUP METHODS
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
        if(other.CompareTag("HealthFruit"))
        {
            RestoreHealth(other.gameObject);
        }
    }
    #endregion
    
    #region CHECK METHODS
    private void CheckDirectionToFace(bool isMovingRight) {
        if(isMovingRight != isFacingRight) {
            Turn();
        }
    }

    private bool CheckIfGrounded()
    {
        RaycastHit2D leftHit = Physics2D.Raycast(leftFoot.position, Vector2.down, rayDistance, whatIsGround);
        RaycastHit2D rightHit = Physics2D.Raycast(rightFoot.position, Vector2.down, rayDistance, whatIsGround);

        if(leftHit.collider != null && leftHit.collider.CompareTag("Ground") || rightHit.collider != null && rightHit.collider.CompareTag("Ground") && !isJumping) {
            lastOnGroundTime = coyoteTime;
            _extraJumpsLeft = extraJumpAmount;
            return true;
        } else {
            return false;
        }
    }

    private void FrontWallCheck() {
        if(((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, whatIsGround) && isFacingRight) || (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, whatIsGround) && !isFacingRight)) && !isWallJumping) {
            lastOnWallRightTime = coyoteTime;
        }
    }

    private void FrontTwoWallCheck() {
        if(((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, whatIsGround) && !isFacingRight) || (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, whatIsGround) && isFacingRight)) && !isWallJumping) {
            lastOnWallLeftTime = coyoteTime;
        }
    }

    private bool CanJump() {
        return (lastOnGroundTime > 0 || _extraJumpsLeft > 0) && !isJumping;
    }

    private bool CanWallJump() {
        return lastPressedJumpTime > 0 && lastOnWallTime > 0 && lastOnGroundTime <= 0 && (!isWallJumping || (lastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (lastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
    }

    private bool CanJumpCut()
    {
		return isJumping && rb.velocity.y > 0;
    }

	private bool CanWallJumpCut()
	{
		return isWallJumping && rb.velocity.y > 0;
	}

    private bool CanDash()
	{
		if (!isDashing && _dashesLeft < dashAmount && lastOnGroundTime > 0 && !_dashRefilling)
		{
			StartCoroutine(nameof(RefillDash), 1);
		}

		return _dashesLeft > 0;
	}
    #endregion

    #region GENERAL METHODS
	public bool CanSlide()
    {
		if (lastOnWallTime > 0 && !isJumping && !isWallJumping && !isDashing && lastOnGroundTime <= 0)
			return true;
		else
			return false;
	}

    private void Sleep(float duration) {
        StartCoroutine(nameof(PerformSleep), duration);
    }

    private IEnumerator PerformSleep(float duration) {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }
    #endregion

    private void OnDrawGizmos() {
        // Set the color for the front wall check
        Gizmos.color = Color.red;
        // Draw a wire cube at the _frontWallCheckPoint with the size of _wallCheckSize
        Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);

        // Set the color for the back wall check
        Gizmos.color = Color.blue;
        // Draw a wire cube at the _backWallCheckPoint with the size of _wallCheckSize
        Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);
    }
}
// https://github.com/DawnosaurDev/platformer-movement/tree/main (WALLJUMP SKRIVER RN)