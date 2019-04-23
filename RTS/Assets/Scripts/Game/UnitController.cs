using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitController : MonoBehaviour {
    [SerializeField]
    private Vector3 targetDirection;
    [SerializeField]
    private float rotateSpeed = 0.65f;
    private float rotateCounter = 0;
    private float radius;
    [SerializeField]
    protected int health = 20;
    private bool moving = false;
    private bool rotating = false;
    protected StateManager state;

    protected AnimationController anim;
    protected ActionController actions;
    private NavMeshAgent agent;
    public StateManager.EntityType type;
    short ownerNetID;
    short unitID;

    // Use this for initialization
    void Start () {
        state = StateManager.state;
        if (!state) {
            throw new System.Exception ("no state found");
        }
        anim = GetComponent<AnimationController> ();
        actions = GetComponent<ActionController> ();
        agent = GetComponent<NavMeshAgent> ();
        radius = GetComponent<CapsuleCollider> ().radius;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        agent.destination = this.transform.position;
        rotateSpeed = rotateSpeed / 60;
        targetDirection = new Vector3 (0, 0, 0);

        string netID = this.transform.parent.name;
		ownerNetID = short.Parse (netID.Remove (0, 3));
        unitID = short.Parse(this.name);
    }

    // Called by user
    public virtual void CmdMoveTo (Vector3 targetPos) {
        MoveTo (targetPos.x, targetPos.y, targetPos.z);
        state.network.SendMessage (new MoveSingle {
            id = name,
                ownerID = state.network.networkID,
                x = targetPos.x,
                y = targetPos.y,
                z = targetPos.z
        });
    }
    // Called by other users
    public virtual void MoveTo (float x, float y, float z) {
        rotating = false;
        if (actions != null && actions.CancelAttack ()) {
            anim.SetIdle ();
            anim.ResetAttack ();
        }
        agent.destination = new Vector3 (x, y, z);
    }

    public void TakeDamage (int damage) {
        health -= damage;
    }

    private void DestroyThis () {
        actions.CancelAttack ();
        state.RemoveUnit (this.gameObject);
        this.transform.parent = null;
        state.selection.CleanupSelection (this);
        FlagActions flag = null;
        if (GetComponent<SoldierActions> () != null &&
            (flag = GetComponentInChildren<FlagActions> ()) != null) {
            flag.getDropped ();
            flag.transform.position = new Vector3 (transform.position.x, transform.position.y, transform.position.z);
        }

        Destroy (this.gameObject);
    }

    private void RotateTowards (float x, float z) {
        rotateCounter = 0;
        targetDirection = Vector3.Normalize (new Vector3 (x, transform.position.y, z) - transform.position);
        rotating = true;
    }

    /*
    public virtual void CmdAttack (Vector3 targetPos) {
        Attack (targetPos.x, targetPos.z);
        state.network.SendMessage (new Attack {
            id = name,
                ownerID = state.network.networkID,
                x = targetPos.x,
                z = targetPos.z
        });
    }
    */

    public void ShowTeaserAttack(float x, float z, bool enabled) {
        actions.ShowTeaserReticle(Vector3.Normalize (new Vector3 (x, transform.position.y, z) - transform.position), enabled);
    }

    public virtual void Attack (float x, float z) {
        RotateTowards (x, z);
        anim.SetAttack ();
        actions.Attack (targetDirection);
    }

    public virtual void CmdSyncPos () {
        state.SubmitForSync(this.name, this.ownerNetID, new Vector2(this.transform.position.x, this.transform.position.z));
    }

    public virtual void SyncPos (float x, float z) {
        if (state != null && !state.isServer) {
            this.transform.position = new Vector3 (x, this.transform.position.y, z);
            //Debug.Log("Trying to set position to: " + x + "," + z);
        }
    }

    // Update is called once per frame
    private void FixedUpdate () {
        if (health <= 0) {
            DestroyThis ();
            return;
        }

        // check if distance to target is greater than distance threshold
        if (Vector3.Distance (transform.position, agent.steeringTarget) > radius) {
            //float step = moveSpeed * Time.deltaTime;
            //transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
            moving = true;
            Vector3 lookDir = (agent.steeringTarget - this.transform.position) - this.transform.forward;
            this.transform.forward += lookDir * rotateSpeed;
        } else {
            moving = false;
            if (rotating) {
                if (rotateCounter < 1) {
                    rotateCounter += rotateSpeed;
                    this.transform.forward = Vector3.Lerp (this.transform.forward, targetDirection, rotateCounter);
                } else {
                    targetDirection = this.transform.position;
                    rotating = false;
                    rotateCounter = 0;
                }
            }
        }

        anim.SetMove (moving);

        // && unitID + state.frameCount % state.syncRate == 0 (created weird desync issues)
        if (moving && state.isServer) {
            CmdSyncPos ();
        }

    }
}