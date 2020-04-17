using HarmonyLib;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;

namespace PreventEscape.HarmonyPatches
{
	[UsedImplicitly]
	[HarmonyPatch]
	class MakePeaceActionPatch
	{
		[UsedImplicitly]
		[HarmonyPatch(typeof(TaleWorlds.CampaignSystem.Actions.MakePeaceAction), "ReleasePrisoners", MethodType.Normal)]
		static bool Prefix(IFaction faction1, IFaction faction2)
		{
			// Just disable prisoners release
			return false;
		}
		
	}
}
