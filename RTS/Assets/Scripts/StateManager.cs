using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Maintains simple-to-access state variables
public class StateManager : MonoBehaviour {

    //This enum is used in the InputManager class to handle context switches
    //keep new members ordered as array indices
    public enum View {
        Global = 0,
        Lobby = 1,
        RTS = 2
    }

    [Serializable]
    public enum EntityType {
        Soldier = 0,
        Ironfoe = 1,
        Dog = 2,
        Mortar = 3,
        Barracks,
        FlagPlatform,
        Flag
    }

    [Header ("Network")]
    [ReadOnly]
    public NetworkManager network;
    [Header ("GUI")]
    [ReadOnly]
    public GuiManager gui;
    [Header ("Input")]
    [ReadOnly]
    public InputManager input;
    [Header ("Prefabs")]
    [SerializeField]
    private GameObject barracks;
    [SerializeField]
    private GameObject guyPrefab;
    [SerializeField]
    private GameObject ironfoePrefab;
    [SerializeField]
    private GameObject gameUnitsPrefab;
    [SerializeField]
    private GameObject flagPlatformPrefab;
    [SerializeField]
    private GameObject dogPrefab;
    [ReadOnly]
    public GameObject gameUnits;
    [ReadOnly]
    public Selection selection;
    [Header ("View")]
    [ReadOnly]
    public View gameView;
    [ReadOnly]
    public bool isServer;
    [ReadOnly]
    public bool inGame;
    [ReadOnly]
    private int[] points;
    public MapData currentMap;
    [ReadOnly]
    public Dictionary<short, int> unitCounts; // map network id to count
    private Dictionary<string, UnitController> unitLookup;
    private Transform[] unitLists;
    private Transform[] objectLists;
    private List<string> syncIDs;
    private List<Vector2> syncPos;
    private short readyClients;
    private static StateManager s;
    [HideInInspector]
    public static StateManager state {
        get {
            if (!s) {
                throw new System.Exception ("StateManager hasn't been initialized yet");
            } else {
                return s;
            }
        }
    }
    private View lastViewFromMenu;

    //variables for managing building units
    private bool unitIsBuilding;
    private EntityType unitToBuild;
    private float remainingBuildTime;
    public float soldierBuildTime = 10;
    public float ironfoeBuildTime = 15;
    public float dogBuildTime = 5;

    // Changed to awake for early init
    void Awake () {
        isServer = false;
        gameView = View.Lobby;
        inGame = false;
        unitCounts = new Dictionary<short, int> ();
        unitLookup = new Dictionary<string, UnitController> ();

        if (!guyPrefab) {
            throw new System.Exception ("No Guy defined");
        }

        if (!ironfoePrefab) {
            throw new System.Exception ("No Ironfoe defined");
        }

        network = GetComponent<NetworkManager> ();
        if (!network) {
            throw new System.Exception ("No NetworkManager defined");
        }

        gui = GetComponent<GuiManager> ();
        if (!gui) {
            throw new System.Exception ("No GUIManager defined");
        }

        input = GetComponent<InputManager> ();
        if (!input) {
            throw new System.Exception ("No InputManager defined");
        }

        selection = GetComponent<Selection> ();
        if (!selection) {
            throw new System.Exception ("No Selection defined");
        }

        // Start in the Lobby
        //gameView = View.Lobby;
        selection.enabled = false;
        s = this;
    }

    void Start () {
        input.Subscribe (SpawnShootGuy, InputActions.RTS.SPAWN_SHOOTGUY);
        input.Subscribe (SpawnIronfoe, InputActions.RTS.SPAWN_IRONFOE);
        input.Subscribe (SpawnDog, InputActions.RTS.SPAWN_DOG);
        input.Subscribe (CancelBuild, InputActions.RTS.CANCEL_BUILD);
    }

    void FixedUpdate () {
        if (unitIsBuilding) {
            remainingBuildTime -= Time.deltaTime;
            gui.UpdateUnitLoad (remainingBuildTime / BuildTime (unitToBuild));
            if (remainingBuildTime <= 0) {
                network.requestNewUnit (unitToBuild);
                CancelBuild ();
            }
        }

        if(isServer && inGame && syncIDs.Count > 0) {
            network.SendMessage(new SyncAll{
                ids = syncIDs.ToArray(),
                pos = syncPos.ToArray()
            });

            syncIDs.Clear();
            syncPos.Clear();
        }
    }

