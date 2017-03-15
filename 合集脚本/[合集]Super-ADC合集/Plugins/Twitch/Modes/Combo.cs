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
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Twitch.Modes
{
    internal class Combo : Twitch
    {
        public static void Execute()
        {
            if (R.IsReady() && Settings.Combo.UseR && Settings.Combo.RifTargetOutOfRange && !E.IsReady() && (Player.Instance.Spellbook.GetSpell(SpellSlot.E).CooldownExpires - Game.Time > 2))
            {
                var enemy = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).FirstOrDefault(x =>
                    !x.IsDead && x.IsValidTargetCached(850) &&
                    (x.DistanceCached(Player.Instance) > Player.Instance.GetAutoAttackRange()) &&
                    (x.Health <
                     Player.Instance.CalculateDamageOnUnit(x, DamageType.Physical,
                         Player.Instance.TotalAttackDamage + Damage.RBonusAd[R.Level], false, true)*2));

                if ((enemy != null) && enemy.IsValidTargetCached(750) && (enemy.Health > Damage.GetPassiveDamage(enemy)) &&
                    Orbwalker.CanAutoAttack &&
                    (enemy.Health + Player.Instance.CalculateDamageOnUnit(enemy, DamageType.Physical,
                        Player.Instance.TotalAttackDamage + Damage.RBonusAd[R.Level], false, true) >
                     IncomingDamage.GetIncomingDamage(enemy)))
                {
                    Misc.PrintInfoMessage("Casting R to kill <font color=\"#ff1493\">" + enemy.Hero + "</font>.");
                    R.Cast();
                }
            }

            if (W.IsReady() && Settings.Combo.UseW && !(Settings.Combo.BlockWIfRIsActive && IsCastingR))
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Physical, Player.Instance.Position);

                if (target != null)
                {
                    var prediction = W.GetPrediction(target);
                    if (prediction.HitChancePercent > 50)
                    {
                        W.Cast(prediction.CastPosition);
                    }
                }
            }

            if (E.IsReady() && Settings.Combo.UseE)
            {
                if (Settings.Combo.EMode == 0) // percentage
                {
                    var enemyUnit = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).ToList().Find(unit => !unit.IsDead && unit.IsValidTargetCached(E.Range) && HasDeadlyVenomBuff(unit));

                    if ((enemyUnit != null) && Damage.CanCastEOnUnit(enemyUnit))
                    {
                        var percentDamage = Damage.GetEDamage(enemyUnit) / enemyUnit.TotalHealthWithShields() * 100;
                        if (percentDamage >= Settings.Combo.EAt)
                        {
                            E.Cast();
                            Misc.PrintDebugMessage($"Casting E cause it will deal {percentDamage} percent of enemy hp.");
                        }
                    }
                }
                if (Settings.Combo.EMode == 1) // at stacks
                {
                    if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                            unit => !unit.IsZombie && unit.IsValidTargetCached(E.Range) && HasDeadlyVenomBuff(unit) &&
                                (Damage.CountEStacks(unit) >= Settings.Combo.EAt)).Any())
                    {
                        E.Cast();
                    }
                }
            }
        }
    }
}
