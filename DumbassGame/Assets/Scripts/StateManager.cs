using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Maintains simple-to-access state variables
public class StateManager : MonoBehaviour 
{

    //This enum is used in the InputManager class to handle context switches
    //keep new members ordered as array indices
    public enum View {
        Global = 0,
        Lobby = 1,
        RTS = 2, 

    }

    [Header("Network")]
    [ReadOnly]
    public NetworkManager network;
    [Header("GUI")]
    [ReadOnly]
    public GuiManager gui;
    [Header("Input")]
    [ReadOnly]
    public InputManager input;
    [Header("Prefabs")]
    public GameObject barracks;
    public GameObject guyPrefab;
    public GameObject ironfoePrefab;
    public GameObject gameUnitsPrefab;
    [ReadOnly]
    public GameObject gameUnits;
    [ReadOnly]
    public Selection selection;
    [Header("View")]
    [ReadOnly]
    public View gameView;
    [ReadOnly]
    public bool isServer;
    [ReadOnly]
    public bool inGame;
    [ReadOnly]
    public Dictionary<short, int> unitCounts; // map network id to count
    private Dictionary<string, GameObject> unitLookup;
    private static StateManager s;
    [HideInInspector]
    public static StateManager state {
        get {
            if (!s) {
                throw new System.Exception("StateManager hasn't been initialized yet");
            } else {
                return s;
            }
        }
    }
    private View lastViewFromMenu;

    // Changed to awake for early init
    void Awake() {
        isServer = false;
        gameView = View.Lobby;
        inGame = false;
        unitCounts = new Dictionary<short, int>();
        unitLookup = new Dictionary<string, GameObject>();

        if (!guyPrefab) {
            throw new System.Exception("No Guy defined");
        }

        if (!ironfoePrefab) {
            throw new System.Exception("No Ironfoe defined");
        }

        network = GetComponent<NetworkManager>();
        if (!network) {
            throw new System.Exception("No NetworkManager defined");
        }

        gui = GetComponent<GuiManager>();
        if (!gui) {
            throw new System.Exception("No GUIManager defined");
        }

        input = GetComponent<InputManager> ();
        if (!input) {
            throw new System.Exception("No InputManager defined");
        }

        selection = GetComponent<Selection>();
        if (!selection) {
            throw new System.Exception("No Selection defined");
        }        

        // Start in the Lobby
        //gameView = View.Lobby;
        selection.enabled = false;
        gui.LobbyGUI();
        s = this;
    }

    void Start()
    {
        input.Subscribe(SpawnShootGuy, InputActions.RTS.SPAWN_SHOOTGUY);
        input.Subscribe(SpawnIronfoe, InputActions.RTS.SPAWN_IRONFOE);
    }

    // called from NetworkManager
    public void StartGame() {        
        CreateTopLevelGameUnits();
        inGame = true;
        if (isServer) {
            network.requestNewUnit(0, -8, 8);
        }       

        if(gameView == View.Global)
            gui.Menu();

        gameView = View.RTS;        
        selection.enabled = true;
        // show game UI
        gui.GameGUI();
    }

    private void SpawnShootGuy() {
        network.requestNewUnit(1, Random.Range(-3, 3), Random.Range(-3, 3));
    }
    private void SpawnIronfoe() {
        network.requestNewUnit(2, Random.Range(-3, 3), Random.Range(-3, 3));
    }

    private GameObject GetNetUserGameUnits(short netID) {
        string myGameUnitsName = "GU-" + netID;
        foreach (var child in gameUnits.GetComponentsInChildren<Transform>()) {
            if (child.name.Equals(myGameUnitsName)) {
                return child.gameObject;
            }
        }
        GameObject newGameUnits = Instantiate(gameUnitsPrefab, gameUnits.transform);
        newGameUnits.name = myGameUnitsName;
        return newGameUnits;
    }

    private int GetNextUnitCount(short netID) {
        if (unitCounts.ContainsKey(netID) == false) {
            unitCounts.Add(netID, 0);
            return 0;
        }
        int count;
        unitCounts.TryGetValue(netID, out count);
        unitCounts.Remove(netID);
        unitCounts.Add(netID, count + 1);
        return count + 1;
    }

