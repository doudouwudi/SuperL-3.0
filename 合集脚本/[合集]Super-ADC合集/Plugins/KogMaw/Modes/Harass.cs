#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Harass.cs" company="EloBuddy">
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

namespace Marksman_Master.Plugins.KogMaw.Modes
{
    internal class Harass : KogMaw
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Harass.UseQ && Player.Instance.ManaPercent >= Settings.Harass.MinManaToUseQ)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                if (target != null && !target.HasSpellShield() && !target.HasUndyingBuffA())
                {
                    var qPrediction = Q.GetPrediction(target);

                    if (qPrediction.HitChancePercent > 80)
                        Q.Cast(qPrediction.CastPosition);
                }
            }

            if (W.IsReady() && Settings.Harass.UseW && Player.Instance.ManaPercent >= Settings.Harass.MinManaToUseW && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(W.Range)))
            {
                W.Cast();
            }

            if (R.IsReady() && Settings.Harass.UseR)
            {
                foreach (
                    var enemy in
                        EntityManager.Heroes.Enemies.Where(
                            x => x.IsValidTarget(R.Range) && Settings.Harass.IsHarassEnabledFor(x) && !x.HasSpellShield() && !x.HasUndyingBuffA())
                            .OrderBy(TargetSelector.GetPriority)
                            .ThenByDescending(x => R.GetPrediction(x).HitChancePercent))
                {
                    if(!R.IsReady())
                        break;

                    if (HasKogMawRBuff && GetKogMawRBuff.Count <= Settings.Harass.RAllowedStacks)
                    {
                            var rPrediction = R.GetPrediction(enemy);

                        if (rPrediction.HitChancePercent > 80)
                        {
                            R.Cast(rPrediction.CastPosition);
                        }
                    } else if (!HasKogMawRBuff)
                    {
                        var rPrediction = R.GetPrediction(enemy);

                        if (rPrediction.HitChancePercent > 80)
                        {
                            R.Cast(rPrediction.CastPosition);
                        }
                    }
                }
            }
        }
    }
}
