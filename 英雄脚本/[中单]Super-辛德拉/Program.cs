using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using static Dark_Syndra.Menus;
using EloBuddy.SDK.Menu.Values;

namespace Dark_Syndra
{
    internal class Loader
    {
        

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs bla)
        {
            if (Player.Instance.Hero != Champion.Syndra) return;
            SpellsManager.InitializeSpells();
            Menus.CreateMenu();
            ModeManager.InitializeModes();
            DrawingsManager.InitializeDrawings();
            EventsManager.Initialize();


            Chat.Print("<font color='#FA5858'>Wladis Syndra loaded</font>");
            Chat.Print("Credits to ExRaZor, T2N1Scar, Definitely not Kappa, gero, MarioGK, 2Phones");
        }
    }
}