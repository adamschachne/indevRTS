using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reticle : MonoBehaviour {

	protected Collider coll;
	public List<GameObject> collObjects;
	public int ignoreLayer;
	public int damage;

	// Use this for initialization
	protected virtual void Awake() {
		coll = GetComponent<Collider>();
		collObjects = new List<GameObject>();
	}

	void OnTriggerEnter(Collider col) {
		if(col.gameObject.layer != ignoreLayer) 
			collObjects.Add(col.gameObject);
	}

	void OnTriggerExit(Collider col) {
		if(col.gameObject.layer != ignoreLayer)
			collObjects.Remove(col.gameObject);
	}

	public void ResolveAttack() {
		foreach(GameObject unit in collObjects) {
			UnitController uc = unit.GetComponent<UnitController>();
			if(uc != null) {
				uc.TakeDamage(damage);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
