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
using EloBuddy.SDK.Events;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Kalista.Modes
{
    internal class Combo : Kalista
    {
        public static void Execute()
        {
            if (Settings.Combo.JumpOnMinions && Orbwalker.CanAutoAttack)
            {
                var target = TargetSelector.GetTarget(1500, DamageType.Physical);

                if (target != null && !Player.Instance.IsInAutoAttackRange(target))
                {
                    var minion =
                        StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions,
                            unit => Player.Instance.IsInRangeCached(unit, Player.Instance.GetAutoAttackRange()))
                            .FirstOrDefault();

                    if (minion != null)
                    {
                        Orbwalker.ForcedTarget = minion;
                    }
                }
            }

            if (E.IsReady() && Settings.Combo.UseE)
            {
                var enemiesWithRendBuff =
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        unit => unit.IsValid && unit.IsValidTargetCached(E.Range) && Damage.HasRendBuff(unit)).Count();

                if(enemiesWithRendBuff == 0)
                    return;

                if (Settings.Combo.UseEToSlow)
                {
                    var count =
                        StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions,
                            unit => unit.IsValid && unit.IsValidTargetCached(E.Range) && Damage.IsTargetKillableByRend(unit) && (Prediction.Health.GetPrediction(unit, 250) > 15)).Count();

                    if (count >= Settings.Combo.UseEToSlowMinMinions)
                    {
                        Misc.PrintDebugMessage("Casting E to slow.");
                        E.Cast();
                    }
                }

                if (Settings.Combo.UseEBeforeEnemyLeavesRange && enemiesWithRendBuff == 1)
                {
                    var enemyUnit = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero).ToList().Find(unit => !unit.IsDead && unit.IsValid && unit.IsValidTargetCached(E.Range) && Damage.HasRendBuff(unit));

                    if (enemyUnit != null && Damage.CanCastEOnUnit(enemyUnit) && enemyUnit.DistanceCached(Player.Instance) > E.Range - 100)
                    {
                        var percentDamage = Damage.GetRendDamageOnTarget(enemyUnit) /enemyUnit.TotalHealthWithShields()*100;

                        if (percentDamage >= Settings.Combo.MinDamagePercToUseEBeforeEnemyLeavesRange)
                        {
                            E.Cast();
                            Misc.PrintDebugMessage($"Casting E cause it will deal {percentDamage} percent of enemy hp.");
                        }
                    }
                }

                if (Settings.Combo.UseEBeforeDeath && Player.Instance.HealthPercent < 5 && IncomingDamage.GetIncomingDamage(Player.Instance) > Player.Instance.Health)
                {
                    E.Cast();
                    Misc.PrintDebugMessage("Casting E before death.");
                }
            }

            if (!Q.IsReady() || !Settings.Combo.UseQ || (Player.Instance.Mana - (50 + 5 * (Q.Level - 1)) < 45))
                return;

            var possibleTargets =
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(Q.Range) && !x.HasSpellShield() && !x.HasUndyingBuffA());

            var hero = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

            if (hero == null || Player.Instance.IsDashing())
                return;

            Q.CastMinimumHitchance(hero, 60);
        }
    }
}
