using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour {
    public StateManager.EntityType type;
    void OnTriggerEnter (Collider other) {
        GetComponent<InteractableActions> ().trigger (other);
    }
}