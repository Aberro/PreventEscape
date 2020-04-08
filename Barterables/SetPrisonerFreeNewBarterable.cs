using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Barterables;

namespace PreventEscape.Barterables
{
	public class SetPrisonerFreeNewBarterable : SetPrisonerFreeBarterable
	{
		public static int RansomRelationImprovement;
		public static int RansomLeaderRelationImprovement;
		private readonly Hero _prisonerCharacter;
		private readonly Hero _ransompayer;

		public SetPrisonerFreeNewBarterable(Hero prisonerCharacter, Hero captor, PartyBase ownerParty, Hero ransompayer) : base(prisonerCharacter, captor, ownerParty, ransompayer)
		{
			_prisonerCharacter = prisonerCharacter;
			_ransompayer = ransompayer;
		}
		public override int GetUnitValueForFaction(IFaction faction)
		{
			return HeroEvaluator.Evaluate(_prisonerCharacter, OriginalOwner, _ransompayer, faction);
		}
		public override void Apply()
		{
			base.Apply();
			if (_prisonerCharacter == null || _ransompayer == null)
				return;
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_ransompayer, _prisonerCharacter, RansomRelationImprovement);
			if (!_prisonerCharacter.IsFactionLeader)
			{
				if (_prisonerCharacter.Clan?.Leader != null && _prisonerCharacter.Clan?.Leader != _prisonerCharacter)
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_ransompayer, _prisonerCharacter.Clan?.Leader, RansomLeaderRelationImprovement);
				if(_prisonerCharacter.MapFaction?.Leader != null && _prisonerCharacter.MapFaction?.Leader != _prisonerCharacter.Clan?.Leader && _prisonerCharacter.MapFaction?.Leader != _prisonerCharacter)
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_ransompayer, _prisonerCharacter.MapFaction?.Leader, RansomLeaderRelationImprovement);
			}
		}
	}
}
