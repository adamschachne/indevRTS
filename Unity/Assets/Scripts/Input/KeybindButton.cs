using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using InputActions;

public class KeybindButton : MonoBehaviour {

	private List<ActionType> group;
	private Button thisButton;
	public Text keyLabel;
	private Text actionLabel;
	private ModKey key;
	private bool binding = false;

	void Start() {
		thisButton = GetComponent<Button>();
		thisButton.onClick.AddListener(StartBind);
		Text[] texts = GetComponentsInChildren<Text>();
		keyLabel = texts[0];
		actionLabel = texts[1];

		if(group != null && key != null) {
			keyLabel.text = key.ToString();
			actionLabel.text = group[0].group;
		}

	}

	void Update()
	{
		if(!binding)
		{
			key = group[0].currentKey;
			keyLabel.text = key.ToString();
		}
	}

	public void Init(List<ActionType> _group) {
		group = _group;
		key = group[0].defaultKey;
	}

	private void StartBind() {
		if(Input.GetKeyUp(KeyCode.Mouse0))
			StateManager.state.input.startBind(this.gameObject, group);

		binding = true;
	}

	public void EndBind()
	{
		key = group[0].currentKey;
		binding = false;
	}
}
