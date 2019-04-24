using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VoteableGroup : MonoBehaviour {
    List<VoteableGroupMember> groupMembers;
    Image border;
    RectTransform rt;
    float left;
    float right;
    float top;
    float bot;
    private const float yBuffer = 50f;
    private const float xBuffer = 50f;
    public VoteableGroup () {
        groupMembers = new List<VoteableGroupMember> ();
    }

    void Awake () {
        if (border == null) {
            border = this.transform.GetChild (0).GetComponent<Image> ();
            rt = this.transform.GetChild (0).GetComponent<RectTransform> ();
        }
    }

    public void RegisterGroupMember (VoteableGroupMember v) {
        if (!groupMembers.Contains (v)) {
            groupMembers.Add (v);

            if(v.rt.localPosition.x - v.rt.sizeDelta.x/2 - xBuffer < left) left = v.rt.localPosition.x - v.rt.sizeDelta.x/2 - xBuffer;
            if(v.rt.localPosition.x + v.rt.sizeDelta.x/2 + xBuffer > right) right = v.rt.localPosition.x + v.rt.sizeDelta.x/2 + xBuffer;
            if(v.rt.localPosition.y - v.rt.sizeDelta.y/2 - yBuffer < bot) bot = v.rt.localPosition.y - v.rt.sizeDelta.y/2 - yBuffer;
            if(v.rt.localPosition.y + v.rt.sizeDelta.y/2 + yBuffer > top) top = v.rt.localPosition.y + v.rt.sizeDelta.y/2 + yBuffer;

            
            rt.sizeDelta = new Vector2(Mathf.Abs(left) + right, Mathf.Abs(bot) + top);

            
            rt.localPosition = new Vector3((right+left)/2, (top+bot)/2, 0);
        }

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