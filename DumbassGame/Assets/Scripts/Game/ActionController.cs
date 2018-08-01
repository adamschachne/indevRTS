using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ActionController : MonoBehaviour {

	public GameObject hitReticle;
	
	public virtual void Attack(Vector3 attackPos, Vector3 targetDirection)
	{

	}

	public virtual bool CancelAttack()
	{
		return false;
	}
}
