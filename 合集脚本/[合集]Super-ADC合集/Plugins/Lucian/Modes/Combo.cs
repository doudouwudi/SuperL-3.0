#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Combo.cs" company="EloBuddy">
//
// Marksman Master
// Copyright (C) 2016 by gero
// All rights reserved
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/.
// </copyright>
// <summary>
//
// Email: geroelobuddy@gmail.com
// PayPal: geroelobuddy@gmail.com
// </summary>
// ---------------------------------------------------------------------
#endregion

using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Lucian.Modes
{
    internal class Combo : Lucian
    {
        public static void Execute()
        {
            var qTarget = TargetSelector.GetTarget(925, DamageType.Physical);

            if ((qTarget != null) && PossibleToInterruptQ(qTarget))
            {
                var positionAfterE = Prediction.Position.PredictUnitPosition(qTarget, 300);
                var pos = Player.Instance.Position.Extend(Game.CursorPos, positionAfterE.Distance(Player.Instance) + qTarget.BoundingRadius).To3D();

                if (!pos.IsVectorUnderEnemyTower())
                {
                    E.Cast(pos);
                    return;
                }
            }

            ELogics();

            if (Q.IsReady() && Settings.Combo.UseQ && !IsCastingR && !HasPassiveBuff && !Player.Instance.HasSheenBuff())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                var target2 = TargetSelector.GetTarget(925, DamageType.Physical);

                if(PossibleEqCombo(target) || PossibleEqCombo(target2))
                    return;

                if (!IsPostAttack && (target != null) && Orbwalker.CanAutoAttack)
                {
                    var predictedPosition = Prediction.Position.PredictUnitPosition(target,
                        (int)((Player.Instance.AttackCastDelay + Player.Instance.AttackDelay) * 1000) + Game.Ping / 2);

                    if (Player.Instance.IsInRange(predictedPosition, Player.Instance.GetAutoAttackRange()))
                    {
                        goto WRLogc;
                    }
                }

                if ((target != null) && target.IsValidTarget(Q.Range) &&
                    (((Player.Instance.Mana - QMana > EMana + (R.IsReady() ? RMana : 0)) && !Player.Instance.IsDashing()) ||
                     (Player.Instance.GetSpellDamageCached(target, SpellSlot.Q) + Player.Instance.GetAutoAttackDamageCached(target, true) * 3 > target.TotalHealthWithShields())))
                {
                    Q.Cast(target);
                    return;
                }

                if (Settings.Combo.ExtendQOnMinions && (target2 != null) &&
                    (((Player.Instance.Mana - QMana > EMana + (R.IsReady() ? RMana : 0)) && !Player.Instance.IsDashing()) ||
                     (Player.Instance.GetSpellDamageCached(target2, SpellSlot.Q) +
                      Player.Instance.GetAutoAttackDamageCached(target2, true)*3 > target2.TotalHealthWithShields())) && !Player.Instance.IsDashing())
                {
                    var source = GetQExtendSource(target2);

                    if (source != null)
                    {
                        Q.Cast(source);
                        return;
                    }
                }
            }

            WRLogc:

            if (W.IsReady() && Settings.Combo.UseW && !IsCastingR && !HasPassiveBuff && !Player.Instance.HasSheenBuff())
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);

                if ((target != null) && (((Player.Instance.Mana - WMana > (R.IsReady() ? RMana : 0)) && !Player.Instance.IsDashing()) ||
                    (Player.Instance.GetSpellDamageCached(target, SpellSlot.W) > target.TotalHealthWithShields())))
                {
                    if (Settings.Combo.IgnoreCollisionW)
                    {
                        var orbwalkingTarget = Orbwalker.GetTarget() as AIHeroClient;

                        if (orbwalkingTarget != null)
                        {
                            W.Cast(target);
                            return;
                        }
                    }

                    var wPrediction = W.GetPrediction(target);

                    if (wPrediction.HitChance == HitChance.Medium)
                    {
                        W.Cast(wPrediction.CastPosition);
                        return;
                    }
                }
            }

            if (!R.IsReady() || !Settings.Combo.UseR || Player.Instance.IsUnderTurret())
                return;

            if (Player.Instance.CountEnemiesInRange(Player.Instance.GetAutoAttackRange() + 150) == 0)
            {
                var rTarget = TargetSelector.GetTarget(R.Range - 100, DamageType.Physical);

                if ((rTarget == null) || rTarget.HasUndyingBuffA())
                    return;

                var health = rTarget.TotalHealthWithShields() - IncomingDamage.GetIncomingDamage(rTarget);

                if (health < 0)
                    return;

                int[] shots = { 0, 20, 25, 30 };

                var damage = 0f;
                var singleShot = Damage.GetSingleRShotDamage(rTarget);
                var distance = Player.Instance.Distance(rTarget);

                if (Player.Instance.MoveSpeed >= rTarget.MoveSpeed)
                {
                    damage = singleShot*shots[R.Level];
                }
                else if((rTarget.Path.Last().Length() > 100) && (Player.Instance.MoveSpeed < rTarget.MoveSpeed))
                {
                    var difference = rTarget.MoveSpeed - Player.Instance.MoveSpeed;

                    for (var i = 1; i < shots[R.Level]; i++)
                    {
                        if ((distance > R.Range) || (i >= shots[R.Level]))
                            continue;

                        distance += difference / 1000 * (3000f / shots[R.Level] * i);
                        damage = singleShot* i;
                    }
                }
                if ((damage >= health) && (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name == "LucianR"))
                {
                    R.CastMinimumHitchance(rTarget, 65);
                }
            }
            else if (Player.Instance.CountEnemiesInRange(Player.Instance.GetAutoAttackRange() + 300) == 1)
            {
                var target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange(), DamageType.Physical);

                if ((target == null) || !HasWDebuff(target) || !target.IsFacingB(Player.Instance) || !target.IsMoving ||
                    (target.Distance(Player.Instance) < 200) ||
                    (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name != "LucianR"))
                    return;

                var health = target.TotalHealthWithShields() - IncomingDamage.GetIncomingDamage(target);

                if (health < GetComboDamage(target, 3))
                    return;

                R.CastMinimumHitchance(target, HitChance.High);
            }
        }
    }
}
