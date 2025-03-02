using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    
    [SerializeField] private float jumpForce = 680f; // 4 rutor height

    [SerializeField] private AudioClip trampolineSound;

    private float jumpCooldownTime = 0.4f;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            Rigidbody2D playerRigidbody = other.GetComponent<Rigidbody2D>();
            playerRigidbody.velocity = new Vector2(playerRigidbody.velocity.x, 0);
            playerRigidbody.AddForce(new Vector2(0, jumpForce));
            other.gameObject.GetComponent<PlayerMovement>().JumpCooldown(jumpCooldownTime);
            other.gameObject.GetComponent<PlayerMovement>().AddExtraJump();
            GetComponent<Animator>().SetTrigger("Jump");
            audioSource.PlayOneShot(trampolineSound, 0.5f);
        }
    }
}
