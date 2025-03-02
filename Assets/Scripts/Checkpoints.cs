using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoints : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player")) {
            other.GetComponent<PlayerMovement>().SetRespawnpoint(respawnPoint);
            anim.SetTrigger("Flag");

        }
    }
}
