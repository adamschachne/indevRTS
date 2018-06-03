using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour {

    public Vector3 targetPosition;
    public Vector3 targetDirection;
    public float moveSpeed = 3.0f;
    public float rotateSpeed = 0.08f;

	// Use this for initialization
	void Start () {
        targetPosition = transform.position;
    }

    public void moveTo(Vector3 targetPos) {
        targetPosition = new Vector3(targetPos.x, transform.position.y, targetPos.z);
        // unit will proceed to move to targetPosition
        targetDirection = targetPosition - transform.position;
        //transform.LookAt(targetDirection);
    }
	
	// Update is called once per frame
	void Update () {
        //Debug.Log("current position: " + transform.position);
        //Debug.Log("target position: " + targetPosition);
        // check if distance to target is greater than distance threshold

        if (Vector3.Distance(transform.position, targetPosition) > 0.1f) {
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);
        }

        Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDirection, rotateSpeed, 0.0f);
        //Debug.DrawRay(transform.position, newDir, Color.red);
        // Move our position a step closer to the target.
        transform.rotation = Quaternion.LookRotation(newDir);
    }
}
