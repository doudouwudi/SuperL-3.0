using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using static Dark_Syndra.Menus;

namespace Dark_Syndra
{
    internal class ModeManager
    {
        public static void InitializeModes()
        {
            Game.OnTick += Game_OnTick;
        }

        private static void Game_OnTick(EventArgs args)
        {
            var orbMode = Orbwalker.ActiveModesFlags;
            var playerMana = Player.Instance.ManaPercent;


            if (orbMode.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo.Execute();

            if (orbMode.HasFlag(Orbwalker.ActiveModes.Harass)&& (playerMana > HarassMenu["manaSlider"].Cast<Slider>().CurrentValue))
                Harass.Execute1();
            
            if (orbMode.HasFlag(Orbwalker.ActiveModes.Flee))
                Flee.Execute10();

            if (orbMode.HasFlag(Orbwalker.ActiveModes.LaneClear) && (playerMana > LaneClearMenu["manaSlider"].Cast<Slider>().CurrentValue))
                LaneClear.Execute2();

            if (HarassMenu["AutoQ"].Cast<CheckBox>().CurrentValue && (playerMana > HarassMenu["manaSlider"].Cast<Slider>().CurrentValue))
                AutoHarass.Execute6();

            if (HarassMenu["AutoW"].Cast<CheckBox>().CurrentValue && (playerMana > HarassMenu["manaSlider"].Cast<Slider>().CurrentValue))
                AutoHarass.Execute7();

            if (KillStealMenu["Q"].Cast<CheckBox>().CurrentValue)
                KillSteal.Execute2();

            if (KillStealMenu["W"].Cast<CheckBox>().CurrentValue)
                KillSteal.Execute3();

            if (KillStealMenu["E"].Cast<CheckBox>().CurrentValue)
                KillSteal.Execute4();

            if (KillStealMenu["R"].Cast<CheckBox>().CurrentValue)
                KillSteal.Execute5();

            if (orbMode.HasFlag(Orbwalker.ActiveModes.JungleClear))
                LaneClear.JungleClear();


        }
    }
}