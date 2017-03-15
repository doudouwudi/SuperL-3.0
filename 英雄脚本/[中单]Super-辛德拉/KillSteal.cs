using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using static Dark_Syndra.Menus;

namespace Dark_Syndra
{
    class KillSteal
    {
        public static void Execute2()
        {
            var qtarget = TargetSelector.GetTarget(SpellsManager.Q.Range, DamageType.Magical);

            if ((qtarget == null) || qtarget.IsInvulnerable)
                return;
            //Cast Q
            if (SpellsManager.Q.IsReady() && qtarget.IsValidTarget((SpellsManager.Q.Range)) &&
                Prediction.Health.GetPrediction(qtarget, SpellsManager.Q.CastDelay) <=
                SpellsManager.GetTotalDamage(qtarget, SpellSlot.Q))
            {
                SpellsManager.Q.Cast(qtarget);
            }
        }

        public static void Execute3()
        {

        }

        public static void Execute4()
        {
            var etarget = TargetSelector.GetTarget(SpellsManager.E.Range, DamageType.Magical);

            if ((etarget == null) || etarget.IsInvulnerable)
                return;
            //Cast Q
                if (SpellsManager.E.IsReady() && etarget.IsValidTarget((SpellsManager.E.Range)) &&
                    Prediction.Health.GetPrediction(etarget, SpellsManager.E.CastDelay) <=
                    SpellsManager.GetTotalDamage(etarget, SpellSlot.E))
            {
                SpellsManager.E.Cast(etarget);
            }
        }

        public static void Execute5()
        {
            var rtarget = TargetSelector.GetTarget(SpellsManager.R.Range, DamageType.Magical);

            if ((rtarget == null) || rtarget.IsInvulnerable)
                return;


            if (SpellsManager.R.IsReady() && rtarget.IsValidTarget(SpellsManager.R.Range) &&
                Prediction.Health.GetPrediction(rtarget, SpellsManager.R.CastDelay) <=
                SpellsManager.RDamage(SpellSlot.R, rtarget))
            {
                SpellsManager.R.Cast(rtarget);
            }

        }
    }
}