    // called from NetworkManager
    public void StartGame () {
        CreateTopLevelGameUnits ();
        inGame = true;
        CancelBuild ();

        if (gameView == View.Global)
            gui.KeybindMenu ();

        gameView = View.RTS;
        selection.enabled = true;
        // show game UI
        gui.RTSGUI ();
        // zero out each player's points
        points = new int[4];
        currentMap = Instantiate (gui.mapSelect.GetMapData ().gameObject, this.transform).GetComponent<MapData> ();
        unitLists = new Transform[currentMap.mapInfo.numberSupportedPlayers];
        objectLists = new Transform[currentMap.mapInfo.numberSupportedPlayers];
        syncIDs = new List<string>();
        syncPos = new List<Vector2>();
        readyClients = 0;
        for (int i = 0; i < currentMap.mapInfo.numberSupportedPlayers; i++) {
            unitLists[i] = Instantiate (gameUnitsPrefab, gameUnits.transform).GetComponent<Transform> ();
            unitLists[i].gameObject.name = "GU-" + i;
            objectLists[i] = Instantiate (gameUnitsPrefab, gameUnits.transform).GetComponent<Transform> ();
            objectLists[i].gameObject.name = "IO-" + i;
        }

        Camera.main.transform.position = currentMap.mapInfo.cameraPosition;
        Camera.main.transform.eulerAngles = currentMap.mapInfo.cameraAngle;

    }

    private void SpawnShootGuy () {
        SpawnUnit (EntityType.Soldier);
    }
    private void SpawnIronfoe () {
        SpawnUnit (EntityType.Ironfoe);
    }
    private void SpawnDog () {
        SpawnUnit (EntityType.Dog);
    }

    private void SpawnUnit (EntityType type) {
        if (!unitIsBuilding && !Input.GetKey(InputActions.RTS.SELECT_DOWN.currentKey.key)) {
            unitIsBuilding = true;
            unitToBuild = type;
            remainingBuildTime = BuildTime (type);
            gui.StartBuildUnit (type);
        }
    }

    private void CancelBuild () {
        unitIsBuilding = false;
        remainingBuildTime = 0;
        gui.StopBuildUnit ();
    }

    private int GetNextUnitCount (short netID) {
        if (unitCounts.ContainsKey (netID) == false) {
            unitCounts.Add (netID, 0);
            return 0;
        }
        int count;
        unitCounts.TryGetValue (netID, out count);
        unitCounts.Remove (netID);
        unitCounts.Add (netID, count + 1);
        return count + 1;
    }

    public GameObject AddUnit (short netID, EntityType unitType, string name = null) {
        // get my game units
        Transform myUnits = unitLists[netID];
        // create a fucking guy in the gameUnits
        GameObject unit;

        Vector3 unitSpawnPositions;
        Vector3 xyzPosition;
        int unitNum = GetNextUnitCount (netID);

        if (unitType == EntityType.FlagPlatform) {
            xyzPosition = currentMap.mapInfo.flagPositions[netID];
        } else {
            unitSpawnPositions = currentMap.mapInfo.unitSpawnPositions[netID];
            xyzPosition = new Vector3 (unitSpawnPositions.x + (-2 + (unitNum % 16) % 4),
                unitSpawnPositions.y,
                unitSpawnPositions.z + (-2 + (unitNum % 16) / 4));
        }

        switch (unitType) {
            case EntityType.Barracks:
                xyzPosition.y += barracks.transform.position.y;
                unit = Instantiate (barracks, xyzPosition, barracks.transform.rotation, myUnits);
                break;

            case EntityType.Soldier:
                xyzPosition.y += guyPrefab.transform.position.y;
                unit = Instantiate (guyPrefab, xyzPosition, guyPrefab.transform.rotation, myUnits);
                break;

            case EntityType.Ironfoe:
                xyzPosition.y += ironfoePrefab.transform.position.y;
                unit = Instantiate (ironfoePrefab, xyzPosition, ironfoePrefab.transform.rotation, myUnits);
                break;

            case EntityType.FlagPlatform:
                xyzPosition.y += flagPlatformPrefab.transform.position.y;
                unit = Instantiate (flagPlatformPrefab, xyzPosition, flagPlatformPrefab.transform.rotation, objectLists[netID]);
                return unit;

            case EntityType.Flag:
                Debug.Log ("Tried to spawn a flag through addUnit");
                return null;

            case EntityType.Dog:
                xyzPosition.y += dogPrefab.transform.position.y;
                unit = Instantiate (dogPrefab, xyzPosition, flagPlatformPrefab.transform.rotation, myUnits.transform);
                break;
            default:
                Debug.Log("Entity Type: " + unitType + " did not match any known unitType");
                Debug.Log(StackTraceUtility.ExtractStackTrace());
                unit = null;
                break;
        }
        unit.layer = 10 + netID;
        if (name == null) {
            unit.name = unitNum.ToString ();
        } else {
            unit.name = name;
        }

        unit.GetComponent<UnityEngine.AI.NavMeshAgent> ().destination = xyzPosition;
        unitLookup.Add (myUnits.gameObject.name + unit.name, unit.GetComponent<UnitController> ());

        return unit;
    }

