using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Linq;
using LunraGames;
using Newtonsoft.Json;

namespace LunraGames.Interloper
{
	public class InterloperWindow : EditorWindow {

		const string SettingsKey = "LG_Interloper_Settings";

		const string FieldName = "System.MonoField";
		const string MethodName = "System.MonoMethod";

		[Serializable]
		class Entry
		{
			public string CachedName;

			public string TypeName;
			public string InfoName;
			public string InfoTypeName;

			public Entry(object info)
			{
				if (info is FieldInfo)
				{
					var field = info as FieldInfo;
					InfoTypeName = FieldName;
					CachedName = GetTypeFullName(field.DeclaringType)+"."+field.Name;
					TypeName = field.DeclaringType.AssemblyQualifiedName;
					InfoName = field.Name;
				}
				else if (info is MethodInfo)
				{
					// todo: there's probably a less repetative way to do this...
					var method = info as MethodInfo;
					InfoTypeName = MethodName;
					CachedName = GetTypeFullName(method.DeclaringType)+"."+method.Name;
					TypeName = method.DeclaringType.AssemblyQualifiedName;
					InfoName = method.Name;
				}
			}

			public object GetInfo()
			{
				var declaring = Type.GetType(TypeName);
				if (InfoTypeName == FieldName || InfoTypeName == typeof(FieldInfo).FullName)
				{
					return declaring.GetField(InfoName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				}
				else if (InfoTypeName == MethodName || InfoTypeName == typeof(MethodInfo).FullName)
				{
					return declaring.GetMethod(InfoName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				}
				return null;
			}
		}

		[Serializable]
		class Config
		{
			public bool HasPlayed;
			public List<Entry> Entries = new List<Entry>();
			public List<bool> EntriesShown = new List<bool>();
			public List<bool> EntriesEnabled = new List<bool>();
			public List<string> EntryRunValues = new List<string>();
			public List<string> EntryDefaultValues = new List<string>();
			public Vector2 ScrollPosition = Vector2.zero;
		}

		static Type[] SupportedReflectedTypes = new Type[]
		{
			typeof(FieldInfo),
			typeof(MethodInfo)
		};

		internal static bool IsDirty;

		bool EditingEntry;
		Assembly[] BrowsingAssemblies;
		List<string> BrowsingAssemblyNames;
		int SelectedAssembly;
		Type[] BrowsingTypes;
		List<string> BrowsingTypeNames;
		int SelectedType;
		List<string> BrowsingReflectedTypeNames;
		int SelectedReflectedType;
		object[] Infos;
		List<string> InfoNames;
		int SelectedInfo;

		[SerializeField]
		Config Settings;
	
		InterloperWindow()
		{
			Settings = JsonConvert.DeserializeObject<Config>(EditorPrefs.GetString (SettingsKey, JsonConvert.SerializeObject(new Config())));
			if (!EditorApplication.isPlayingOrWillChangePlaymode) InterloperWindow.IsDirty = true;
			EditorApplication.update += InterloperUpdate;
		}

		[MenuItem ("Window/Lunra Games/Interloper")]
		static void Init () {
			var window = EditorWindow.GetWindow(typeof (InterloperWindow), false, "Interloper") as InterloperWindow;
			window.Show();
		}

