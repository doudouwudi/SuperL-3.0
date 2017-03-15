using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;

namespace Dark_Syndra
{
    internal class Flee
    {
        public static void Execute10()
        {

            if (Menus.FleeMenu["E"].Cast<CheckBox>().CurrentValue)
                if (SpellsManager.Q.IsReady() && SpellsManager.E.IsReady())
                {
                    SpellsManager.Q.Cast(Game.CursorPos);
                }
            if (Menus.FleeMenu["E"].Cast<CheckBox>().CurrentValue)
                if (SpellsManager.E.IsReady())
                {
                    SpellsManager.E.Cast(Game.CursorPos);
                }
        }
    }
}