    public void MoveCommand (short ownerID, string name, float x, float y, float z) {
        Transform units = unitLists[ownerID];
        UnitController unit;
        if (unitLookup.TryGetValue (units.gameObject.name + name, out unit)) {
            unit.MoveTo (x, y, z);
        } else {
            Debug.Log ("Unit with name: " + units.gameObject.name + name + " was not found!");
        }
    }

    public UnitController[] GetUnits(string[] ids, short ownerID) {
        UnitController[] units = new UnitController[ids.Length];
        string listName = unitLists[ownerID].gameObject.name;
        UnitController unit;

        int numFound = 0;
        for (int i = 0; i < ids.Length; ++i) {
            if (unitLookup.TryGetValue (listName + ids[i], out unit)) {
                Vector3 pos = unit.transform.position;
                ++numFound;
                units[i] = unit;
            }
        }

        //check if any units were null and amend units array
        if (numFound < units.Length) {
            units = units.Where (c => c != null).ToArray ();
        }
        return units;
    }

    public Vector2[] GetRelativePoints(UnitController[] units, float x, float z) {
        Vector3 center = Vector3.zero;
        for (int i = 0; i < units.Length; ++i) {
            center += units[i].transform.position;
        }

        center /= units.Length;
        //calculate offsets of each unit from center and move to those offsets
        Vector2[] relativePoints = new Vector2[units.Length];
        for (int i = 0; i < units.Length; ++i) {
            relativePoints[i] = new Vector2(x + (units[i].transform.position.x - center.x), z + (units[i].transform.position.z - center.z));
        }
        return relativePoints;
    }

    public void BlobMove (string[] ids, short ownerID, float x, float y, float z) {
        //units list will be used to calc midpoint and stuff
        UnitController[] units = GetUnits(ids, ownerID);

        //magic box
        float left = float.MaxValue, bot = float.MaxValue;
        float right = float.MinValue, top = float.MinValue;

        for (int i = 0; i < ids.Length; ++i) {
            Vector3 pos = units[i].transform.position;
            //check box bounds
            if (pos.x < left) left = pos.x;
            if (pos.x > right) right = pos.x;
            if (pos.z < bot) bot = pos.z;
            if (pos.z > top) top = pos.z;
        }

        //if moveTo point is outside magic box...
        if (x < left || x > right || z > top || z < bot) {
            //calculate offsets of each unit from center and move to those offsets
            Vector2[] relativePoints = GetRelativePoints(units, x, z);
            for (int i = 0; i < units.Length; ++i) {
                units[i].MoveTo (relativePoints[i].x, y, relativePoints[i].y);
            }
        } else {
            foreach (UnitController u in units) {
                u.MoveTo (x, y, z);
            }
        }

    }

    public void AttackCommand (short ownerID, string name, float x, float z) {
        Transform units = unitLists[ownerID];
        UnitController unit;
        if (unitLookup.TryGetValue (units.gameObject.name + name, out unit)) {
            unit.Attack (x, z);
        } else {
            Debug.Log ("Unit with name: " + units.gameObject.name + name + " was not found!");
        }
    }

