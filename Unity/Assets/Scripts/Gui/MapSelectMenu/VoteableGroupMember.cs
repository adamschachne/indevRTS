using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VoteableGroupMember : Voteable {
    VoteableGroup voteableGroup;
    [HideInInspector]
    public RectTransform rt;

    public override VoteInfo init (MapSelect mapSelect, short voteableID) {
        voteableGroup = this.transform.parent.GetComponent<VoteableGroup> ();
        if (voteableGroup == null) {
            Debug.Log ("Voteable Group parent not set!");
        }
        rt = GetComponent<RectTransform> ();
        voteableGroup.RegisterGroupMember (this);
        return base.init (mapSelect, voteableID);
    }

    public override void RecieveVote (short networkID, bool vote) {
        if (StateManager.state.isServer) {
            if (voteInfo.votes[networkID] != vote && (networkID == 0 || !menuState.ownerControl)) {
                voteableGroup.ProcessVote (this, networkID, vote);
            }
        }
    }

    public void RecieveVoteNoReport (short networkID, bool vote) {
        if (StateManager.state.isServer) {
            voteInfo.votes[networkID] = vote;
        }
    }

    public void RecieveVoteAndSync (short networkID, bool vote) {
        if (StateManager.state.isServer) {
            voteInfo.votes[networkID] = vote;
            mapSelect.SendSync ();
        }
    }
}