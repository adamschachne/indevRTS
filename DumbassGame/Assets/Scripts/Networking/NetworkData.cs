using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NetTypes {
    public const string SYNC = "SYNC"; // list containing all current units in the game; this should not be sent often
    public const string ADD_UNIT = "ADDUNIT"; // sent when a new unit was added to the game
    public const string REQUEST_UNIT = "REQUESTUNIT"; // sent to the server asking for a new unit
    public const string MOVE_UNIT = "MOVE"; // command move a unit
    public const string STOP_UNIT = "STOP"; // command a unit to stop
    public const string ATTACK_UNIT = "ATTACK"; //command a unit to attack
    public const string DAMAGE_UNIT = "DAMAGE"; //send damage to a unit
    public const string SYNC_POS = "SYNCPOSITION"; //sync a unit's position across network
    public const string BATCH = "BATCH"; //sync a list of units all at once
}

[Serializable]
public class NetworkJSON {
    public string type;
    public string json;
}

[Serializable]
public class NetworkUnit {
    public string id;
    public float x;
    public float z;
    public short ownerID; // network id of the owner
    public short unitType;
}

[Serializable]
public class SyncUnits {
    public List<NetworkUnit> units;
    //public short connectionId;
}

//[Serializable]
//public class SyncPackage {
//    public short newID; // the connection that just joined
//    public short toID; // the connection this package is directed to
//    public List<SyncUnits> cxns;
//}

[Serializable]
public class AddUnit {
    public short ownerID; // the connection that added a unit
    public NetworkUnit unit; // the unit added

    [NonSerialized]
    public static Action<short, NetworkUnit> action = (short netId, NetworkUnit netunit) => {
        Debug.Log("HANDLING ADD UNIT");
        // Client Only
        if (StateManager.state.isServer == true) {
            return;
        }
        GameObject unit = StateManager.state.addUnit(netId, netunit.id, netunit.unitType);
        unit.transform.SetPositionAndRotation(new Vector3(netunit.x, unit.transform.position.y, netunit.z), unit.transform.rotation);
    };
}

[Serializable]
public class RequestUnit {
    public short ownerID; // the connection that requested a unit
    public short unitType;
}

[Serializable]
public class Move {
    public string id; // name of the unit that is commanded
    public float x;
    public float z;
    public short ownerID; // network id of the owner
}

[Serializable]
public class Stop {
    public string id;
    public short ownerID;
}

[Serializable]
public class Attack {
    public string id;
    public short ownerID;
    public float x;
    public float z;
}

[Serializable]
public class Damage {
    public string id;
    public short ownerID;
    public int damage;
}

[Serializable]
public class SyncPos {
    public string id;
    public short ownerID;
    public float x;
    public float z;
}

[Serializable]
public class Batch {
    public List<NetworkJSON> cmds;
}