using System.Collections;
using System.Collections.Generic;
using InputActions;
using UnityEngine;
using UnityEngine.UI;

public class GuiManager : MonoBehaviour {

    public Canvas canvas;
    public GameObject menu;
    public GameObject KeyButtonPrefab;
    public GameObject joinButton;
    public GameObject createButton;
    public GameObject disconnectButton;
    public InputField inputField;
    public Text roomID;
    private StateManager state;
    private bool menuOpen = true;

    // Use this for initialization
    void Start () {
        if (!(joinButton && createButton && disconnectButton && inputField && roomID && canvas && menu && KeyButtonPrefab)) {
            throw new System.Exception ("GUI elements not defined");
        }
        state = GetComponent<StateManager> ();
        state.input.Subscribe (this.Menu, Global.MENU);
        Menu ();
    }

    public void LobbyGUI () {
        joinButton.SetActive (true);
        inputField.ActivateInputField ();
        createButton.SetActive (true);
        disconnectButton.SetActive (false);
        roomID.enabled = false;
    }

    public void GameGUI () {
        joinButton.SetActive (false);
        inputField.DeactivateInputField ();
        createButton.SetActive (false);
        disconnectButton.SetActive (true);
        roomID.enabled = true;
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
                keyButton.transform.SetParent (menu.transform, false);
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

    public void Menu () {
        menuOpen = !menuOpen;
        menu.SetActive (menuOpen);
        state.OpenMenu (menuOpen);
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
}