using System.Collections;
using System.Collections.Generic;
using System.Text;
using Byn.Net;
using Newtonsoft.Json;
using UnityEngine;

public class NetworkManager : MonoBehaviour {

    private IBasicNetwork mNetwork = null;
    private List<ConnectionId> mConnections = new List<ConnectionId> ();
    private short[] playerSlots = new short[3];
    private string lobbyAddress = "";
    // Connection Info
    public string uSignalingUrl = "ws://riftwalk.io:12776/chatapp";
    public string uIceServer = "stun:riftwalk.io:12779";
    public string uIceServerUser = "";
    public string uIceServerPassword = "";
    public string uIceServer2 = "stun:stun.l.google.com:19302";
    [ReadOnly]
    public short networkID = 0;
    private ConnectionId serverID = ConnectionId.INVALID;
    private StateManager state;
    private List<Message> batch;
    JsonSerializerSettings jssettings;

    // Use this for initialization
    void Start () {
        state = GetComponent<StateManager> ();
        jssettings = new JsonSerializerSettings ();
        jssettings.TypeNameHandling = TypeNameHandling.All;
    }

    private void Setup () {
        Debug.Log ("Initializing webrtc network");
        mNetwork = WebRtcNetworkFactory.Instance.CreateDefault (uSignalingUrl, new IceServer[] { new IceServer (uIceServer, uIceServerUser, uIceServerPassword), new IceServer (uIceServer2) });
        if (mNetwork != null) {
            Debug.Log ("WebRTCNetwork created");
        } else {
            Debug.Log ("Failed to access webrtc ");
        }
        batch = new List<Message> ();
    }

    public void OnJoinRoomClicked () {
        // get input value inside field
        string roomName = state.gui.inputField.text;
        // clear input
        state.gui.inputField.text = "";
        state.gui.roomID.text = "RoomID: " + roomName;
        Debug.Log ("trying to connect to: " + roomName);
        // connect to this room
        JoinRoom (roomName);
    }

    public void OnCreateRoomClicked () {
        Setup ();
        // All players will connect to the same room for now TODO
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        System.Random rand = new System.Random ();
        string roomName = "";
        for (int i = 0; i < 6; i++) {
            roomName += chars[rand.Next (chars.Length)];
        }
        mNetwork.StartServer (roomName);
        Debug.Log ("StartServer " + roomName);
    }

    private void JoinRoom (string address) {
        Setup ();
        mNetwork.Connect (address);
        Debug.Log ("Connecting to " + address + " ...");
    }

    public void Reset () {
        Debug.Log ("Cleanup!");
        state.isServer = false;
        serverID = ConnectionId.INVALID;
        networkID = 0;
        mConnections = new List<ConnectionId> ();
        playerSlots = new short[3];
        lobbyAddress = "";
        Cleanup ();
        if(state.inGame) {
            state.LeaveGame ();
        }
        else {
            state.gui.Cleanup();
        }
    }

    private void Cleanup () {
        if (mNetwork != null) {
            mNetwork.Dispose ();
        }
        mNetwork = null;
    }

    private void OnDestroy () {
        if (mNetwork != null) {
            Cleanup ();
        }
    }

    private void SendString (string msg, short connectionID = -1, bool reliable = true) {
        if (mNetwork == null || mConnections.Count == 0) {
            Debug.Log ("No connection. Can't send message.");
        } else {
            byte[] msgData = Encoding.UTF8.GetBytes (msg);
            if(connectionID == -1) {
                Debug.Log("Sending message to all.");
                foreach (ConnectionId id in mConnections) {
                    mNetwork.SendData (id, msgData, 0, msgData.Length, reliable);
                }
            }
            else {
                foreach(ConnectionId id in mConnections) {
                    if(id.id == connectionID) {
                        Debug.Log("Sending message just to " + connectionID);
                        mNetwork.SendData(id, msgData, 0, msgData.Length, reliable);
                        return;
                    }
                }
                Debug.Log("Could not send message to connectionID: " + connectionID);
            }
        }
    }

    public void SetupClient(short playerID, string address) {
        this.networkID = playerID;
        Debug.Log ("Server started. Address: " + address);
        state.gui.MapSelectMenu ();
        state.gui.mapSelect.init (address);
        state.gui.SetUnitIconPosition (playerID);
        SendMessage(new ConfirmClient {
            playerID = playerID
        }, serverID.id, false);
        Debug.Log("I am client " + playerID + " and my server is connection " + this.serverID);
    }

