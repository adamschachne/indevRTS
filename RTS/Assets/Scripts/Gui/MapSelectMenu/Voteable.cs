using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Voteable : MonoBehaviour {

    [Serializable]
    public class VoteInfo {
        public bool[] votes;
        public bool decision;
        public VoteInfo () {
            this.votes = new bool[4];
            this.decision = false;
        }

        public void MirrorState (VoteInfo other) {
            for (int i = 0; i < 4; ++i) {
                this.votes[i] = other.votes[i];
            }
            this.decision = other.decision;
        }

        public void ClearVotes(short playerid) {
            this.votes[playerid] = false;
        }
    }

    [System.Serializable]
    public class VotableEvent : UnityEvent<Voteable> { }
    protected MapSelect mapSelect;
    [SerializeField]
    [ReadOnly]
    private short voteableID;
    protected MapSelect.MapSelectState menuState;
    private RectTransform bounds;
    [SerializeField]
    private GameObject votePrefab;
    private GameObject[] votePips;
    private const float topOffset = 10f;
    public VoteInfo voteInfo;
    public VotableEvent decisionCallback;
    private bool triggered;

    //init returns the newly-active voteInfo data
    public virtual VoteInfo init (MapSelect mapSelect, short voteableID) {
        if (decisionCallback == null) {
            Debug.Log ("ERROR IN: " + this.gameObject.name + " object, decision callback was null!");
        }

        //init state info and stuff
        voteInfo = new VoteInfo ();
        this.voteableID = voteableID;
        this.mapSelect = mapSelect;
        this.menuState = mapSelect.GetMapSelectState ();
        this.bounds = GetComponent<RectTransform> ();
        float votePrefabHeight = votePrefab.GetComponent<RectTransform> ().rect.height;

        //init and position votepip objects
        if (votePips == null) {
            votePips = new GameObject[4];
        }
        for (short i = 0; i < 4; ++i) {
            if (votePips[i] == null) {
                votePips[i] = Instantiate (votePrefab, this.gameObject.transform);
            }
            votePips[i].GetComponent<Image> ().color = GuiManager.GetColorByNetID (i);
            votePips[i].SetActive (false);
            float scaleFactor = (float) i / 3.0f;
            Vector3 newPosition = new Vector3 (bounds.rect.width * scaleFactor - bounds.rect.width / 2, bounds.rect.height / 2 + votePrefabHeight + topOffset, 0);
            votePips[i].GetComponent<RectTransform> ().localPosition = newPosition;
        }

        //setup onclick listener to toggle vote
        GetComponent<Button> ().onClick.RemoveAllListeners ();
        GetComponent<Button> ().onClick.AddListener (() => {
            if (StateManager.state.isServer) {
                RecieveVote (0, !voteInfo.votes[StateManager.state.network.networkID]);
            } else {
                //send a vote command for this object
                StateManager.state.network.SendMessage (new Vote {
                    networkID = StateManager.state.network.networkID,
                        votableID = this.voteableID,
                        vote = !voteInfo.votes[StateManager.state.network.networkID]
                });
            }
        });

        //setup callback for when decision is true
        triggered = false;

        //return voteInfo back to MapSelect to be used as a reference
        return voteInfo;
    }

    void Update () {
        if (voteInfo.votes != null) {
            if (menuState.ownerControl) {
                voteInfo.decision = voteInfo.votes[0];
                votePips[0].SetActive (voteInfo.votes[0]);
                votePips[1].SetActive (false);
                votePips[2].SetActive (false);
                votePips[3].SetActive (false);
            } else {
                float totalConnected = 0;
                float totalVoted = 0;
                for (int i = 0; i < 4; ++i) {
                    if (menuState.connectedPlayers[i]) {
                        totalConnected++;
                        votePips[i].SetActive (voteInfo.votes[i]);
                        if (voteInfo.votes[i]) {
                            totalVoted++;
                        }
                    }
                }
                voteInfo.decision = (totalVoted / totalConnected) > 0.5f;
            }
        }

        //only the server will call decision callbacks
        if (StateManager.state.isServer) {
            //trigger the decision callback when consensus has been made
            if (voteInfo.decision && !triggered) {
                decisionCallback.Invoke (this);
                triggered = true;
            }
            //otherwise, if the decision switches back to false, un-trigger this so it can trigger again
            else if (!voteInfo.decision && triggered) {
                triggered = false;
            }
        }
    }

    public virtual void RecieveVote (short networkID, bool vote) {
        if (StateManager.state.isServer) {
            if (voteInfo.votes[networkID] != vote && (networkID == 0 || !menuState.ownerControl)) {
                voteInfo.votes[networkID] = vote;
                mapSelect.SendSync ();
            }
        }
    }

    public short GetVotableID () {
        return voteableID;
    }
}