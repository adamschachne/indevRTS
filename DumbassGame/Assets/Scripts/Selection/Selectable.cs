using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selectable : MonoBehaviour {

    public GameObject selectionCircle;
    private StateManager state;


    void Start() {
        state = GameObject.Find("StateManager").GetComponent<StateManager>();
        if (!state) {
            throw new System.Exception("no state found");
        }

        // make unselectable if not the owner of this unit
        if (!transform.parent.gameObject.name.Equals("GU-" + state.network.networkID)) {
            Destroy(this);
            return;
        }
        
    }
}

