using System;
using System.Collections.Generic;
using System.Reflection;
using PreventEscape.Barterables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Barterables;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.BarterBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace PreventEscape.CampaignBehaviors 
{
	public class RequirePrisonerFreeBehavior : CampaignBehaviorBase
	{
		private class Conversation
		{
			public Hero Prisoner;
			public Hero Offerer;
		}
		public static ConversationContext PrisonerBarterConversationContext;
		public static double RequirementChance;

		private const string RequirePrisonerFree = nameof(RequirePrisonerFree);
		private const string RequirePrisonerFreeAccept = nameof(RequirePrisonerFreeAccept);
		private const string RequirePrisonerFreeReject = nameof(RequirePrisonerFreeReject);
		private const string RequirePrisonerFreeBarter = nameof(RequirePrisonerFreeBarter);
		private const string RequirePrisonerFreeAccepted = nameof(RequirePrisonerFreeAccepted);
		private const string RequirePrisonerFreeRejected = nameof(RequirePrisonerFreeRejected);
		private const string RequirePrisonerFreeBarterAccepted = nameof(RequirePrisonerFreeBarterAccepted);
		private const string RequirePrisonerFreeBarterRejected = nameof(RequirePrisonerFreeBarterRejected);

		private const string ProposePrisonerFree = nameof(ProposePrisonerFree);
		private const string ProposePrisonerFreeAccept = nameof(ProposePrisonerFreeAccept);
		private const string ProposePrisonerFreeBarter = nameof(ProposePrisonerFreeBarter);
		private const string ProposePrisonerFreeReject = nameof(ProposePrisonerFreeReject);
		private const string ProposePrisonerFreeAccepted = nameof(ProposePrisonerFreeAccepted);
		private const string ProposePrisonerFreeBarterAccepted = nameof(ProposePrisonerFreeBarterAccepted);
		private const string ProposePrisonerFreeBarterRejected = nameof(ProposePrisonerFreeBarterRejected);
		private const string ProposePrisonerFreeRejected = nameof(ProposePrisonerFreeRejected);

		private const string BegPrisonerFree = nameof(BegPrisonerFree);
		private const string BegPrisonerFreeAccept = nameof(BegPrisonerFreeAccept);
		private const string BegPrisonerFreeBarter = nameof(BegPrisonerFreeBarter);
		private const string BegPrisonerFreeReject = nameof(BegPrisonerFreeReject);
		private const string BegPrisonerFreeAccepted = nameof(BegPrisonerFreeAccepted);
		private const string BegPrisonerFreeBarterAccepted = nameof(BegPrisonerFreeBarterAccepted);
		private const string BegPrisonerFreeBarterRejected = nameof(BegPrisonerFreeBarterRejected);
		private const string BegPrisonerFreeRejected = nameof(BegPrisonerFreeRejected);


		private Action<Hero> _originalBarterHeroTick;
		private Hero _subjectPrisoner;
		private IMission _currentConversationMission;
		private Queue<Conversation> _conversationsQueue;
		private Dictionary<Hero, Hero> _rejectedPrisoners;

		public override void RegisterEvents()
		{
			_conversationsQueue = new Queue<Conversation >();
			CampaignEvents.OnSessionLaunchedEvent?.AddNonSerializedListener(this, SessionLaunched);
			CampaignEvents.OnMissionEndedEvent?.AddNonSerializedListener(this, MissionEnded);
			CampaignEvents.OnNewGameCreatedEvent?.AddNonSerializedListener(this, NewGameCreated);
			CampaignEvents.PrisonerReleased?.AddNonSerializedListener(this, PrisonerReleased);
			var barterBehaviour = Campaign.Current?.GetCampaignBehavior<DiplomaticBartersBehavior>();
			var originalBarterHeroTickMethodInfo = typeof(DiplomaticBartersBehavior).GetMethod("DailyTickHero", BindingFlags.NonPublic | BindingFlags.Instance);
			_originalBarterHeroTick = originalBarterHeroTickMethodInfo != null ? (Action<Hero>)Delegate.CreateDelegate(typeof(Action<Hero>), barterBehaviour, originalBarterHeroTickMethodInfo) : null;
			if (CampaignEvents.DailyTickHeroEvent == null)
				return;
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, RequirePrisonerFreeDailyHeroTick);
		}
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData("_rejectedPrisoners", ref _rejectedPrisoners);
		}
		private void NewGameCreated(CampaignGameStarter obj)
		{
			_rejectedPrisoners = new Dictionary<Hero, Hero>();
		}
		private void PrisonerReleased(Hero prisoner, IFaction arg2, EndCaptivityDetail arg3)
		{
			if(prisoner != null)
				_rejectedPrisoners?.Remove(prisoner);
		}
		private void MissionEnded(IMission mission)
		{
			if (_conversationsQueue == null || mission != _currentConversationMission || _conversationsQueue.IsEmpty())
				return;
			PlayNextConversation();
		}
		private void PlayNextConversation()
		{
			Conversation conversation = null;
			Hero offerer = null;
			Hero prisoner = null;
			while (!_conversationsQueue.IsEmpty() && (conversation == null || prisoner == null || offerer == null))
			{
				conversation = _conversationsQueue.Dequeue();
				offerer = conversation?.Offerer;
				prisoner = conversation?.Prisoner;
			}
			if (conversation == null || prisoner == null || offerer == null)
				return;
			if (Campaign.Current != null) Campaign.Current.CurrentConversationContext = PrisonerBarterConversationContext;
			_subjectPrisoner = prisoner;
			_currentConversationMission = CampaignMission.OpenConversationMission(
				new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty),
				new ConversationCharacterData(conversation.Offerer.CharacterObject));
		}
		private void SessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			if(_rejectedPrisoners == null)
				_rejectedPrisoners = new Dictionary<Hero, Hero>();
			AddDialogs(campaignGameStarter);
		}
		private void RequirePrisonerFreeDailyHeroTick(Hero hero)
		{
			try
			{
				if(hero == null)
					return;
				if (hero.IsPrisoner && MBRandom.RandomFloat >= RequirementChance)
				{
					var captorParty = hero.PartyBelongedToAsPrisoner;
					var captorFaction = captorParty?.MapFaction;
					var offerer = hero.Clan?.Leader;
					if (offerer.IsPrisoner && offerer.Clan?.MapFaction?.Leader != null)
						offerer = offerer.Clan.MapFaction.Leader;
					var captorLeader = captorFaction?.Leader;
					var prisonerClan = hero.Clan;
					if (captorFaction == Hero.MainHero.MapFaction)
					{
						var conversation = new Conversation()
						{
							Prisoner = hero,
							Offerer = offerer
						};
						_conversationsQueue.Enqueue(conversation);
						if(_currentConversationMission == null)
							PlayNextConversation();
					}
				}
				return;
				{
					if (hero == null || !hero.IsPrisoner || hero.Clan == null ||
					    (hero.PartyBelongedToAsPrisoner == null || MBRandom.RandomFloat >= RequirementChance))
						return;
					var captorParty = hero.PartyBelongedToAsPrisoner;
					var captorFaction = captorParty.MapFaction;
					var offerer = hero.Clan.Leader;
					if (offerer.IsPrisoner && offerer.Clan?.MapFaction?.Leader != null)
						offerer = offerer.Clan.MapFaction.Leader;
					var captorLeader = captorFaction?.Leader;
					var prisonerClan = hero.Clan;
					SetPrisonerFreeBarterable prisonerFreeBarterable = new SetPrisonerFreeNewBarterable(hero, captorLeader, captorParty, offerer);
					if (prisonerFreeBarterable.GetValueForFaction(captorFaction) + prisonerFreeBarterable.GetValueForFaction(prisonerClan) <= 0)
						return;
					if (captorFaction?.IsBanditFaction ?? false)
					{
						BarterData barterData = new BarterData(hero.Clan.Leader, captorFaction.Leader, null, null,
							null, 0, true);
						barterData.AddBarterable<DefaultsBarterGroup>(prisonerFreeBarterable);
						Campaign.Current?.BarterManager?.ExecuteAIBarter(barterData, captorFaction, hero.Clan, captorFaction.Leader,
							hero.Clan.Leader);
					}

					if (captorFaction == PartyBase.MainParty?.MapFaction)
					{
						if (offerer.IsPrisoner && offerer.PartyBelongedToAsPrisoner == null ||
						    offerer.PartyBelongedToAsPrisoner.LeaderHero != Hero.MainHero)
							return;
						if (Campaign.Current != null)
							Campaign.Current.CurrentConversationContext = PrisonerBarterConversationContext;
						_subjectPrisoner = hero;
						if (offerer != null)
							CampaignMission.OpenConversationMission(
								new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty),
								new ConversationCharacterData(offerer.CharacterObject));

					}
				}
			}
			catch (Exception)
			{
				_originalBarterHeroTick?.Invoke(hero);
			}
		}
		private void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			if (campaignGameStarter == null)
				return;
			var texts = new Dictionary<string, string>
			{
				{ RequirePrisonerFree, "I require you to set one of my subjects {PRISONER.NAME} free immediately, as you have no right to keep {?PRISONER.GENDER}her{?}him{\\?} against {?PRISONER.GENDER}her{?}his{\\?} will!" },
				{ RequirePrisonerFreeAccept, "I will give my order to free {?PRISONER.GENDER}her{?}him{\\?} immediately." },
				{ RequirePrisonerFreeBarter, "We shall discuss on terms of your requirement." },
				{ RequirePrisonerFreeReject, "That's not going to happen." },
				{ RequirePrisonerFreeAccepted, "I will forget about this incident. For now." },
				{ RequirePrisonerFreeBarterAccepted, "I shall agree, but this is inexcusably arrogant!" },
				{ RequirePrisonerFreeBarterRejected, "You will come to an agreement, one way or another!" },
				{ RequirePrisonerFreeRejected, "This won't be tolerated!" },
				{ ProposePrisonerFree, "I'm here to discuss terms of releasing one of my subjects, {PRISONER.NAME}." },
				{ ProposePrisonerFreeAccept, "There's no terms, {?PRISONER.GENDER}she{?}he{\\?} is free to go." },
				{ ProposePrisonerFreeBarter, "Let's discuss then." },
				{ ProposePrisonerFreeReject, "There's nothing to discuss." },
				{ ProposePrisonerFreeAccepted, "This is very noble of you." },
				{ ProposePrisonerFreeBarterAccepted, "That's good, I agree." },
				{ ProposePrisonerFreeBarterRejected, "Too bad." },
				{ ProposePrisonerFreeRejected, "You will regret this." },
				{ BegPrisonerFree, "I beg you for mercy! Maybe, you could indulge me?" },
				{ BegPrisonerFreeAccept, "I'm in good mood today. So now you are free." },
				{ BegPrisonerFreeBarter, "What could you propose for your freedom?" },
				{ BegPrisonerFreeReject, "Get off of my sight!" },
				{ BegPrisonerFreeAccepted, "Thank you! I will not forged your kindness!" },
				{ BegPrisonerFreeBarterAccepted, "I have to agree." },
				{ BegPrisonerFreeBarterRejected, "This is unacceptable..." },
				{ BegPrisonerFreeRejected, "You can't keep me here forever!" }
			};
			var builder = new DialogBuilder(campaignGameStarter, texts);
			builder.GetStartToken()
				.AddDialogLine(RequirePrisonerFree, RequirePrisonerFreeCondition)
					.SetPriority(1000)
					.SetExpressions(Expressions.IdleBody.Aggressive | Expressions.IdleFace.ConvoIrritable | Expressions.ReactionBody.VeryNegative | Expressions.ReactionFace.VeryNegative)
					.Decision()
						.AddVariant(RequirePrisonerFreeAccept, variant => variant
							.Response(RequirePrisonerFreeAccepted)
								.SetConsequence(RequirePrisonerFreeAcceptedConsequence)
								.CloseWindow())
						.AddVariant(RequirePrisonerFreeBarter, variant => variant
							.Barter(RequirePrisonerFreeBarterInit) 
								.BarterAccept(RequirePrisonerFreeBarterAccepted, 
									accepted => accepted
										.SetExpressions(Expressions.ReactionBody.Negative)
										.SetConsequence(RequirePrisonerFreeBarterAcceptedConsequence)
										.CloseWindow())
								.BarterReject(RequirePrisonerFreeBarterRejected, 
									rejected => rejected
										.SetExpressions(Expressions.ReactionBody.VeryNegative | Expressions.IdleBody.Warrior | Expressions.IdleFace.IdleAngry)
										.SetConsequence(RequirePrisonerFreeBarterRejectedConsequence)
										.CloseWindow()))
						.AddVariant(RequirePrisonerFreeReject, variant => variant
							.Response(RequirePrisonerFreeRejected)
								.SetExpressions(Expressions.IdleBody.Warrior | Expressions.IdleFace.IdleAngry | Expressions.ReactionBody.VeryNegative)
								.SetConsequence(PrisonerRequirementUnconditionalRejectedConsequence)
								.CloseWindow())
				.Build();
			builder.GetStartToken()
				.AddDialogLine(ProposePrisonerFree, ProposePrisonerFreeCondition)
					.SetPriority(1000)
					.SetExpressions(Expressions.IdleBody.Closed | Expressions.IdleFace.ConvoGrim)
					.Decision()
						.AddVariant(ProposePrisonerFreeAccept, variant => variant
							.Response(ProposePrisonerFreeAccepted)
								.SetExpressions(Expressions.ReactionFace.Happy | Expressions.ReactionBody.VeryPositive | Expressions.IdleFace.Happy | Expressions.IdleBody.Demure)
								.SetConsequence(ProposePrisonerFreeAcceptedConsequence)
								.CloseWindow())
						.AddVariant(ProposePrisonerFreeBarter, variant => variant
							.Barter(ProposePrisonerFreeBarterInit)
								.BarterAccept(ProposePrisonerFreeBarterAccepted, 
									accepted => accepted
										.SetExpressions(Expressions.ReactionFace.Happy | Expressions.ReactionBody.Positive)
										.SetConsequence(ProposePrisonerFreeBarterAcceptedConsequence)
										.CloseWindow())
								.BarterReject(ProposePrisonerFreeBarterRejected, 
									rejected => rejected
										.SetExpressions(Expressions.IdleBody.Closed | Expressions.IdleFace.ConvoGrim | Expressions.ReactionBody.Negative)
										.SetConsequence(ProposePrisonerFreeBarterRejectedConsequence)
										.CloseWindow()))
						.AddVariant(ProposePrisonerFreeReject, variant => variant
							.Response(ProposePrisonerFreeRejected)
								.SetExpressions(Expressions.ReactionFace.VeryNegative | Expressions.ReactionBody.VeryPositive | Expressions.IdleFace.IdleAngry | Expressions.IdleBody.Closed)
								.SetConsequence(ProposePrisonerFreeRejectedConsequence)
								.CloseWindow())
				.Build();
			builder.GetStartToken()
				.AddDialogLine(BegPrisonerFree, BegPrisonerFreeCondition)
					.SetPriority(1000)
					.SetExpressions(Expressions.IdleBody.Demure | Expressions.IdleFace.ConvoGrave | Expressions.ReactionBody.Unsure)
					.Decision()
						.AddVariant(BegPrisonerFreeAccept, variant => variant
							.Response(BegPrisonerFreeAccepted)
								.SetExpressions(Expressions.ReactionFace.Happy | Expressions.ReactionBody.VeryPositive | Expressions.IdleFace.Happy | Expressions.IdleBody.Demure)
								.SetConsequence(BegPrisonerFreeAcceptedConsequence)
								.CloseWindow())
						.AddVariant(BegPrisonerFreeBarter, variant => variant
							.Barter(BegPrisonerFreeBarterInit)
								.BarterAccept(BegPrisonerFreeBarterAccepted,
									accepted => accepted
										.SetExpressions(Expressions.ReactionFace.Happy | Expressions.ReactionBody.VeryPositive | Expressions.IdleFace.Happy | Expressions.IdleBody.Demure)
										.SetConsequence(BegPrisonerFreeBarterAcceptedConsequence)
										.CloseWindow())
								.BarterReject(BegPrisonerFreeBarterRejected,
									rejected => rejected
										.SetExpressions(Expressions.IdleBody.Demure | Expressions.IdleFace.ConvoGrim | Expressions.ReactionBody.Negative)
										.SetConsequence(BegPrisonerFreeBarterRejectedConsequence)
										.CloseWindow()))
						.AddVariant(BegPrisonerFreeReject, variant => variant
							.Response(BegPrisonerFreeRejected)
								.SetExpressions(Expressions.ReactionBody.VeryNegative | Expressions.IdleFace.ConvoGrave | Expressions.IdleBody.Demure)
								.SetConsequence(BegPrisonerFreeRejectedConsequence)
								.CloseWindow())
				.Build();

		}
		private bool RequirePrisonerFreeCondition()
		{
			// Not implemented
			return false;
		}
		private void RequirePrisonerFreeAcceptedConsequence()
		{
			throw new NotImplementedException();
		}
		private IEnumerable<Barterable> RequirePrisonerFreeBarterInit()
		{
			throw new NotImplementedException();
		}
		private void RequirePrisonerFreeBarterAcceptedConsequence()
		{
			throw new NotImplementedException();
		}
		private void RequirePrisonerFreeBarterRejectedConsequence()
		{
			throw new NotImplementedException();
		}
		private void PrisonerRequirementUnconditionalRejectedConsequence()
		{
			throw new NotImplementedException();
		}
		private bool ProposePrisonerFreeCondition()
		{
			return Campaign.Current?.CurrentConversationContext == PrisonerBarterConversationContext && _subjectPrisoner != Hero.OneToOneConversationHero;
		}
		private void ProposePrisonerFreeAcceptedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, Hero.MainHero, 5);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, 5);
		}
		private IEnumerable<Barterable> ProposePrisonerFreeBarterInit()
		{
			if(_subjectPrisoner != null)
				return new[]
				{
					new SetPrisonerFreeNewBarterable(_subjectPrisoner, Hero.MainHero, _subjectPrisoner.PartyBelongedToAsPrisoner, Hero.OneToOneConversationHero)
				};
			return new Barterable[0];
		}
		private void ProposePrisonerFreeBarterAcceptedConsequence()
		{
		}
		private void ProposePrisonerFreeBarterRejectedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -1);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, Hero.MainHero, -1);
		}
		private void ProposePrisonerFreeRejectedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -2);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, Hero.MainHero, -2);
			_rejectedPrisoners.Add(_subjectPrisoner, Hero.MainHero);
		}
		private bool BegPrisonerFreeCondition()
		{
			return Campaign.Current?.CurrentConversationContext == PrisonerBarterConversationContext &&
			       Hero.OneToOneConversationHero == _subjectPrisoner;

		}
		private void BegPrisonerFreeAcceptedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, 10);
		}
		private IEnumerable<Barterable> BegPrisonerFreeBarterInit()
		{
			if(_subjectPrisoner != null)
				return new[]
				{
					new SetPrisonerFreeNewBarterable(_subjectPrisoner, Hero.MainHero, _subjectPrisoner.PartyBelongedToAsPrisoner, _subjectPrisoner)
				};
			return new Barterable[0];
		}
		private void BegPrisonerFreeBarterAcceptedConsequence()
		{
		}
		private void BegPrisonerFreeBarterRejectedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -1);
			_rejectedPrisoners.Add(_subjectPrisoner, Hero.MainHero);
		}
		private void BegPrisonerFreeRejectedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -2);
			_rejectedPrisoners.Add(_subjectPrisoner, Hero.MainHero);
		}
	}
}
