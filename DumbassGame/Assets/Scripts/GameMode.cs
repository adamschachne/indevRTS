using System.Collections;
using System.Collections.Generic;
using System;

namespace GameMode
{
	//inheritable typesafe enum implementation
	public abstract class ActionType
	{
		protected readonly int index;
		protected readonly string name;
		protected readonly List<Action> subscribers;

		protected ActionType(int _index, string _name)
		{
			index = _index;
			name = _name;
			subscribers = new List<Action>();
		}

		//overload the (int) operator
		public static explicit operator Int32(ActionType a)
		{
			return a.GetIndex();
		}

		public static explicit operator String(ActionType a)
		{
			return a.ToString();
		}

		private int GetIndex()
		{
			return index;
		}

		public override string ToString()
		{
			return name;
		}

		public void AddSubscriber(Action f)
		{
			subscribers.Add(f);
		}

		public void RemoveSubscriber(Action f)
		{
			subscribers.Remove(f);
		}

		public void Execute()
		{
			foreach(Action f in subscribers)
			{
				f();
			}
		}

	}

	public class Lobby : ActionType
	{
		
		public static readonly Lobby NONE = new Lobby(0, "NONE");

		public static ActionType[] actions = new ActionType[]{NONE};
		public static StateManager.View view = StateManager.View.Lobby;
		private Lobby(int _index, string _name) : base(_index, _name){ }
	}

	//use this as reference for making new ActionTypes
	public class RTS : ActionType
	{

		public static readonly RTS SELECT_DOWN = new RTS(0, "SELECT_DOWN");
		public static readonly RTS SELECT_UP = new RTS(1, "SELECT_UP");
		public static readonly RTS MOVE = new RTS(2, "MOVE");

		//this variable MUST be named actions
		//put the const values into the array in the same order you initalized them
		public static ActionType[] actions = new ActionType[]{SELECT_DOWN, SELECT_UP, MOVE};
		//this variable MUST be named view
		public static StateManager.View view = StateManager.View.RTS;

		private RTS(int _index, string _name) : base(_index, _name) { } 
	}

}
