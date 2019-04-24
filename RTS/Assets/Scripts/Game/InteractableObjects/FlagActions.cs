using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagActions : InteractableActions {
    [SerializeField]
    private bool carried = false;
    private Vector3 carryPos = new Vector3 (0, 0.875f, -.15f);
    private short netID;
    private FlagPlatformActions home;
    [SerializeField]
    private MeshRenderer flagBox;
    private Quaternion startingRotation;

    // Start is called before the first frame update
    void Awake () {
        string parentName = transform.parent.gameObject.name;
        netID = short.Parse (parentName.Remove (0, 3));
        flagBox.material.color = GuiManager.GetColorByNetID (netID);
        startingRotation = this.transform.rotation;
    }

    public void setHome (GameObject other) {
        home = other.GetComponent<FlagPlatformActions> ();
    }

    public override void trigger (Collider other) {
        SoldierActions soldier;
        if ((soldier = other.gameObject.GetComponent<SoldierActions> ()) != null && !carried) {
            string parentName = other.transform.parent.gameObject.name;
            int ID = int.Parse (parentName.Remove (0, 3));
            //ally touched us
            if (ID == netID) {
                if (!home.isFlagHome ()) {
                    home.returnFlag ();
                }
            }
            //enemy touched us
            else {
                getCarried ();
                transform.SetParent (soldier.transform, false);
                home.setFlagIsHome (false);
            }
        }
    }

    public short getNetID () {
        return netID;
    }

    public void returnFlag () {
        home.returnFlag ();
    }

    private void getCarried () {
        carried = true;
        this.transform.position = carryPos;
    }

    public void getDropped () {
        carried = false;
        this.transform.position = Vector3.zero;
        this.transform.rotation = startingRotation;
        this.transform.parent = home.transform.parent;
    }
}