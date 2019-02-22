using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Message {
    public virtual void process () { Debug.Log ("Wrong function was called :("); }
}

[Serializable]
public class NetworkUnit : Message {
    public string id;
    public float x;
    public float z;
    public short ownerID; // network id of the owner
    public StateManager.EntityType unitType;
}

[Serializable]
public class SyncUnits : Message {
    public List<NetworkUnit> units;
    //public short connectionId;
    public override void process () {
        Debug.Log ("HANDLING SYNC");
        // Client Only
        if (StateManager.state.isServer == true) {
            return;
        }

        foreach (NetworkUnit netUnit in this.units) {
            short ownerID = netUnit.ownerID;
            StateManager.state.addUnit (ownerID, netUnit.unitType, new Vector2 (netUnit.x, netUnit.z), netUnit.id);
        }

        StateManager.state.ResetScores ();
    }
}

[Serializable]
public class AddUnit : Message {
    public short ownerID; // the connection that added a unit
    public NetworkUnit unit; // the unit added
    public override void process () {
        Debug.Log ("HANDLING ADD UNIT");
        // Client Only
        if (StateManager.state.isServer == true) {
            return;
        }
        StateManager.state.addUnit (this.ownerID, this.unit.unitType, new Vector2 (this.unit.x, this.unit.z), this.unit.id);
    }
}

[Serializable]
public class RequestUnit : Message {
    public short ownerID; // the connection that requested a unit
    public StateManager.EntityType unitType;
    public float x;
    public float z;
    public override void process () {
        StateManager.state.network.HandleRequestUnit (this);
    }
}

[Serializable]
public class Move : Message {
    public string id; // name of the unit that is commanded
    public float x;
    public float z;
    public short ownerID; // network id of the owner

    public override void process () {
        StateManager.state.MoveCommand (this.ownerID, this.id, this.x, this.z);
    }
}

[Serializable]
public class Stop : Message {
    public string id;
    public short ownerID;
    public override void process () {
        StateManager.state.StopCommand (this.ownerID, this.id);
    }
}

[Serializable]
public class Attack : Message {
    public string id;
    public short ownerID;
    public float x;
    public float z;
    public override void process () {
        StateManager.state.AttackCommand (this.ownerID, this.id, this.x, this.z);
    }
}

[Serializable]
public class Damage : Message {
    public string id;
    public short ownerID;
    public int damage;
    public override void process () {
        Debug.Log ("Recieved Damage signal. Processing Damage Unit.");
        StateManager.state.DamageUnit (this.ownerID, this.id, this.damage);
    }
}

[Serializable]
public class SyncPos : Message {
    public string id;
    public short ownerID;
    public float x;
    public float z;
    public override void process () {
        StateManager.state.SyncPos (this.ownerID, this.id, this.x, this.z);
    }
}

[Serializable]
public class Batch : Message {
    public List<Message> cmds;
    public override void process () {
        foreach (Message cmd in this.cmds) {
            cmd.process ();
        }
    }
}

[Serializable]
public class Connected : Message {
    public short playerID;

    public override void process () {
        StateManager.state.gui.mapSelect.RecieveConnected (playerID);
    }
}

[Serializable]
public class SyncMapSelect : Message {
    public MapSelect.MapSelectState state;
    public override void process () {
        StateManager.state.gui.mapSelect.RecieveSync (this.state);
    }
}

[Serializable]
public class StartGame : Message {
    //when server sees a request
    public override void process () {
        if (!StateManager.state.isServer) {
            StateManager.state.StartGame ();
            StateManager.state.network.SendMessage (new RequestSync ());
        }
    }
}

[Serializable]
//todo: only waits to hear 1 sync request to send the sync out to all clients.
public class RequestSync : Message {
    public override void process () {
        if (StateManager.state.isServer) {
            //start of game units
            StateManager.state.addUnit (0, StateManager.EntityType.FlagPlatform, new Vector2 (8, 8), null);
            StateManager.state.addUnit (1, StateManager.EntityType.FlagPlatform, new Vector2 (-8, 8), null);

            StateManager.state.network.SendSync ();
        }
    }
}

[Serializable]
public class Vote : Message {
    public short networkID;
    public short votableID;
    public bool vote;

    public override void process () {
        if (StateManager.state.isServer) {
            StateManager.state.gui.mapSelect.RecieveVote (this.votableID, this.networkID, this.vote);
        }
    }
}