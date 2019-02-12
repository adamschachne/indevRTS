using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagPlatformActions : InteractableActions
{
    public GameObject flagPrefab;
    private GameObject flag;
    private bool flagIsHome;
    private Vector3 flagPos;
    private short netID;
    // Start is called before the first frame update
    void Start()
    {
        netID = StateManager.state.network.networkID;
        flagPos = new Vector3(this.transform.position.x, flagPrefab.transform.position.y, this.transform.position.z);
        flag = Instantiate(flagPrefab, flagPos, flagPrefab.transform.rotation, this.transform.parent);
        flag.GetComponent<FlagActions>().setHome(this.gameObject);
        flagIsHome = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void trigger(Collider other){
        
    }

    public void returnFlag() {
        flagIsHome = true;
        flag.transform.position = flagPos;
    }
}
