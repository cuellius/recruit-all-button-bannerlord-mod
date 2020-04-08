using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RecruitAllButton
{
    public class RecruitAllBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                starter.AddGameMenuOption("village", "village_j_recruit_all_button",
                    "Recruit all available troops",
                    GameMenuVillageRecruitTroopsOnCondition, RecruitAllAvailableTroops,
                    false, 3);

                starter.AddGameMenuOption("town", "town_j_recruit_all_button",
                    "Recruit all available troops",
                    GameMenuTownRecruitTroopsOnCondition, RecruitAllAvailableTroops,
                    false, 7);
            }
            catch (Exception ex)
            {
                NativeMethods.MessageBox(IntPtr.Zero, ex.Message, "RecruitAllButton -- OnSessionLaunched", NativeMethods.MB_ICONERROR | NativeMethods.MB_OK);
            }
        }

        private static bool GameMenuTownRecruitTroopsOnCondition(MenuCallbackArgs args)
        {
            var canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.RecruitTroops, out var shouldBeDisabled, out var disabledText);
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
        }

        private static bool GameMenuVillageRecruitTroopsOnCondition(MenuCallbackArgs args)
        {
            var canPlayerDo = Settlement.CurrentSettlement.Village.VillageState == Village.VillageStates.Normal &&
                              Settlement.CurrentSettlement.GetNumberOfAvailableRecruits() > 0;
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            return canPlayerDo;
        }

        private static void RecruitAllAvailableTroops(MenuCallbackArgs args)
        {
            try
            {
                var minusMoney = 0;
                var recruitedAnyTroop = false;
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
                        recruitedAnyTroop = true;
                        GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, cost, true);
                        notable.VolunteerTypes[i] = null;
                        MobileParty.MainParty.MemberRoster.AddToCounts(troop, 1, false, 0, 0, true, -1);
                        CampaignEventDispatcher.Instance.OnUnitRecruited(troop, 1);

                        InformationManager.DisplayMessage(new InformationMessage($"You recruited one {troop.Name}."));
                    }
                }

                if (recruitedAnyTroop)
                {
                    MBTextManager.SetTextVariable("GOLD_AMOUNT", minusMoney, false);
                    InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_gold_removed_with_icon", null).ToString(), "event:/ui/notification/coins_negative"));
                }
            }
            catch (Exception ex)
            {
                NativeMethods.MessageBox(IntPtr.Zero, ex.Message, "RecruitAllButton -- RecruitAllAvailableTroops", NativeMethods.MB_ICONERROR | NativeMethods.MB_OK);
            }
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
