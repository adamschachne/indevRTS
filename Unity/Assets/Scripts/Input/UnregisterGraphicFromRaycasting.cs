using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnregisterGraphicFromRaycasting : MonoBehaviour {

	// Use this for initialization
	void Start () {
		GraphicRegistry.UnregisterGraphicForCanvas (StateManager.state.gui.canvas, GetComponent<Graphic> ());
	}

	void OnEnable () {
		GraphicRegistry.UnregisterGraphicForCanvas (StateManager.state.gui.canvas, GetComponent<Graphic> ());
	}

}