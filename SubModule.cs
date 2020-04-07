using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RecruitAllButton
{
    public class RecruitAllBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("village", "village_j_recruit_all_button",
                    "Recruit all available troops",
                    args => Settlement.CurrentSettlement.Village.VillageState == Village.VillageStates.Normal &&
                            Settlement.CurrentSettlement.GetNumberOfAvailableRecruits() > 0, RecruitAllAvailableTroops,
                    false, 3);

                starter.AddGameMenuOption("town", "town_j_recruit_all_button",
                    "Recruit all available troops",
                    GameMenuTownRecruitTroopsOnCondition, RecruitAllAvailableTroops,
                    false, 7);
            }
            catch (Exception ex)
            {
                NativeMethods.MessageBox(IntPtr.Zero, ex.Message, "RecruitAllButton -- OnSessionLaunched", 0);
            }
        }

        private static bool GameMenuTownRecruitTroopsOnCondition(MenuCallbackArgs args)
        {
            var canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.RecruitTroops, out var shouldBeDisabled, out var disabledText);
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
        }

        private static void RecruitAllAvailableTroops(MenuCallbackArgs args)
        {
            try
            {
                var minusMoney = 0;
                foreach (var notable in Settlement.CurrentSettlement.Notables)
                {
                    for (var i = 0; i < 6; ++i)
                    {
                        if (notable.VolunteerTypes[i] == null ||
                            !HeroHelper.HeroCanRecruitFromHero(Hero.MainHero, notable, i)) continue;

                        var troop = notable.VolunteerTypes[i];
                        var cost = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(troop, Hero.MainHero, false);

                        if (cost > Hero.MainHero.Gold) continue;
                        minusMoney += cost;
                        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, cost, true);
                        notable.VolunteerTypes[i] = null;
                        MobileParty.MainParty.MemberRoster.AddToCounts(Hero.MainHero.CharacterObject, 1, false, 0, 0, true, -1);
                        CampaignEventDispatcher.Instance.OnUnitRecruited(Hero.MainHero.CharacterObject, 1);

                        InformationManager.DisplayMessage(new InformationMessage($"You recruited one {troop.Name}."));
                    }
                }

                MBTextManager.SetTextVariable("GOLD_AMOUNT", minusMoney, false);
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_gold_removed_with_icon", null).ToString(), "event:/ui/notification/coins_negative"));
            }
            catch (Exception ex)
            {
                NativeMethods.MessageBox(IntPtr.Zero, ex.Message, "RecruitAllButton -- RecruitAllAvailableTroops", 0);
            }
            //var sb = new StringBuilder();
        }

        public override void SyncData(IDataStore dataStore) { }
    }

    public class SubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            try
            {
                if (!(game.GameType is Campaign)) return;
                if (!(gameStarterObject is CampaignGameStarter gameInitializer)) return;
                gameInitializer.AddBehavior(new RecruitAllBehavior());
            }
            catch (Exception ex)
            {
                NativeMethods.MessageBox(IntPtr.Zero, ex.Message, "RecruitAllButton -- OnGameStart", 0);
            }
        }
    }
}
