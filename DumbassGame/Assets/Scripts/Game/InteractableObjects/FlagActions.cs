using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagActions : InteractableActions
{
    private bool carried = false;
    private Vector3 carryPos = new Vector3(0, 0.875f, -.15f);
    
    private FlagPlatformActions home;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setHome(GameObject other) {
        home = other.GetComponent<FlagPlatformActions>();
    }

    public override void trigger(Collider other){
        if(other.gameObject.GetComponent<SoldierActions>() != null && !carried) {
            getCarried();
            other.gameObject.GetComponent<SoldierActions>().CarryFlag(this.gameObject);
        } else if(other.gameObject.GetComponent<FlagPlatformActions>() != null) {

        }
    }

    public void returnHome() {
        home.returnFlag();
    }

    private void getCarried() {
        carried = true;
        this.transform.position = carryPos;
    }

    public void getDropped() {
        carried = false;
        this.transform.position = Vector3.zero;
    }
}
