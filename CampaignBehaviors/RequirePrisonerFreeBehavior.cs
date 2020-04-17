using System;
using System.Collections.Generic;
using System.Reflection;
using BannerLib.Misc;
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
			public Hero Captor;
			public PartyBase CaptorParty;
		}

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

		private const string RansomPrisoner = nameof(RansomPrisoner);
		private const string RansomPrisonerRequire = nameof(RansomPrisonerRequire);
		private const string RansomPrisonerBarter = nameof(RansomPrisonerBarter);
		private const string RansomPrisonerReject = nameof(RansomPrisonerReject);
		private const string RansomPrisonerRequirementRejected = nameof(RansomPrisonerRequirementRejected);
		private const string RansomPrisonerRequirementAccepted = nameof(RansomPrisonerRequirementAccepted);
		private const string RansomPrisonerBarterAccepted = nameof(RansomPrisonerBarterAccepted);
		private const string RansomPrisonerBarterRejected = nameof(RansomPrisonerBarterRejected);
		private const string RansomPrisonerRejected = nameof(RansomPrisonerRejected);

		private Action<Hero> _originalBarterHeroTick;
		private Hero _subjectPrisoner;
		private IMission _currentConversationMission;
		private Queue<Conversation> _conversationsQueue;

		public override void RegisterEvents()
		{
			_conversationsQueue = new Queue<Conversation >();
			CampaignEvents.OnSessionLaunchedEvent?.AddNonSerializedListener(this, SessionLaunched);
			CampaignEvents.OnMissionEndedEvent?.AddNonSerializedListener(this, MissionEnded);
			CampaignEvents.PrisonerReleased?.AddNonSerializedListener(this, PrisonerReleased);
			var barterBehaviour = Campaign.Current?.GetCampaignBehavior<DiplomaticBartersBehavior>();
			var originalBarterHeroTickMethodInfo = typeof(DiplomaticBartersBehavior).GetMethod("DailyTickHero", BindingFlags.NonPublic | BindingFlags.Instance);
			_originalBarterHeroTick = originalBarterHeroTickMethodInfo != null ? (Action<Hero>)Delegate.CreateDelegate(typeof(Action<Hero>), barterBehaviour, originalBarterHeroTickMethodInfo) : null;
			if (CampaignEvents.DailyTickHeroEvent == null)
				return;
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, RequirePrisonerFreeDailyHeroTick);
			CampaignEvents.OnMissionStartedEvent?.AddNonSerializedListener(this, MissionStarted);
		}
		private void MissionStarted(IMission obj)
		{
			
		}
		public override void SyncData(IDataStore dataStore)
		{
		}
		private void PrisonerReleased(Hero prisoner, IFaction arg2, EndCaptivityDetail arg3)
		{
			if(arg3 == EndCaptivityDetail.Ransom)
				InformationManager.DisplayMessage(new InformationMessage($"{prisoner.Name} from {prisoner.MapFaction.Name} has been ransomed from captivity."));
		}
		private void MissionEnded(IMission mission)
		{
			_subjectPrisoner = null;
			if (_conversationsQueue == null || mission != _currentConversationMission || _conversationsQueue.IsEmpty())
			{
				_currentConversationMission = null;
				return;
			}
			_currentConversationMission = null;
			PlayNextConversation();
		}
		private void PlayNextConversation()
		{
			Conversation conversation = null;
			Hero ransomPayer = null;
			Hero prisoner = null;
			Hero captor = null;
			PartyBase captorParty = null;
			while (!_conversationsQueue.IsEmpty() && (conversation == null || prisoner == null || ransomPayer == null))
			{
				conversation = _conversationsQueue.Dequeue();
				if (conversation != null)
				{
					ransomPayer = conversation.Offerer;
					prisoner = conversation.Prisoner;
					captor = conversation.Captor;
					captorParty = conversation.CaptorParty;
				}
			}
			if (conversation == null || prisoner == null || ransomPayer == null || Campaign.Current == null)
				return;
			_subjectPrisoner = prisoner;
			MBTextManager.SetTextVariable("PRISONER", _subjectPrisoner);
			MBTextManager.SetTextVariable("RANSOMPAYER", ransomPayer);
			string titleText = null;
			string descriptionText = null;
			if (ransomPayer != prisoner && ransomPayer != Hero.MainHero)
			{
				titleText = "An emissary asks for you audience.";
				descriptionText = $"An emissary from {ransomPayer.Name} from {ransomPayer.MapFaction?.Name} has arrived to discuss with you the ransom for one of your prisoners. Would you like to speak with him?";
			}
			else if (prisoner.PartyBelongedToAsPrisoner == PartyBase.MainParty)
			{
				titleText = "A prisoner asks for your audience.";
				descriptionText = $"{prisoner.Name} from {prisoner.MapFaction?.Name} asks for your audience. Would you like to speak with him?";
			}
			else if(prisoner.PartyBelongedToAsPrisoner?.MapFaction?.Leader == Hero.MainHero)
			{
				titleText = "A prisoner asks for your audience.";
				descriptionText = $"{prisoner.Name} from {prisoner.MapFaction?.Name} has sent you a message asking for your audience. Would you like to speak with him?";
			}
			else if (ransomPayer == Hero.MainHero && captorParty != null && captorParty.MapFaction.IsBanditFaction)
			{
				titleText = "Ransom offering";
				descriptionText = $"Bandit leader has sent you a message offering to ransom {_subjectPrisoner.Name} from {(_subjectPrisoner.Clan == Clan.PlayerClan ? "your clan" : _subjectPrisoner.Clan?.FullName?.ToString())}.";
			}
			else if (ransomPayer == Hero.MainHero && captorParty != null && !(captorParty.MapFaction?.IsBanditFaction ?? true))
			{
				titleText = "Ransom offering";
				descriptionText = $"{captor?.Name} from {captor?.MapFaction?.Name} has sent you a message offering to ransom {_subjectPrisoner.Name} from {(_subjectPrisoner.Clan == Clan.PlayerClan ? "your clan" : _subjectPrisoner.Clan?.FullName?.ToString())}.";
			}

			if (titleText == null)
			{
				_subjectPrisoner = null;
				return;
			}

			InquiryBuilder
				.Create(titleText)
				.WithDescription(descriptionText)
				.WithAffirmative("Accept", () =>
				{
					Campaign.Current.CurrentConversationContext = Settings.Instance.PrisonerBarterConversationContext;
					if (ransomPayer != Hero.MainHero)
					{
						_currentConversationMission = CampaignMission.OpenConversationMission(
							new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty),
							new ConversationCharacterData(ransomPayer?.CharacterObject));
					}
					else
					{
						if ((captor?.CharacterObject ?? captorParty?.Leader) != null)
							_currentConversationMission = CampaignMission.OpenConversationMission(
								new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty),
								new ConversationCharacterData(captor?.CharacterObject ?? captorParty?.Leader, captorParty));
					}
				}).WithNegative("Decline", () =>
				{
					if(_subjectPrisoner != ransomPayer && ransomPayer != Hero.MainHero)
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(ransomPayer, Hero.MainHero, -Settings.Instance.RansomRejectRelationDeterioration);
					if(_subjectPrisoner != Hero.MainHero)
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -Settings.Instance.RansomRejectRelationDeterioration);
					PlayNextConversation();
				}).BuildAndPublish(true);
		}
		private void SessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			AddDialogs(campaignGameStarter);
		}
		private void TryPrisonerRelease(Hero prisoner, Hero captor, PartyBase captorParty, Hero ransomPayer)
		{
			if (prisoner == null || ransomPayer == null || captor == null || captor.IsPrisoner || (ransomPayer != prisoner && ransomPayer.IsPrisoner))
				return;
			if (captor == Hero.MainHero || captor.Clan?.Leader == Hero.MainHero || captor.MapFaction?.Leader == Hero.MainHero || ransomPayer == Hero.MainHero)
			{
				var conversation = new Conversation()
				{
					Prisoner = prisoner,
					Offerer = ransomPayer,
					Captor = captor,
					CaptorParty = captorParty
				};
				_conversationsQueue.Enqueue(conversation);
				if (_currentConversationMission == null)
					PlayNextConversation();
			}
			else
			{
				TryRansomPrisoner(prisoner, captor, ransomPayer);
			}
		}
		private void TryRansomPrisoner(Hero prisoner, Hero captor, Hero ransomPayer)
		{
			if (prisoner == null || ransomPayer == null || captor == null)
				return;
			var price = Math.Abs(HeroEvaluator.Evaluate(prisoner, captor, ransomPayer, captor.MapFaction));
			if(prisoner.Gold >= price)
				Ransom(prisoner, captor, ransomPayer, prisoner.PartyBelongedToAsPrisoner, null);
			else if (prisoner.Clan?.Leader != prisoner && prisoner.Clan?.Leader != null && prisoner.Clan?.Leader.Gold >= price && (!prisoner.Clan?.Leader.IsPrisoner ?? false))
				Ransom(prisoner, captor, prisoner.Clan.Leader, prisoner.PartyBelongedToAsPrisoner, prisoner.Clan.Leader.PartyBelongedTo?.Party);
			else if (prisoner.MapFaction?.Leader != prisoner && prisoner.MapFaction?.Leader != null && prisoner.MapFaction?.Leader.Gold >= price && (!prisoner.MapFaction?.Leader.IsPrisoner ?? false))
				Ransom(prisoner, captor, prisoner.MapFaction?.Leader, prisoner.PartyBelongedToAsPrisoner, prisoner.MapFaction?.Leader.PartyBelongedTo?.Party);
		}
		private void TryRansomPrisonerFromBandits(Hero prisoner)
		{
			var bandits = prisoner?.PartyBelongedToAsPrisoner;
			if (bandits == null || Campaign.Current == null || (!bandits.MapFaction?.IsBanditFaction ?? true))
				return;
			var price = Math.Abs(HeroEvaluator.Evaluate(prisoner, null, prisoner, prisoner.MapFaction));
			if (prisoner.Gold >= price)
				Ransom(prisoner, null, prisoner, bandits, null);
			else if(prisoner.Clan?.Leader != prisoner && prisoner.Clan?.Leader != null && prisoner.Clan?.Leader.Gold >= price && (!prisoner.Clan?.Leader.IsPrisoner ?? false))
				Ransom(prisoner, null, prisoner.Clan.Leader, bandits, prisoner.Clan.Leader.PartyBelongedTo?.Party);
			else if(prisoner.MapFaction?.Leader != prisoner && prisoner.MapFaction?.Leader != null && prisoner.MapFaction?.Leader.Gold >= price && (!prisoner.MapFaction?.Leader.IsPrisoner ?? false))
				Ransom(prisoner, null, prisoner.MapFaction?.Leader, bandits, prisoner.MapFaction?.Leader.PartyBelongedTo?.Party);
		}
		private void Ransom(Hero prisoner, Hero captor, Hero ransomPayer, PartyBase captorParty, PartyBase ransomPayerParty)
		{
			if (prisoner == null || ransomPayer == null || BarterManager.Instance == null)
				return;
			if (ransomPayer == Hero.MainHero)
			{
				var conversation = new Conversation()
				{
					Prisoner = prisoner,
					Offerer = ransomPayer,
					Captor = captor,
					CaptorParty = captorParty,
				};
				_conversationsQueue.Enqueue(conversation);
				if (_currentConversationMission == null)
					PlayNextConversation();
				return;
			}
			BarterData barterData = new BarterData(prisoner, captor, ransomPayerParty, captorParty, null, 0, true);
			barterData.AddBarterGroup(new DefaultsBarterGroup());
			Barterable barterable = new SetPrisonerFreeNewBarterable(prisoner, captor, captorParty, ransomPayer);
			barterable.SetIsOffered(true);
			barterData.AddBarterable<DefaultsBarterGroup>(barterable, true);
			var price = barterable.GetValueForFaction(captor?.MapFaction ?? captorParty?.MapFaction);
			barterable = new GoldBarterable(ransomPayer, captor, ransomPayerParty, captorParty, ransomPayer.Gold);
			barterData.AddBarterable<DefaultsBarterGroup>(barterable, true);
			if ((captor?.IsFactionLeader ?? false) && captor.MapFaction == Hero.MainHero?.MapFaction
			                                       && ((prisoner.PartyBelongedTo?.Party?.IsSettlement ?? false) && prisoner.PartyBelongedTo.Party.Settlement?.OwnerClan?.Leader == Hero.MainHero
												   || (prisoner.PartyBelongedTo?.Party?.IsMobile ?? false) && prisoner.PartyBelongedTo.LeaderHero == Hero.MainHero))
				InquiryBuilder.Create("Your lord's order.")
					.WithDescription($"Your lord, {captor.Name}, orders you to release one of your prisoners, {prisoner.Name}!")
					.BuildAndPublish(true);
			if(prisoner.PartyBelongedToAsPrisoner.MapFaction.IsBanditFaction)
				ransomPayer.ChangeHeroGold(price);
			else
				BarterManager.Instance.ExecuteAIBarter(barterData, prisoner.PartyBelongedToAsPrisoner?.MapFaction, prisoner.MapFaction, captor ?? captorParty?.LeaderHero, ransomPayer);
		}
		private void RequirePrisonerFreeDailyHeroTick(Hero hero)
		{
			try
			{
				if(hero == null)
					return;
				if (hero.Name?.ToString() == "Ira")
					new object();
				if (hero.IsPrisoner && MBRandom.RandomFloat >= Settings.Instance.RansomChance)
				{
					if (hero.PartyBelongedToAsPrisoner == null)
					{
						EndCaptivityAction.ApplyByEscape(hero);
						return;
					}
					Hero prisoner = hero;
					Hero prisonerClanLeader = prisoner.Clan?.Leader;
					Hero prisonerFactionLeader = prisoner.MapFaction?.Leader;
					Hero captor = null;
					Hero captorClanLeader = null;
					PartyBase captorParty = null;

					if (prisoner.PartyBelongedToAsPrisoner?.IsMobile ?? false)
					{
						captor = prisoner.PartyBelongedToAsPrisoner.LeaderHero;
						captorClanLeader = captor?.Clan?.Leader;
						captorParty = prisoner.PartyBelongedToAsPrisoner;
					}
					else if (prisoner.PartyBelongedToAsPrisoner?.IsSettlement ?? false)
					{
						var settlement = prisoner.PartyBelongedToAsPrisoner.Settlement;
						captor = captorClanLeader;
						captorClanLeader = settlement?.OwnerClan?.Leader;
						captorParty = captorClanLeader?.PartyBelongedTo?.Party;
					}
					var captorFactionLeader = captor?.MapFaction?.Leader;
					if (captor == null)
					{
						if (prisoner.PartyBelongedToAsPrisoner?.MapFaction?.IsBanditFaction ?? false)
						{
							// Bandits..
							TryRansomPrisonerFromBandits(prisoner);
						}
						return;
					}
					if(MBRandom.RandomFloat < 0.3333f || (prisonerClanLeader == null && prisonerFactionLeader == null))
						if(MBRandom.RandomFloat < 0.3333f || (captorClanLeader == null && captorFactionLeader == null))
							TryPrisonerRelease(prisoner, captor, captorParty, prisoner);
						else if(MBRandom.RandomFloat < 0.5f || captorFactionLeader == null)
							TryPrisonerRelease(prisoner, captorClanLeader, captorParty, prisoner);
						else
							TryPrisonerRelease(prisoner, captorFactionLeader, captorParty, prisoner);
					else if(MBRandom.RandomFloat < 0.5 || prisonerFactionLeader == null)
						if(MBRandom.RandomFloat < 0.3333f || (captorClanLeader == null && captorFactionLeader == null))
							TryPrisonerRelease(prisoner, captor, captorParty, prisonerClanLeader);
						else if(MBRandom.RandomFloat < 0.5f || captorFactionLeader == null)
							TryPrisonerRelease(prisoner, captorClanLeader, captorParty, prisonerClanLeader);
						else
							TryPrisonerRelease(prisoner, captorFactionLeader, captorParty, prisonerClanLeader);
					else
					{
						if (MBRandom.RandomFloat < 0.3333f || (captorClanLeader == null && captorFactionLeader == null))
							TryPrisonerRelease(prisoner, captor, captorParty, prisonerFactionLeader);
						else if (MBRandom.RandomFloat < 0.5f || captorFactionLeader == null)
							TryPrisonerRelease(prisoner, captorClanLeader, captorParty, prisonerFactionLeader);
						else
							TryPrisonerRelease(prisoner, captorFactionLeader, captorParty, prisonerFactionLeader);
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
				{ RequirePrisonerFree, "I require you to set my subject, {PRISONER.NAME}, free immediately! You have no right to keep {?PRISONER.GENDER}her{?}him{\\?} against {?PRISONER.GENDER}her{?}his{\\?} will!" },
				{ RequirePrisonerFreeAccept, "I will give my order to free {?PRISONER.GENDER}her{?}him{\\?} immediately." },
				{ RequirePrisonerFreeBarter, "We shall discuss the terms of your requirement." },
				{ RequirePrisonerFreeReject, "That's not going to happen." },
				{ RequirePrisonerFreeAccepted, "I will overlook this incident. For now." },
				{ RequirePrisonerFreeBarterAccepted, "I shall agree, but this is inexcusably arrogant!" },
				{ RequirePrisonerFreeBarterRejected, "You will come to an agreement, one way or another!" },
				{ RequirePrisonerFreeRejected, "This will not be tolerated!" },
				{ ProposePrisonerFree, "I'm here to discuss terms of releasing my subject, {PRISONER.NAME}." },
				{ ProposePrisonerFreeAccept, "There's no need for terms, {?PRISONER.GENDER}she{?}he{\\?} is free to go." },
				{ ProposePrisonerFreeBarter, "Let's discuss then." },
				{ ProposePrisonerFreeReject, "There's nothing to discuss." },
				{ ProposePrisonerFreeAccepted, "This is very noble of you." },
				{ ProposePrisonerFreeBarterAccepted, "That's good, I agree." },
				{ ProposePrisonerFreeBarterRejected, "Unfortunate." },
				{ ProposePrisonerFreeRejected, "You will come to regret this." },
				{ BegPrisonerFree, "I beg you for mercy! Maybe, you could indulge me?" },
				{ BegPrisonerFreeAccept, "I'm in good mood today. So now you are free." },
				{ BegPrisonerFreeBarter, "What could you offer for your freedom?" },
				{ BegPrisonerFreeReject, "Get out of my sight!" },
				{ BegPrisonerFreeAccepted, "Thank you! I will not forget your kindness!" },
				{ BegPrisonerFreeBarterAccepted, "Of course I agree!" },
				{ BegPrisonerFreeBarterRejected, "You are being unreasonable..." },
				{ BegPrisonerFreeRejected, "You can't keep me here forever!" },
				{ RansomPrisoner, "As you should know, {PRISONER.NAME} is currently being held in our dungeon. Do you want {?PRISONER.GENDER}her{?}him{\\?} free?" },
				{ RansomPrisonerRequire, "I demand you to free {?PRISONER.GENDER}her{?}him{\\?} immediately, or I'll do it by force!" },
				{ RansomPrisonerBarter, "What do you want for {?PRISONER.GENDER}her{?}him{\\?}?" },
				{ RansomPrisonerReject, "I won't bargain with such as you." },
				{ RansomPrisonerRequirementRejected, "You can try!" },
				{ RansomPrisonerRequirementAccepted, "Think of it as a gesture of goodwill." },
				{ RansomPrisonerBarterAccepted, "That's agreeable proposition." },
				{ RansomPrisonerBarterRejected, "As you want."  },
				{ RansomPrisonerRejected, "I will convey your best wishes to the prisoner." },
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
							.Barter(PrisonerFreeBarterInit) 
								.BarterAccept(RequirePrisonerFreeBarterAccepted, accepted => accepted
									.SetExpressions(Expressions.ReactionBody.Negative)
									.SetConsequence(RequirePrisonerFreeBarterAcceptedConsequence)
									.CloseWindow())
								.BarterReject(RequirePrisonerFreeBarterRejected, rejected => rejected
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
					.SetExpressions(Expressions.IdleBody.Closed | Expressions.IdleFace.ConvoStonefaced)
					.Decision()
						.AddVariant(ProposePrisonerFreeAccept, variant => variant
							.Response(ProposePrisonerFreeAccepted)
								.SetExpressions(Expressions.ReactionFace.Happy | Expressions.ReactionBody.VeryPositive | Expressions.IdleFace.Happy | Expressions.IdleBody.Demure)
								.SetConsequence(ProposePrisonerFreeAcceptedConsequence)
								.CloseWindow())
						.AddVariant(ProposePrisonerFreeBarter, variant => variant
							.Barter(PrisonerFreeBarterInit)
								.BarterAccept(ProposePrisonerFreeBarterAccepted, accepted => accepted
									.SetExpressions(Expressions.ReactionFace.Happy | Expressions.ReactionBody.Positive)
									.SetConsequence(ProposePrisonerFreeBarterAcceptedConsequence)
									.CloseWindow())
								.BarterReject(ProposePrisonerFreeBarterRejected, rejected => rejected
									.SetExpressions(Expressions.IdleBody.Aggressive | Expressions.IdleFace.ConvoGrave | Expressions.ReactionBody.Negative)
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
							.Barter(PrisonerFreeBarterInit)
								.BarterAccept(BegPrisonerFreeBarterAccepted, accepted => accepted
									.SetExpressions(Expressions.ReactionFace.Happy | Expressions.ReactionBody.VeryPositive | Expressions.IdleFace.Happy | Expressions.IdleBody.Demure)
									.SetConsequence(BegPrisonerFreeBarterAcceptedConsequence)
									.CloseWindow())
								.BarterReject(BegPrisonerFreeBarterRejected, rejected => rejected
									.SetExpressions(Expressions.IdleBody.Demure | Expressions.IdleFace.ConvoGrave | Expressions.ReactionBody.Negative)
									.SetConsequence(BegPrisonerFreeBarterRejectedConsequence)
									.CloseWindow()))
						.AddVariant(BegPrisonerFreeReject, variant => variant
							.Response(BegPrisonerFreeRejected)
								.SetExpressions(Expressions.ReactionBody.VeryNegative | Expressions.IdleFace.ConvoGrave | Expressions.IdleBody.Demure)
								.SetConsequence(BegPrisonerFreeRejectedConsequence)
								.CloseWindow())
				.Build();
			builder.GetStartToken()
				.AddDialogLine(RansomPrisoner, RansomPrisonerCondition)
					.SetPriority(1000)
					.SetExpressions(Expressions.IdleBody.Normal | Expressions.IdleFace.ConvoCharitable | Expressions.ReactionFace.VeryNegative)
					.Decision()
						.AddVariant(RansomPrisonerRequire, variant => variant
							.Response(RansomPrisonerRequirementAccepted)
								.SetPriority(101)
								.SetExpressions(Expressions.IdleBody.Closed | Expressions.IdleFace.ConvoStonefaced | Expressions.ReactionBody.Negative)
								.SetCondition(RansomPrisonerRequirementAcceptedCondition)
								.SetConsequence(RansomPrisonerRequirementAcceptedConsequence)
								.CloseWindow()
							.Response(RansomPrisonerRequirementRejected)
								.SetExpressions(Expressions.IdleBody.Warrior | Expressions.IdleFace.ConvoMocking | Expressions.ReactionBody.Trivial)
								.SetConsequence(RansomPrisonerRequirementRejectedConsequence)
								.CloseWindow())
						.AddVariant(RansomPrisonerBarter, variant => variant
							.Barter(RansomPrisonerBarterInit)
								.BarterAccept(RansomPrisonerBarterAccepted, accepted => accepted
									.SetExpressions(Expressions.IdleBody.Normal | Expressions.IdleFace.ConvoNonchalant | Expressions.ReactionBody.Positive)
									.SetConsequence(RansomPrisonerBarterAcceptedConsequence)
									.CloseWindow())
								.BarterReject(RansomPrisonerBarterRejected, rejected => rejected
									.SetExpressions(Expressions.IdleBody.Closed | Expressions.IdleFace.ConvoStonefaced)
									.SetConsequence(RansomPrisonerBarterRejectedConsequence)
									.CloseWindow()))
						.AddVariant(RansomPrisonerReject, variant => variant
							.Response(RansomPrisonerRejected)
								.SetExpressions(Expressions.IdleBody.Demure | Expressions.IdleFace.IdleDespise | Expressions.ReactionBody.Unsure)
								.SetConsequence(RansomPrisonerRejectedConsequence)
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
			return Campaign.Current?.CurrentConversationContext == Settings.Instance.PrisonerBarterConversationContext 
				   && _subjectPrisoner != null
			       && _subjectPrisoner != Hero.OneToOneConversationHero 
			       && _subjectPrisoner?.PartyBelongedToAsPrisoner?.MapFaction == Hero.MainHero?.MapFaction;
		}
		private void ProposePrisonerFreeAcceptedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, Hero.MainHero, Settings.Instance.RansomRelationImprovement/2);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, Settings.Instance.RansomRelationImprovement);
		}
		private IEnumerable<Barterable> PrisonerFreeBarterInit()
		{
			if (_subjectPrisoner != null)
			{
				var barterable = new SetPrisonerFreeNewBarterable(_subjectPrisoner, Hero.MainHero, _subjectPrisoner.PartyBelongedToAsPrisoner,
					Hero.OneToOneConversationHero);
				barterable.SetIsOffered(true);
				return new[] { barterable };
			}

			return new Barterable[0];
		}
		private void ProposePrisonerFreeBarterAcceptedConsequence()
		{
		}
		private void ProposePrisonerFreeBarterRejectedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -Settings.Instance.RansomRejectBarterRelationDeterioration);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, Hero.MainHero, -Settings.Instance.RansomRejectBarterRelationDeterioration);
		}
		private void ProposePrisonerFreeRejectedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -Settings.Instance.RansomRejectRelationDeterioration);
			if(Hero.OneToOneConversationHero != _subjectPrisoner)
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.OneToOneConversationHero, Hero.MainHero, -Settings.Instance.RansomRejectRelationDeterioration);
		}
		private bool BegPrisonerFreeCondition()
		{
			return Campaign.Current?.CurrentConversationContext == Settings.Instance.PrisonerBarterConversationContext
				   && _subjectPrisoner != null
			       && Hero.OneToOneConversationHero == _subjectPrisoner;

		}
		private void BegPrisonerFreeAcceptedConsequence()
		{
			new SetPrisonerFreeNewBarterable(_subjectPrisoner, Hero.MainHero, PartyBase.MainParty, Hero.MainHero).Apply();
		}
		private void BegPrisonerFreeBarterAcceptedConsequence()
		{
		}
		private void BegPrisonerFreeBarterRejectedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -Settings.Instance.RansomRejectBarterRelationDeterioration);
		}
		private void BegPrisonerFreeRejectedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -Settings.Instance.RansomRejectRelationDeterioration);
		}
		private bool RansomPrisonerCondition()
		{
			return Campaign.Current?.CurrentConversationContext == Settings.Instance.PrisonerBarterConversationContext
			       && _subjectPrisoner != null
			       && (_subjectPrisoner.MapFaction == Hero.MainHero?.MapFaction);
		}
		private bool RansomPrisonerRequirementAcceptedCondition()
		{
			if (_subjectPrisoner?.PartyBelongedToAsPrisoner?.MapFaction?.IsBanditFaction ?? true)
				return false;
			return (Clan.PlayerClan?.TotalStrength ?? 0) / (Hero.OneToOneConversationHero?.Clan?.TotalStrength ?? 0)
			       > 2f / ((Hero.MainHero?.GetSkillValue(DefaultSkills.Charm) ?? 0) / 100f);
		}
		private void RansomPrisonerRequirementAcceptedConsequence()
		{
			new SetPrisonerFreeNewBarterable(_subjectPrisoner, Hero.OneToOneConversationHero, null, Hero.MainHero).Apply();

		}
		private void RansomPrisonerRequirementRejectedConsequence()
		{
		}
		private IEnumerable<Barterable> RansomPrisonerBarterInit()
		{
			if (_subjectPrisoner != null)
			{
				var barterable = new SetPrisonerFreeNewBarterable(_subjectPrisoner, Hero.OneToOneConversationHero, null, Hero.MainHero);
				barterable.SetIsOffered(true);
				return new[] { barterable };
			}

			return new Barterable[0];
		}
		private void RansomPrisonerBarterAcceptedConsequence()
		{
		}
		private void RansomPrisonerBarterRejectedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -Settings.Instance.RansomRejectBarterRelationDeterioration);
		}
		private void RansomPrisonerRejectedConsequence()
		{
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(_subjectPrisoner, Hero.MainHero, -Settings.Instance.RansomRejectRelationDeterioration);
		}
	}
}
