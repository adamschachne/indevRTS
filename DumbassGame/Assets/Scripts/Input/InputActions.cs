using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace InputActions {

	//ModKey is a class that stores a KeyCode and if it has been modified by Shift, Ctrl and Alt
	public class ModKey{
		public KeyCode key;
		public bool shift;
		public bool ctrl;
		public bool alt;

		public static readonly ModKey none = new ModKey(KeyCode.None);

		public ModKey(KeyCode _key, bool _shift = false, bool _ctrl = false, bool _alt = false)
		{
			key = _key;
			shift = _shift;
			ctrl = _ctrl;
			alt = _alt;
		}

		public ModKey Copy()
		{
			return new ModKey(key, shift, ctrl, alt);
		}

		public override bool Equals(System.Object obj)
		{
			ModKey other = obj as ModKey;
			if(other == null)
				return false;
			else
				return (key == other.key &&
						shift == other.shift &&
						ctrl == other.ctrl &&
						alt == other.alt);
		}

		public override int GetHashCode()
		{
			int hashCode = (int)key;

			if(shift) hashCode += 1024;
			if(ctrl) hashCode += 2048;
			if(alt) hashCode += 4096;

			return hashCode;
		}

		public override String ToString()
		{
			String s = "";
			if(shift) s += "Shift + ";
			if(ctrl) s += "Ctrl + ";
			if(alt) s += "Alt + ";
			s += key;
			return s;
		}
	}

	//inheritable typesafe enum implementation
	public abstract class ActionType {

		public enum InputType {
			Down = 0,
			Hold = 1,
			Up	 = 2
		}

		//Index is what the data structures in the InputManager will use to find an Action in its list of arrays. Please make it a unique, sequential number.
		protected readonly int index;

		//Name is used for debug purposes and for easy checking when things go wrong in an action call. It is not used in logic anywhere
		protected readonly string name;

		//Group is an optional parameter. You can define an Action as in a particular group, and then the GetActionGroups will assemble those Actions into groups 
		//The group is a string, and the strings of everything in a group should be identical. This string is used as the name of the keybind.
		public readonly String group;

		//This is how to define whether an Action should trigger when the input is pressed down, is held, or is released
		public readonly InputType inputType;

		//this is the default key combo that this action will be set to.
		public readonly ModKey defaultKey;
		public ModKey currentKey;

		protected readonly List<Action> subscribers;

		protected ActionType(int _index, string _name, ModKey _defaultKey = null, String _group = "NONE", InputType _inputType = InputType.Down) {
			index = _index;
			name = _name;
			subscribers = new List<Action>();
			if(_defaultKey == null)
			{
				defaultKey = ModKey.none;
				currentKey = ModKey.none;
			}
			else
			{
				defaultKey = _defaultKey;
				currentKey = _defaultKey;
			}

			if(_group.Equals("NONE")) {
				group = name;
			} else {
				group = _group;
			}
			inputType = _inputType;
			
		}

		//overload the (int) operator
		public static explicit operator Int32(ActionType a) {
			return a.GetIndex();
		}

		public static explicit operator String(ActionType a) {
			return a.ToString();
		}

		private int GetIndex() {
			return index;
		}

		public override string ToString() {
			return name;
		}

		public void AddSubscriber(Action f) {
			subscribers.Add(f);
		}

		public void RemoveSubscriber(Action f) {
			subscribers.Remove(f);
		}

		public void Execute() {
			foreach(Action f in subscribers) {
				f();
			}
		}

		public static List<List<ActionType>> GetActionGroups(ActionType[] actions) {
			HashSet<String> groupNames = new HashSet<String>();
			List<List<ActionType>> groups = new List<List<ActionType>>();
			foreach(ActionType a in actions) {
				//"ungrouped" actions should be made into their own groups
				groupNames.Add(a.group);
			}

			//TODO: If necessary, optimize this for many groups by using a hashset. currently pretty slow for large # of actions
			foreach(String name in groupNames) {
				List<ActionType> group = new List<ActionType>();
				//iterates through the whole list to find string match (bad)
				foreach(ActionType a in actions) {
					if(a.group.Equals(name)) {
						group.Add(a);
					}
				}
				groups.Add(group);
			}

			return groups; 
		}

	}

	public class Global : ActionType {
		
		public static readonly Global MENU = new Global(0, "Open/Close Menu", new ModKey(KeyCode.Escape));

		public static ActionType[] actions = new ActionType[]{MENU};

		public static readonly StateManager.View view = StateManager.View.Global;
		private Global(int _index, string _name, ModKey _defaultKey = null, String _group = "NONE", InputType _inputType = InputType.Down) 
			: base(_index, _name, _defaultKey, _group, _inputType){ }
	}

	public class Lobby : ActionType {
		
		//public static readonly Lobby NONE = new Lobby(0, "NONE");
		public static ActionType[] actions = new ActionType[]{};
		public static readonly StateManager.View view = StateManager.View.Lobby;
		private Lobby(int _index, string _name, ModKey _defaultKey = null, String _group = "NONE", InputType _inputType = InputType.Down) 
			: base(_index, _name, _defaultKey, _group, _inputType){ }
	}

	//use this as reference for making new ActionTypes
	public class RTS : ActionType {

		public static readonly RTS SELECT_DOWN = new RTS(0, "SELECT_DOWN", new ModKey(KeyCode.Mouse0), "Select", InputType.Down);
		public static readonly RTS SELECT_UP = new RTS(1, "SELECT_UP", new ModKey(KeyCode.Mouse0), "Select",  InputType.Up);
		public static readonly RTS MOVE = new RTS(2, "Move", new ModKey(KeyCode.Mouse1));
		public static readonly RTS STOP = new RTS(3, "Stop", new ModKey(KeyCode.S));
		public static readonly RTS SPAWN_SHOOTGUY = new RTS(4, "Spawn Soldier", new ModKey(KeyCode.Alpha1));

		public static readonly RTS SPAWN_IRONFOE = new RTS(5, "Spawn Ironfoe", new ModKey(KeyCode.Alpha2));
		public static readonly RTS ATTACK = new RTS(6, "Attack", new ModKey(KeyCode.A));
		//this variable MUST be named actions
		//put the const values into the array in the same order you initalized them
		public static ActionType[] actions = new ActionType[]{SELECT_DOWN, SELECT_UP, MOVE, STOP, SPAWN_SHOOTGUY, SPAWN_IRONFOE, ATTACK};
		//this variable MUST be named view
		public static readonly StateManager.View view = StateManager.View.RTS;

		private RTS(int _index, string _name, ModKey _defaultKey = null, String _group = "NONE", InputType _inputType = InputType.Down) 
			: base(_index, _name, _defaultKey, _group, _inputType){ }
	}



}
