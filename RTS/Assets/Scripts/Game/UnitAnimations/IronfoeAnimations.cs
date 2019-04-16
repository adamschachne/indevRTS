using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IronfoeAnimations : AnimationController {

	override public void SetIdle()
	{
		anim.SetTrigger("Stop");
	}
	override public void SetMove(bool moving)
	{
		anim.SetBool("Moving", moving);
	}
}
