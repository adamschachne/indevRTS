using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reticle : MonoBehaviour {

	protected Collider coll;
	public List<GameObject> collObjects;
	public int ignoreLayer;
	public int damage;
	public float currentDelay;
	public Vector3 shootPos;

	// Use this for initialization
	protected virtual void Awake () {
		coll = GetComponent<Collider> ();
		collObjects = new List<GameObject> ();
	}

	public void Disable() {
		this.enabled = false;
		coll.enabled = false;
	}

	public virtual void Update () {
		if (currentDelay > 0) {
			currentDelay -= Time.deltaTime;
		}

		if (currentDelay <= 0) {
			ResolveAttack ();
		}
	}

	void OnTriggerEnter (Collider col) {
		if (col.gameObject.layer != ignoreLayer)
			collObjects.Add (col.gameObject);
	}

	void OnTriggerExit (Collider col) {
		if (col.gameObject.layer != ignoreLayer)
			collObjects.Remove (col.gameObject);
	}

	public virtual void ResolveAttack () {
		foreach (GameObject unit in collObjects) {
			UnitController uc = unit.GetComponent<UnitController> ();
			if (uc != null) {
				string netID = unit.transform.parent.gameObject.name;
				int ID = int.Parse (netID.Remove (0, 3));
				StateManager.state.network.SendMessage (new Damage {
					id = name,
						ownerID = (short) ID,
						damage = damage
				});
			}
		}
		Destroy (this.gameObject);
	}
}