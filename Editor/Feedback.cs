using UnityEditor;

namespace LunraGames.Interloper
{
	public static class Feedback
	{
		[MenuItem(LunraGames.Strings.Feedback+Strings.Plugin)]
		static void LaunchFeedback()
		{
			PlugIt.Helper.LaunchFeedback(LunraGames.Strings.Company, Strings.Plugin);
		}
	}
}