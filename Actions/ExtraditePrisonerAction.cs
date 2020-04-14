using System.Collections;
using System.Linq;
using System.Reflection;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace PreventEscape.Actions
{
	public static class ExtraditePrisonerAction
	{
		private delegate void OnPrisonerTakenDelegate(PartyBase capturer, Hero prisoner);
		private delegate void OnPrisonerReleasedDelegate(Hero prisoner, IFaction capturerFaction, EndCaptivityDetail detail);
		private static OnPrisonerTakenDelegate OnPrisonerTaken;
		private static OnPrisonerReleasedDelegate OnPrisonerReleased;
		private static StatisticsDataIdentifier ActionIdentifier;

		static void Initialize()
		{
			OnPrisonerTaken = (OnPrisonerTakenDelegate)typeof(CampaignEventDispatcher).GetMethod("OnPrisonerTaken", BindingFlags.NonPublic | BindingFlags.Instance)?.CreateDelegate(typeof(OnPrisonerTakenDelegate), CampaignEventDispatcher.Instance);
			OnPrisonerReleased = (OnPrisonerReleasedDelegate)typeof(CampaignEventDispatcher).GetMethod("OnPrisonerReleased", BindingFlags.NonPublic | BindingFlags.Instance)?.CreateDelegate(typeof(OnPrisonerReleasedDelegate), CampaignEventDispatcher.Instance);
			var rootFieldInfo = typeof(StatisticsDataLogger).GetField("_rootData", BindingFlags.NonPublic | BindingFlags.Static);
			var root = rootFieldInfo?.GetValue(null);
			var statDataType = root?.GetType();
			var childrenFieldInfo = statDataType?.GetField("Children");
			var identifierFieldInfo = statDataType?.GetField("Identifier");
			var campaign = (childrenFieldInfo?.GetValue(root) as IEnumerable)?.Cast<object>().FirstOrDefault();
			var categories = childrenFieldInfo?.GetValue(campaign) as IEnumerable;
			StatisticsDataIdentifier parent = null;
			if (categories != null)
			{
				foreach (var children in categories)
				{
					var identifier = identifierFieldInfo?.GetValue(children) as StatisticsDataIdentifier;
					if (identifier?.Description == "trade")
					{
						parent = identifier;
						break;
					}
				}

				if (parent != null)
				{
					ActionIdentifier = new StatisticsDataIdentifier("ExtraditePrisonerAction");
					StatisticsDataLogger.RegisterDataIdentifier(ActionIdentifier, parent);
				}
			}
		}
		public static void ApplyExchange(Hero prisoner, Hero originalCaptor, PartyBase captorParty, Hero buyer, PartyBase buyerParty)
		{
			if(prisoner == null || originalCaptor == null || buyer == null)
				return;
			if (ActionIdentifier == null)
				Initialize();
			if(ActionIdentifier != null)
				StatisticsDataLogger.AddStat(ActionIdentifier, 1, $"{originalCaptor.Name} extradite {prisoner.Name} to {buyer.Name}");
			PartyBase belongedToAsPrisoner = prisoner.PartyBelongedToAsPrisoner;
			IFaction capturerFaction = belongedToAsPrisoner?.MapFaction ?? CampaignData.NeutralFaction;
			if (prisoner == Hero.MainHero)
			{
				StringHelpers.SetCharacterProperties("EXTRADICTOR", originalCaptor.CharacterObject);
				StringHelpers.SetCharacterProperties("FACILITATOR", buyer.CharacterObject);
				InformationManager.AddQuickInformation(new TextObject("{EXTRADICTOR.NAME} extradite you to {FACILITATOR.NAME}."));
			}
			else
			{
				if (buyer == Hero.MainHero)
				{
					StringHelpers.SetCharacterProperties("EXTRADICTOR", originalCaptor.CharacterObject);
					StringHelpers.SetCharacterProperties("PRISONER", prisoner.CharacterObject);
					InformationManager.AddQuickInformation(new TextObject("{EXTRADICTOR.NAME} extradite {PRISONER.NAME} to you."));
				}
			}
			if (belongedToAsPrisoner != null && belongedToAsPrisoner.PrisonRoster != null && belongedToAsPrisoner.PrisonRoster.Contains(prisoner.CharacterObject))
				belongedToAsPrisoner.PrisonRoster.RemoveTroop(prisoner.CharacterObject);
			OnPrisonerReleased?.Invoke(prisoner, capturerFaction, EndCaptivityDetail.Ransom);
			OnPrisonerTaken?.Invoke(buyerParty, prisoner);
			if (buyerParty != null && buyerParty.PrisonRoster != null)
				buyerParty.AddPrisoner(prisoner.CharacterObject, 1, 0);
			if (prisoner == Hero.MainHero)
				PlayerCaptivity.StartCaptivity(buyerParty);
		}
	}
}
