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
