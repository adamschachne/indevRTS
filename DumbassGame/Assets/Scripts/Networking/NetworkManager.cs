using System.Collections;
using System.Collections.Generic;
using System.Text;
using Byn.Net;
using Newtonsoft.Json;
using UnityEngine;

public class NetworkManager : MonoBehaviour {

    private IBasicNetwork mNetwork = null;
    private List<ConnectionId> mConnections = new List<ConnectionId> ();

    // Connection Info
    public string uSignalingUrl = "ws://riftwalk.io:12776/chatapp";
    public string uIceServer = "stun:riftwalk.io:12779";
    public string uIceServerUser = "";
    public string uIceServerPassword = "";
    public string uIceServer2 = "stun:stun.l.google.com:19302";
    [ReadOnly]
    public short networkID = 0;

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

    public void OnInputEndEdit () {
        if (Input.GetKey (KeyCode.Return)) {
            // join room
            OnJoinRoomClicked ();
        }
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
        mConnections = new List<ConnectionId> ();
        Cleanup ();
        state.LeaveGame ();
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

    private void SendString (string msg, bool reliable = true) {
        if (mNetwork == null || mConnections.Count == 0) {
            Debug.Log ("No connection. Can't send message.");
        } else {
            byte[] msgData = Encoding.UTF8.GetBytes (msg);
            foreach (ConnectionId id in mConnections) {
                mNetwork.SendData (id, msgData, 0, msgData.Length, reliable);
            }
        }
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

        syncData.units = netUnits;

        // send the new unit to connections
        SendMessage (syncData);
    }

    public void SendMessage (Message message, bool toBatch = true) {
        if (message != null) {
            if (toBatch) {
                batch.Add (message);
            } else {
                SendString (JsonConvert.SerializeObject (message, jssettings), true);
            }
        } else {
            Debug.Log ("You tried to send a null message!");
        }
    }

    private void SendBatch () {
        Batch cmdBatch = new Batch {
            cmds = batch
        };

        SendMessage (cmdBatch, false);
        batch.Clear ();
    }

    public void HandleRequestUnit (RequestUnit ru) {
        Debug.Log ("HANDLING REQUEST UNIT");
        // Server Only
        if (state.isServer == false) {
            return;
        }

        // Do logic to check if they can add a unit
        // if they cant, do nothing
        // if they can, make a unit and send an addUnit message out
        short netID = ru.ownerID;
        StateManager.EntityType unitType = ru.unitType;
        GameObject unit = state.addUnit (netID, unitType, null);

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

    public void requestNewUnit (StateManager.EntityType unitType, float x, float z) {
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
            SendBatch ();

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
                        // regardless of server or client, save this connection so it can be used
                        mConnections.Add (connection);

                        // CLIENT ONLY
                        if (!state.isServer) {
                            // currently, the client is always 1 TODO
                            networkID = 1;
                            state.gui.SetUnitIconPosition (state.isServer);
                            /*
                            if (state.inGame == false) {
                                Debug.Log ("starting game");
                                state.StartGame ();
                            }
                            */
                            SendMessage (new Connected {
                                playerID = networkID
                            });
                            string serverAddress = evt.Info;
                            state.gui.mapSelect.init (serverAddress);
                        }
                        break;
                    case NetEventType.ConnectionFailed:
                        //Outgoing connection failed. Inform the user.
                        Debug.Log ("Connection failed");
                        Reset ();
                        break;
                    case NetEventType.Disconnected:
                        mConnections.Remove (evt.ConnectionId);
                        // A connection was disconnected
                        Debug.Log ("Local Connection ID " + evt.ConnectionId + " disconnected");
                        if (state.isServer == false) {
                            Reset ();
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
}