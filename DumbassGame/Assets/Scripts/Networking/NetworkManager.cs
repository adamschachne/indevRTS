﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Byn.Net;

public class NetworkManager : MonoBehaviour {

    private IBasicNetwork mNetwork = null;
    private List<ConnectionId> mConnections = new List<ConnectionId>();

    // Connection Info
    public string uSignalingUrl = "ws://riftwalk.io:12776/chatapp";
    public string uIceServer = "stun:riftwalk.io:12779";
    public string uIceServerUser = "";
    public string uIceServerPassword = "";
    public string uIceServer2 = "stun:stun.l.google.com:19302";
    [ReadOnly]
    public short networkID = 0;

    private StateManager state;

    // Use this for initialization
    void Start() {
        state = GetComponent<StateManager>();
    }

    private void Setup() {
        Debug.Log("Initializing webrtc network");
        mNetwork = WebRtcNetworkFactory.Instance.CreateDefault(uSignalingUrl, new IceServer[] { new IceServer(uIceServer, uIceServerUser, uIceServerPassword), new IceServer(uIceServer2) });
        if (mNetwork != null) {
            Debug.Log("WebRTCNetwork created");
        } else {
            Debug.Log("Failed to access webrtc ");
        }
    }

    public void OnInputEndEdit() {
        if (Input.GetKey(KeyCode.Return)) {
            // join room
            OnJoinRoomClicked();
        }
    }

    public void OnJoinRoomClicked() {
        Setup();
        // get input value inside field
        string roomName = state.gui.inputField.text;
        // clear input
        state.gui.inputField.text = "";
        state.gui.roomID.text = "RoomID: " + roomName;
        Debug.Log("trying to connect to: " + roomName);
        // connect to this room
        JoinRoom(roomName);
    }

    public void OnCreateRoomClicked() {
        CreateRoom();
    }

    public void CreateRoom() {
        Setup();
        // All players will connect to the same room for now TODO
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        System.Random rand = new System.Random();
        string roomName = "";
        for (int i = 0; i < 6; i++) {
            roomName += chars[rand.Next(chars.Length)];
        }
        mNetwork.StartServer(roomName);
        Debug.Log("StartServer " + roomName);
    }

    private void JoinRoom(string address) {
        Setup();
        mNetwork.Connect(address);
        Debug.Log("Connecting to " + address + " ...");
    }

    public void Reset() {
        Debug.Log("Cleanup!");
        state.isServer = false;
        mConnections = new List<ConnectionId>();
        Cleanup();
        state.LeaveGame();
    }

    private void Cleanup() {
        if (mNetwork != null) {
            mNetwork.Dispose();
        }
        mNetwork = null;
    }

    private void OnDestroy() {
        if (mNetwork != null) {
            Cleanup();
        }
    }

    private void SendString(string msg, bool reliable = true) {
        if (mNetwork == null || mConnections.Count == 0) {
            Debug.Log("No connection. Can't send message.");
        } else {
            byte[] msgData = Encoding.UTF8.GetBytes(msg);
            foreach (ConnectionId id in mConnections) {
                mNetwork.SendData(id, msgData, 0, msgData.Length, reliable);
            }
        }
    }

    private void SendSync() {
        // From Client Only
        if (state.isServer == false) {
            return;
        }

        var syncData = new SyncUnits();
        List<NetworkUnit> netUnits = new List<NetworkUnit>();

        // add all existing units to the list
        foreach (Transform gameUnits in state.gameUnits.transform) {
            // get all the units in this GO
            foreach (Transform unit in gameUnits) {
                var networkUnit = new NetworkUnit();
                short ownerID = short.Parse(gameUnits.name.Split('-')[1]);
                networkUnit.ownerID = ownerID;
                networkUnit.id = unit.name;
                networkUnit.x = unit.position.x;
                networkUnit.z = unit.position.z;
                netUnits.Add(networkUnit);
            }           
        }

        syncData.units = netUnits;
        NetworkJSON netjson = new NetworkJSON();
        netjson.json = JsonUtility.ToJson(syncData);
        netjson.type = NetTypes.SYNC;

        // send the new unit to connections
        SendString(JsonUtility.ToJson(netjson), true);
    }

    public void SendMove(string name, float x, float z) {
        Debug.Log("SENDING MOVE COMMAND");
        Move move = new Move {
            id = name,
            ownerID = networkID,
            x = x,
            z = z
        };

        NetworkJSON netjson = new NetworkJSON {
            json = JsonUtility.ToJson(move),
            type = NetTypes.MOVE_UNIT
        };

        // send the new unit to connections
        SendString(JsonUtility.ToJson(netjson), true);
    }

    private void HandleSync(SyncUnits su) {
        Debug.Log("HANDLING SYNC");
        // Client Only
        if (state.isServer == true) {
            return;
        }
        // clear any existing game units
        //state.CleanObjects();
        // create the top level
        //state.CreateTopLevelGameUnits();

        foreach (NetworkUnit netUnit in su.units) {          
            short ownerID = netUnit.ownerID;
            GameObject unit = state.addUnit(ownerID, netUnit.id);
            unit.transform.SetPositionAndRotation(new Vector3(netUnit.x, unit.transform.position.y, netUnit.z), unit.transform.rotation);
        }
    }

    private void HandleAddUnit(AddUnit au) {
        Debug.Log("HANDLING ADD UNIT");
        // Client Only
        if (state.isServer == true) {
            return;
        }
        GameObject unit = state.addUnit(networkID, au.unit.id);
        unit.transform.SetPositionAndRotation(new Vector3(au.unit.x, unit.transform.position.y, au.unit.z), unit.transform.rotation);
    }

