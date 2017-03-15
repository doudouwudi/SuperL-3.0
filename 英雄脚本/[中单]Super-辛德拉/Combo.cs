using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu.Values;
using System;
using System.Linq;
using static Dark_Syndra.Menus;

namespace Dark_Syndra
{
    internal static class Combo
    {
        public static AIHeroClient myhero
        {
            get { return ObjectManager.Player; }
        }

        private static int lastWCast;

        public static void Execute()
        {
            var sphere =
    ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(a => a.Name == "Seed" && a.IsValid);

            var QEtarget = TargetSelector.GetTarget(SpellsManager.W.Range, DamageType.Magical);
            var target = TargetSelector.GetTarget(SpellsManager.W.Range, DamageType.Magical);

            if ((target == null) || target.IsInvulnerable)
                return;

            if (ComboMenu["QE"].Cast<CheckBox>().CurrentValue && target.IsValidTarget(SpellsManager.QE.Range) && SpellsManager.Q.IsReady() && SpellsManager.E.IsReady())
            {
                var pred = SpellsManager.Q.GetPrediction(QEtarget);
                SpellsManager.Q.Cast(Player.Instance.Position.Extend(pred.CastPosition, SpellsManager.E.Range - 10).To3D());
                SpellsManager.E.Cast(Player.Instance.Position.Extend(pred.CastPosition, SpellsManager.E.Range - 10).To3D());
            }

            if (ComboMenu["Q"].Cast<CheckBox>().CurrentValue)
                if (target.IsValidTarget(SpellsManager.Q.Range) && SpellsManager.Q.IsReady())
                {
                    var pred = SpellsManager.Q.GetPrediction(target);
                    SpellsManager.Q.Cast(pred.CastPosition);
                }

            //Cast W
            if (ComboMenu["W"].Cast<CheckBox>().CurrentValue)
                if (target.IsValidTarget(SpellsManager.W.Range + 100) && SpellsManager.W.IsReady() && !target.IsDead)
            {
                    var pred = SpellsManager.W.GetPrediction(target);

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

            if (myhero.HasBuff("SyndraW"))
            {
                var pred = SpellsManager.W.GetPrediction(target);
                SpellsManager.W.Cast(pred.CastPosition);
            }
            /*if (ComboMenu["QE"].Cast<CheckBox>().CurrentValue)
                if (target.IsValidTarget(SpellsManager.QE.Range) && SpellsManager.E.IsReady())
                    if (Sphere.CountEnemiesInRange(75) >= 1)
                        SpellsManager.Q.Cast(target);
            SpellsManager.E.Cast(target);*/

            if (target.IsValidTarget(SpellsManager.Q.Range) && SpellsManager.E.IsReady())
                if (sphere.CountEnemiesInRange(75) >= 1)
            SpellsManager.E.Cast(target);


            //Cast r
            // if (Menus.ComboMenu["R"].Cast<CheckBox>().CurrentValue)
            //   if (rtarget.IsValidTarget(SpellsManager.R.Range) && SpellsManager.R.IsReady())
            //      SpellsManager.R.TryToCast(SpellsManager.R.GetKillableHero(), Menus.ComboMenu);


            if (ComboMenu[target.ChampionName].Cast<CheckBox>().CurrentValue && SpellsManager.R.IsReady() && target.IsValidTarget(SpellsManager.R.Range) && !target.HasUndyingBuff() &&
                Prediction.Health.GetPrediction(target, SpellsManager.R.CastDelay) <=
                SpellsManager.RDamage(SpellSlot.R, target))
            {
                SpellsManager.R.Cast(target);
            }


            var Summ = TargetSelector.GetTarget(Ignite.Range, DamageType.Mixed);

            if ((Summ == null) || Summ.IsInvulnerable)
                return;
            //Ignite
            if (ComboMenu["Ignite"].Cast<CheckBox>().CurrentValue)
                if (Player.Instance.CountEnemiesInRange(600) >= 1 && Ignite.IsReady() && Ignite.IsLearned && Summ.IsValidTarget(Ignite.Range))
                    if (target.Health >
                  target.GetTotalDamage())
                        Ignite.Cast(Summ);


        }

        public static Spell.Targeted Ignite = new Spell.Targeted(ReturnSlot("summonerdot"), 600);

        public static SpellSlot ReturnSlot(string Name)
        {
            return Player.Instance.GetSpellSlotFromName(Name);
        }
    }
}
    


