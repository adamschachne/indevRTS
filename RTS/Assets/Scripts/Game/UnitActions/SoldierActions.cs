using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoldierActions : ActionController {

	public float attackWidth;
	public float attackHeight;
	public float attackDelayInSeconds;
	public int attackDamage;
	private BoxReticle teaserReticle;
	
	private GameObject lastAttack;
	public override void Start() {
		base.Start();
		teaserReticle = Instantiate(hitReticle, this.transform.parent).GetComponent<BoxReticle>();
		teaserReticle.projWidth = attackWidth;
		teaserReticle.projHeight = attackHeight;
		teaserReticle.ignoreLayer = gameObject.layer;
		teaserReticle.name = "TeaserReticle";
		teaserReticle.updateValues();
		teaserReticle.Disable();
		teaserReticle.gameObject.SetActive(false);
	}
	override public void Attack (Vector3 targetDirection) {
		GameObject hit = Instantiate (hitReticle);
		lastAttack = hit;
		BoxReticle reticle = hit.GetComponent<BoxReticle> ();
		reticle.projWidth = attackWidth;
		reticle.projHeight = attackHeight;
		reticle.ignoreLayer = gameObject.layer;
		reticle.damage = attackDamage;
		reticle.currentDelay = attackDelayInSeconds;
		reticle.shootPos = new Vector3 (this.transform.position.x, this.transform.position.y, this.transform.position.z);

		//set rotation of reticle to be pointing towards target direction
		hit.transform.rotation = Quaternion.LookRotation (targetDirection);
		hit.transform.eulerAngles = new Vector3 (90, hit.transform.eulerAngles.y, hit.transform.eulerAngles.z);

		//set position of reticle to be slightly offset from this unit
		hit.transform.SetParent (this.transform.parent);
		hit.transform.position = this.transform.position + //center on unit
			Vector3.Normalize (targetDirection) * (0.75f + reticle.projHeight / 2) + //offset by height/2
			Vector3.up * reticle.transform.position.y; //raise above platform
	}

	override public void ShowTeaserReticle(Vector3 targetDirection, bool enabled) {
		if(enabled) {
			teaserReticle.gameObject.SetActive(true);
			teaserReticle.Disable();
			teaserReticle.transform.SetParent(this.transform);
			teaserReticle.transform.localPosition = hitReticle.transform.localPosition;
			//set rotation of reticle to be pointing towards target direction
			teaserReticle.transform.rotation = Quaternion.LookRotation (targetDirection);
			teaserReticle.transform.eulerAngles = new Vector3 (90, teaserReticle.transform.eulerAngles.y, teaserReticle.transform.eulerAngles.z);

			//set position of reticle to be slightly offset from this unit
			teaserReticle.transform.SetParent (this.transform.parent);
			
			teaserReticle.transform.position = this.transform.position + //center on unit
			Vector3.Normalize (targetDirection) * (0.75f + teaserReticle.projHeight / 2) + //offset by height/2
			Vector3.up * hitReticle.transform.position.y; //raise above platform
				
		} else {
			teaserReticle.gameObject.SetActive(false);
		}

	}

	

	override public bool CancelAttack () {
		if (lastAttack != null) {
			Destroy (lastAttack);
			return true;
		}
		return false;
	}

	private void ResolveAttack () {
		if (lastAttack != null) {
			BoxReticle r = lastAttack.GetComponent<BoxReticle> ();
			r.ResolveAttack ();
		}
		CancelAttack ();
	}

}