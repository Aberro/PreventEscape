using System.Collections.Generic;
using Helpers;
using PreventEscape.Actions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace PreventEscape.Barterables
{
	public class ExtraditePrisonerBarterable : Barterable
	{
		[SaveableField(900)]
		private readonly Hero _prisonerCharacter;
		[SaveableField(901)]
		private readonly Hero _ransompayer;

		public ExtraditePrisonerBarterable(
			Hero prisonerCharacter,
			Hero captor,
			PartyBase ownerParty,
			Hero ransompayer)
			: base(captor, ownerParty)
		{
			this._prisonerCharacter = prisonerCharacter;
			this._ransompayer = ransompayer;
		}
		public override int GetUnitValueForFaction(IFaction faction)
		{
			if ((_ransompayer?.IsFactionLeader ?? false) && (OriginalOwner?.MapFaction == _ransompayer.MapFaction))
				return 0;
			if (OriginalOwner?.MapFaction == _ransompayer?.MapFaction)
				return 0;
			return HeroEvaluator.Evaluate(_prisonerCharacter, OriginalOwner, _ransompayer, faction);
		}
		public override string GetEncyclopediaLink()
		{
			return this._prisonerCharacter?.EncyclopediaLink;
		}
		public override ImageIdentifier GetVisualIdentifier()
		{
			return _prisonerCharacter != null ? new ImageIdentifier(CharacterCode.CreateFrom(_prisonerCharacter.CharacterObject)) : null;
		}
		public override void Apply()
		{
			ExtraditePrisonerAction.ApplyExchange(_prisonerCharacter, OriginalOwner, OriginalParty, _ransompayer, _ransompayer?.PartyBelongedTo?.Party);
		}
		public override string StringID => "extradite_prisoner_barterable";
		public override TextObject Name
		{
			get
			{
				StringHelpers.SetCharacterProperties("PRISONER", this._prisonerCharacter?.CharacterObject);
				return new TextObject("Extradite {PRISONER.NAME}");
			}
		}
	}
}
