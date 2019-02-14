using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DogActions : ActionController {
    override public void Attack (Vector3 attackPos, Vector3 targetDirection) {

    }

    override public bool CancelAttack () {
        return false;
    }
}