    public GameObject addUnit(short netID, string name = null, short unitType = 1) {
        // get my game units
        GameObject myUnits = GetNetUserGameUnits(netID);
        // create a fucking guy in the gameUnits
        GameObject unit;
        switch(unitType)
        {
            case 0:
            unit = Instantiate(barracks, myUnits.transform);
            break;

            case 1:
            unit = Instantiate(guyPrefab, myUnits.transform);
            break;

            case 2:
            unit = Instantiate(ironfoePrefab, myUnits.transform);
            break;

            default:
            unit = Instantiate(guyPrefab, myUnits.transform);
            break;

        }
        unit.layer = 10 + netID;
        if (name == null) {
            unit.name = GetNextUnitCount(netID).ToString();
        } else {
            unit.name = name;
        }

        unitLookup.Add(myUnits.name + unit.name, unit);

        return unit;
    }

    public void MoveCommand(short ownerID, string name, float x, float z) {
        GameObject units = GetNetUserGameUnits(ownerID);
        
        GameObject unit;
        if(unitLookup.TryGetValue(units.name+name, out unit))
        {
            unit.GetComponent<UnitController>().MoveTo(x, z);
        }
        else
        {
            Debug.Log("Unit with name: " + units.name+name + " was not found!");
        }
    }

    public void StopCommand(short ownerID, string name)
    {
        GameObject units = GetNetUserGameUnits(ownerID);
        GameObject unit;
        if(unitLookup.TryGetValue(units.name+name, out unit))
        {
            unit.GetComponent<UnitController>().Stop();
        }
        else
        {
            Debug.Log("Unit with name: " + units.name+name + " was not found!");
        }
    }

    public void AttackCommand(short ownerID, string name, float x, float z)
    {
        GameObject units = GetNetUserGameUnits(ownerID);
        GameObject unit;
        if(unitLookup.TryGetValue(units.name+name, out unit))
        {
            unit.GetComponent<UnitController>().Attack(x, z);
        }
        else
        {
            Debug.Log("Unit with name: " + units.name+name + " was not found!");
        }
    }

    public void DamageUnit(short ownerID, string name, int damage)
    {
        GameObject units = GetNetUserGameUnits(ownerID);
        GameObject unit;
        if(unitLookup.TryGetValue(units.name+name, out unit))
        {
            unit.GetComponent<UnitController>().TakeDamage(damage);
        }
        else
        {
            Debug.Log("Unit with name: " + units.name+name + " was not found!");
        }

    }

    public void SyncPos(short ownerID, string name, float x, float z) {
        GameObject units = GetNetUserGameUnits(ownerID);
        GameObject unit;
        if(unitLookup.TryGetValue(units.name+name, out unit))
        {
            //Debug.Log("Trying to sync unit: " + units.name+name);
            unit.GetComponent<UnitController>().SyncPos(x, z);
        }
        else
        {
            Debug.Log("Unit with name: " + units.name+name + " was not found!");
        }
    }

    // called from NetworkManager
    public void LeaveGame() {
        unitCounts = new Dictionary<short, int>();
        if (!inGame) {
            return;            
        }
        if(gameView == View.Global)
        {
            gui.Menu();
        }
        gameView = View.Lobby;
        isServer = false;        
        inGame = false;
        CleanObjects();
        selection.enabled = false;
        //show Lobby UI
        gui.LobbyGUI();
    }

    public void CreateTopLevelGameUnits() {
        gameUnits = Instantiate(gameUnitsPrefab);
        gameUnits.name = "GameUnits";
    }

    public void RemoveUnit(GameObject unit)
    {
        if(!unitLookup.Remove(unit.transform.parent.name + unit.name))
            Debug.Log("Could not remove unit with name " + unit.transform.parent.name + unit.name + ".");
    }

    public void CleanObjects() {
        gameUnits.transform.SetParent(null);
        Destroy(gameUnits);
        unitLookup.Clear();
    }

    public void OpenMenu(bool open)
    {
        if(open)
        {
            lastViewFromMenu = gameView;
            gameView = View.Global;
        }
        else
        {
            if(gameView == View.Global)
                gameView = lastViewFromMenu;
        }
    }
}
