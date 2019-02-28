using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VoteableGroup : MonoBehaviour {
    List<VoteableGroupMember> groupMembers;
    Image border;
    RectTransform rt;
    Vector2 sizeDeltaRef;
    private const float yBuffer = 100f;
    private const float xBuffer = 50f;
    public VoteableGroup () {
        groupMembers = new List<VoteableGroupMember> ();
    }

    void Awake () {
        if (border == null) {
            border = this.transform.GetChild (0).GetComponent<Image> ();
            rt = this.transform.GetChild (0).GetComponent<RectTransform> ();
            sizeDeltaRef = new Vector2 (xBuffer, yBuffer);
        }
    }

    public void RegisterGroupMember (VoteableGroupMember v) {
        groupMembers.Add (v);

        sizeDeltaRef.x += v.rt.sizeDelta.x + xBuffer;
        if (sizeDeltaRef.y - yBuffer < v.rt.sizeDelta.y) {
            sizeDeltaRef.y = v.rt.sizeDelta.y + yBuffer;
        }
        rt.sizeDelta = sizeDeltaRef;

        Vector3 avgPos = new Vector3 ();
        foreach (VoteableGroupMember vgm in groupMembers) {
            avgPos += vgm.rt.position;
        }

        avgPos /= groupMembers.Count;
        rt.position = avgPos;

    }

    public void ProcessVote (VoteableGroupMember v, short networkID, bool vote) {
        foreach (VoteableGroupMember vgm in groupMembers) {
            if (vgm != v) {
                vgm.RecieveVoteNoReport (networkID, false);
            }
        }

        v.RecieveVoteAndSync (networkID, vote);
    }
}