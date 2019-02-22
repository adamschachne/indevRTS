using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapSelect {
    private StateManager state;
    private GuiManager gui;
    private NetworkManager network;
    private GameObject[] players;
    private Toggle ownerControl;
    private Text roomID;
    private Voteable[] voteables;

    [Serializable]
    public class MapSelectState {
        public MapSelectState (int voteableLength) {
            this.connectedPlayers = new bool[4];
            this.ownerControl = false;
            voteables = new Voteable.VoteInfo[voteableLength];
        }

        public void MirrorState (MapSelectState other) {
            for (int i = 0; i < 4; ++i) {
                connectedPlayers[i] = other.connectedPlayers[i];
            }
            ownerControl = other.ownerControl;

            for (int i = 0; i < voteables.Length; ++i) {
                voteables[i].MirrorState (other.voteables[i]);
            }
        }
        public bool[] connectedPlayers;
        public bool ownerControl;
        public Voteable.VoteInfo[] voteables;

    }

    private MapSelectState mapState;

    public MapSelect (GameObject mapSelectMenu) {
        state = StateManager.state;
        gui = state.gui;
        network = state.network;
        players = new GameObject[4];
        GameObject playerList = mapSelectMenu.transform.Find ("Player List").gameObject;
        for (int i = 0; i < 4; ++i) {
            players[i] = playerList.transform.GetChild (i).gameObject;
        }
        ownerControl = mapSelectMenu.transform.Find ("Owner Control").GetComponent<Toggle> ();
        roomID = mapSelectMenu.transform.Find ("RoomID").GetComponent<Text> ();
        voteables = mapSelectMenu.GetComponentsInChildren<Voteable> (true);
    }

    public void init (string address) {
        mapState = new MapSelectState (voteables.Length);

        for (short i = 0; i < voteables.Length; ++i) {
            mapState.voteables[i] = voteables[i].init (this, i);
        }

        ownerControl.isOn = false;

        if (state.isServer) {
            roomID.text = "Room ID: " + address;
            mapState.connectedPlayers[0] = true;
            players[0].SetActive (true);
            players[1].SetActive (false);
            players[2].SetActive (false);
            players[3].SetActive (false);

            ownerControl.onValueChanged.AddListener (delegate (bool toggleIsOn) {
                this.mapState.ownerControl = toggleIsOn;
                SendSync ();
            });
        } else {
            ownerControl.interactable = false;
        }
    }
    public MapSelectState GetMapSelectState () {
        return mapState;
    }

    //networking functions to send and recieve messages

    public void SendConnected () {
        if (!state.isServer) {
            network.SendMessage (new Connected {
                playerID = network.networkID
            });
        }
    }

    public void RecieveConnected (short playerid) {
        if (state.isServer) {
            players[playerid].SetActive (true);
            mapState.connectedPlayers[playerid] = true;
            SendSync ();
        }
    }

    public void RecieveVote (short voteableID, short networkID, bool vote) {
        voteables[voteableID].RecieveVote (networkID, vote);
    }

    public void SendSync () {
        if (state.isServer) {
            network.SendMessage (new SyncMapSelect {
                state = this.mapState
            });
        }
    }

    public void RecieveSync (MapSelectState mapState) {
        if (!state.isServer) {
            this.mapState.MirrorState (mapState);
            for (int i = 0; i < 4; ++i) {
                players[i].SetActive (mapState.connectedPlayers[i]);
            }
            ownerControl.isOn = mapState.ownerControl;
        }
        gui.MapSelectMenu ();
    }
}