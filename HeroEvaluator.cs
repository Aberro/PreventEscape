using System;
using JetBrains.Annotations;
using TaleWorlds.CampaignSystem;

namespace PreventEscape
{
	public static class HeroEvaluator
	{
		public static int Evaluate(Hero prisoner, Hero captor, Hero ransomer, IFaction evaluatorFaction)
		{
			if (prisoner == null || ransomer == null || evaluatorFaction == null || Campaign.Current == null)
				return 0;

			var estimatedValue = (prisoner.Clan?.TotalStrength * Settings.Instance.StrengthFactor ?? 0)
			                     + (prisoner.Clan?.Renown * Settings.Instance.RenownFactor ?? 0)
			                     + (Campaign.Current?.Models?.PartyWageModel?.GetTroopRecruitmentCost(prisoner.CharacterObject, null) * Settings.Instance.RecruitmentCostFactor ?? 0);
			estimatedValue *= (1.0f + (ransomer.GetRelation(prisoner)/100.0f) * Settings.Instance.RelationsFactor);
			if (prisoner.IsFactionLeader)
				if (prisoner.MapFaction?.IsKingdomFaction ?? false)
					estimatedValue *= Settings.Instance.KingdomLeaderFactor;
				else
					estimatedValue *= Settings.Instance.FactionLeaderFactor;
			else
				estimatedValue *= Settings.Instance.OtherFactor;
			var clanGoldChange = Math.Max(0, Campaign.Current.Models?.ClanFinanceModel?.CalculateClanGoldChange(prisoner.Clan, null, true) ?? 0);
			var clanGold = 0;
			if (prisoner.Clan?.Heroes != null)
			{
				if (prisoner.Clan.Leader == prisoner)
					foreach (var hero in prisoner.Clan.Heroes)
						clanGold += hero.Gold;
				else
					clanGold = prisoner.Clan.Leader?.Gold ?? 0;
			}

			// Limit ransom price, so it's not greater than clan could pay.
			var maxRansom = Math.Max(clanGold, clanGoldChange * Settings.Instance.PriceAgreementDelay * 0.8f);
			if (estimatedValue > maxRansom)
				estimatedValue = maxRansom;
			if ((prisoner.PartyBelongedToAsPrisoner?.MapFaction?.IsBanditFaction ?? true) || evaluatorFaction.IsBanditFaction)
			{
				if (!evaluatorFaction.IsBanditFaction)
					return (int) (estimatedValue * Settings.Instance.BanditsFactor * HalfLifeFactor(prisoner, true));
				return (int) (-estimatedValue * Settings.Instance.BanditsFactor * HalfLifeFactor(prisoner, false));
			}

			estimatedValue = (int)estimatedValue;
			// If prisoner is enemy of evaluator's kingdom
			if (FactionManager.IsAtWarAgainstFaction(prisoner.MapFaction, evaluatorFaction) ||
			    FactionManager.IsAtWarAgainstFaction(prisoner.Clan?.MapFaction, captor?.Clan?.MapFaction))
				estimatedValue *= Settings.Instance.AtWarFactor;
			if(evaluatorFaction == captor?.Clan || evaluatorFaction == captor?.MapFaction || evaluatorFaction == prisoner.PartyBelongedToAsPrisoner?.MapFaction)
				return -(int)(estimatedValue * HalfLifeFactor(prisoner, false));
			return evaluatorFaction == prisoner.Clan || evaluatorFaction == prisoner.MapFaction || evaluatorFaction.MapFaction == prisoner.MapFaction
				? (int)(estimatedValue * HalfLifeFactor(prisoner, true))
				: 0;
		}
		private static double HalfLifeFactor([NotNull]Hero prisoner, bool ransomer)
		{
			return Math.Pow(ransomer ? 2 : 0.5, (prisoner.CaptivityStartTime.ElapsedDaysUntilNow - Settings.Instance.PriceAgreementDelay) / Settings.Instance.PriceHalfLife);
		}
	}
}
