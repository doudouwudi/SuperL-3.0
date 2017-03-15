using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;

namespace Dark_Syndra
{

    internal static class SpellsManager
    {
        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Skillshot E;
        public static Spell.Targeted R;
        public static Spell.Skillshot QE;
        public static List<Spell.SpellBase> SpellList = new List<Spell.SpellBase>();
        
        public static void InitializeSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 820, SkillShotType.Circular, 550, int.MaxValue, 125)
            {
                AllowedCollisionCount = int.MaxValue
            };
            W = new Spell.Skillshot(SpellSlot.W, 950, SkillShotType.Circular, 350, 1500, 130)
            {
                AllowedCollisionCount = int.MaxValue
            };
            E = new Spell.Skillshot(SpellSlot.E, 670, SkillShotType.Cone, 250, 2500, 50)
            {
                AllowedCollisionCount = int.MaxValue
            };
            R = new Spell.Targeted(SpellSlot.R, 675);

            QE = new Spell.Skillshot(SpellSlot.E, 1150, SkillShotType.Linear, 600, 2400, 18)
            {
                AllowedCollisionCount = int.MaxValue
            };
            Obj_AI_Base.OnLevelUp += AutoLevel.Obj_AI_Base_OnLevelUp;
        }

        #region Damages

        public static float GetTotalDamage(this Obj_AI_Base target, SpellSlot slot)
        {
            var damageType = DamageType.Magical;
            var ap = Player.Instance.FlatMagicDamageMod;
            var sLevel = Player.GetSpell(slot).Level - 1;

            var dmg = 0f;

            switch (slot)
            {
                case SpellSlot.Q:
                    if (Q.IsReady())
                        dmg += new float[] { 50, 95, 140, 185, 230 }[sLevel] + 0.75f * ap;
                    break;
                case SpellSlot.W:
                    if (W.IsReady())
                        dmg += new float[] { 80, 120, 160, 200, 240 }[sLevel] + 0.8f * ap;
                    break;
                case SpellSlot.E:
                    if (E.IsReady())
                        dmg += new float[] { 60 , 105 , 150 , 195 , 240 }[sLevel] + 0.6f * ap;
                    break;
                        case SpellSlot.R:
                         if (R.IsReady())
                         dmg += new float[] { 300, 400, 500 }[sLevel] + 0.6f * ap * (BallsCount());
                        break;
            }
            return Player.Instance.CalculateDamageOnUnit(target, damageType, dmg - 10);

        }

        public static float RDamage(SpellSlot r, AIHeroClient rtarget)
        {

            var ap = Player.Instance.FlatMagicDamageMod;
            var index = Player.GetSpell(SpellSlot.R).Level - 1;
            var mindmg = new float[] { 270, 405, 540 }[index] + 0.6f * ap;
            var maxdmg = new float[] { 630, 975, 1260 }[index] + 1.4f * ap;
            var perballdmg = (new float[] { 90, 135, 180 }[index] + 0.2f * ap) * BallsCount();

            return Player.Instance.CalculateDamageOnUnit(rtarget, DamageType.Magical, Math.Min(mindmg, maxdmg) + perballdmg);
        }
        public static int BallsCount()
        {
            return ObjectManager.Get<Obj_AI_Base>().Count(a => a.Name == "Seed" && a.IsValid);
        }

        public static float GetTotalDamage(this Obj_AI_Base target)
        {
            var slots = new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
            var dmg = Player.Spells.Where(s => slots.Contains(s.Slot)).Sum(s => target.GetTotalDamage(s.Slot));

            return dmg;
        }
    }
    }
    #endregion damages
    // internal static float GetDamage(Spell.Targeted r1, SpellSlot r2, AIHeroClient target)
    // {
    //    var damageType = DamageType.Magical;
    //   var ap = Player.Instance.FlatMagicDamageMod;
    //   var sLevel = Player.GetSpell(SpellSlot.R).Level - 1;

    //    var dmg = 0f;

    //    switch (SpellSlot.R)
    //    {
    //       case SpellSlot.R:
    //         if (R.IsReady())
    //            dmg += new float[] { 270, 405, 540 }[sLevel] + 0.6f * ap * (Functions.SpheresCount());
    //        break;

//  return Player.Instance.CalculateDamageOnUnit(target, damageType, dmg - 10);