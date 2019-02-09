using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BuildingController : UnitController {
    private NavMeshObstacle navObstacle;

    // Use this for initialization
    void Start () {
        state = StateManager.state;
        health = 100;
        if (!state) {
            throw new System.Exception("no state found");
        }
        navObstacle = GetComponent<NavMeshObstacle>();
        //anim = GetComponent<AnimationController>();
        //actions = GetComponent<ActionController>();
    }

    public override void CmdMoveTo(Vector3 targetPos) {        
        MoveTo(targetPos.x, targetPos.z);
        state.network.SendMove(this.name, targetPos.x, targetPos.z);
    }

    // Called by other users
    public override void MoveTo(float x, float z) {
        
    }

    public override void CmdStop()
    {
        Stop();
        state.network.SendStop(this.name);
    }
    public override void Stop() {
        Debug.Log("Triggered stop on a building.");
    }

    private void DestroyThis()
    {
        state.RemoveUnit(this.gameObject);
        this.transform.parent = null;
        state.selection.CleanupSelection(this.gameObject);
        Destroy(this.gameObject);
    }

    public override void CmdAttack(Vector3 targetPos){}

    public override void Attack(float x, float z){}

    public override void CmdSyncPos() {}

    public override void SyncPos(float x, float z) {}

    // Update is called once per frame
    private void FixedUpdate () {

    }
}
