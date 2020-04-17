using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Barterables;

namespace PreventEscape.Barterables
{
	public class SetPrisonerFreeNewBarterable : SetPrisonerFreeBarterable
	{
		private readonly Hero _prisoner;
		private readonly Hero _ransomPayer;

		public SetPrisonerFreeNewBarterable(Hero prisoner, Hero captor, PartyBase ownerParty, Hero ransomPayer) : base(prisoner, captor, ownerParty, ransomPayer)
		{
			_prisoner = prisoner;
			_ransomPayer = ransomPayer;
		}
		public override int GetUnitValueForFaction(IFaction faction)
		{
			if ((_ransomPayer?.IsFactionLeader ?? false) && (OriginalOwner?.MapFaction == _ransomPayer.MapFaction))
				return 0;
			if (OriginalOwner?.MapFaction == _ransomPayer?.MapFaction)
				return 0;
			var result = HeroEvaluator.Evaluate(_prisoner, OriginalOwner, _ransomPayer, faction);
			return result;
		}
		public override void Apply()
		{
			base.Apply();
			if (_prisoner == null || _ransomPayer == null)
				return;
			if(_ransomPayer != _prisoner)
				// Improve relation of prisoner to ransomPayer
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_prisoner, _ransomPayer, Settings.Instance.RansomRelationImprovement);
			// Improve relation of prisoner's leaders to ransomPayer
			if (_prisoner.Clan?.Leader != null && _prisoner.Clan?.Leader != _prisoner && _prisoner.Clan.Leader != _ransomPayer)
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_prisoner.Clan?.Leader,  _ransomPayer, (int)(Settings.Instance.RansomRelationImprovement/2f));
			if(_prisoner.MapFaction?.Leader != null && _prisoner.MapFaction?.Leader != _prisoner.Clan?.Leader && _prisoner.MapFaction?.Leader != _prisoner && _prisoner.MapFaction.Leader != _ransomPayer)
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_prisoner.MapFaction?.Leader, _ransomPayer, (int)(Settings.Instance.RansomRelationImprovement/2f));
		}
	}
}
