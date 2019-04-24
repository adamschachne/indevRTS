using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ActionController : MonoBehaviour {

	public GameObject hitReticle;
	[SerializeField]
	private Renderer colorRenderer;
	public virtual void Start() {
		if(colorRenderer != null) {
			MaterialPropertyBlock mpb = new MaterialPropertyBlock();
			colorRenderer.GetPropertyBlock(mpb);
			string netID = this.transform.parent.name;
			int ID = int.Parse (netID.Remove (0, 3));
			mpb.SetInt("_NetworkID", ID);
			colorRenderer.SetPropertyBlock(mpb);
		}
	}
	
	public virtual void Attack(Vector3 targetDirection)
	{

	}

	public virtual void ShowTeaserReticle(Vector3 targetDirection, bool enabled) {

	}
	
	public virtual bool CancelAttack()
	{
		return false;
	}
}