    public void SendSync () {
        // From Server Only
        if (state.isServer == false) {
            return;
        }

        SyncUnits syncData = new SyncUnits ();
        List<NetworkUnit> netUnits = new List<NetworkUnit> ();

        // add all existing units to the list
        foreach (Transform gameUnits in state.gameUnits.transform) {
            if (gameUnits.name.Split ('-') [0].Equals ("GU")) {
                // get all the units in this GO
                foreach (Transform unit in gameUnits) {
                    var networkUnit = new NetworkUnit ();
                    short ownerID = short.Parse (gameUnits.name.Split ('-') [1]);
                    networkUnit.ownerID = ownerID;
                    networkUnit.id = unit.name;
                    networkUnit.unitType = unit.gameObject.GetComponent<UnitController> ().type;
                    netUnits.Add (networkUnit);
                }
            } else if (gameUnits.name.Split ('-') [0].Equals ("IO")) {
                foreach (Transform unit in gameUnits) {
                    if (unit.gameObject.GetComponent<FlagActions> () == null) {
                        var networkUnit = new NetworkUnit ();
                        short ownerID = short.Parse (gameUnits.name.Split ('-') [1]);
                        networkUnit.ownerID = ownerID;
                        networkUnit.id = unit.name;
                        networkUnit.unitType = unit.gameObject.GetComponent<Interactable> ().type;
                        netUnits.Add (networkUnit);
                    }
                }
            }
        }

        syncData.units = netUnits.ToArray ();
        // send the new unit to connections
        SendMessage (syncData);
    }

    public void SendMessage (Message message, short connectionID = -1, bool toBatch = true) {
        if(serverID == ConnectionId.INVALID) {
            if (message != null) {
                if(connectionID == -1) {
                    if (toBatch) {
                        batch.Add (message);
                    } else {
                        SendString (JsonConvert.SerializeObject (message, jssettings), -1, true);
                    }
                }
                else {
                    SendString(JsonConvert.SerializeObject(message, jssettings), connectionID, true);
                }
            } else {
                Debug.Log ("You tried to send a null message!");
            }
        }
        else {
            SendString(JsonConvert.SerializeObject(message, jssettings), serverID.id, true);
        }
    }

    public void SendMessageToServer(Message message, bool toBatch = true) {
        if(serverID != ConnectionId.INVALID) {
            SendMessage(message, serverID.id, toBatch);
        } else {
            Debug.Log("You are not properly connected to the server!");
        }
    }

    private void SendBatch () {
        if (batch.Count > 0) {
            Batch cmdBatch = new Batch {
                cmds = batch.ToArray ()
            };

            SendMessage (cmdBatch, -1, false);
            batch.Clear ();
        }
    }

    public void HandleRequestUnit (RequestUnit ru) {
        Debug.Log ("HANDLING REQUEST UNIT");

        // Do logic to check if they can add a unit
        // if they cant, do nothing
        // if they can, make a unit and send an addUnit message out
        short netID = ru.ownerID;
        StateManager.EntityType unitType = ru.unitType;
        GameObject unit = state.AddUnit (netID, unitType, null);

        AddUnit addUnit = new AddUnit ();
        addUnit.ownerID = netID;
        addUnit.unit = new NetworkUnit ();
        addUnit.unit.id = unit.name;
        addUnit.unit.unitType = unitType;

        // send the new unit to connections
        SendMessage (addUnit);
    }

    private void HandleIncommingMessage (ref NetworkEvent evt) {
        MessageDataBuffer buffer = (MessageDataBuffer) evt.MessageData;
        string msg = Encoding.UTF8.GetString (buffer.Buffer, 0, buffer.ContentLength);

        try {
            //Debug.Log ("Processing message: " + msg);
            Message message = JsonConvert.DeserializeObject<Message> (msg, jssettings);

            if (message == null) {
                Debug.Log ("Json was null!");
            }
            message.process ();

            if(state.isServer && message.mt != MessageType.ServerOnly) {
                foreach(ConnectionId id in mConnections) {
                    if(id != evt.ConnectionId) {
                        SendMessage(message, id.id, false);
                    }
                }
            }

        } catch (System.NullReferenceException e) {
            Debug.Log ("Exception thrown: " + e.ToString ());
            if (msg != null)
                Debug.Log (msg);
            else
                Debug.Log ("A message was null :(");
        }
        //return the buffer so the network can reuse it
        buffer.Dispose ();
    }

    public void requestNewUnit (StateManager.EntityType unitType) {
        if (state.isServer) {
            RequestUnit req = new RequestUnit ();
            req.ownerID = networkID;
            req.unitType = unitType;
            HandleRequestUnit (req);
        } else {
            RequestUnit req = new RequestUnit ();
            req.ownerID = networkID;
            req.unitType = unitType;
            SendMessage (req);
        }
    }

