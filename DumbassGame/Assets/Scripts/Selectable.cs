using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Selectable : NetworkBehaviour {

    public GameObject selectionCircle;

    void Start() {
        // prevent non-local players from selecting this unit
        if (!isLocalPlayer) {
            Destroy(this);
            return;
        }
    }
}

