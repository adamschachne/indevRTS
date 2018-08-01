using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour {

    public Vector3 targetPosition;
    public Vector3 targetDirection;
    public float moveSpeed = 3.0f;
    public float rotateSpeed = 0.08f;
    public bool moving = false;

    private StateManager state;

    private AnimationController anim;
    private ActionController actions;

    // Use this for initialization
    void Start () {
        //state = GameObject.Find("StateManager").GetComponent<StateManager>();    
        //if (!transform.parent.gameObject.name.Equals("GU-" + state.network.networkID)) {
        //}
        state = StateManager.state;
        if (!state) {
            throw new System.Exception("no state found");
        }
        targetPosition = transform.position;
        anim = GetComponent<AnimationController>();
        actions = GetComponent<ActionController>();
    }

    // Called by user
    public void CmdMoveTo(Vector3 targetPos) {        
        targetPosition = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        RotateTowards(targetPos.x, targetPos.z);

        // send data across network TODO
        state.network.SendMove(this.name, targetPos.x, targetPos.z);
    }

    public void Stop()
    {
        CmdMoveTo(this.transform.position);
        if(actions.CancelAttack())
            anim.SetIdle();
    }

    // Called by other users
    public void MoveTo(float x, float z) {
        targetPosition = new Vector3(x, transform.position.y, z);
        RotateTowards(x, z);
    }

    public void RotateTowards(float x, float z) {
        targetDirection = new Vector3(x, transform.position.y, z) - transform.position;
    }

    public void CmdAttack(Vector3 targetPos)
    {
        RotateTowards(targetPos.x, targetPos.z);
        anim.SetAttack();
        actions.Attack(targetPos, targetDirection);
    }

    // Update is called once per frame
    void Update () {
        // check if distance to target is greater than distance threshold
        if (Vector3.Distance(transform.position, targetPosition) > 0.1f) {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
            moving = true;
        }
        else
        {
            moving = false;
        }

        anim.SetMove(moving);

        Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDirection, rotateSpeed, 0.0f);
        transform.rotation = Quaternion.LookRotation(newDir);
    }
}
