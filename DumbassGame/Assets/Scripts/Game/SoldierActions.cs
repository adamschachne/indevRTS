using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoldierActions : ActionController {

	public float attackWidth;
	public float attackHeight;
	public float attackDelayInSeconds;
	private float currentDelay;

	private GameObject lastAttack;
	void Update()
	{
		if(currentDelay > 0)
		{
			currentDelay -= Time.deltaTime;
			if(currentDelay < 0)
			{
				CancelAttack();
				currentDelay = 0;
			}
		}
	}

	override public void Attack(Vector3 attackPos, Vector3 targetDirection)
	{
		GameObject hit = Instantiate(hitReticle);
		lastAttack = hit;
		BoxReticle reticle = hit.GetComponent<BoxReticle>();
		reticle.projWidth = attackWidth;
		reticle.projHeight = attackHeight;

		//set rotation of reticle to be pointing towards target direction
		hit.transform.rotation = Quaternion.LookRotation(targetDirection);
		hit.transform.eulerAngles = new Vector3(90, hit.transform.eulerAngles.y, hit.transform.eulerAngles.z);

		//set position of reticle to be slightly offset from this unit
		hit.transform.SetParent(this.transform.parent);
		hit.transform.position = this.transform.position + 					//center on unit
		Vector3.Normalize(targetDirection)*(0.75f + reticle.projHeight/2) + //offset by height/2
		Vector3.up*reticle.transform.position.y;							//raise above platform

		currentDelay = attackDelayInSeconds;
	}

	override public bool CancelAttack()
	{
		if(lastAttack != null)
		{
			Destroy(lastAttack);
			return true;
		}
		return false;
	}

}
