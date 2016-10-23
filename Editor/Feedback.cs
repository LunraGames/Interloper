using UnityEditor;
using LunraGames.Interloper;

namespace LunraGamesEditor.Interloper
{
	public static class Feedback
	{
		[MenuItem(Strings.Feedback+Strings.Plugin)]
		static void LaunchFeedback()
		{
			PlugIt.Helper.LaunchFeedback(LunraGamesEditor.Strings.Company, Strings.Plugin);
		}
	}
}