using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagPlatformActions : InteractableActions {
    public GameObject flagPrefab;
    private FlagActions flag;
    [SerializeField]
    private bool flagIsHome;
    private Vector3 flagPos;
    private short netID;
    [SerializeField]
    private MeshRenderer platformRenderer;
    // Start is called before the first frame update
    void Awake () {
        string parentName = transform.parent.gameObject.name;
        flagPos = new Vector3 (this.transform.position.x, this.transform.position.y + flagPrefab.transform.position.y, this.transform.position.z);
        if(flag == null) {
            flag = Instantiate (flagPrefab, flagPos, flagPrefab.transform.rotation, this.transform.parent).GetComponent<FlagActions>();
        }
        flag.setHome (this.gameObject);
        flagIsHome = true;
        netID = short.Parse (parentName.Remove (0, 3));
        platformRenderer.material.color = GuiManager.GetColorByNetID (netID);
    }

    //ToDo: If soldier is standing on platform and flag is returned, point is not scored
    //until onTriggerEnter event happens between ally flag and ally platform.
    public override void trigger (Collider other) {
        FlagActions flag;
        //an un-allied flag collided with this platform
        if (flagIsHome && (flag = other.gameObject.GetComponent<FlagActions> ()) != null && flag.getNetID () != netID) {
            //score a point
            StateManager.state.ScorePoint (netID);
            //return the opponent's flag
            flag.returnFlag ();
            //set lockout window
        }
    }

    public void setFlagIsHome (bool flagIsHome) {
        this.flagIsHome = flagIsHome;
    }

    public bool isFlagHome () {
        return flagIsHome;
    }

    public void returnFlag () {
        flag.getDropped ();
        flagIsHome = true;
        flag.transform.position = flagPos;
    }
}