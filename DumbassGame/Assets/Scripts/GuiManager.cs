using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuiManager : MonoBehaviour {

    public GameObject JoinButton;
    public GameObject CreateButton;
    public GameObject DisconnectButton;

    private StateManager state;

    // Use this for initialization
    void Start() {
        if (!(JoinButton && CreateButton && DisconnectButton)) {
            throw new System.Exception("GUI Buttons not defined");
        }

        state = FindObjectOfType<StateManager>();
    }

    public void LobbyGUI() {
        JoinButton.SetActive(true);
        CreateButton.SetActive(true);
        DisconnectButton.SetActive(false);
    }

    public void GameGUI() {
        JoinButton.SetActive(false);
        CreateButton.SetActive(false);
        DisconnectButton.SetActive(true);
    }
}
