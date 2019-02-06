using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class OldUnitController : MonoBehaviour {
    public Vector3 targetDirection;
    public float moveSpeed = 3.0f;
    public float rotateSpeed = 0.65f;
    private float rotateCounter = 0;
    public int health = 20;
    private bool moving = false;
    private bool rotating = false;
    private StateManager state;

    private AnimationController anim;
    private ActionController actions;
    private NavMeshAgent agent;

    // Use this for initialization
    void Start () {
        state = StateManager.state;
        if (!state) {
            throw new System.Exception("no state found");
        }
        anim = GetComponent<AnimationController>();
        actions = GetComponent<ActionController>();
        agent = GetComponent<NavMeshAgent>();
        if(state.isServer) {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        } else {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.updatePosition = false;
        }

        agent.destination = this.transform.position;
        rotateSpeed = rotateSpeed/60;
        targetDirection = new Vector3(0,0,0);
    }

    // Called by user
    public void CmdMoveTo(Vector3 targetPos) {        
        MoveTo(targetPos.x, targetPos.z);
        state.network.SendMove(this.name, targetPos.x, targetPos.z);
    }
    // Called by other users
    public void MoveTo(float x, float z) {
        if(actions.CancelAttack())
        {
            anim.SetIdle();
            anim.ResetAttack();
        }
        rotating = false;
        agent.destination = new Vector3(x, transform.position.y, z);
    }

    public void CmdStop()
    {
        Stop();
        state.network.SendStop(this.name);
    }
    public void Stop()
    {
        MoveTo(this.transform.position.x, this.transform.position.z);
        rotating = false;
        if(actions.CancelAttack())
        {
            anim.SetIdle();
            anim.ResetAttack();
        }
    }

    public void TakeDamage(int damage) {
        health -= damage;
        if(health <= 0) {
            DestroyThis();
        }
    }

    private void DestroyThis()
    {
        actions.CancelAttack();
        state.RemoveUnit(this.gameObject);
        this.transform.parent = null;
        state.selection.RemoveSelection(this.gameObject);
        Destroy(this.gameObject);
    }

    public void RotateTowards(float x, float z) {
        rotateCounter = 0;
        targetDirection = Vector3.Normalize(new Vector3(x, transform.position.y, z) - transform.position);
        rotating = true;
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

    public void CmdSyncPos() {
        if(agent.obstacleAvoidanceType == ObstacleAvoidanceType.HighQualityObstacleAvoidance) {
            state.network.SendSyncPos(this.name, short.Parse(this.transform.parent.name.Remove(0, 3)), this.transform.position.x, this.transform.position.z);
        }
    }

    public void SyncPos(float x, float z) {
        if(state != null && !state.isServer) {
            this.transform.position = new Vector3(x, this.transform.position.y, z);
            //Debug.Log("Trying to set position to: " + x + "," + z);
        }
    }

    // Update is called once per frame
    void FixedUpdate () {

        // check if distance to target is greater than distance threshold
        if (Vector3.Distance(transform.position, agent.steeringTarget) > 0.1f) {
            //float step = moveSpeed * Time.deltaTime;
            //transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
            moving = true;
            Vector3 lookDir = (agent.steeringTarget - this.transform.position) - this.transform.forward;
            this.transform.forward += lookDir*rotateSpeed;
        }
        else
        {
            moving = false;
            if(rotating) {
                if(rotateCounter < 1) {
                    rotateCounter += rotateSpeed;
                    this.transform.forward = Vector3.Lerp(this.transform.forward, targetDirection, rotateCounter);
                } 
                else {
                    targetDirection = Vector3.zero;
                    rotating = false;
                    rotateCounter = 0;
                }
            }
        }

        if(agent.obstacleAvoidanceType == ObstacleAvoidanceType.HighQualityObstacleAvoidance) {
            CmdSyncPos();
        }
        anim.SetMove(moving);

    }
}
