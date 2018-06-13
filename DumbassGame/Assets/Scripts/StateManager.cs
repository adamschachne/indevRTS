using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Maintains simple-to-access state variables
public class StateManager : MonoBehaviour 
{

    //This enum is used in the InputManager class to handle context switches
    //keep new members ordered as array indices
    public enum View {
        Lobby = 0,
        RTS = 1
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
    public GameObject guyPrefab;
    [ReadOnly]
    public Selection selection;
    [Header("View")]
    [ReadOnly]
    public View gameView;
    [ReadOnly]
    public bool isServer;
    [ReadOnly]
    public bool inGame;
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


    // Changed to awake for early init
    void Awake() {
        isServer = false;
        gameView = View.Lobby;
        inGame = false;

        if (!guyPrefab) {
            throw new System.Exception("No Guy defined");
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
            throw new System.Exception ("No InputManager defined");
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

    // called from NetworkManager
    public void StartGame() {
        inGame = true;
        if (isServer) {
            // initialize
        }
        gameView = View.RTS;
        GameObject guy = Instantiate(guyPrefab);
        guy.transform.position += new Vector3(Random.Range(-3,3), 0, Random.Range(-3, 3));
        guy = Instantiate(guyPrefab);
        guy.transform.position += new Vector3(Random.Range(-3,3), 0, Random.Range(-3, 3));
        guy = Instantiate(guyPrefab);
        guy.transform.position += new Vector3(Random.Range(-3,3), 0, Random.Range(-3, 3));
        selection.enabled = true;

        // show game UI
        gui.GameGUI();
    }

    // called from NetworkManager
    public void LeaveGame() {
        if (!inGame) {
            return;            
        }
        gameView = View.Lobby;
        isServer = false;        
        inGame = false;
        CleanObjects();
        selection.enabled = false;
        //show Lobby UI
        gui.LobbyGUI();
    }

    private void CleanObjects() {
        GameObject[] units = GameObject.FindGameObjectsWithTag("Unit");
        foreach (GameObject unit in units) {
            Destroy(unit);
        }
    }
}
