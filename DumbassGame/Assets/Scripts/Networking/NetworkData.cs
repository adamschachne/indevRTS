using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameMessage {
    public virtual void process () { Debug.Log ("Wrong function was called :("); }
}

[Serializable]
public class NetworkUnit : GameMessage {
    public string id;
    public float x;
    public float z;
    public short ownerID; // network id of the owner
    public StateManager.EntityType unitType;
}

[Serializable]
public class SyncUnits : GameMessage {
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
            GameObject unit = StateManager.state.addUnit (ownerID, netUnit.unitType, new Vector2 (netUnit.x, netUnit.z), netUnit.id);
        }

        StateManager.state.ResetScores ();
    }
}

[Serializable]
public class AddUnit : GameMessage {
    public short ownerID; // the connection that added a unit
    public NetworkUnit unit; // the unit added
    public override void process () {
        Debug.Log ("HANDLING ADD UNIT");
        // Client Only
        if (StateManager.state.isServer == true) {
            return;
        }
        GameObject unit = StateManager.state.addUnit (this.ownerID, this.unit.unitType, new Vector2 (this.unit.x, this.unit.z), this.unit.id);
    }
}

[Serializable]
public class RequestUnit : GameMessage {
    public short ownerID; // the connection that requested a unit
    public StateManager.EntityType unitType;
    public float x;
    public float z;
    public override void process () {
        StateManager.state.network.HandleRequestUnit (this);
    }
}

[Serializable]
public class Move : GameMessage {
    public string id; // name of the unit that is commanded
    public float x;
    public float z;
    public short ownerID; // network id of the owner

    public override void process () {
        StateManager.state.MoveCommand (this.ownerID, this.id, this.x, this.z);
    }
}

[Serializable]
public class Stop : GameMessage {
    public string id;
    public short ownerID;
    public override void process () {
        StateManager.state.StopCommand (this.ownerID, this.id);
    }
}

[Serializable]
public class Attack : GameMessage {
    public string id;
    public short ownerID;
    public float x;
    public float z;
    public override void process () {
        StateManager.state.AttackCommand (this.ownerID, this.id, this.x, this.z);
    }
}

[Serializable]
public class Damage : GameMessage {
    public string id;
    public short ownerID;
    public int damage;
    public override void process () {
        Debug.Log ("Recieved Damage signal. Processing Damage Unit.");
        StateManager.state.DamageUnit (this.ownerID, this.id, this.damage);
    }
}

[Serializable]
public class SyncPos : GameMessage {
    public string id;
    public short ownerID;
    public float x;
    public float z;
    public override void process () {
        StateManager.state.SyncPos (this.ownerID, this.id, this.x, this.z);
    }
}

[Serializable]
public class Batch : GameMessage {
    public List<GameMessage> cmds;
    public override void process () {
        foreach (GameMessage cmd in this.cmds) {
            cmd.process ();
        }
    }
}