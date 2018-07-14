using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuiManager : MonoBehaviour {

    public GameObject joinButton;
    public GameObject createButton;
    public GameObject disconnectButton;
    public InputField inputField;
    public Text roomID;
    //private StateManager state;

    // Use this for initialization
    void Start() {
        if (!(joinButton && createButton && disconnectButton && inputField && roomID)) {
            throw new System.Exception("GUI elements not defined");
        }
        //state = GetComponent<StateManager>();
    }

    public void LobbyGUI() {
        joinButton.SetActive(true);
        inputField.ActivateInputField();
        createButton.SetActive(true);
        disconnectButton.SetActive(false);
    }

    public void GameGUI() {
        joinButton.SetActive(false);
        inputField.DeactivateInputField();
        createButton.SetActive(false);
        disconnectButton.SetActive(true);
    }
}
