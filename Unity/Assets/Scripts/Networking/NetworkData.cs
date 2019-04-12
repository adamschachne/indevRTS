using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Message {
    public virtual void process () { Debug.Log ("Network Message not typecasted correctly, or Process func not defined."); }
}

[Serializable]
public class NetworkUnit : Message {
    public string id;
    public short ownerID; // network id of the owner
    public StateManager.EntityType unitType;
}

[Serializable]
public class SyncUnits : Message {
    public NetworkUnit[] units;
    //public short connectionId;
    public override void process () {
        // Client Only
        if (StateManager.state.isServer == true) {
            return;
        }

        foreach (NetworkUnit netUnit in this.units) {
            short ownerID = netUnit.ownerID;
            StateManager.state.AddUnit (ownerID, netUnit.unitType, netUnit.id);
        }

        StateManager.state.ResetScores ();
    }
}

[Serializable]
public class AddUnit : Message {
    public short ownerID; // the connection that added a unit
    public NetworkUnit unit; // the unit added
    public override void process () {
        // Client Only
        if (StateManager.state.isServer == true) {
            return;
        }
        StateManager.state.AddUnit (this.ownerID, this.unit.unitType, this.unit.id);
    }
}

[Serializable]
public class RequestUnit : Message {
    public short ownerID; // the connection that requested a unit
    public StateManager.EntityType unitType;
    public override void process () {
        StateManager.state.network.HandleRequestUnit (this);
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
        /*
        foreach(string id in this.ids) {
            StateManager.state.MoveCommand(this.ownerID, id, this.x, this.y, this.z);
        }
        */
    }
}

/*
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
*/

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
                StateManager.state.AttackCommand (this.ownerID, id, this.x, this.z);
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
    public Message[] cmds;
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
            //instantiate start of game units
            StateManager.state.StartOfGameUnits ();

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