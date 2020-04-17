using HarmonyLib;
using JetBrains.Annotations;
using PreventEscape.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.BarterBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PreventEscape
{
	[UsedImplicitly]
    public class PreventEscapeSubModule : MBSubModuleBase
    {
	    protected override void OnSubModuleLoad()
	    {
		    base.OnSubModuleLoad();
			var harmony = new Harmony("PreventEscape");
		    harmony.PatchAll();
		}
	    public override void OnGameLoaded(Game game, object initializerObject)
		{
			base.OnGameLoaded(game, initializerObject);
			var campaignGameStarter = initializerObject as CampaignGameStarter;
			if(campaignGameStarter != null)
			{ 
				campaignGameStarter.AddBehavior(new PrisonerBarterBehavior());
				campaignGameStarter.AddBehavior(new RequirePrisonerFreeBehavior());
				campaignGameStarter.AddBehavior(new SetPrisonerFreeBarterBehavior());
				campaignGameStarter.AddBehavior(new PrisonerEscapeNewBehavior());
			}
		}
		public override bool DoLoading(Game game)
		{
			if (Campaign.Current == null)
				return true;
			var prisonerEscapeBehaviour = Campaign.Current.GetCampaignBehavior<PrisonerEscapeCampaignBehavior>();
			var barterBehaviour = Campaign.Current.GetCampaignBehavior<DiplomaticBartersBehavior>();
			var setPrisonerFreeBarterBehavior = Campaign.Current.GetCampaignBehavior<SetPrisonerFreeBarterBehavior>();
			CampaignEvents.DailyTickHeroEvent?.ClearListeners(prisonerEscapeBehaviour);
			CampaignEvents.DailyTickHeroEvent?.ClearListeners(barterBehaviour);
			CampaignEvents.BarterablesRequested?.ClearListeners(setPrisonerFreeBarterBehavior);
			return base.DoLoading(game);
		}
	}
}
