using Dark_Syndra;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace Dark_Syndra
{
    internal static class EventsManager
    {
        public static Vector3 SpherePos;
        public static void Initialize()
        {
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.Slot == SpellSlot.Q)
            {
                SpherePos = args.End;
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (Menus.MiscMenu["Gapcloser"].Cast<CheckBox>().CurrentValue)
            if (!sender.IsEnemy) return;

            if (sender.IsValidTarget(SpellsManager.E.Range))
            {
                SpellsManager.E.Cast(sender.Position);
            }
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (Menus.MiscMenu["Interrupt"].Cast<CheckBox>().CurrentValue)
                if (!sender.IsEnemy) return;

            if (e.DangerLevel == DangerLevel.High && sender.IsValidTarget(SpellsManager.E.Range))
            {
                SpellsManager.E.Cast(sender.Position);
            }
        }
    }
}