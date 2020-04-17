using System;
using System.Reflection;
using PreventEscape.Barterables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Barterables;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace PreventEscape.CampaignBehaviors
{
	class SetPrisonerFreeNewBarterBehavior : CampaignBehaviorBase
	{
		public override void RegisterEvents()
		{
			if(CampaignEvents.BarterablesRequested != null)
				CampaignEvents.BarterablesRequested.AddNonSerializedListener(this, CheckForBarters);
		}

		public override void SyncData(IDataStore dataStore)
		{
		}

		public void CheckForBarters(BarterData args)
		{
			if (args == null)
				return;
			PartyBase offererParty = args.OffererParty;
			PartyBase otherParty = args.OtherParty;
			if (offererParty == null || otherParty == null)
				return;
			// Remove prisoners from barterable if current barter is about lord defection.
			bool isDefectionBarter = false;
			foreach (var barterable in args.GetOfferedBarterables())
			{
				if (barterable is JoinKingdomAsClanBarterable)
				{
					isDefectionBarter = true;
					break;
				}
			}
			if(isDefectionBarter || args.ContextInitializer?.Method.Name == nameof(BarterManager.InitializeJoinFactionBarterContext))
				return;
			var offererPrisoners = offererParty.PrisonerHeroes();
			if(offererPrisoners != null)
				foreach (var prisonerHero in offererPrisoners)
				{
					if (prisonerHero.IsHero && !FactionManager.IsAtWarAgainstFaction(prisonerHero.HeroObject?.MapFaction, offererParty.MapFaction))
					{
						Barterable barterable = new SetPrisonerFreeNewBarterable(prisonerHero.HeroObject, args.OffererHero, args.OffererParty, args.OtherHero);
						args.AddBarterable<PrisonerBarterGroup>(barterable);
					}
				}

			var otherPrisoners = otherParty.PrisonerHeroes();
			if(otherPrisoners != null)
				foreach (var prisonerHero in otherPrisoners)
				{
					if (prisonerHero.IsHero && !FactionManager.IsAtWarAgainstFaction(prisonerHero.HeroObject?.MapFaction, offererParty.MapFaction))
					{
						Barterable barterable = new SetPrisonerFreeNewBarterable(prisonerHero.HeroObject, args.OtherHero, args.OtherParty, args.OffererHero);
						args.AddBarterable<PrisonerBarterGroup>(barterable);
					}
				}
		}
	}
}
