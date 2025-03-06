using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {
    public Transform player; // Reference to the player’s transform
    private Collider2D platformCollider;

    private void Start() {
        platformCollider = GetComponent<Collider2D>();
    }

    private void Update() {
        if (player.position.y < transform.position.y - 0.005f) {
            platformCollider.enabled = false; // Disable collision when player is below
        } else {
            platformCollider.enabled = true; // Re-enable collision when player is above
        }
    }
}
