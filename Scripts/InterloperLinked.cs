using System;
using System.Reflection;

namespace LunraGames.Interloper
{
	public interface ILinked
	{
		string Name { get; }
		string Description { get; }
	}
	public interface ILinkedMethod : ILinked
	{
		MethodInfo Method { get; }
	}

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
	public class InterloperLinked : Attribute, ILinked, ILinkedMethod
	{
		public enum LinkedTypes
		{
			Method
		}

		public string Name { get; set; }
		public string Description { get; set; }
		public LinkedTypes LinkedType { get; set; }
		public MethodInfo Method { get; set; }

		public InterloperLinked(string name = null, string description = null)
		{
			Name = name;
			Description = description;
		}
	}
}