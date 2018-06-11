using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour {

    public Vector3 targetPosition;
    public Vector3 targetDirection;
    public float moveSpeed = 3.0f;
    public float rotateSpeed = 0.08f;

    private StateManager state;

	// Use this for initialization
	void Start () {
        state = StateManager.state;
        targetPosition = transform.position;
    }

    public void CmdMoveTo(Vector3 targetPos) {        
        targetPosition = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        targetDirection = targetPosition - transform.position;

        // send data across network TODO
        // state.network.SendMsg();
    }
	
	// Update is called once per frame
	void Update () {
        // check if distance to target is greater than distance threshold
        if (Vector3.Distance(transform.position, targetPosition) > 0.1f) {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
        }

        Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDirection, rotateSpeed, 0.0f);
        transform.rotation = Quaternion.LookRotation(newDir);
    }
}
