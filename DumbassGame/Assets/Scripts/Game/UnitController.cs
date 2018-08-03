using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : MonoBehaviour {

    public Vector3 targetPosition;
    public Vector3 targetDirection;
    public float moveSpeed = 3.0f;
    public float rotateSpeed = 0.08f;
    public int health = 20;
    private bool moving = false;

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
        MoveTo(targetPos.x, targetPos.z);

        // send data across network TODO
        state.network.SendMove(this.name, targetPos.x, targetPos.z);
    }
    // Called by other users
    public void MoveTo(float x, float z) {
        if(actions.CancelAttack())
        {
            anim.SetIdle();
            anim.ResetAttack();
        }
        targetPosition = new Vector3(x, transform.position.y, z);
        RotateTowards(x, z);
    }

    public void CmdStop()
    {
        Stop();
        state.network.SendStop(this.name);
    }
    public void Stop()
    {
        MoveTo(this.transform.position.x, this.transform.position.z);
        if(actions.CancelAttack())
        {
            anim.SetIdle();
            anim.ResetAttack();
        }
    }

    public void TakeDamage(int damage) {
        health -= damage;
        if(health <= 0) {
            Destroy(this.gameObject);
        }
    }

    public void RotateTowards(float x, float z) {
        targetDirection = new Vector3(x, transform.position.y, z) - transform.position;
    }

    public void CmdAttack(Vector3 targetPos)
    {
        Attack(targetPos.x, targetPos.z);
        state.network.SendAttack(this.name, targetPos.x, targetPos.z);
    }

    public void Attack(float x, float z)
    {
        RotateTowards(x, z);
        anim.SetAttack();
        actions.Attack(new Vector3(x, transform.position.y, z), targetDirection);
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
