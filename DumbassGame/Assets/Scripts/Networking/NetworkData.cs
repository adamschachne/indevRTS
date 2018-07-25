using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NetTypes {
    public const string SYNC = "SYNC"; // list containing all current units in the game; this should not be sent often
    public const string ADD_UNIT = "ADDUNIT"; // sent when a new unit was added to the game
    public const string REQUEST_UNIT = "REQUESTUNIT"; // sent to the server asking for a new unit
    public const string MOVE_UNIT = "MOVE"; // command move a unit
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
        GameObject unit = StateManager.state.addUnit(netId, netunit.id);
        unit.transform.SetPositionAndRotation(new Vector3(netunit.x, unit.transform.position.y, netunit.z), unit.transform.rotation);
    };
}

[Serializable]
public class RequestUnit {
    public short ownerID; // the connection that requested a unit
}

[Serializable]
public class Move {
    public string id; // name of the unit that is commanded
    public float x;
    public float z;
    public short ownerID; // network id of the owner
}