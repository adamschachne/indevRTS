using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimationController : MonoBehaviour {

	protected Animator anim;
	void Start()
	{
		anim = GetComponentInChildren<Animator>();
	}

	public virtual void SetAttack()
	{
		Debug.Log("SetAttack function not overridden in an Animation controller!");
	}

	public virtual void ResetAttack()
	{
		Debug.Log("SetAttack function not overridden in an Animation controller!");
	}

	public virtual void SetMove(bool moving)
	{
		Debug.Log("SetMove Animation not overridden in an Animation controller!");
	}

	public virtual void SetIdle()
	{
		Debug.Log("SetIdle Animation not overridden in an Animation controller!");
	}
}
