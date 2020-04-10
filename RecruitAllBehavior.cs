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
                    "Recruit all the troops available",
                    GameMenuVillageRecruitTroopsOnCondition, RecruitAllAvailableTroops,
                    false, 3);

                starter.AddGameMenuOption("town", "town_j_recruit_all_button",
                    "Recruit all the troops available",
                    GameMenuTownRecruitTroopsOnCondition, RecruitAllAvailableTroops,
                    false, 7);

                starter.AddGameMenuOption("town_backstreet", "town_backstreet_j_hire_one_merc",
                    "Hire one {JMERCNAME} ({JONEMERCCOST}{GOLD_ICON})",
                    GameMenuTavernHireOneMercOnCondition, GameMenuTavernHireOneMercOnConsequnce,
                    false, 1);

                starter.AddGameMenuOption("town_backstreet", "town_backstreet_j_hire_all_merc",
                    "Hire all {JMERCNAME}s ({JALLMERCCOST}{GOLD_ICON})",
                    GameMenuTavernHireAllMercOnCondition, GameMenuTavernHireAllMercOnConsequnce,
                    false, 2);
            }
            catch (Exception ex)
            {
                NativeMethods.MessageBox(IntPtr.Zero, ex.Message, "RecruitAllButton -- OnSessionLaunched", NativeMethods.MB_ICONERROR | NativeMethods.MB_OK);
            }
        }

        private static int GetMercenariesCount() => PlayerEncounter.Settlement.Town.MercenaryData.Number;

        private static int GetMercenaryCost() =>
            Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(
                PlayerEncounter.Settlement.Town.MercenaryData.TroopType, Hero.MainHero, false);

        private static bool GameMenuTavernHireOneMercOnCondition(MenuCallbackArgs args)
        {
            RefreshMercenaryData();
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            return GetMercenariesCount() > 0 && GetMercenaryCost() <= Hero.MainHero.Gold;
        }

        private static void GameMenuTavernHireOneMercOnConsequnce(MenuCallbackArgs args)
        {
            RecruitMercenaries(1);
            RefreshMercenaryData();
            GameMenu.SwitchToMenu("town_backstreet");
        }

        private static bool GameMenuTavernHireAllMercOnCondition(MenuCallbackArgs args)
        {
            RefreshMercenaryData();
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            return GetMercenariesCount() > 1 && GetMercenaryCost() * GetMercenariesCount() <= Hero.MainHero.Gold;
        }

        private static void GameMenuTavernHireAllMercOnConsequnce(MenuCallbackArgs args)
        {
            RecruitMercenaries(GetMercenariesCount());
            RefreshMercenaryData();
            GameMenu.SwitchToMenu("town_backstreet");
        }

        private static void RecruitMercenaries(int count)
        {
            try
            {
                var cost = GetMercenaryCost();
                MobileParty.MainParty.AddElementToMemberRoster(PlayerEncounter.Settlement.Town.MercenaryData.TroopType, count,
                    false);
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, count * cost, true);
                CampaignEventDispatcher.Instance.OnUnitRecruited(PlayerEncounter.Settlement.Town.MercenaryData.TroopType, count);
                PlayerEncounter.Settlement.Town.MercenaryData.ChangeMercenaryCount(-count);

                if (count == 1)
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"You recruited one {PlayerEncounter.Settlement.Town.MercenaryData.TroopType.Name}."));
                else
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"You recruited {count} {PlayerEncounter.Settlement.Town.MercenaryData.TroopType.Name}s."));

                MBTextManager.SetTextVariable("GOLD_AMOUNT", count * cost, false);
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_gold_removed_with_icon", null).ToString(), "event:/ui/notification/coins_negative"));
            }
            catch (Exception ex)
            {
                NativeMethods.MessageBox(IntPtr.Zero, ex.Message, "RecruitAllButton -- RecruitMercenaries", NativeMethods.MB_ICONERROR | NativeMethods.MB_OK);
            }
        }

        private static void RefreshMercenaryData()
        {
            try
            {
                MBTextManager.SetTextVariable("JONEMERCCOST", GetMercenaryCost(), false);
                MBTextManager.SetTextVariable("JALLMERCCOST", GetMercenaryCost() * GetMercenariesCount(), false);
                MBTextManager.SetTextVariable("JMERCNAME", PlayerEncounter.Settlement.Town.MercenaryData.TroopType.Name, false);
                //MBTextManager.SetTextVariable("JMERCNAMEPL", PlayerEncounter.Settlement.Town.MercenaryData.TroopType.Name + "s", false);
            }
            catch (Exception ex)
            {
                NativeMethods.MessageBox(IntPtr.Zero, ex.Message, "RecruitAllButton -- RefreshMercenaryData", NativeMethods.MB_ICONERROR | NativeMethods.MB_OK);
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
