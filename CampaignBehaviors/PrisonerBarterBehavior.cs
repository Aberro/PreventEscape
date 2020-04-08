using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PreventEscape.Barterables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace PreventEscape.CampaignBehaviors
{
	public class PrisonerBarterBehavior : CampaignBehaviorBase
	{
		public override void RegisterEvents()
		{
			CampaignEvents.BarterablesRequested?.AddNonSerializedListener(this, CheckForBarters);
		}

		public override void SyncData(IDataStore dataStore)
		{
		}

		public void CheckForBarters([NotNull]BarterData args)
		{
			PartyBase offererParty = args.OffererParty;
			PartyBase otherParty = args.OtherParty;
			if (offererParty == null || otherParty == null)
				return;
			var offererPrisoners = new List<Hero>();
			var otherPrisoners = new List<Hero>();
			var offererLeader = offererParty.LeaderHero;
			var otherLeader = otherParty.LeaderHero;
			var offererClan = offererLeader?.Clan;
			var otherClan = otherLeader?.Clan;
			if (offererLeader == null || otherLeader == null)
				return;

			// Find prisoners in offerer party:
			offererPrisoners.AddRange((offererParty.PrisonerHeroes() ?? Enumerable.Empty<CharacterObject>())
				.Where(prisoner => prisoner != null && prisoner.IsHero && prisoner.HeroObject != null).Select(prisoner => prisoner.HeroObject));
			// Find prisoners in offerer settlements:
			// Only clan leaders are able to offer bailing from settlements.
			if (offererClan?.Leader != null && offererClan.Leader == offererLeader)
			{
				if(offererClan.Settlements != null)
					offererPrisoners.AddRange(
						offererClan.Settlements
							.Where(settlement => settlement != null)
							.SelectMany(
								settlement => (settlement.HeroesWithoutParty ?? Enumerable.Empty<Hero>()).Where(hero => hero.IsPrisoner)
									.Union(settlement.Party?.PrisonerHeroes()?.Where(character => character != null && character.IsHero)
										.Select(character => character.HeroObject) ?? Enumerable.Empty<Hero>()))
							.Where(hero => hero != null && hero.IsPrisoner));
			}
			// If offerer is the faction leader, find prisoners in each faction clan
			if (offererLeader.IsFactionLeader && offererParty.MapFaction is Kingdom offererKingdom && offererKingdom.Settlements != null)
			{
				offererPrisoners.AddRange(offererKingdom.Settlements
					.Where(settlement => settlement != null && settlement.OwnerClan != offererClan)
					.SelectMany(
						settlement => (settlement.HeroesWithoutParty ?? Enumerable.Empty<Hero>()).Where(hero => hero.IsPrisoner)
							.Union(settlement.Party?.PrisonerHeroes()?.Where(character => character != null && character.IsHero)
								.Select(character => character.HeroObject) ?? Enumerable.Empty<Hero>())));
			}

			// Find prisoners in other party:
			otherPrisoners.AddRange((otherParty.PrisonerHeroes() ?? Enumerable.Empty<CharacterObject>())
				.Where(prisoner => prisoner != null && prisoner.IsHero && prisoner.HeroObject != null).Select(prisoner => prisoner.HeroObject));

			//Find prisoners in other leader settlements
			// Only clan leaders are able to offer bailing from settlements.
			if (otherClan?.Leader != null && otherClan.Leader == otherLeader)
			{
				if(otherClan.Settlements != null)
					otherPrisoners.AddRange(
						otherClan.Settlements
							.Where(settlement => settlement != null)
							.SelectMany(
								settlement => (settlement.HeroesWithoutParty ?? Enumerable.Empty<Hero>()).Where(hero => hero.IsPrisoner)
									.Union(settlement.Party?.PrisonerHeroes()?.Where(character => character?.IsHero ?? false)
										.Select(character => character.HeroObject) ?? Enumerable.Empty<Hero>()))
							.Where(hero => hero != null && hero.IsPrisoner));
			}
			// If other hero is the faction leader, find prisoners in each faction clan
			if (otherLeader.IsFactionLeader && otherParty.MapFaction is Kingdom otherKingdom && otherKingdom.Settlements != null)
			{
				otherPrisoners.AddRange(otherKingdom.Settlements
					.Where(settlement => settlement != null && settlement.OwnerClan != otherClan)
					.SelectMany(
						settlement => (settlement.HeroesWithoutParty ?? Enumerable.Empty<Hero>()).Where(hero => hero.IsPrisoner)
							.Union(settlement.Party?.PrisonerHeroes()?.Where(character => character != null && character.IsHero)
								       .Select(character => character.HeroObject) ?? Enumerable.Empty<Hero>())));
			}

			foreach (var prisoner in offererPrisoners)
			{
				if (prisoner.MapFaction == otherParty.MapFaction || otherParty == PartyBase.MainParty)
				{
					var barterable = new SetPrisonerFreeNewBarterable(prisoner, offererLeader, offererParty, otherLeader);
					args.AddBarterable<PrisonerBarterGroup>(barterable);
				}
				else
				{
					new object();
				}

				if (FactionManager.IsAtWarAgainstFaction(prisoner.MapFaction, otherParty.MapFaction))
				{
					var barterable = new ExtraditePrisonerBarterable(prisoner, offererLeader, offererParty, otherLeader);
					args.AddBarterable<PrisonerBarterGroup>(barterable);
				}
			}

			foreach (var prisoner in otherPrisoners)
			{
				if (prisoner.MapFaction == offererParty.MapFaction || offererParty == PartyBase.MainParty)
				{
					var barterable = new SetPrisonerFreeNewBarterable(prisoner, otherLeader, otherParty, offererLeader);
					args.AddBarterable<PrisonerBarterGroup>(barterable);
				}
				else
				{
					new object();
				}
				if (FactionManager.IsAtWarAgainstFaction(prisoner.MapFaction, otherParty.MapFaction))
				{
					var barterable = new ExtraditePrisonerBarterable(prisoner, otherLeader, otherParty, offererLeader);
					args.AddBarterable<PrisonerBarterGroup>(barterable);
				}
			}
		}
	}
}
