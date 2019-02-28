using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RareIdle : StateMachineBehaviour {
    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    //override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}
    [SerializeField]
    private double chancePerLoop = 0.05;

    // OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
    override public void OnStateUpdate (Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (Random.value < ((chancePerLoop * Time.deltaTime) / stateInfo.length)) {
            animator.SetTrigger ("RareIdle");
        }
    }

    // OnStateExit is called before OnStateExit is called on any state inside this state machine
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called before OnStateMove is called on any state inside this state machine
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateIK is called before OnStateIK is called on any state inside this state machine
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMachineEnter is called when entering a state machine via its Entry Node
    //override public void OnStateMachineEnter (Animator animator, int stateMachinePathHash) {
    //    if (Random.value < 0.05) {
    //        animator.SetTrigger ("RareIdle");
    //    }
    //}

    // OnStateMachineExit is called when exiting a state machine via its Exit Node
    //override public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    //{
    //    
    //}
}