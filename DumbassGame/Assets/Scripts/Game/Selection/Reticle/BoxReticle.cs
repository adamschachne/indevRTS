using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxReticle : Reticle {

	private Projector proj;
	private BoxCollider box;
	private float width {
		get { return Width; }
		set {
			Width = value;
			updateValues ();
		}
	}
	private float height {
		get { return Height; }
		set {
			Height = value;
			updateValues ();
		}
	}
	private float Width;
	private float Height;

	public float projWidth = 1;
	public float projHeight = 1;
	private Vector3 sizeVector;
	private Vector3 posVector;

	// Use this for initialization
	protected override void Awake () {
		base.Awake ();
		proj = GetComponent<Projector> ();
		box = GetComponent<BoxCollider> ();
		sizeVector = new Vector3 (width, height, proj.farClipPlane);
		posVector = new Vector3 (0, 0, sizeVector.z / 2);
		width = projWidth;
		height = projHeight;
	}

	// Update is called once per frame
	public override void Update () {
		base.Update ();
		if (width != projWidth) width = projWidth;
		if (height != projHeight) height = projHeight;
	}

	private void updateValues () {
		setProj ();
		setCollider ();
	}

	private void setProj () {
		proj.aspectRatio = width / height;
		proj.orthographicSize = height / 2;
	}

	private void setCollider () {
		sizeVector.x = width;
		sizeVector.y = height;
		sizeVector.z = proj.farClipPlane;
		posVector.z = sizeVector.z / 2;
		box.size = sizeVector;
		box.center = posVector;
	}

	override public void ResolveAttack () {
		if (StateManager.state.isServer) {
			GameObject closest = null;
			float lastDistance = float.MaxValue;

			foreach (GameObject unit in collObjects) {
				UnitController uc = null;
				if (unit != null) uc = unit.GetComponent<UnitController> ();
				if (uc != null) {
					if ((unit.transform.position - shootPos).sqrMagnitude < lastDistance) {
						lastDistance = (unit.transform.position - this.transform.position).sqrMagnitude;
						closest = unit;
					}
				}
			}

			try {
				if (closest != null && closest.transform.parent != null) {
					if (closest.GetComponent<UnitController> ().type != StateManager.EntityType.Ironfoe) {
						string netID = closest.transform.parent.gameObject.name;
						short ID = short.Parse (netID.Remove (0, 3));
						StateManager.state.network.SendMessage (new Damage {
							id = closest.name,
								ownerID = ID,
								damage = this.damage
						}, false);
						StateManager.state.DamageUnit (ID, closest.name, this.damage);
					}
				}
				Destroy (this.gameObject);
			} catch (System.NullReferenceException e) {
				Debug.Log ("Null Reference Exception at BoxReticle ResolveAttack");
				Debug.Log ("Closest Unit isNull: " + closest);
				Debug.Log ("Closest Unit parent transform isNull: " + closest.transform.parent);
				Debug.Log ("Closest Unit parent gameObject isNull: " + closest.transform.parent.gameObject);
				Debug.Log (e.StackTrace);
			}
		} else {
			Destroy (this.gameObject);
		}
	}
}