    public void RelativeAttack(string[] ids, short ownerID, float x, float z) {
        UnitController[] units = GetUnits(ids, ownerID);
        Vector2[] relativePoints = GetRelativePoints(units, x, z);
        for (int i = 0; i < units.Length; ++i) {
            units[i].ShowTeaserAttack(0, 0, false);
            units[i].Attack (relativePoints[i].x, relativePoints[i].y);
        }

    }

    public void DamageUnit (short ownerID, string name, int damage) {
        Transform units = unitLists[ownerID];
        UnitController unit;
        if (unitLookup.TryGetValue (units.gameObject.name + name, out unit)) {
            unit.TakeDamage (damage);
        } else {
            Debug.Log ("Unit from player: " + units.gameObject.name + "with name: " + name + " was not found!");
        }

    }

    public void SubmitForSync(string name, short ownerID, Vector2 pos) {
        syncIDs.Add(ownerID + "," + name);
        syncPos.Add(pos);
    }

    public void SyncPos (string[] ids, Vector2[] pos) {
        for(int i = 0; i < ids.Length; ++i) {
            UnitController unit;
            string[] ownerAndName = ids[i].Split(',');
            short ownerID = short.Parse(ownerAndName[0]);
            if (unitLookup.TryGetValue (unitLists[ownerID].gameObject.name + ownerAndName[1], out unit)) {
                unit.SyncPos (pos[i].x, pos[i].y);
            } else {
                Debug.Log ("Unit with name: " + unitLists[ownerID].gameObject.name + name + " was not found!");
            }
        }
    }

    // called from NetworkManager
    public void LeaveGame () {
        unitCounts = new Dictionary<short, int> ();
        if (!inGame) {
            return;
        }
        if (gameView == View.Global) {
            gui.KeybindMenu ();
        }
        gameView = View.Lobby;
        isServer = false;
        inGame = false;
        CleanObjects ();
        CancelBuild ();
        readyClients = 0;
        selection.enabled = false;
        //show Lobby UI
        gui.Cleanup ();
    }

    public void CreateTopLevelGameUnits () {
        gameUnits = Instantiate (gameUnitsPrefab);
        gameUnits.name = "GameUnits";
    }

    public void RemoveUnit (GameObject unit) {
        if (!unitLookup.Remove (unit.transform.parent.name + unit.name))
            Debug.Log ("Could not remove unit with name " + unit.transform.parent.name + unit.name + ".");
    }

    public void CleanObjects () {
        gameUnits.transform.SetParent (null);
        Destroy (gameUnits);
        unitLookup.Clear ();
        Destroy (currentMap.gameObject);
    }

    public void OpenMenu (bool open) {
        if (open) {
            lastViewFromMenu = gameView;
            gameView = View.Global;
        } else {
            if (gameView == View.Global)
                gameView = lastViewFromMenu;
        }
    }

    public void ScorePoint (short netID) {
        points[netID]++;
        gui.reportScore (netID, points[netID]);
    }

    public void ResetScores () {
        for (short i = 0; i < 4; ++i) {
            points[i] = 0;
            gui.reportScore (i, 0);
        }
    }

    public void ClientReady() {
        if(++readyClients == network.GetConnectionsCount()) {
            StartOfGameUnits();
            network.SendSync();
        }
    }

    private void StartOfGameUnits () {
        Debug.Log("Creating start of game units for " + network.GetConnectionsCount() + " connections.");
        for(short i = 0; i <= network.GetConnectionsCount(); ++i) {
            StateManager.state.AddUnit (i, StateManager.EntityType.FlagPlatform, null);
        }
    }

    public void AssignStartMap (Voteable v) {
        if (v.GetComponent<MapButton> () != null) {
            gui.mapSelect.SetCurrentMap (v.GetVotableID ());
        }
    }

    private float BuildTime (EntityType type) {
        switch (type) {

            case EntityType.Soldier:
                return soldierBuildTime;
            case EntityType.Ironfoe:
                return ironfoeBuildTime;
            case EntityType.Dog:
                return dogBuildTime;

            default:
                Debug.Log ("You tried to build a: " + type + " don't do that.");
                return 0;
        }
    }
}