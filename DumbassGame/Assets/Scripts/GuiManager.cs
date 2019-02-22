﻿using System.Collections;
using System.Collections.Generic;
using InputActions;
using UnityEngine;
using UnityEngine.UI;

public class GuiManager : MonoBehaviour {

    public Canvas canvas;
    public GameObject keybindMenu;
    [SerializeField]
    private GameObject KeyButtonPrefab;
    [SerializeField]
    private GameObject joinButton;
    [SerializeField]
    private GameObject createButton;
    [SerializeField]
    private GameObject disconnectButton;
    [SerializeField]
    private GameObject scores;
    public InputField inputField;
    public Text roomID;
    public MapSelect mapSelect;
    private StateManager state;
    private bool menuOpen = true;
    private GameObject connectMenu;
    private GameObject RTSGUIParent;
    private GameObject mapSelectParent;

    // Use this for initialization
    void Start () {
        if (!(joinButton && createButton && disconnectButton && inputField && roomID && canvas && keybindMenu && KeyButtonPrefab)) {
            throw new System.Exception ("GUI elements not defined");
        }
        state = GetComponent<StateManager> ();
        state.input.Subscribe (this.KeybindMenu, Global.MENU);

        connectMenu = canvas.transform.Find ("ConnectMenu").gameObject;
        RTSGUIParent = canvas.transform.Find ("RTS GUI").gameObject;
        mapSelectParent = canvas.transform.Find ("MapSelect").gameObject;
        mapSelect = new MapSelect (mapSelectParent);

        KeybindMenu ();

        for (int i = 1; i <= 4; ++i) {
            Transform scoreText = scores.transform.Find ("Player" + i);
            if (scoreText != null) {
                scoreText.GetComponent<Text> ().color = GetColorByNetID ((short) (i - 1));
            }
        }

        ConnectMenu ();
    }

    public void ConnectMenu () {
        connectMenu.SetActive (true);
        RTSGUIParent.SetActive (false);
        mapSelectParent.SetActive (false);
    }

    public void RTSGUI () {
        connectMenu.SetActive (false);
        RTSGUIParent.SetActive (true);
        mapSelectParent.SetActive (false);
    }

    public void MapSelectMenu () {
        connectMenu.SetActive (false);
        RTSGUIParent.SetActive (false);
        mapSelectParent.SetActive (true);
    }

    public void CreateKeybindButtons (List<List<ActionType>>[] groups, StateManager.View[] gameModes) {
        if (KeyButtonPrefab == null) {
            Debug.Log ("Keybind Button Prefab was null.");
            return;
        }

        int xPos = (int) (((Screen.width / 2) * -0.9) + canvas.transform.position.x);
        int yPos = (int) (Screen.height * 0.85);

        RectTransform buttonSize = KeyButtonPrefab.GetComponent<RectTransform> ();

        foreach (StateManager.View v in gameModes) {
            if (groups[(int) v].Count > 0)
                xPos += (int) (buttonSize.rect.width * canvas.scaleFactor * 1.2);
            yPos = (int) (Screen.height * 0.85);
            foreach (List<ActionType> group in groups[(int) v]) {
                GameObject keyButton = Instantiate (KeyButtonPrefab);
                Transform keyButtonTransform = keyButton.GetComponent<Transform> ();
                KeybindButton buttonScript = keyButton.GetComponent<KeybindButton> ();
                keyButton.transform.SetParent (keybindMenu.transform, false);
                buttonScript.Init (group);
                keyButtonTransform.position = new Vector3 (xPos, yPos, 0);
                yPos -= (int) (buttonSize.rect.height * canvas.scaleFactor * 1.5);

                if (yPos < buttonSize.rect.height * canvas.scaleFactor * 1.5) {
                    xPos += (int) (buttonSize.rect.width * canvas.scaleFactor * 1.2);
                    yPos = (int) (Screen.height * 0.85);
                }
            }
        }
    }

    public void Cleanup () {
        for (short i = 0; i < 4; ++i) {
            reportScore (i, 0);
        }
        ConnectMenu ();
    }

    public void KeybindMenu () {
        menuOpen = !menuOpen;
        keybindMenu.SetActive (menuOpen);
        state.OpenMenu (menuOpen);
    }

    public void reportScore (short netID, int score) {
        Text scoreField = null;
        Transform scoreText = scores.transform.Find ("Player" + (netID + 1));
        if (scoreText != null) {
            scoreField = scoreText.GetComponent<Text> ();
            scoreField.text = "Player " + (netID + 1) + "\nScore: " + score;
        }
    }

    public static Color GetColorByNetID (short netID) {
        switch (netID) {
            case 0:
            default:
                return Color.red;
            case 1:
                return Color.blue;
            case 2:
                return Color.green;
            case 3:
                return Color.yellow;
        }
    }

    //functions to bind MapSelect button decision callbacks to.
    //they have to be here because they need to reference a MonoBehavior.

    //sends a start game message to all clients. All clients that recieve this will then request a unit sync from the server.
    public void SendStartGame () {
        Debug.Log ("Start game called.");
        if (state.isServer) {
            state.StartGame ();
            state.network.SendMessage (new StartGame ());
        }
    }

}