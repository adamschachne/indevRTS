using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InputActions {

	//ModKey is a class that stores a KeyCode and if it has been modified by Shift, Ctrl and Alt
	public class ModKey {
		public KeyCode key;
		public bool shift;
		public bool ctrl;
		public bool alt;

		public static readonly ModKey none = new ModKey (KeyCode.None);

		public ModKey (KeyCode _key, bool _shift = false, bool _ctrl = false, bool _alt = false) {
			key = _key;
			shift = _shift;
			ctrl = _ctrl;
			alt = _alt;
		}

		public ModKey Copy () {
			return new ModKey (key, shift, ctrl, alt);
		}

		public ModKey[] ModularCopy () {
			ModKey[] modularKeys = new ModKey[4];
			this.alt = false;
			this.ctrl = false;
			this.shift = false;
			modularKeys[0] = this;
			modularKeys[1] = this.Copy ();
			modularKeys[1].shift = true;
			modularKeys[2] = this.Copy ();
			modularKeys[2].ctrl = true;
			modularKeys[3] = this.Copy ();
			modularKeys[3].alt = true;

			return modularKeys;
		}

		public override bool Equals (System.Object obj) {
			ModKey other = obj as ModKey;
			if (other == null)
				return false;
			else
				return (key == other.key &&
					shift == other.shift &&
					ctrl == other.ctrl &&
					alt == other.alt);
		}

		public override int GetHashCode () {
			int hashCode = (int) key;

			if (shift) hashCode += 1024;
			if (ctrl) hashCode += 2048;
			if (alt) hashCode += 4096;

			return hashCode;
		}

		public override String ToString () {
			String s = "";
			if (shift) s += "Shift + ";
			if (ctrl) s += "Ctrl + ";
			if (alt) s += "Alt + ";
			s += key;
			return s;
		}
	}

	//inheritable typesafe enum implementation
	public abstract class ActionType {

		public enum InputType {
			Down = 0,
			Hold = 1,
			Up = 2
		}

		//Index is what the data structures in the InputManager will use to find an Action in its list of arrays. Please make it a unique, sequential number.
		protected readonly int index;

		//Name can be used as a tag to check whether an action is of a particular type. Use getTaggedActions() in the inputmanager to do so
		public readonly string name;

		//Group is an optional parameter. You can define an Action as in a particular group, and then the GetActionGroups will assemble those Actions into groups 
		//The group is a string, and the strings of everything in a group should be identical. This string is used as the name of the keybind.
		public readonly String group;

		//This is how to define whether an Action should trigger when the input is pressed down, is held, or is released
		public readonly InputType inputType;

		//modular is a bool that can be flagged if the action needs to listen to any variations of shift/alt/ctrl + the key
		public readonly bool modular;

		//this is the default key combo that this action will be set to.
		public readonly ModKey defaultKey;
		public ModKey currentKey;

		protected readonly List<Action> subscribers;

		protected ActionType (int _index, string _name, ModKey _defaultKey = null, String _group = "NONE", InputType _inputType = InputType.Down, bool _modular = false) {
			index = _index;
			name = _name;
			subscribers = new List<Action> ();
			if (_defaultKey == null) {
				defaultKey = ModKey.none;
				currentKey = ModKey.none;
			} else {
				defaultKey = _defaultKey;
				currentKey = _defaultKey;
			}

			if (_group.Equals ("NONE")) {
				group = name;
			} else {
				group = _group;
			}
			inputType = _inputType;
			modular = _modular;

		}

		//overload the (int) operator
		public static explicit operator Int32 (ActionType a) {
			return a.GetIndex ();
		}

		public static explicit operator String (ActionType a) {
			return a.ToString ();
		}

		private int GetIndex () {
			return index;
		}

		public override string ToString () {
			return name;
		}

		public void AddSubscriber (Action f) {
			subscribers.Add (f);
		}

		public void RemoveSubscriber (Action f) {
			subscribers.Remove (f);
		}

		public void Execute () {
			foreach (Action f in subscribers) {
				f ();
			}
		}

		public static List<List<ActionType>> GetActionGroups (ActionType[] actions) {
			HashSet<String> groupNames = new HashSet<String> ();
			List<List<ActionType>> groups = new List<List<ActionType>> ();
			foreach (ActionType a in actions) {
				//"ungrouped" actions should be made into their own groups
				groupNames.Add (a.group);
			}

			//TODO: If necessary, optimize this for many groups by using a hashset. currently pretty slow for large # of actions
			foreach (String name in groupNames) {
				List<ActionType> group = new List<ActionType> ();
				//iterates through the whole list to find string match (bad)
				foreach (ActionType a in actions) {
					if (a.group.Equals (name)) {
						group.Add (a);
					}
				}
				groups.Add (group);
			}

			return groups;
		}

	}

	public class Global : ActionType {

		public static readonly Global MENU = new Global (0, "Open/Close Menu", new ModKey (KeyCode.Tab));

		public static ActionType[] actions = new ActionType[] { MENU };

		public static readonly StateManager.View view = StateManager.View.Global;
		private Global (int _index, string _name, ModKey _defaultKey = null, String _group = "NONE", InputType _inputType = InputType.Down, bool _modular = false) : base (_index, _name, _defaultKey, _group, _inputType, _modular) { }
	}

	public class Lobby : ActionType {

		//public static readonly Lobby NONE = new Lobby(0, "NONE");
		public static ActionType[] actions = new ActionType[] { };
		public static readonly StateManager.View view = StateManager.View.Lobby;
		private Lobby (int _index, string _name, ModKey _defaultKey = null, String _group = "NONE", InputType _inputType = InputType.Down, bool _modular = false) : base (_index, _name, _defaultKey, _group, _inputType, _modular) { }
	}

	//use this as reference for making new ActionTypes
	public class RTS : ActionType {

		public static readonly RTS SELECT_DOWN = new RTS (0, "SELECT_DOWN", new ModKey (KeyCode.Mouse0), "Select", InputType.Down, true);
		public static readonly RTS SELECT_UP = new RTS (1, "SELECT_UP", new ModKey (KeyCode.Mouse0), "Select", InputType.Up, true);
		public static readonly RTS MOVE = new RTS (2, "Move", new ModKey (KeyCode.Mouse1));
		public static readonly RTS STOP = new RTS (3, "Stop", new ModKey (KeyCode.S));
		public static readonly RTS SPAWN_SHOOTGUY = new RTS (4, "Build Soldier", new ModKey (KeyCode.Q));
		public static readonly RTS SPAWN_IRONFOE = new RTS (5, "Build Ironfoe", new ModKey (KeyCode.W));
		public static readonly RTS SPAWN_DOG = new RTS (6, "Build Dog", new ModKey (KeyCode.E));
		public static readonly RTS SPAWN_MORTAR = new RTS (7, "Build Mortar (Unimplemented)", new ModKey (KeyCode.R));
		public static readonly RTS CANCEL_BUILD = new RTS (8, "Cancel Build", new ModKey (KeyCode.X));
		public static readonly RTS TEASE_ATTACK = new RTS(9, "Tease Attack", new ModKey(KeyCode.A), "Attack", InputType.Hold, true);
		public static readonly RTS ATTACK = new RTS (10, "Attack", new ModKey (KeyCode.A), "Attack", InputType.Up, true);
		public static readonly RTS CONTROL_GROUP_1 = new RTS (11, "Control Group 1", new ModKey (KeyCode.Alpha1), "NONE", InputType.Down, true);
		public static readonly RTS CONTROL_GROUP_2 = new RTS (12, "Control Group 2", new ModKey (KeyCode.Alpha2), "NONE", InputType.Down, true);
		public static readonly RTS CONTROL_GROUP_3 = new RTS (13, "Control Group 3", new ModKey (KeyCode.Alpha3), "NONE", InputType.Down, true);
		public static readonly RTS CONTROL_GROUP_4 = new RTS (14, "Control Group 4", new ModKey (KeyCode.Alpha4), "NONE", InputType.Down, true);
		public static readonly RTS CONTROL_GROUP_5 = new RTS (15, "Control Group 5", new ModKey (KeyCode.Alpha5), "NONE", InputType.Down, true);

		//this variable MUST be named actions
		//put the const values into the array in the same order you initalized them
		public static ActionType[] actions = new ActionType[] {
			SELECT_DOWN,
			SELECT_UP,
			MOVE,
			STOP,
			SPAWN_SHOOTGUY,
			SPAWN_IRONFOE,
			SPAWN_DOG,
			SPAWN_MORTAR,
			CANCEL_BUILD,
			TEASE_ATTACK,
			ATTACK,
			CONTROL_GROUP_1,
			CONTROL_GROUP_2,
			CONTROL_GROUP_3,
			CONTROL_GROUP_4,
			CONTROL_GROUP_5
		};
		//this variable MUST be named view
		public static readonly StateManager.View view = StateManager.View.RTS;

		private RTS (int _index, string _name, ModKey _defaultKey = null, String _group = "NONE", InputType _inputType = InputType.Down, bool _modular = false) : base (_index, _name, _defaultKey, _group, _inputType, _modular) { }
	}

}