    private void HandleRequestUnit(RequestUnit ru) {
        Debug.Log("HANDLING REQUEST UNIT");
        // Server Only
        if (state.isServer == false) {
            return;
        }

        // Do logic to check if they can add a unit
        // if they cant, do nothing
        // if they can, make a unit and send an addUnit message out
        short netID = ru.ownerID;
        GameObject unit = state.addUnit(netID);
        Vector3 deltaPos = new Vector3(Random.Range(-3, 3), 0, Random.Range(-3, 3));
        unit.transform.position += deltaPos;

        AddUnit addUnit = new AddUnit();
        addUnit.ownerID = networkID;
        addUnit.unit = new NetworkUnit();
        addUnit.unit.id = unit.name;
        addUnit.unit.x = unit.transform.position.x;
        addUnit.unit.z = unit.transform.position.z;

        NetworkJSON netjson = new NetworkJSON();
        netjson.json = JsonUtility.ToJson(addUnit);
        netjson.type = NetTypes.ADD_UNIT;

        // send the new unit to connections
        SendString(JsonUtility.ToJson(netjson), true);
    }

    private void HandleMoveCommand(Move move) {
        Debug.Log("HANDLING MOVE COMMAND");
        state.MoveCommand(move.ownerID, move.id, move.x, move.z);
    }

    private void HandleIncommingMessage(ref NetworkEvent evt) {
        short requestID = evt.ConnectionId.id;
        MessageDataBuffer buffer = (MessageDataBuffer)evt.MessageData;
        string msg = Encoding.UTF8.GetString(buffer.Buffer, 0, buffer.ContentLength);
        try {
            NetworkJSON netjson = JsonUtility.FromJson<NetworkJSON>(msg);
            switch (netjson.type) {
                case NetTypes.SYNC:
                    SyncUnits su = JsonUtility.FromJson<SyncUnits>(netjson.json);
                    HandleSync(su);
                    break;
                case NetTypes.ADD_UNIT:
                    Debug.Log("unit added");
                    AddUnit au = JsonUtility.FromJson<AddUnit>(netjson.json);
                    HandleAddUnit(au);
                    break;
                case NetTypes.REQUEST_UNIT:
                    Debug.Log("requested add unit from: " + requestID);
                    RequestUnit ru = JsonUtility.FromJson<RequestUnit>(netjson.json);
                    HandleRequestUnit(ru);
                    break;
                case NetTypes.MOVE_UNIT:
                    Debug.Log("requested move unit from: " + requestID);
                    Move move = JsonUtility.FromJson<Move>(netjson.json);
                    HandleMoveCommand(move);
                    break;
                default:
                    Debug.Log("UNKNOWN TYPE: " + netjson.type);
                    break;
            }
        } catch (System.Exception e) {
            Debug.Log(e.InnerException.ToString() + ": " + msg);
        }
        //return the buffer so the network can reuse it
        buffer.Dispose();
    }

    private void FixedUpdate() {
        //check if the network was created
        if (mNetwork != null) {
            mNetwork.Update();
            NetworkEvent evt;
            while (mNetwork != null && mNetwork.Dequeue(out evt)) {
                switch (evt.Type) {
                    case NetEventType.ServerInitialized:
                        //server initialized message received
                        state.isServer = true;
                        // currently the server is always 0
                        networkID = 0;
                        state.StartGame();
                        string address = evt.Info;
                        state.gui.roomID.text = "Room ID: " + address;
                        Debug.Log("Server started. Address: " + address);
                        break;               
                    case NetEventType.ServerInitFailed:
                        state.isServer = false;
                        Debug.Log("Server start failed.");
                        Reset();
                        break;                 
                    case NetEventType.ServerClosed:
                        state.isServer = false;
                        state.LeaveGame();
                        Debug.Log("Server closed. No incoming connections possible until restart.");
                        break;
                    case NetEventType.NewConnection:
                        ConnectionId connection = evt.ConnectionId;
                        Debug.Log("new conection: " + connection.id.ToString());
                        // regardless of server or client, save this connection so it can be used
                        mConnections.Add(connection);

                        // SERVER ONLY
                        if (state.isServer == true) {
                            SendSync();
                        }
                        // CLIENT ONLY
                        else {
                            // currently, the client is always 1 TODO
                            networkID = 1;
                            if (state.inGame == false) {
                                Debug.Log("starting game");
                                state.StartGame();
                            }
                            // send out a request for a unit
                            RequestUnit req = new RequestUnit();
                            req.ownerID = networkID;
                            NetworkJSON netjson = new NetworkJSON();
                            netjson.json = JsonUtility.ToJson(req);
                            netjson.type = NetTypes.REQUEST_UNIT;
                            SendString(JsonUtility.ToJson(netjson), true);
                        }                        
                        break;
                    case NetEventType.ConnectionFailed:
                        //Outgoing connection failed. Inform the user.
                        Debug.Log("Connection failed");
                        Reset();
                        break;
                    case NetEventType.Disconnected:
                        mConnections.Remove(evt.ConnectionId);
                        // A connection was disconnected
                        Debug.Log("Local Connection ID " + evt.ConnectionId + " disconnected");
                        if (state.isServer == false) {
                            Reset();
                        }
                        break;
                    case NetEventType.ReliableMessageReceived:
                    case NetEventType.UnreliableMessageReceived:
                        HandleIncommingMessage(ref evt);
                        break;
                }
            }
            //finish this update by flushing the messages out if the network wasn't destroyed during update
            if (mNetwork != null)
                mNetwork.Flush();
        }
    }
}