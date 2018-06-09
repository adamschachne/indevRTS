using System.Collections;
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

        // Do something with GUI TODO
    }

    public void CreateRoom() {
        Setup();

        // All players will connect to the same room for now TODO
        string roomName = "123456";
        mNetwork.StartServer(roomName);
        Debug.Log("StartServer " + roomName);
    }

    public void JoinRoom() {
        Setup();
        string roomName = "123456";
        mNetwork.Connect(roomName);
        Debug.Log("Connecting to " + roomName + " ...");
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

    public void SendMsg(string msg) {
        SendString(msg, true);
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

    private void HandleIncommingMessage(ref NetworkEvent evt) {
        MessageDataBuffer buffer = (MessageDataBuffer)evt.MessageData;

        string msg = Encoding.UTF8.GetString(buffer.Buffer, 0, buffer.ContentLength);

        //if server -> forward the message to everyone else including the sender
        if (state.isServer) {
            //we use the server side connection id to identify the client
            string idAndMessage = evt.ConnectionId + ":" + msg;
            SendString(idAndMessage);
            Debug.Log(idAndMessage);
        } else {
            //client received a message from the server -> simply print
            Debug.Log(msg);
        }

        //return the buffer so the network can reuse it
        buffer.Dispose();
    }

    private void FixedUpdate() {
        //check if the network was created
        if (mNetwork != null) {
            //first update it to read the data from the underlaying network system
            mNetwork.Update();

            //handle all new events that happened since the last update
            NetworkEvent evt;
            //check for new messages and keep checking if mNetwork is available. it might get destroyed
            //due to an event
            while (mNetwork != null && mNetwork.Dequeue(out evt)) {
                //print to the console for debugging
                //Debug.Log(evt);

                //check every message
                switch (evt.Type) {
                    case NetEventType.ServerInitialized: {
                        //server initialized message received
                        //this is the reaction to StartServer -> switch GUI mode
                        state.isServer = true;
                        state.StartGame();
                        string address = evt.Info;
                        Debug.Log("Server started. Address: " + address);
                        //uRoomName.text = "" + address;
                    }
                    break;
                    case NetEventType.ServerInitFailed: {
                        //user tried to start the server but it failed
                        //maybe the user is offline or signaling server down?
                        state.isServer = false;                        
                        Debug.Log("Server start failed.");
                        Reset();
                    }
                    break;
                    case NetEventType.ServerClosed: {
                        //server shut down. reaction to "Shutdown" call or
                        //StopServer or the connection broke down
                        state.isServer = false;
                        state.LeaveGame();
                        Debug.Log("Server closed. No incoming connections possible until restart.");
                    }
                    break;
                    case NetEventType.NewConnection: {
                        mConnections.Add(evt.ConnectionId);
                        //either user runs a client and connected to a server or the
                        //user runs the server and a new client connected
                        Debug.Log("New local connection! ID: " + evt.ConnectionId);

                        //if server -> send announcement to everyone and use the local id as username
                        if (state.isServer) {
                            //user runs a server. announce to everyone the new connection
                            //using the server side connection id as identification
                            string msg = "New user " + evt.ConnectionId + " joined the room.";
                            Debug.Log(msg);
                            SendString(msg);
                        }
                    }
                    break;
                    case NetEventType.ConnectionFailed: {
                        //Outgoing connection failed. Inform the user.
                        Debug.Log("Connection failed");
                        Reset();
                    }
                    break;
                    case NetEventType.Disconnected: {
                        mConnections.Remove(evt.ConnectionId);
                        //A connection was disconnected
                        //If this was the client then he was disconnected from the server
                        //if it was the server this just means that one of the clients left
                        Debug.Log("Local Connection ID " + evt.ConnectionId + " disconnected");
                        if (state.isServer == false) {
                            Reset();
                        } else {
                            string userLeftMsg = "User " + evt.ConnectionId + " left the room.";

                            //show the server the message
                            Debug.Log(userLeftMsg);

                            //other users left? inform them 
                            if (mConnections.Count > 0) {
                                SendString(userLeftMsg);
                            }
                        }
                    }
                    break;
                    case NetEventType.ReliableMessageReceived:
                    case NetEventType.UnreliableMessageReceived: {
                        HandleIncommingMessage(ref evt);
                    }
                    break;
                }
            }

            //finish this update by flushing the messages out if the network wasn't destroyed during update
            if (mNetwork != null)
                mNetwork.Flush();
        }
    }
}