		void OnGUI () {
			GUILayout.Label("Snoop on static values, and have them set on runtime. Re-added values may appear incorrect until recompiled or played.", EditorStyles.wordWrappedLabel);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Copy Config")) 
			{
				EditorGUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(Settings.Entries, Formatting.Indented);
				ShowNotification(new GUIContent("Copied config to clipboard!"));
			}
			if (GUILayout.Button("Paste Config"))
			{
				try
				{
					var result = JsonConvert.DeserializeObject<List<Entry>>(EditorGUIUtility.systemCopyBuffer);
					if (result == null) ShowNotification(new GUIContent("Can't paste a null config!"));
					else if (result.Count == 0) ShowNotification(new GUIContent("Can't paste an empty config!"));
					{
						ResetEditor();
						ResetEntries(result);
						ShowNotification(new GUIContent("Pasted config from clipboard!"));
					}
				}
				catch (Exception e)
				{
					UnityEditor.EditorUtility.DisplayDialog("Failed to paste config!", "Encountered exception:\n"+e.Message, "Okay");
				}
			}
			EditorGUILayout.EndHorizontal();

			try
			{
				if (EditingEntry && EditorApplication.isPlaying) ResetEditor();
				var wasEnabled = GUI.enabled;
				GUI.enabled = !EditorApplication.isPlaying;
				if (EditingEntry) EntryEditor();
				else if (GUILayout.Button("Add new..."))
				{
					ResetEditor();
					EditingEntry = true;
				}
				GUI.enabled = wasEnabled;

				Settings.ScrollPosition = EditorGUILayout.BeginScrollView(Settings.ScrollPosition);
				int? removedIndex = null;
				for (var i = 0; i < Settings.Entries.Count; i++)
				{
					var entry = Settings.Entries[i];
					var shown = Settings.EntriesShown[i];
					EditorGUILayout.BeginHorizontal();
					var wasFoldoutColor = GUI.contentColor;
					GUI.contentColor = (!Settings.EntriesEnabled[i] || Settings.EntryRunValues[i] == Settings.EntryDefaultValues[i]) ? Color.white : Color.yellow;
					Settings.EntriesShown[i] = EditorGUILayout.Foldout(shown, entry.CachedName);
					GUI.contentColor = wasFoldoutColor;
					if (GUILayout.Button("X", GUILayout.ExpandWidth(false))) removedIndex = i;
					EditorGUILayout.EndHorizontal();
					if (!shown) continue;
					EditorGUI.indentLevel++;
					{
						var info = entry.GetInfo();
						if (info == null) EditorGUILayout.HelpBox("Can't resolve "+entry.InfoName+" to a reflected type, make sure it still exists!", MessageType.Error);
						else if (entry.InfoTypeName == FieldName)
						{
							EditorGUILayout.BeginHorizontal();
							{
								GUILayout.Space(20f);
								var wasColor = GUI.contentColor;
								GUI.contentColor = Settings.EntriesEnabled[i] ? Color.green : Color.red;
								if (GUILayout.Button(Settings.EntriesEnabled[i] ? "Enabled" : "Disabled")) Settings.EntriesEnabled[i] = !Settings.EntriesEnabled[i];
								GUI.contentColor = wasColor;
							}
							EditorGUILayout.EndHorizontal();

							var field = info as FieldInfo;
							if (field.FieldType == typeof(bool)) Settings.EntryRunValues[i] = EntryToggle(Settings.EntriesEnabled[i], field, Settings.EntryDefaultValues[i], Settings.EntryRunValues[i]);
							else if (field.FieldType == typeof(string)) Settings.EntryRunValues[i] = EntryText(Settings.EntriesEnabled[i], field, Settings.EntryDefaultValues[i], Settings.EntryRunValues[i]);
							else if (field.FieldType == typeof(int)) Settings.EntryRunValues[i] = EntryInt(Settings.EntriesEnabled[i], field, Settings.EntryDefaultValues[i], Settings.EntryRunValues[i]);
							else EditorGUILayout.HelpBox("Fields of type "+field.FieldType.FullName+" are not currently supported.", MessageType.Warning);
						}
						else if (entry.InfoTypeName == MethodName)
						{
							EditorGUILayout.BeginHorizontal();
							{
								GUILayout.Space(20f);
								var method = info as MethodInfo;
								if (GUILayout.Button("Invoke")) 
								{
									try
									{
										method.Invoke(null, null);
									}
									catch (Exception e)
									{
										Debug.LogException(e);
									}
								}
							}
							EditorGUILayout.EndHorizontal();
						}
					}
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndScrollView();

				if (removedIndex.HasValue)
				{
					Settings.Entries.RemoveAt(removedIndex.Value);
					Settings.EntriesShown.RemoveAt(removedIndex.Value);
					Settings.EntriesEnabled.RemoveAt(removedIndex.Value);
					Settings.EntryRunValues.RemoveAt(removedIndex.Value);
					Settings.EntryDefaultValues.RemoveAt(removedIndex.Value);
				}	
			}
			catch (Exception e)
			{
				EditorGUILayout.HelpBox("An error occured, likely to do with serializing incompatable data. Reset the current values, or fix the issue.\n\nError: "+e.Message, MessageType.Error);
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Reset serialized values")) 
				{
					ResetEntries();
					ResetEditor();
				}
				if (GUILayout.Button("Print stack")) Debug.Log(e.StackTrace);
				EditorGUILayout.EndHorizontal();
			}
		}

		void InterloperUpdate()
		{
			if (EditorApplication.isPlaying)
			{
				if (!Settings.HasPlayed)
				{
					var gameWindow = EditorUtility.GetGameWindow();
					var modificationsMade = false;
					Settings.HasPlayed = true;
					for (var i = 0; i < Settings.Entries.Count; i++)
					{
						if (!Settings.EntriesEnabled[i]) continue;
						else modificationsMade = true;

						var entry = Settings.Entries[i];
						var info = entry.GetInfo();
						if (entry.InfoTypeName == FieldName)
						{
							var field = info as FieldInfo;
							if (field.FieldType == typeof(bool)) 
							{
								var runValue = Settings.EntryRunValues[i];
								if (runValue != null) field.SetValue(null, bool.Parse(runValue));
							}
							else if (field.FieldType == typeof(string))
							{
								var runValue = Settings.EntryRunValues[i];
								if (runValue != null) field.SetValue(null, runValue);
							}
							else if (field.FieldType == typeof(int))
							{
								var runValue = Settings.EntryRunValues[i];
								if (runValue != null) field.SetValue(null, int.Parse(runValue));
							}
						}
					}
					if (gameWindow != null && modificationsMade) gameWindow.ShowNotification(new GUIContent("Interloper is modifying static values!"));
					Repaint();
				}
			}
			else Settings.HasPlayed = false;

			if (IsDirty) Refresh();
		}

		void Refresh()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling) return;
			IsDirty = false;
			for (var i = 0; i < Settings.Entries.Count; i++)
			{
				var entry = Settings.Entries[i];
				var info = entry.GetInfo();
				if (entry.InfoTypeName == FieldName)
				{
					var field = info as FieldInfo;
					var fieldValue = field == null ? null : field.GetValue(null);
					Settings.EntryDefaultValues[i] = fieldValue == null ? null : fieldValue.ToString();
				}
			}
			Repaint();
		}

