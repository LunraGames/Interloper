using System;

namespace LunraGames.Interloper
{
	public class Strings : LunraGames.Strings
	{
		public const string Plugin = "Interloper";
		public const string SettingsKey = "LG_Interloper_Settings";
		public const string FieldName = "System.MonoField";
		public const string MethodName = "System.MonoMethod";

		public static string GetTypeFullName(Type type)
		{
			if (type.IsGenericType)
			{
				var name = type.FullName.Split(',')[0];
				return name.Substring(1 + name.LastIndexOfAny(new char[] { '[' }));
			}
			return type.FullName;
		}
	}
}