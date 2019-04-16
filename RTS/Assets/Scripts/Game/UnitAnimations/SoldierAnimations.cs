using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoldierAnimations : AnimationController {

	override public void SetAttack()
	{
		anim.SetTrigger("Attack");
	}

	override public void ResetAttack()
	{
		anim.ResetTrigger("Attack");
	}

	override public void SetMove(bool moving)
	{
		anim.SetBool("Moving", moving);
	}

	override public void SetIdle()
	{
		anim.SetTrigger("Stop");
	}
	
}