		string EntryToggle(bool enabled, FieldInfo info, string defaultValue, string targetValue)
		{
			var value = (bool)info.GetValue(null);
			var startValue = StringExtensions.IsNullOrWhiteSpace(defaultValue) ? value : bool.Parse(defaultValue);
			var runValue = StringExtensions.IsNullOrWhiteSpace(targetValue) ? value : bool.Parse(targetValue);

			var wasEnabled = GUI.enabled;
			GUI.enabled = false;
			if (EditorApplication.isPlaying) EditorGUILayout.Toggle("Current", value);
			else EditorGUILayout.Toggle("Default", startValue); 
			GUI.enabled = enabled;

			var runToggleText = EditorApplication.isPlaying ? "Set" : "On Run";
			runValue = EditorGUILayout.Toggle(runToggleText, runValue);
			// If the values have changed, HasPlayed needs to be called again.
			if (enabled && Settings.HasPlayed) Settings.HasPlayed = value == runValue;

			GUI.enabled = wasEnabled;
			return runValue.ToString();
		}

		string EntryText(bool enabled, FieldInfo info, string defaultValue, string targetValue)
		{
			var value = (string)info.GetValue(null);
			var startValue = StringExtensions.IsNullOrWhiteSpace(defaultValue) ? value : defaultValue;
			var runValue = StringExtensions.IsNullOrWhiteSpace(targetValue) ? value : targetValue;

			var wasEnabled = GUI.enabled;
			GUI.enabled = false;
			if (EditorApplication.isPlaying) EditorGUILayout.TextField("Current", value);
			else EditorGUILayout.LabelField("Default", startValue); 
			GUI.enabled = enabled;

			var runText = EditorApplication.isPlaying ? "Set" : "On Run";
			runValue = EditorGUILayout.TextField(runText, runValue);
			// If the values have changed, HasPlayed needs to be called again.
			if (enabled && Settings.HasPlayed) Settings.HasPlayed = value == runValue;

			GUI.enabled = wasEnabled;
			return runValue;
		}

