using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Barterables;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.BarterBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace PreventEscape
{
    public class PreventEscapeSubModule : MBSubModuleBase
    {
	    public static double BaseEscapeChance = 0.01;
	    public static double BaseEscapeChanceFromBandits = 0.1;
	    public static double EscapeFromPlayerModifier = 0.33;
	    public static double AtWarModifier = 0.1;
		protected override void OnSubModuleLoad()
		{
			var path = BasePath.Name + "Modules/PreventEscape/SubModule.xml";
			if (File.Exists(path))
			{
				try
				{
					var doc = new XmlDocument();
					using (var stringReader = new System.IO.StringReader(File.ReadAllText(path)))
					using (var xmlReader = XmlReader.Create(stringReader))
						doc.Load(xmlReader);
					XmlNode mainNode = doc.SelectSingleNode("/Module/Config");
					if (mainNode != null)
					{
						if (!double.TryParse(mainNode["BaseEscapeChance"]?.InnerText ?? "", out BaseEscapeChance))
							BaseEscapeChance = 0.01;
						if (!double.TryParse(mainNode["BaseEscapeChanceFromBandits"]?.InnerText ?? "", out BaseEscapeChanceFromBandits))
							BaseEscapeChanceFromBandits = 0.1;
						if (!double.TryParse(mainNode["EscapeFromPlayerModifier"]?.InnerText ?? "", out EscapeFromPlayerModifier))
							EscapeFromPlayerModifier = 0.33;
						if (!double.TryParse(mainNode["AtWarModifier"]?.InnerText ?? "", out AtWarModifier))
							AtWarModifier = 0.1;
					}
				}
				catch
				{
					BaseEscapeChance = 0.01;
					BaseEscapeChanceFromBandits = 0.1;
					EscapeFromPlayerModifier = 0.33;
					AtWarModifier = 0.1;
				}
			}
		}
		public override bool DoLoading(Game game)
		{
			if (Campaign.Current == null)
				return true;
			var prisonerEscapeBehaviour = Campaign.Current.GetCampaignBehavior<PrisonerEscapeCampaignBehavior>();
			var barterBehaviour = Campaign.Current.GetCampaignBehavior<DiplomaticBartersBehavior>();
			var originalPrisonerEscapeHeroTick = prisonerEscapeBehaviour != null ? new Action<Hero>(prisonerEscapeBehaviour.DailyHeroTick) : null;
			var originalBarterHeroTickMethodInfo = typeof(DiplomaticBartersBehavior).GetMethod("DailyTickHero", BindingFlags.NonPublic | BindingFlags.Instance);
			var originalBarterHeroTick = originalBarterHeroTickMethodInfo != null ? (Action<Hero>)Delegate.CreateDelegate(typeof(Action<Hero>), barterBehaviour, originalBarterHeroTickMethodInfo) : null;
			if (CampaignEvents.DailyTickHeroEvent == null)
				return true;
			CampaignEvents.DailyTickHeroEvent.ClearListeners(prisonerEscapeBehaviour);
			CampaignEvents.DailyTickHeroEvent.ClearListeners(barterBehaviour);
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(prisonerEscapeBehaviour, hero => PrisonerEscapeHeroDailyTickWrapper(hero, originalPrisonerEscapeHeroTick));
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(prisonerEscapeBehaviour, hero => BarterDailyTickHeroWrapper(hero, originalBarterHeroTick));
			return base.DoLoading(game);
		}
		private void BarterDailyTickHeroWrapper(Hero hero, Action<Hero> originalBehavior)
		{
			try
			{
				if (!hero.IsPrisoner || hero.Clan == null || (hero.PartyBelongedToAsPrisoner == null || (double)MBRandom.RandomFloat >= 0.100000001490116))
					return;
				var captorParty = hero.PartyBelongedToAsPrisoner;
				var captorFaction = captorParty.MapFaction;
				var offerer = hero.Clan.Leader;
				var captorLeader = captorFaction.Leader;
				var prisonerClan = hero.Clan;
				SetPrisonerFreeBarterable prisonerFreeBarterable = new SetPrisonerFreeBarterable(hero, captorLeader, captorParty, offerer);
				if (prisonerFreeBarterable.GetValueForFaction(captorFaction) + prisonerFreeBarterable.GetValueForFaction(prisonerClan) <= 0)
					return;
				if (captorFaction.IsBanditFaction)
				{
					BarterData barterData = new BarterData(hero.Clan.Leader, captorFaction.Leader, (PartyBase)null, (PartyBase)null,
						(BarterManager.BarterContextInitializer)null, 0, true);
					barterData.AddBarterable<DefaultsBarterGroup>(prisonerFreeBarterable);
					Campaign.Current.BarterManager.ExecuteAIBarter(barterData, captorFaction, hero.Clan, captorFaction.Leader, hero.Clan.Leader);
				}
			}
			catch (Exception e)
			{
				if (originalBehavior != null)
					originalBehavior(hero);
			}
		}
		private void PrisonerEscapeHeroDailyTickWrapper(Hero hero, Action<Hero> originalBehavior)
		{
			try
			{
				if (!hero.IsPrisoner || hero.PartyBelongedToAsPrisoner == null || hero == Hero.MainHero)
					return;
				double chance = hero.PartyBelongedToAsPrisoner.MapFaction.IsBanditFaction ? BaseEscapeChanceFromBandits : BaseEscapeChance;
				if (hero.PartyBelongedToAsPrisoner.IsMobile)
					chance *= 6 - Math.Pow(hero.PartyBelongedToAsPrisoner.NumberOfHealthyMembers, 0.25);
				if (hero.PartyBelongedToAsPrisoner.MapFaction == Hero.MainHero.MapFaction)
					chance *= EscapeFromPlayerModifier;
				if (hero.MapFaction.IsAtWarWith(hero.PartyBelongedToAsPrisoner.MapFaction))
					chance *= AtWarModifier;
				if (MBRandom.RandomFloat >= chance)
					return;
				EndCaptivityAction.ApplyByEscape(hero, (Hero)null);
			}
			catch (Exception exception)
			{
				if (originalBehavior != null)
					originalBehavior(hero);
			}
		}
	}
}
