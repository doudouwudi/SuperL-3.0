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
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Quinn.Modes
{
    internal class Combo : Quinn
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Combo.UseQ)
            {
                var possibleTargets =
                    EntityManager.Heroes.Enemies.Where(
                        x => x.IsValidTarget(Q.Range) && !HasWBuff(x) && !x.HasUndyingBuffA() && !x.HasSpellShield());

                Q.CastIfItWillHit();

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                if ((target != null) && !HasRBuff)
                {
                    Q.CastMinimumHitchance(target, HitChance.High);
                }
            }

            if (W.IsReady() && Settings.Combo.UseW)
            {
                if (EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsUserInvisibleFor(500)).Select(source =>
                    source.GetVisibilityTrackerData())
                    .Any(data => (data.LastHealthPercent < 30) && (data.LastPosition.Distance(Player.Instance) < 2000)))
                {
                    W.Cast();
                }
            }

            if (!E.IsReady() || !Settings.Combo.UseE)
                return;

            var possibleETargets =
                EntityManager.Heroes.Enemies.Where(
                    x => x.IsValidTarget(E.Range) && !HasWBuff(x) && !x.HasUndyingBuffA() && !x.HasSpellShield());

            var eTarget = TargetSelector.GetTarget(possibleETargets, DamageType.Physical);

            if (eTarget == null)
                return;

            if ((eTarget.TotalHealthWithShields() < GetComboDamage(eTarget)) && (eTarget.CountEnemiesInRange(600) <= 2))
            {
                E.Cast(eTarget);
            }
            else if (IsAfterAttack && (eTarget.CountEnemiesInRange(600) <= 1))
            {
                E.Cast(eTarget);
            }
            else if (HasRBuff && (eTarget.CountEnemiesInRange(600) <= 2))
            {
                E.Cast(eTarget);
            }
        }
    }
}