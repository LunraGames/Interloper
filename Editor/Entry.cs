using System;
using System.Reflection;

namespace LunraGames.Interloper
{
	[Serializable]
	public class Entry
	{
		public string CachedName;

		public string TypeName;
		public string InfoName;
		public string InfoTypeName;
		public bool FromAttribute;

		public Entry(object info, bool fromAttribute = false)
		{
			FromAttribute = fromAttribute;

			if (info is FieldInfo)
			{
				var field = info as FieldInfo;
				InfoTypeName = Strings.FieldName;
				CachedName = Strings.GetTypeFullName(field.DeclaringType)+"."+field.Name;
				TypeName = field.DeclaringType.AssemblyQualifiedName;
				InfoName = field.Name;
			}
			else if (info is MethodInfo)
			{
				// todo: there's probably a less repetative way to do this...
				var method = info as MethodInfo;
				InfoTypeName = Strings.MethodName;
				CachedName = Strings.GetTypeFullName(method.DeclaringType)+"."+method.Name;
				TypeName = method.DeclaringType.AssemblyQualifiedName;
				InfoName = method.Name;
			}
		}

		public object GetInfo()
		{
			var declaring = Type.GetType(TypeName);
			if (InfoTypeName == Strings.FieldName || InfoTypeName == typeof(FieldInfo).FullName)
			{
				return declaring.GetField(InfoName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			}
			else if (InfoTypeName == Strings.MethodName || InfoTypeName == typeof(MethodInfo).FullName)
			{
				return declaring.GetMethod(InfoName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			}
			return null;
		}
	}
}