using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapSelect {
    private StateManager state;
    private GuiManager gui;
    private NetworkManager network;
    private Text[] players;
    private Toggle ownerControl;
    private Text roomID;
    private Transform mapButtonParent;
    private Voteable[] voteables;
    private Image mapPreview;
    private static Button startGame;

    [Serializable]
    public class MapSelectState {
        public MapSelectState (int voteableLength) {
            this.currentMapID = -1;
            this.connectedPlayers = new bool[4];
            this.ownerControl = false;
            voteables = new Voteable.VoteInfo[voteableLength];
        }

        public void MirrorState (MapSelectState other) {
            ownerControl = other.ownerControl;
            currentMapID = other.currentMapID;
            MapSelect.startGame.interactable = (currentMapID > -1);
            for (int i = 0; i < 4; ++i) {
                connectedPlayers[i] = other.connectedPlayers[i];
            }
            for (int i = 0; i < voteables.Length; ++i) {
                voteables[i].MirrorState (other.voteables[i]);
            }
        }

        public short currentMapID;
        public bool ownerControl;
        public bool[] connectedPlayers;
        public Voteable.VoteInfo[] voteables;

    }

    private MapSelectState mapState;

    public MapSelect (GameObject mapSelectMenu) {
        state = StateManager.state;
        gui = state.gui;
        network = state.network;
        players = new Text[4];
        GameObject playerList = mapSelectMenu.transform.Find ("Player List").gameObject;
        for (int i = 0; i < 4; ++i) {
            players[i] = playerList.transform.GetChild (i).GetComponent<Text>();
        }
        ownerControl = mapSelectMenu.transform.Find ("Owner Control").GetComponent<Toggle> ();
        roomID = mapSelectMenu.transform.Find ("RoomID").GetComponent<Text> ();
        mapButtonParent = mapSelectMenu.transform.Find ("MapButtons");
        mapPreview = mapSelectMenu.transform.Find ("MapPreview").GetComponent<Image> ();
        startGame = mapSelectMenu.transform.Find("Start Game").GetComponent<Button> ();

        GameObject mapButtonPrefab = Resources.Load ("Prefabs/MapButton", typeof (GameObject)) as GameObject;
        GameObject[] maps = Resources.LoadAll<GameObject> ("Maps");
        float xPos = -500f;
        float xOffset = 50f;
        float yPos = 0f;

        foreach (GameObject map in maps) {
            GameObject mapButton = UnityEngine.Object.Instantiate (mapButtonPrefab, mapButtonParent);
            MapButton buttonScript = mapButton.GetComponent<MapButton> ();
            mapButton.transform.localPosition = new Vector3 (xPos, yPos, 0);
            xPos += mapButton.GetComponent<RectTransform> ().rect.width + xOffset;
            buttonScript.init (map.GetComponent<MapData> (), mapPreview);
            mapButton.SetActive (true);
        }

        voteables = mapSelectMenu.GetComponentsInChildren<Voteable> (true);
    }

    public void init (string address) {
        mapState = new MapSelectState (voteables.Length);
        mapPreview.gameObject.SetActive (false);

        for (short i = 0; i < voteables.Length; ++i) {
            mapState.voteables[i] = voteables[i].init (this, i);
        }

        ownerControl.isOn = false;
        ownerControl.interactable = state.isServer;
        startGame.interactable = false;

        if (state.isServer) {
            roomID.text = "Room ID: " + address;
            SetPlayerConnected(0, true);
            for(short i = 1; i < 4; i++) {
                SetPlayerConnected(i, false);
            }

            ownerControl.onValueChanged.AddListener (delegate (bool toggleIsOn) {
                this.mapState.ownerControl = toggleIsOn;
                SendSync ();
            });
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

    private void SetPlayerConnected (short id, bool isConnected) {
        if(isConnected) {
            players[id].color = GuiManager.GetColorByNetID(id);
        } else {
            players[id].color = new Color(.45f, .45f, .45f, .45f);
        }
        mapState.connectedPlayers[id] = isConnected;
    }

    public void RecieveConnected (short playerid) {
        if (state.isServer) {
            SetPlayerConnected(playerid, true);
            SendSync ();
        }
    }

    public void RecieveDisconnected(short playerid) {
        if(state.isServer) {
            SetPlayerConnected(playerid, false);
            for (int i = 0; i < mapState.voteables.Length; ++i) {
                mapState.voteables[i].ClearVotes(playerid);
            }
            SendSync();
        }
        else if(playerid == 1) {
            gui.Cleanup();
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
            for (short i = 0; i < 4; ++i) {
                SetPlayerConnected(i, mapState.connectedPlayers[i]);
            }
            ownerControl.isOn = mapState.ownerControl;
        }
        gui.MapSelectMenu ();
    }

    public void SetCurrentMap (short votableID) {
        if (state.isServer) {
            mapState.currentMapID = votableID;
            SendSync ();
        }
        startGame.interactable = true;
    }

    public MapData GetMapData () {
        Voteable v = voteables[mapState.currentMapID];
        Debug.Log ("current map voteable ID: " + mapState.currentMapID);
        MapButton button = v.GetComponent<MapButton> ();
        Debug.Log ("Button being referenced: " + button);
        MapData data = button.GetMap ();
        Debug.Log ("MapData of referenced button: " + data);

        return voteables[mapState.currentMapID].GetComponent<MapButton> ().GetMap ();
    }
}