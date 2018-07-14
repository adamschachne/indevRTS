using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using GameMode;

public class InputManager : MonoBehaviour 
{

    private StateManager state;
    ActionType[][] contexts;
    IEnumerable<Type> actionTypes;

    void Awake()
    {
        state = GetComponent<StateManager>();

        //FISHING AROUND IN THE ASSEMBLY TO FIND ALL SUBCLASSES OF ACTIONTYPE
        actionTypes =   from assembly in AppDomain.CurrentDomain.GetAssemblies()
                        from type in assembly.GetTypes()
                        where type.IsSubclassOf(typeof(ActionType))
                        select type;

        contexts = new ActionType[Enum.GetValues(typeof(StateManager.View)).Length][];

        foreach(Type t in actionTypes)
        {
            //using Type to get public static field named "view" and cast it to StateManager.View
            StateManager.View v = (StateManager.View)t.GetField("view", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            //Using Type to get pulic static field named "actions" and cast it to StateManager.View
            ActionType[] a = (ActionType[])t.GetField("actions", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            //putting Array of ActionTypes into the context array at the position defined by View v
            contexts[(int)v] = a;
        }
    }

    private StateManager.View ViewOfAction(ActionType a)
    {
        foreach(Type t in actionTypes)
        {
            if(System.Object.ReferenceEquals(t, a.GetType()))
            {
                return (StateManager.View)t.GetField("view", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            }
        }

        throw new System.Exception("Could not find the view paired to the object: " + a);
    }

    public void Subscribe(Action f, ActionType a)
    {
        contexts[(int)ViewOfAction(a)][(int)a].AddSubscriber(f);
    }

    public void Unsubscribe(Action f, ActionType a)
    {
        contexts[(int)ViewOfAction(a)][(int)a].RemoveSubscriber(f);
    }

    void Update()
    {
        ActionType[] context = contexts[(int)state.gameView];
        if (Input.GetMouseButtonDown (0)) {
            if(0 < context.Length && context[0] != null)
                context[0].Execute();
        }

        if (Input.GetMouseButtonUp(0)) {
            if(1 < context.Length && context[1] != null)
                context[1].Execute();
        }

        if (Input.GetMouseButtonDown (1)) {
            if(2 < context.Length && context[2] != null)
                context[2].Execute();
        }
    }
}
