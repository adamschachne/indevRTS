using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Byn.Net;

public class NetworkManagerOLD : MonoBehaviour {

    // host network connection
    private IBasicNetwork myNetwork = null;
    // peer connection networks
    private Dictionary<string, IBasicNetwork> mNetworks = new Dictionary<string, IBasicNetwork>();
    // users connected to my network
    private Dictionary<string, ConnectionId> mConnections = new Dictionary<string, ConnectionId>();

    // Connection Info
    public string uSignalingUrl = "ws://riftwalk.io:12776/chatapp";
    public string uIceServer = "stun:riftwalk.io:12779";
    public string uIceServerUser = "";
    public string uIceServerPassword = "";
    public string uIceServer2 = "stun:stun.l.google.com:19302";
    public short networkID = -1;

    private StateManager state;

    // Use this for initialization
    void Start() {
        state = GetComponent<StateManager>();
    }

    private void Setup() {
        Debug.Log("Initializing webrtc network");
        myNetwork = WebRtcNetworkFactory.Instance.CreateDefault(uSignalingUrl, new IceServer[] { new IceServer(uIceServer, uIceServerUser, uIceServerPassword), new IceServer(uIceServer2) });
        if (myNetwork != null) {
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
        mNetworks.Add(roomName, myNetwork);
        myNetwork.StartServer(roomName);
        Debug.Log("StartServer " + roomName);
    }

    private void JoinRoom(string address) {
        var mNetwork = WebRtcNetworkFactory.Instance.CreateDefault(
            uSignalingUrl,
            new IceServer[] {
                new IceServer(uIceServer, uIceServerUser, uIceServerPassword),
                new IceServer(uIceServer2)
            }
        );
        mNetworks.Add(address, mNetwork);
        mNetwork.Connect(address);        
        Debug.Log("Connecting to " + address + " ...");
    }

    public void Reset() {
        Debug.Log("Cleanup!");
        state.isServer = false;
        mConnections = new Dictionary<string, ConnectionId>();
        Cleanup();
        state.LeaveGame();
    }

    private void Cleanup() {
        foreach (var mNetworkPair in mNetworks) {
            var mNetwork = mNetworkPair.Value;
            if (mNetwork != null) {
                mNetwork.Dispose();
            }
        }
        mNetworks.Clear();
        mNetworks = new Dictionary<string, IBasicNetwork>();
    }

    private void cleanNetwork(string networkAddress) {
        IBasicNetwork mNetwork;
        if (!mNetworks.TryGetValue(networkAddress, out mNetwork)) {
            return;
        }

        if (mNetwork != null) {
            mNetwork.Dispose();
        }

        // mNetworks.Remove(networkAddress);
    }

    public bool RequestCreateUnit() {
        // send a message to server and ask for a new unit TODO
        
        return true;
    }

    private void HandleIncommingMessage(string originAddress, ref NetworkEvent evt) {
        MessageDataBuffer buffer = (MessageDataBuffer)evt.MessageData;
        string msg = Encoding.UTF8.GetString(buffer.Buffer, 0, buffer.ContentLength);
        byte[] msgData = evt.GetDataAsByteArray();
        foreach (var mNetworkPair in mNetworks) {
            string address = mNetworkPair.Key;
            var mNetwork = mNetworkPair.Value;       
            if (address.Equals(originAddress)) {
                continue;
            }
            ConnectionId connection;
            mConnections.TryGetValue(address, out connection);
            mNetwork.SendData(connection, msgData, 0, msgData.Length, true);
        }
        //we use the server side connection id to identify the client
        string idAndMessage = evt.ConnectionId + ":" + msg;
        Debug.Log(idAndMessage);
        //return the buffer so the network can reuse it
        buffer.Dispose();
    }

    private void FixedUpdate() {
        // null check
        if (mNetworks == null && mNetworks.Count == 0) {
            return;
        }

        foreach (var mNetworkPair in mNetworks) {
            var mNetwork = mNetworkPair.Value;
            var mNetworkAddress = mNetworkPair.Key;
            mNetwork.Update();
            NetworkEvent evt;
            while (mNetwork != null && mNetwork.Dequeue(out evt)) {
                //check every message
                switch (evt.Type) {
                    case NetEventType.ServerInitialized: {
                        // if this is the first connection established, then this user becomes the host
                        if (mNetworks.Count == 1) {
                            state.isServer = true;
                            Debug.Log("starting game");
                            state.StartGame();
                        }                                                
                        string address = evt.Info;
                        state.gui.roomID.text = "Room ID: " + address;
                        Debug.Log("Server started. Address: " + address);
                        break;
                    }
                    case NetEventType.ServerInitFailed: {                        
                        state.isServer = false;
                        Debug.Log("Server start failed.");
                        Reset();
                        break;
                    }
                    case NetEventType.ServerClosed: {
                        // if the server left the game, reassign the host TODO
                        state.isServer = false;
                        state.LeaveGame();
                        Debug.Log("Server closed. No incoming connections possible until restart.");
                        break;
                    }
                    case NetEventType.NewConnection: {
                        ConnectionId connection = evt.ConnectionId;
                        Debug.Log("new conection: " + connection.id.ToString());
                        // regardless of server or client, save this connection so it can be used
                        mConnections.Add(mNetworkAddress, connection);
                        // if this user is the server, tell this user to connect to all other users' networks
                        if (state.isServer == true) {
                            // TODO
                        } else if (mNetworks.Count == 1) {
                            if (state.inGame == false) {
                                state.StartGame();
                            }
                            Debug.Log("first network client; starting game");
                        }                                        
                        break;
                    }
                    case NetEventType.ConnectionFailed: {
                        //Outgoing connection failed. Inform the user.
                        Debug.Log("Connection failed");
                        Reset();
                        break;
                    }
                    case NetEventType.Disconnected: {
                        //A connection was disconnected
                        Debug.Log("connection disonnected");
                        //cleanNetwork(mNetworkAddress);
                        Reset();
                        break;
                    }
                    case NetEventType.ReliableMessageReceived:
                    case NetEventType.UnreliableMessageReceived: {
                        HandleIncommingMessage(mNetworkAddress, ref evt);
                        break;
                    }
                }
            }

            //finish this update by flushing the messages out if the network wasn't destroyed during update
            if (mNetwork != null) {
                mNetwork.Flush();
            }
        }
    }

    private void OnDestroy() {
        Cleanup();
    }
}
