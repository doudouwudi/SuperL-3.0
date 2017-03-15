using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using System;

namespace Dark_Syndra
{
    internal static class Harass
    {

        private static int lastWCast;

        public static void Execute1()
        {

            var qtarget = TargetSelector.GetTarget(SpellsManager.Q.Range, DamageType.Magical);

            if ((qtarget == null) || qtarget.IsInvulnerable)
                return;
            //Cast Q
            if (Menus.HarassMenu["Q"].Cast<CheckBox>().CurrentValue)
                if (qtarget.IsValidTarget(SpellsManager.Q.Range) && SpellsManager.Q.IsReady())
                    SpellsManager.Q.Cast(qtarget);

            var wtarget = TargetSelector.GetTarget(SpellsManager.W.Range, DamageType.Magical);

            if ((wtarget == null) || wtarget.IsInvulnerable)
                return;
            //Cast W
            if (Menus.HarassMenu["W"].Cast<CheckBox>().CurrentValue)
                if (wtarget.IsValidTarget(SpellsManager.W.Range) && SpellsManager.W.IsReady())
                {
                    var pred = SpellsManager.W.GetPrediction(wtarget);

                    if (Player.Instance.Spellbook.GetSpell(SpellSlot.W).ToggleState != 2 &&
                        lastWCast + 700 < Environment.TickCount)
                    {
                        SpellsManager.W.Cast(Functions.GrabWPost(true));
                        lastWCast = Environment.TickCount;
                    }
                    if (Player.Instance.Spellbook.GetSpell(SpellSlot.W).ToggleState >= 1 &&
                        lastWCast + 300 < Environment.TickCount)
                    {
                        SpellsManager.W.Cast(pred.CastPosition);
                    }
                }
            
        }



        /*
        //Summoners Target
        var Summ1 = TargetSelector.GetTarget(Smite.Range, DamageType.Mixed);
        var Summ2 = TargetSelector.GetTarget(Ignite.Range, DamageType.Mixed);
        if ((Summ1 == null) || Summ1.IsInvulnerable)
            return;
        //Cast Smite
        if (ComboMenu["Smite"].Cast<CheckBox>().CurrentValue)
            if (Summ1.IsValidTarget(Smite.Range) && Smite.IsReady())
                Smite.Cast(Smite.GetKillableHero());
        if ((Summ2 == null) || Summ2.IsInvulnerable)
            return;
        //Cast Ignite
        if (ComboMenu["Ignite"].Cast<CheckBox>().CurrentValue)
            if (Summ2.IsValidTarget(Ignite.Range) && Ignite.IsReady())
                Ignite.Cast(Ignite.GetKillableHero());
        */
    }
}