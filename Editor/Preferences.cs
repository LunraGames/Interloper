using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace LunraGames.Interloper
{
	[Serializable]
	public class Preferences
	{
		[JsonIgnore]
		public static List<string> DefaultAssemblies
		{
			get 
			{
				return new List<string> 
				{
					"Assembly-CSharp",
					"Assembly-CSharp-firstpass",
					"Assembly-CSharp-Editor",
					"Assembly-CSharp-Editor-firstpass"
				};
			}
		}

		public string ProjectId;
		public Tabs ActiveTab;
		public List<string> ActiveAssemblies = DefaultAssemblies;

		public bool HasPlayed;
		public List<Entry> Entries = new List<Entry>();
		public List<bool> EntriesShown = new List<bool>();
		public List<bool> EntriesEnabled = new List<bool>();
		public List<string> EntryRunValues = new List<string>();
		public List<string> EntryDefaultValues = new List<string>();
		public Vector2 ScrollPosition = Vector2.zero;
	}
}