		string EntryInt(bool enabled, FieldInfo info, string defaultValue, string targetValue)
		{
			var value = (int)info.GetValue(null);
			var startValue = StringExtensions.IsNullOrWhiteSpace(defaultValue) ? value : int.Parse(defaultValue);
			var runValue = StringExtensions.IsNullOrWhiteSpace(targetValue) ? value : int.Parse(targetValue);

			var wasEnabled = GUI.enabled;
			GUI.enabled = false;
			if (EditorApplication.isPlaying) EditorGUILayout.IntField("Current", value);
			else EditorGUILayout.IntField("Default", startValue); 
			GUI.enabled = enabled;

			var runText = EditorApplication.isPlaying ? "Set" : "On Run";
			runValue = EditorGUILayout.IntField(runText, runValue);
			// If the values have changed, HasPlayed needs to be called again.
			if (enabled && Settings.HasPlayed) Settings.HasPlayed = value == runValue;

			GUI.enabled = wasEnabled;
			return runValue.ToString();
		}

		void EntryEditor()
		{
			EditingEntry = !GUILayout.Button("Cancel");
			if (!EditingEntry) return;

			var completable = false;
			if (SelectedAssembly == 0) SelectedAssembly = EditorGUILayout.Popup(SelectedAssembly, BrowsingAssemblyNames.ToArray());
			else 
			{
				EditorGUILayout.LabelField(BrowsingAssemblyNames[SelectedAssembly]);
				if (BrowsingTypes == null)
				{
					BrowsingTypes = BrowsingAssemblies[SelectedAssembly - 1].GetTypes().OrderBy(type => type.FullName).ToArray();
					foreach (var type in BrowsingTypes) BrowsingTypeNames.Add(type.FullName);
				}
				if (SelectedType == 0) SelectedType = EditorGUILayout.Popup(SelectedType, BrowsingTypeNames.ToArray());
				else
				{
					EditorGUILayout.LabelField(BrowsingTypeNames[SelectedType]);
					var type = BrowsingTypes[SelectedType - 1];
					if (SelectedReflectedType == 0) SelectedReflectedType = EditorGUILayout.Popup(SelectedReflectedType, BrowsingReflectedTypeNames.ToArray());
					else
					{
						EditorGUILayout.LabelField(BrowsingReflectedTypeNames[SelectedReflectedType]);
						var reflectedType = SupportedReflectedTypes[SelectedReflectedType - 1];
						if (Infos == null)
						{
							if (reflectedType == typeof(FieldInfo))
							{
								Infos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).OrderBy(info => info.Name).ToArray();
								InfoNames.Add("Select field...");
								foreach (FieldInfo info in Infos) InfoNames.Add(info.Name);
							}
							else if (reflectedType == typeof(MethodInfo))
							{
								Infos = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy).OrderBy(info => info.Name).ToArray();
								InfoNames.Add("Select method...");
								foreach (MethodInfo info in Infos) InfoNames.Add(info.Name);
							}
						}
						if (SelectedInfo == 0) SelectedInfo = EditorGUILayout.Popup(SelectedInfo, InfoNames.ToArray());
						else
						{
							completable = true;
							if (reflectedType == typeof(FieldInfo))
							{
								var field = Infos[SelectedInfo - 1] as FieldInfo;
								EditorGUILayout.LabelField(field.Name);
							}
							if (reflectedType == typeof(MethodInfo))
							{
								var method = Infos[SelectedInfo - 1] as MethodInfo;
								EditorGUILayout.LabelField(method.Name);
							}
						}
					}
				}
			}
			var wasEnabled = GUI.enabled;
			GUI.enabled = completable;
			if (GUILayout.Button("Complete")) CompleteEntries();
			GUI.enabled = wasEnabled;
		}

