using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxReticle : MonoBehaviour {

	private Projector proj;
	private BoxCollider coll;
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
	void Awake () {
		proj = GetComponent<Projector>();
		coll = GetComponent<BoxCollider>();
		sizeVector = new Vector3(width, height, proj.farClipPlane);
		posVector = new Vector3(0, 0, sizeVector.z/2);
		width = projWidth;
		height = projHeight;
	}
	
	// Update is called once per frame
	void Update () {
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
		coll.size = sizeVector;
		coll.center = posVector;
	}
}
