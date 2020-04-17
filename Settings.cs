using System;
using System.Configuration;
using JetBrains.Annotations;
using MBOptionScreen.Attributes;
using MBOptionScreen.Settings;
using TaleWorlds.CampaignSystem;

namespace PreventEscape
{
	public class Settings : AttributeSettings<Settings>
	{
		[NotNull]
		public new static Settings Instance => AttributeSettings<Settings>.Instance ?? throw new InvalidOperationException("Singleton pattern is broken.");
		public override string Id { get; set; } = "PreventEscape_v1.2.6";
		public override string ModuleFolderName { get; } = "PreventEscape";
		public override string ModName { get; } = "Prevent Escape";
		[SettingPropertyGroup("Escape")]
		[SettingProperty("Base chance, %/day", 0.0f, 1.0f, false, "Base chance that a prisoner will escape from noble")]
		public float BaseEscapeChance { get; [UsedImplicitly]set; } = 0.01f;
		[SettingPropertyGroup("Escape")]
		[SettingProperty("Base chance (bandits), %/day", 0.0f, 1.0f, false, "Base chance that a prisoner will escape from bandits")]
		public float BaseEscapeChanceFromBandits { get; [UsedImplicitly]set; } = 0.1f;
		[SettingPropertyGroup("Escape")]
		[SettingProperty("Player factor", 0.0f, 4.0f, false, "Base chance is increased by this factor if a captor is a player")]
		public float EscapeFromPlayerModifier { get; [UsedImplicitly]set; } = 0.33f;
		[SettingPropertyGroup("Escape")]
		[SettingProperty("[At War] factor", 0.0f, 1.0f, false, "Base chance is increased by this factor if a prisoner and a captor factions are at war")]
		public float EscapeAtWarModifier { get; [UsedImplicitly]set; } = 0.1f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("Ransom chance, %/day", 0.0f, 1.0f, false, "Base chance that a captor will be asked for a ransom")]
		public float RansomChance { get; [UsedImplicitly]set; } = 0.1f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("Strength factor, gold", 0.0f, 300.0f, false, "Prisoner clan strength is multiplied by this value and the result is added to ransom price")]
		public float StrengthFactor { get; [UsedImplicitly]set; } = 50.0f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("Renown factor, gold", 0.0f, 100.0f, false, "Prisoner clan renown is multiplied by this value and the result is added to ransom price")]
		public float RenownFactor { get; [UsedImplicitly]set; } = 10.0f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("Recruitment cost factor, gold", 0.0f, 100.0f, false, "Prisoner recruitment cost (depends on level) is multiplied by this value and the result is added to ransom price")]
		public float RecruitmentCostFactor { get; [UsedImplicitly]set; } = 10.0f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("[At War] factor", 0.0f, 500.0f, false, "Prisoner ransom price is multiplied by this value if a prisoner and a captor factions is at war")]
		public float AtWarFactor { get; [UsedImplicitly]set; } = 2.0f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("Ransom factor", 0.0f, 20.0f, false, "If prisoner is a kingdom leader, ransom price is multiplied by this value")]
		public float KingdomLeaderFactor { get; [UsedImplicitly]set; } = 4.0f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("King ransom factor", 0.0f, 20.0f, false, "If prisoner is a clan leader, ransom price is multiplied by this value")]
		public float FactionLeaderFactor { get; [UsedImplicitly]set; } = 2.0f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("Ransom factor", 0.0f, 20.0f, false, "If prisoner is not a clan leader nor a kingdom leader, ransom price is multiplied by this value")]
		public float OtherFactor { get; [UsedImplicitly]set; } = 1.0f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("Relations factor", 0.0f, 0.99f, false, "How much relation of ransom payer affects maximum ransom price")]
		public float RelationsFactor { get; [UsedImplicitly]set; } = 0.8f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("Bandits factor", 0.0f, 4f, false, "If captors is bandits, ransom price is multiplied by this value")]
		public float BanditsFactor { get; [UsedImplicitly]set; } = 0.1f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("Price change rate, days", 0.0f, 100f, false, "Captors will ask half as much as [value] days before, and ransomers will agree to pay twice as much.")]
		public float PriceHalfLife { get; [UsedImplicitly]set; } = 10f;
		[SettingPropertyGroup("Ransom")]
		[SettingProperty("Price agreement delay, days", 0.0f, 100f, false, "How much days on average should pass before ransomer and captor would be able to agree on ransom (this is affected by relations factor too)")]
		public float PriceAgreementDelay { get; [UsedImplicitly]set; } = 5f;
		[SettingPropertyGroup("Relations")]
		[SettingProperty("Relations improvement", 0.0f, 100f, false, "Increase in relations of prisoner to it's ransomer")]
		public int RansomRelationImprovement { get; [UsedImplicitly]set; } = 10;
		[SettingPropertyGroup("Relations")]
		[SettingProperty("Relations improvement", 0.0f, 100f, false, "Decrease in relations of prisoner to it's captor when captor decline offer")]
		public int RansomRejectRelationDeterioration { get; [UsedImplicitly] set; } = 2;
		[SettingPropertyGroup("Relations")]
		[SettingProperty("Relations improvement", 0.0f, 100f, false, "Increase in relations of prisoner to it's captor when captor decline barter offer")]
		public int RansomRejectBarterRelationDeterioration { get; [UsedImplicitly] set; } = 1;
		public ConversationContext PrisonerBarterConversationContext = (ConversationContext)6;
	}
}