		void CompleteEntries()
		{
			Settings.Entries.Add(new Entry(Infos[SelectedInfo - 1]));
			Settings.EntriesShown.Add(true);
			Settings.EntriesEnabled.Add(false);
			Settings.EntryRunValues.Add(null);
			Settings.EntryDefaultValues.Add(null);
			ResetEditor();
		}

		void ResetEditor()
		{
			EditingEntry = false;
			BrowsingAssemblies = AppDomain.CurrentDomain.GetAssemblies().OrderBy(assembly => assembly.GetName().Name).ToArray();
			BrowsingAssemblyNames = new List<string>();
			BrowsingAssemblyNames.Add("Select assembly...");
			foreach (var assembly in BrowsingAssemblies) BrowsingAssemblyNames.Add(assembly.GetName().Name);
			SelectedAssembly = 0;
			BrowsingTypes = null;
			BrowsingTypeNames = new List<string>();
			BrowsingTypeNames.Add("Select type...");
			SelectedType = 0;
			BrowsingReflectedTypeNames = new List<string>();
			BrowsingReflectedTypeNames.Add("Select field or property type...");
			foreach (var type in SupportedReflectedTypes) BrowsingReflectedTypeNames.Add(type.FullName);
			SelectedReflectedType = 0;
			Infos = null;
			InfoNames = new List<string>();
			SelectedInfo = 0;
		}

		void ResetEntries(List<Entry> replacement = null)
		{
			Settings.Entries = new List<Entry>();
			Settings.EntriesShown = new List<bool>();
			Settings.EntriesEnabled = new List<bool>();
			Settings.EntryRunValues = new List<string>();
			Settings.EntryDefaultValues = new List<string>();

			if (replacement != null)
			{
				Settings.Entries = replacement;
#pragma warning disable 168
				foreach (var entry in Settings.Entries)
#pragma warning restore 168
				{
					Settings.EntriesShown.Add(false);
					Settings.EntriesEnabled.Add(false);
					Settings.EntryRunValues.Add(string.Empty);
					Settings.EntryDefaultValues.Add(string.Empty);
				}
			}
		}

		void OnDestroy()
		{
			SaveSettings ();
			EditorApplication.update -= InterloperUpdate;
		}
		/*
		static IEnumerable<MethodInfo> GetAllMethods(Type type)
		{
			if (type == null) return new List<MethodInfo>();
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			var methods = type.GetMethods(flags).ToList();

			return methods.Concat(GetAllMethods(type.BaseType));
		}
		*/

		static string GetTypeFullName(Type type)
		{
			if (type.IsGenericType) 
			{
				var name = type.FullName.Split(',')[0];
				return name.Substring(1 + name.LastIndexOfAny(new char[] {'['}));
			}
			else return type.FullName;
		}

		void OnLostFocus()
		{
			SaveSettings();
		}

		void SaveSettings()
		{
			EditorPrefs.SetString(SettingsKey, JsonConvert.SerializeObject (Settings));
		}
	}
}