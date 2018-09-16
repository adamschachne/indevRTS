using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxReticle : Reticle {

	private Projector proj;
	private BoxCollider box;
	private float width
	{
		get { return Width;}
		set
		{
			Width = value;
			updateValues();
		}
	}
	private float height
	{
		get { return Height;}
		set
		{
			Height = value;
			updateValues();
		}
	}
	private float Width;
	private float Height;

	public float projWidth = 1;
	public float projHeight = 1;
	private Vector3 sizeVector;
	private Vector3 posVector;

	// Use this for initialization
	protected override void Awake() {
		base.Awake();
		proj = GetComponent<Projector>();
		box = GetComponent<BoxCollider>();
		sizeVector = new Vector3(width, height, proj.farClipPlane);
		posVector = new Vector3(0, 0, sizeVector.z/2);
		width = projWidth;
		height = projHeight;
	}
	
	// Update is called once per frame
	public override void Update() {
		base.Update();
		if(width != projWidth) width = projWidth;
		if(height != projHeight) height = projHeight;
	}

	private void updateValues()
	{
		setProj();
		setCollider();
	}

	private void setProj()
	{
		proj.aspectRatio = width/height;
		proj.orthographicSize = height/2;
	}

	private void setCollider()
	{
		sizeVector.x = width;
		sizeVector.y = height;
		sizeVector.z = proj.farClipPlane;
		posVector.z = sizeVector.z/2;
		box.size = sizeVector;
		box.center = posVector;
	}

	override public void ResolveAttack()
	{
		GameObject closest = null;
		float lastDistance = float.MaxValue;

		foreach(GameObject unit in collObjects) {
			UnitController uc = unit.GetComponent<UnitController>();
			if(uc != null) {
				//uc.TakeDamage(damage);,
				if((unit.transform.position - this.transform.position).sqrMagnitude <  lastDistance) {
					lastDistance = (unit.transform.position - this.transform.position).sqrMagnitude;
					closest = unit;
				}
			}
		}

		if(closest != null)
		{
			string netID = closest.transform.parent.gameObject.name;
			int ID = int.Parse(netID.Remove(0, 3));
			StateManager.state.network.SendDamage(closest.name, ID, damage);
		}
		Destroy(this.gameObject);
	}
}