    private void FixedUpdate () {
        //check if the network was created
        if (mNetwork != null) {
            //sync all units before processing events
            if(batch.Count > 0) {
                SendBatch ();
            }

            mNetwork.Update ();
            NetworkEvent evt;
            while (mNetwork != null && mNetwork.Dequeue (out evt)) {
                switch (evt.Type) {
                    case NetEventType.ServerInitialized:
                        //server initialized message received
                        state.isServer = true;
                        // currently the server is always 0
                        networkID = 0;

                        string address = evt.Info;
                        state.gui.roomID.text = "Room ID: " + address;
                        Debug.Log ("Server started. Address: " + address);
                        lobbyAddress = address;
                        state.gui.SetUnitIconPosition (networkID);
                        state.gui.MapSelectMenu ();
                        state.gui.mapSelect.init (address);

                        break;
                    case NetEventType.ServerInitFailed:
                        state.isServer = false;
                        Debug.Log ("Server start failed.");
                        Reset ();
                        break;
                    case NetEventType.ServerClosed:
                        state.isServer = false;
                        state.LeaveGame ();
                        Debug.Log ("Server closed. No incoming connections possible until restart.");
                        state.gui.ConnectMenu ();
                        break;
                    case NetEventType.NewConnection:
                        ConnectionId connection = evt.ConnectionId;
                        Debug.Log ("new conection: " + connection.id.ToString ());
                        
                        if(!state.isServer && serverID == ConnectionId.INVALID) {
                            //clients unconditionally connect to their server and wait for a response.
                            mConnections.Add (connection);
                            serverID = connection;
                        }
                        else if(state.inGame) {
                            mConnections.Add(connection);
                            SendMessage(new RefuseConnection {
                                reason = "The game you tried to join has already begun."
                            }, evt.ConnectionId.id, false);
                            mConnections.Remove(connection);
                        }
                        else if(state.isServer) {
                            bool openSlot = false;
                            //check if there is an open slot in the playerSlots list
                            for(short i = 0; i < playerSlots.Length; i++) {
                                //if there is, occupy that slot with the connectionID of the client who connected
                                //and add the connection to our connections list.
                                if(playerSlots[i] == 0) {
                                    mConnections.Add (connection);
                                    openSlot = true;
                                    playerSlots[i] = evt.ConnectionId.id;
                                    SendMessage(new AssignClient {
                                        playerID = (short)(i + 1),
                                        address = lobbyAddress
                                    }, evt.ConnectionId.id, false);
                                    break;
                                }
                            }

                            //if there was not an open slot, refuse the connection
                            if(!openSlot) {
                                mConnections.Add(connection);
                                SendMessage(new RefuseConnection {
                                reason = "The lobby you tried to join was full."
                            }, evt.ConnectionId.id, false);
                                mConnections.Remove(connection);
                            }
                        }

                        break;
                    case NetEventType.ConnectionFailed:
                        //Outgoing connection failed. Inform the user.
                        Debug.Log ("Connection failed");
                        Reset ();
                        break;
                    case NetEventType.Disconnected:
                        // A connection was disconnected
                        Debug.Log ("Local Connection ID " + evt.ConnectionId + " disconnected");
                        if(state.isServer) {
                            if (state.inGame) {
                                for(short i = 0; i < playerSlots.Length; i++) {
                                    if(playerSlots[i] == evt.ConnectionId.id) {
                                        Reset ();
                                        break;
                                    }
                                }
                            }
                            else {
                                for(short i = 0; i < playerSlots.Length; i++) {
                                    if(playerSlots[i] == evt.ConnectionId.id) {
                                        mConnections.Remove(evt.ConnectionId);
                                        state.gui.mapSelect.RecieveDisconnected((short)(i + 1));
                                        playerSlots[i] = 0;
                                        break;
                                    }
                                }
                            }
                        }
                        else {
                            if(evt.ConnectionId.id == serverID.id) {
                                Reset();
                                state.gui.Cleanup();
                            }
                        }
                        
                        break;
                    case NetEventType.ReliableMessageReceived:
                    case NetEventType.UnreliableMessageReceived:
                        //Debug.Log ("Recieved message: " + evt);
                        HandleIncommingMessage (ref evt);
                        break;
                }
            }
            //finish this update by flushing the messages out if the network wasn't destroyed during update
            if (mNetwork != null)
                mNetwork.Flush ();
        }
    }

    public int GetConnectionsCount() {
        return mConnections.Count;
    }
}