﻿using System;

namespace LunraGames.Interloper
{
	public static class Strings
	{
		public const string SettingsKey = "LG_Interloper_Settings";
		public const string FieldName = "System.MonoField";
		public const string MethodName = "System.MonoMethod";

		public static string GetTypeFullName(Type type)
		{
			if (type.IsGenericType) 
			{
				var name = type.FullName.Split(',')[0];
				return name.Substring(1 + name.LastIndexOfAny(new char[] {'['}));
			}
			else return type.FullName;
		}
	}
}