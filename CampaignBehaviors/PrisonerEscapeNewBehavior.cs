using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;

namespace PreventEscape.CampaignBehaviors
{
	public class PrisonerEscapeNewBehavior : CampaignBehaviorBase
	{
		private Action<Hero> _originalPrisonerEscapeHeroTick;
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickHeroEvent?.AddNonSerializedListener(this, PrisonerEscapeHeroDailyTickWrapper);

			if (Campaign.Current != null)
			{
				var originalBehavior = Campaign.Current.GetCampaignBehavior<PrisonerEscapeCampaignBehavior>();
				if(originalBehavior != null)
					_originalPrisonerEscapeHeroTick = originalBehavior.DailyHeroTick;
			}
		}
		public override void SyncData(IDataStore dataStore)
		{
		}
		private void PrisonerEscapeHeroDailyTickWrapper(Hero hero)
		{
			try
			{
				if (hero == null)
					return;
				if (!hero.IsPrisoner || hero.PartyBelongedToAsPrisoner == null || hero == Hero.MainHero)
					return;
				float chance = hero.PartyBelongedToAsPrisoner.MapFaction?.IsBanditFaction ?? true ? Settings.Instance.BaseEscapeChanceFromBandits : Settings.Instance.BaseEscapeChance;
				if (hero.PartyBelongedToAsPrisoner.IsMobile)
					chance *= 6 - (float)Math.Pow(hero.PartyBelongedToAsPrisoner.NumberOfHealthyMembers, 0.25f);
				if (hero.PartyBelongedToAsPrisoner.MapFaction == Hero.MainHero?.MapFaction)
					chance *= Settings.Instance.EscapeFromPlayerModifier;
				if (hero.MapFaction?.IsAtWarWith(hero.PartyBelongedToAsPrisoner.MapFaction) ?? false)
					chance *= Settings.Instance.EscapeAtWarModifier;
				if (MBRandom.RandomFloat >= chance / 100f)
					return;
				EndCaptivityAction.ApplyByEscape(hero);
			}
			catch
			{
				_originalPrisonerEscapeHeroTick?.Invoke(hero);
			}
		}
	}
}
