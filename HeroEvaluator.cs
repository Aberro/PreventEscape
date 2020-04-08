using System;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace PreventEscape
{
	public static class HeroEvaluator
	{
		public static double StrengthFactor = 100.0;
		public static double RenownFactor = 10.0;
		public static double RecruitmentCostFactor = 10.0;
		public static double AtWarFactor = 2.0;
		public static double OtherFactor = 1.0;
		public static double FactionLeaderFactor = 2.0;
		public static double KingdomLeaderFactor = 4.0;
		public static double RelationsFactor = 2.0;
		public static int Evaluate(Hero prisoner, Hero captor, Hero ransomer, IFaction evaluatorFaction)
		{
			if (prisoner == null || ransomer == null || evaluatorFaction == null)
				return 0;
			// For his faction, it's leader worth everything.
			if (evaluatorFaction.Leader == prisoner)
				return evaluatorFaction.MapFaction?.Leader?.Gold ?? 0;
			// Same for kings.
			if (prisoner.IsFactionLeader && (prisoner.MapFaction?.IsKingdomFaction ?? false) &&
			    ((prisoner.MapFaction as Kingdom)?.Clans?.Contains(evaluatorFaction) ?? false))
				return evaluatorFaction.MapFaction?.Leader?.Gold ?? 0;

			var minimumValue = prisoner.Gold;
			var estimatedValue = (prisoner.Clan?.TotalStrength * StrengthFactor ?? 0)
			                     + (prisoner.Clan?.Renown * RenownFactor ?? 0)
			                     + (Campaign.Current?.Models?.PartyWageModel?.GetTroopRecruitmentCost(prisoner.CharacterObject, null) * RecruitmentCostFactor ?? 0);
			estimatedValue *= (1.0 + (ransomer.GetRelation(prisoner)/100.0) * RelationsFactor);
			if (prisoner.IsFactionLeader)
				if (prisoner.MapFaction?.IsKingdomFaction ?? false)
					estimatedValue *= KingdomLeaderFactor;
				else
					estimatedValue *= FactionLeaderFactor;
			else
				estimatedValue *= OtherFactor;
			estimatedValue = (int)Math.Max(minimumValue, estimatedValue);
			// If prisoner is enemy of evaluator's kingdom
			if (FactionManager.IsAtWarAgainstFaction(prisoner.MapFaction, evaluatorFaction) ||
			    FactionManager.IsAtWarAgainstFaction(prisoner.Clan?.MapFaction, captor?.Clan?.MapFaction))
				estimatedValue *= AtWarFactor;
			if(captor.MapFaction == evaluatorFaction || captor.Clan == evaluatorFaction)
				return  -(int)estimatedValue;
			return (int)estimatedValue;
		}
	}
}
