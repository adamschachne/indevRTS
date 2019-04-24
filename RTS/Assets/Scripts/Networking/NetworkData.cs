using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MessageType {
    ServerOnly,
    ClientOnly,
    ServerAndClient
}

[Serializable]
public class Message {
    [Newtonsoft.Json.JsonIgnore]
    public virtual MessageType mt {
        get {
            return MessageType.ServerAndClient;
        }
    }
    public virtual void process () { Debug.Log ("Network Message not typecasted correctly, or Process func not defined."); }
    public bool ShouldRun() {
        if(mt == MessageType.ServerAndClient) {
            return true;
        }
        else if(mt == MessageType.ServerOnly && StateManager.state.isServer) {
            return true;
        }
        else if(mt == MessageType.ClientOnly && !StateManager.state.isServer) {
            return true;
        }
        else {
            return false;
        }
    }
}

[Serializable]
public class NetworkUnit : Message {
    public string id;
    public short ownerID; // network id of the owner
    public StateManager.EntityType unitType;
}

[Serializable]
public class SyncUnits : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ClientOnly;
        }
    }
    public NetworkUnit[] units;
    //public short connectionId;
    public override void process () {
        // Client Only
        if (ShouldRun()) {
            foreach (NetworkUnit netUnit in this.units) {
                short ownerID = netUnit.ownerID;
                StateManager.state.AddUnit (ownerID, netUnit.unitType, netUnit.id);
            }

            StateManager.state.ResetScores ();
        }
    }
}

[Serializable]
public class AddUnit : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ClientOnly;
        }
    }
    public short ownerID; // the connection that added a unit
    public NetworkUnit unit; // the unit added
    public override void process () {
        // Client Only
        if (ShouldRun()) {
            StateManager.state.AddUnit (this.ownerID, this.unit.unitType, this.unit.id);
        }
    }
}

[Serializable]
public class RequestUnit : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ServerOnly;
        }
    }
    public short ownerID; // the connection that requested a unit
    public StateManager.EntityType unitType;
    public override void process () {
        if (ShouldRun()) {
            StateManager.state.network.HandleRequestUnit (this);
        }
    }
}

[Serializable]
public class MoveSingle : Message {
    public string id; // name of the unit that is commanded
    public float x;
    public float y;
    public float z;
    public short ownerID; // network id of the owner

    public override void process () {
        StateManager.state.MoveCommand (this.ownerID, this.id, this.x, this.y, this.z);
    }
}

public class MoveMany : Message {
    public string[] ids;
    public float x;
    public float y;
    public float z;
    public short ownerID;

    public override void process () {
        StateManager.state.BlobMove (ids, ownerID, x, y, z);
    }
}

[Serializable]
public class AttackMany : Message {
    public string[] ids;
    public short ownerID;
    public float x;
    public float z;
    public bool relative;
    public override void process () {
        if(this.relative) {
            StateManager.state.RelativeAttack(ids, ownerID, x, z);
        } else {
            foreach(string id in this.ids) {
                StateManager.state.AttackCommand(this.ownerID, id, this.x, this.z);
            }
        }
    }
}

[Serializable]
public class Damage : Message {
    public string id;
    public short ownerID;
    public int damage;
    public override void process () {
        StateManager.state.DamageUnit (this.ownerID, this.id, this.damage);
    }
}

[Serializable]
public class SyncAll : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ClientOnly;
        }
    }
    public string[] ids;
    public Vector2[] pos;
    public override void process () {
        if(ShouldRun()) {
            StateManager.state.SyncPos (ids, pos);
        }
    }
}

[Serializable]
public class Batch : Message {
    public Message[] cmds;
    public override void process () {
        foreach (Message cmd in this.cmds) {
            cmd.process ();
        }
    }
}

/*
[Serializable]
public class Connected : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ServerOnly;
        }
    }
    public short playerID;
    public short serverID;
    public override void process () {
        if(ShouldRun()) {
            StateManager.state.network.RecieveClientConnected(serverID);
        }
    }
}
*/

[Serializable]
public class AssignClient : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ClientOnly;
        }
    }
    public short playerID;
    public string address;
    public override void process() {
        if(ShouldRun()) {
            StateManager.state.network.SetupClient(playerID, address);
        }
    }
}

[Serializable]
public class ConfirmClient : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ServerOnly;
        }
    }
    public short playerID;
    public override void process() {
        if(ShouldRun()) {
            StateManager.state.gui.mapSelect.RecieveConnected (playerID);
        }
    }
}

[Serializable]
public class RefuseConnection : Message {
    public String reason;
    public override void process() {
        Debug.Log("Connection was refused. Reason: " + reason);
        StateManager.state.network.Reset();
        StateManager.state.gui.Cleanup();
    }
}

[Serializable]
public class SyncMapSelect : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ClientOnly;
        }
    }
    public MapSelect.MapSelectState state;
    public override void process () {
        if (ShouldRun()) {
            StateManager.state.gui.mapSelect.RecieveSync(this.state);
        }
    }
}

[Serializable]
public class StartGame : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ClientOnly;
        }
    }
    public override void process () {
        if (ShouldRun()) {
            StateManager.state.StartGame ();
            StateManager.state.network.SendMessageToServer(new RequestSync (), false);
        }
    }
}

[Serializable]
public class RequestSync : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ServerOnly;
        }
    }
    public override void process () {
        if (ShouldRun()) {
            StateManager.state.ClientReady();
        }
    }
}

[Serializable]
public class Vote : Message {
    [Newtonsoft.Json.JsonIgnore]
    public override MessageType mt {
        get {
            return MessageType.ServerOnly;
        }
    }
    public short networkID;
    public short votableID;
    public bool vote;

    public override void process () {
        if (ShouldRun()) {
            StateManager.state.gui.mapSelect.RecieveVote (this.votableID, this.networkID, this.vote);
        }
    }
}