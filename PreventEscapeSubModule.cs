using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using PreventEscape.Actions;
using PreventEscape.Barterables;
using PreventEscape.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Barterables;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.BarterBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
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
			var log = new Logger("PreventEscape");
			log.Print("PreventEscapeTest");
			//Logger.FinishAndCloseAll();
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
						if (!double.TryParse(mainNode["RequirePrisonerFreeChance"]?.InnerText ?? "", out RequirePrisonerFreeBehavior.RequirementChance))
							RequirePrisonerFreeBehavior.RequirementChance = 0.1;
						if (!double.TryParse(mainNode["RansomStrengthFactor"]?.InnerText ?? "", out HeroEvaluator.StrengthFactor))
							HeroEvaluator.StrengthFactor = 100.0;
						if (!double.TryParse(mainNode["RansomRenownFactor"]?.InnerText ?? "", out HeroEvaluator.RenownFactor))
							HeroEvaluator.RenownFactor = 10.0;
						if (!double.TryParse(mainNode["RansomRecruitmentCostFactor"]?.InnerText ?? "", out HeroEvaluator.RecruitmentCostFactor))
							HeroEvaluator.RecruitmentCostFactor = 10.0;
						if (!double.TryParse(mainNode["RansomAtWarFactor"]?.InnerText ?? "", out HeroEvaluator.AtWarFactor))
							HeroEvaluator.AtWarFactor = 2.0;
						if (!double.TryParse(mainNode["RansomOtherFactor"]?.InnerText ?? "", out HeroEvaluator.OtherFactor))
							HeroEvaluator.OtherFactor = 1.0;
						if (!double.TryParse(mainNode["RansomFactionLeaderFactor"]?.InnerText ?? "", out HeroEvaluator.FactionLeaderFactor))
							HeroEvaluator.FactionLeaderFactor = 2.0;
						if (!double.TryParse(mainNode["RansomKingdomLeaderFactor "]?.InnerText ?? "", out HeroEvaluator.KingdomLeaderFactor))
							HeroEvaluator.KingdomLeaderFactor = 4.0;
						if (!double.TryParse(mainNode["RansomRelationFactor"]?.InnerText ?? "", out HeroEvaluator.RelationsFactor))
							HeroEvaluator.RelationsFactor = 2.0;
						if (!int.TryParse(mainNode["RansomRelationImprovement"]?.InnerText ?? "", out SetPrisonerFreeNewBarterable.RansomRelationImprovement))
							SetPrisonerFreeNewBarterable.RansomRelationImprovement = 10;
						if (!int.TryParse(mainNode["RansomLeaderRelationImprovement"]?.InnerText ?? "", out SetPrisonerFreeNewBarterable.RansomLeaderRelationImprovement))
							SetPrisonerFreeNewBarterable.RansomLeaderRelationImprovement= 10;
						int ivalue;
						if (!int.TryParse(mainNode["PrisonerBarterConversationContextId"]?.InnerText ?? "", out ivalue) ||
						    RequirePrisonerFreeBehavior.PrisonerBarterConversationContext < 0)
							RequirePrisonerFreeBehavior.PrisonerBarterConversationContext = (ConversationContext)6;
						else
							RequirePrisonerFreeBehavior.PrisonerBarterConversationContext = (ConversationContext)ivalue;
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

		public override void OnGameLoaded(Game game, object initializerObject)
		{
			base.OnGameLoaded(game, initializerObject);
			var campaignGameStarter = initializerObject as CampaignGameStarter;
			if(campaignGameStarter != null)
			{ 
				campaignGameStarter.AddBehavior(new PrisonerBarterBehavior());
				campaignGameStarter.AddBehavior(new RequirePrisonerFreeBehavior());
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
			return base.DoLoading(game);
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
