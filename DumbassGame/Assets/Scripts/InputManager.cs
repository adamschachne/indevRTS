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

    public void defaultKeys(StateManager.View v)
    {
        keyMap[(int)v].Clear();
        foreach(List<ActionType> singleGroup in groups[(int)v]) {
            ModKey defaultKey = singleGroup.ElementAt(0).defaultKey;
            keyMap[(int)v][defaultKey] = singleGroup;
        }
    }

    private ActionType getActionFromInput(List<ActionType> group, ActionType.InputType inputType)
    {
        foreach(ActionType a in group)
        {
            if(a.inputType == inputType)
                return a;
        }

        return null;
    }

    void Update() {
        //update the state of our current input's modifiers.
        ci.shift = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        ci.ctrl = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        ci.alt = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        //check 
        foreach(KeyCode vKey in keyCodeList) {
            ci.key = vKey;
            if(!keyMap[(int)state.gameView].ContainsKey(ci))
                continue;

            List<ActionType> actionGroup = keyMap[(int)state.gameView][ci];
            if(actionGroup == null) {
                Debug.Log("Found a null actionGroup list when you pressed down " + ci.ToString());
                continue;
            }
            
            ActionType a;
            if(Input.GetKeyDown(vKey)) {
                a = getActionFromInput(actionGroup, ActionType.InputType.Down);
                if(a != null)
                    a.Execute();
            }
            if(Input.GetKey(vKey) && keyMap[(int)state.gameView].ContainsKey(ci)) {
                a = getActionFromInput(actionGroup, ActionType.InputType.Hold);
                if(a != null)
                    a.Execute();
            } 
            if(Input.GetKeyUp(vKey) && keyMap[(int)state.gameView].ContainsKey(ci)) {
                a = getActionFromInput(actionGroup, ActionType.InputType.Up);
                if(a != null)
                    a.Execute();
            } 
        }
    }
}
