using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selectable : MonoBehaviour {
    public GameObject selectionCirclePrefab;
    public GameObject selectionCircle;
    private StateManager state;


    void Start() {
        state = GameObject.Find("StateManager").GetComponent<StateManager>();
        if (!state) {
            throw new System.Exception("no state found");
        }

        selectionCircle = Instantiate(selectionCirclePrefab);
        selectionCircle.transform.SetParent(this.gameObject.transform, false);
        selectionCircle.SetActive(false);
        selectionCircle.GetComponent<Projector>().orthographicSize = this.GetComponent<Collider>().bounds.size.x;
        // make unselectable if not the owner of this unit
        if (!transform.parent.gameObject.name.Equals("GU-" + state.network.networkID)) {
            Destroy(this);
            return;
        }
        
    }
}

