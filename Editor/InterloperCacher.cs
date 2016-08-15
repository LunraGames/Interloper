using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LunraGames.Interloper
{
	public static class InterloperCacher
	{
		static List<InterloperLinked> _Entries;
		public static List<InterloperLinked> Entries { get { return  _Entries ?? (_Entries = GetEntries()); } }

		static List<InterloperLinked> GetEntries()
		{
			var entries = new List<InterloperLinked>();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach(var assembly in assemblies)
			{
				var types = assembly.GetTypes();
				foreach(var type in types)
				{
					var unmodifiedType = type;
					var methods = unmodifiedType.GetMethods();
					foreach(var method in methods)
					{
						var unmodifiedMethod = method;
						var attributes = method.GetCustomAttributes(typeof(InterloperLinked), true);
						if(0 < attributes.Length)
						{
							var attribute = attributes[0] as InterloperLinked;
							attribute.LinkedType = InterloperLinked.LinkedTypes.Method;
							attribute.Method = unmodifiedMethod;
							attribute.Name = unmodifiedMethod.Name;
							entries.Add(attribute);
						}
					}
				}
			}
			return entries;
		}
	}
}