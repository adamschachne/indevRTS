using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using InputActions;

public class InputManager : MonoBehaviour 
{

    private StateManager state;
    private ActionType[][] contexts;            //main array of all found ActionTypes
    private IEnumerable<Type> actionTypes;      //assembly list of all classes that extend ActionType
    private List<List<ActionType>>[] groups;    //grouped list of tagged actions. (int)View gets the List<List<ActionType>> for a particular gamemode
                                                //the outer List contains the inner lists of grouped ActionTypes.
    //Our map of Keybinds -> Actions
    private Dictionary<ModKey, List<ActionType>>[] keyMap;  
    private ModKey ci;                          //variable that stores this frame's current input.
    private KeyCode[] keyCodeList;              //list of all keycodes that we're checking for input 
    private StateManager.View[] gameModes;      //list of all gameModes from our StateManager

    //variables needed to rebind a key from a button
    private bool binding = false;
    private UnityEngine.UI.Text textToBind;
    private List<ActionType> groupToBind;
    private HashSet<KeyCode> keysToIgnore = new HashSet<KeyCode>();
    private bool mouseDown = false;
    private KeybindButton buttonToBind;

    void Awake() {
        //setup initial variables
        state = GetComponent<StateManager>();
        ci = new ModKey(KeyCode.None);
        keyCodeList = Enum.GetValues(typeof(KeyCode)) as KeyCode[];
        gameModes = Enum.GetValues(typeof(StateManager.View)) as StateManager.View[];

        //FISHING AROUND IN THE ASSEMBLY TO FIND ALL SUBCLASSES OF ACTIONTYPE
        actionTypes =   from assembly in AppDomain.CurrentDomain.GetAssemblies()
                        from type in assembly.GetTypes()
                        where type.IsSubclassOf(typeof(ActionType))
                        select type;

        contexts = new ActionType[gameModes.Length][];
        groups = new List<List<ActionType>>[gameModes.Length];
        keyMap = new Dictionary<ModKey, List<ActionType>>[gameModes.Length];
        keysToIgnore.Add(KeyCode.LeftShift);
        keysToIgnore.Add(KeyCode.RightShift);
        keysToIgnore.Add(KeyCode.LeftControl);
        keysToIgnore.Add(KeyCode.RightControl);
        keysToIgnore.Add(KeyCode.LeftAlt);
        keysToIgnore.Add(KeyCode.RightAlt);

        foreach(Type t in actionTypes) {
            //using Type to get public static field named "view" and cast it to StateManager.View
            StateManager.View v = (StateManager.View)t.GetField("view", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            //Using Type to get pulic static field named "actions" and cast it to ActionType[]
            ActionType[] a = (ActionType[])t.GetField("actions", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            //putting Array of ActionTypes into the context array at the position defined by View v
            contexts[(int)v] = a;
            //sorting the actionTypes into groups of paired actions
            groups[(int)v] = ActionType.GetActionGroups(a);
            //initialize the KeyMap of this GameMode to the default keys
            keyMap[(int)v] = new Dictionary<ModKey, List<ActionType>>();
            defaultKeys(v);
        }

        //default to lobby if it found no action set for a particular context
        for(int i = 0; i < contexts.Length; ++i) {
            if(contexts[i] == null) {
                Debug.Log("WARNING: Input Manager did not find an action set for the StateManager View pointing to index: " + i);
                Debug.Log("Defaulting to lobby.");
                contexts[i] = contexts[(int)StateManager.View.Lobby];
                groups[i] = ActionType.GetActionGroups(contexts[(int)StateManager.View.Lobby]);
            }
        }

        state.gui.CreateKeybindButtons(groups, gameModes);
        
    }

    private StateManager.View ViewOfAction(ActionType a) {
        foreach(Type t in actionTypes) {
            if(System.Object.ReferenceEquals(t, a.GetType())) {
                return (StateManager.View)t.GetField("view", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            }
        }
        throw new System.Exception("Could not find the view paired to the object: " + a);
    }

    public void Subscribe(Action f, ActionType a) {
        contexts[(int)ViewOfAction(a)][(int)a].AddSubscriber(f);
    }

    public void Unsubscribe(Action f, ActionType a) {
        contexts[(int)ViewOfAction(a)][(int)a].RemoveSubscriber(f);
    }

    public void defaultKeys(StateManager.View v) {
        keyMap[(int)v].Clear();
        foreach(List<ActionType> singleGroup in groups[(int)v]) {
            if(v == StateManager.View.Global)
                Debug.Log("Global Groups: " + singleGroup[0] + " bound to " + singleGroup[0].defaultKey.ToString());
            bind(v, singleGroup[0].defaultKey, singleGroup);
        }
    }

    public void startBind(GameObject obj, List<ActionType> group) {
        KeybindButton kbb = obj.GetComponent<KeybindButton>();
        
        if(binding) {
            if(kbb.keyLabel == textToBind && group == groupToBind)
            {
                endBind();
                return;
            }
            endBind();
        }

        buttonToBind = kbb;
        textToBind = kbb.keyLabel;
        groupToBind = group;
        binding = true;
        kbb.keyLabel.text = "Cancel";
        mouseDown = true;
    }

    public void endBind()
    {
        binding = false;
        if(textToBind != null && groupToBind != null) 
            textToBind.text = groupToBind[0].currentKey.ToString();
        textToBind = null;
        groupToBind = null;
        mouseDown = false;
        if(buttonToBind != null)
            buttonToBind.EndBind();
        buttonToBind = null;
    }

    private void bind(StateManager.View v, ModKey newKey, List<ActionType> group) {
        
        if(keyMap[(int)v].ContainsKey(group[0].currentKey))
        {
            keyMap[(int)v].Remove(group[0].currentKey);
        }
        

        if(keyMap[(int)v].ContainsKey(newKey)) {
            keyMap[(int)v][newKey][0].currentKey = ModKey.none;
            keyMap[(int)v].Remove(newKey);
        }
        group[0].currentKey = newKey;
        keyMap[(int)v][newKey] = group;
        endBind();
    }

    private ActionType getActionFromInput(List<ActionType> group, ActionType.InputType inputType) {
        foreach(ActionType a in group) {
            if(a.inputType == inputType)
                return a;
        }

        return null;
    }

    void Update() {
        //update the state of our current input's modifiers.
        ci.shift = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        ci.ctrl = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        ci.alt = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));

        //check all possible keys for input
        foreach(KeyCode vKey in keyCodeList) {
            if(keysToIgnore.Contains(vKey))
                continue;

            ci.key = vKey;
            if(binding) {
                if(mouseDown && vKey == KeyCode.Mouse0)
                {
                    if(Input.GetKeyUp(vKey))
                        mouseDown = false;

                    continue;
                }

                if(Input.GetKey(vKey) && vKey != KeyCode.Mouse0) {
                    textToBind.text = ci.ToString();
                }
                if(Input.GetKeyUp(vKey)) {
                    bind(ViewOfAction(groupToBind[0]), ci.Copy(), groupToBind);
                }
                continue;
            }
                
            StateManager.View oldState = state.gameView;
            checkKeymap(state.gameView);
            if(oldState != StateManager.View.Global && state.gameView != StateManager.View.Global)
                checkKeymap(StateManager.View.Global);

        }
    }

    private void checkKeymap(StateManager.View v)
    {
        if(!keyMap[(int)v].ContainsKey(ci))
            return;

        List<ActionType> actionGroup = keyMap[(int)v][ci];
        if(actionGroup == null) {
            Debug.Log("Found a null actionGroup list when you pressed down " + ci.ToString());
            return;
        }
        
        ActionType a;
        if(Input.GetKeyDown(ci.key)) {
            a = getActionFromInput(actionGroup, ActionType.InputType.Down);
            if(a != null)
                a.Execute();
        }
        if(Input.GetKey(ci.key) && keyMap[(int)v].ContainsKey(ci)) {
            a = getActionFromInput(actionGroup, ActionType.InputType.Hold);
            if(a != null)
                a.Execute();
        } 
        if(Input.GetKeyUp(ci.key) && keyMap[(int)v].ContainsKey(ci)) {
            a = getActionFromInput(actionGroup, ActionType.InputType.Up);
            if(a != null)
                a.Execute();
        }
    }
}