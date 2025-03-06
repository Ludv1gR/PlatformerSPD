using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneWayPlatform : MonoBehaviour {
    public GameObject player; // Reference to the player’s transform
    private Collider2D platformCollider;

    private void Start() {
        platformCollider = GetComponent<Collider2D>();
    }

    private void Update() {
        if (player.transform.position.y < transform.position.y - 0.005f) {
            platformCollider.enabled = false; // Disable collision when player is below
        } else {
            platformCollider.enabled = true; // Re-enable collision when player is above
        }
    }
}
