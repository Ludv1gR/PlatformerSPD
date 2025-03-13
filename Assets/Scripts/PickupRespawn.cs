using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupRespawn : MonoBehaviour
{
    public float respawnTime = 3f;
    public GameObject respawnEffect;

    private SpriteRenderer spriteRenderer;
    private Collider2D pickupCollider;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        pickupCollider = GetComponent<Collider2D>();
    }

    public void Collect() {
        spriteRenderer.enabled = false;
        pickupCollider.enabled = false;

        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn() {
        yield return new WaitForSeconds(respawnTime);

        Instantiate(respawnEffect, transform.position, Quaternion.identity);

        spriteRenderer.enabled = true;
        pickupCollider.enabled = true;
    }
}
