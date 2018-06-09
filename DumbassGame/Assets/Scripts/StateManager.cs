using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Maintains simple-to-access state variables
public class StateManager : MonoBehaviour {
    public enum View {
        Lobby = 0,
        Game = 1
    }
    [Header("Network")]
    [ReadOnly]
    public NetworkManager network;
    [Header("GUI")]
    [ReadOnly]
    public GuiManager gui;
    [Header("Prefabs")]
    public GameObject guyPrefab;
    [ReadOnly]
    public Selection selection;
    [Header("State")]
    [ReadOnly]
    public View gameView;
    [ReadOnly]
    public bool isServer;
    public bool inGame;

    // Use this for initialization
    void Start() {
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

        selection = GetComponent<Selection>();
        if (!selection) {
            throw new System.Exception("No Selection defined");
        }

        // Start in the Lobby
        //gameView = View.Lobby;
        selection.enabled = false;
        gui.LobbyGUI();
    }

    // called from NetworkManager
    public void StartGame() {
        inGame = true;
        if (isServer) {
            // initialize
        }
        gameView = View.Game;        
        Instantiate(guyPrefab);
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
