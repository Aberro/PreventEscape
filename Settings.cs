using System.IO;
using System.Xml;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace PreventEscape
{
	public class Settings
	{
		[NotNull]
		public static Settings Instance { get; } = new Settings();
		public string Id { get; } = "PreventEscape_v1.2.6";
		public string ModuleFolderName { get; } = "PreventEscape";
		public string ModName { get; } = "Prevent Escape";
		public float BaseEscapeChance = 0.01f;
		public float BaseEscapeChanceFromBandits = 0.1f;
		public float EscapeFromPlayerModifier = 0.33f;
		public float EscapeAtWarModifier = 0.1f;
		public float RansomChance = 0.1f;
		public float StrengthFactor = 50.0f;
		public float RenownFactor = 10.0f;
		public float RecruitmentCostFactor = 10.0f;
		public float AtWarFactor = 2.0f;
		public float KingdomLeaderFactor = 4.0f;
		public float FactionLeaderFactor = 2.0f;
		public float OtherFactor = 1.0f;
		public float RelationsFactor = 0.8f;
		public float BanditsFactor = 0.1f;
		public float PriceHalfLife = 10f;
		public float PriceAgreementDelay = 5f;
		public int RansomRelationImprovement = 10;
		public int RansomRejectRelationDeterioration = 2;
		public int RansomRejectBarterRelationDeterioration = 1;
		public int CaptivityDaysLimit = CampaignTime.DaysInYear;
		public ConversationContext PrisonerBarterConversationContext = (ConversationContext)6;

		public Settings()
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
						if (!float.TryParse(mainNode["BaseEscapeChance"]?.InnerText ?? "", out BaseEscapeChance))
							BaseEscapeChance = 0.01f;
						if (!float.TryParse(mainNode["BaseEscapeChanceFromBandits"]?.InnerText ?? "", out BaseEscapeChanceFromBandits))
							BaseEscapeChanceFromBandits = 0.1f;
						if (!float.TryParse(mainNode["EscapeFromPlayerModifier"]?.InnerText ?? "", out EscapeFromPlayerModifier))
							EscapeFromPlayerModifier = 0.33f;
						if (!float.TryParse(mainNode["EscapeAtWarModifier"]?.InnerText ?? "", out EscapeAtWarModifier))
							EscapeAtWarModifier = 0.1f;
						if (!float.TryParse(mainNode["RansomChance"]?.InnerText ?? "", out RansomChance))
							RansomChance = 0.1f;
						if (!float.TryParse(mainNode["StrengthFactor"]?.InnerText ?? "", out StrengthFactor))
							StrengthFactor = 50.0f;
						if (!float.TryParse(mainNode["RenownFactor"]?.InnerText ?? "", out RenownFactor))
							RenownFactor = 10.0f;
						if (!float.TryParse(mainNode["RecruitmentCostFactor"]?.InnerText ?? "", out RecruitmentCostFactor))
							RecruitmentCostFactor = 10.0f;
						if (!float.TryParse(mainNode["AtWarFactor"]?.InnerText ?? "", out AtWarFactor))
							AtWarFactor = 2.0f;
						if (!float.TryParse(mainNode["OtherFactor"]?.InnerText ?? "", out OtherFactor))
							OtherFactor = 1.0f;
						if (!float.TryParse(mainNode["FactionLeaderFactor"]?.InnerText ?? "", out FactionLeaderFactor))
							FactionLeaderFactor = 2.0f;
						if (!float.TryParse(mainNode["KingdomLeaderFactor "]?.InnerText ?? "", out KingdomLeaderFactor))
							KingdomLeaderFactor = 4.0f;
						if (!float.TryParse(mainNode["RelationsFactor"]?.InnerText ?? "", out RelationsFactor))
							RelationsFactor = 0.8f;
						if (!float.TryParse(mainNode["BanditsFactor"]?.InnerText ?? "", out BanditsFactor))
							BanditsFactor = 0.1f;
						if (!float.TryParse(mainNode["PriceHalfLife"]?.InnerText ?? "", out PriceHalfLife))
							PriceHalfLife = 10.0f;
						if (!float.TryParse(mainNode["PriceAgreementDelay"]?.InnerText ?? "", out PriceAgreementDelay))
							PriceAgreementDelay = 5.0f;
						if (!int.TryParse(mainNode["RansomRelationImprovement"]?.InnerText ?? "", out RansomRelationImprovement))
							RansomRelationImprovement = 10;
						if (!int.TryParse(mainNode["RansomRejectRelationDeterioration"]?.InnerText ?? "", out RansomRejectRelationDeterioration))
							RansomRejectRelationDeterioration = 2;
						if (!int.TryParse(mainNode["RansomRejectBarterRelationDeterioration"]?.InnerText ?? "", out RansomRejectBarterRelationDeterioration))
							RansomRejectBarterRelationDeterioration = 1;
						if (!int.TryParse(mainNode["CaptivityDaysLimit"]?.InnerText ?? "", out CaptivityDaysLimit))
							CaptivityDaysLimit = CampaignTime.DaysInYear;
						if (!int.TryParse(mainNode["PrisonerBarterConversationContextId"]?.InnerText ?? "", out var iValue) ||
							PrisonerBarterConversationContext < 0)
							PrisonerBarterConversationContext = (ConversationContext)6;
						else
							PrisonerBarterConversationContext = (ConversationContext)iValue;
					}
				}
				catch
				{
					// ignored
				}
			}
		}
	}
}
