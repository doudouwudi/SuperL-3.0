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
using EloBuddy.SDK.Events;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Kalista.Modes
{
    internal class Harass : Kalista
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Harass.UseQ && !Player.Instance.IsDashing() &&
                Player.Instance.ManaPercent >= Settings.Harass.MinManaForQ)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                if (target != null)
                {
                    if (!target.HasSpellShield() && !target.HasUndyingBuffA())
                    {
                        var pred = Q.GetPrediction(target);
                        if (pred.HitChancePercent > 85 && pred.CollisionObjects.Length == 0)
                        {
                            Q.Cast(pred.CastPosition);
                        }
                    }
                }
            }

            if (!E.IsReady() || !Settings.Harass.UseE || !(Player.Instance.ManaPercent >= Settings.Harass.MinManaForE))
                return;

            var enemy = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                x => x.IsValidTargetCached(E.Range) && Damage.HasRendBuff(x) &&
                     (Damage.CountEStacks(x) > Settings.Harass.MinStacksForE));

            if (enemy == null)
                return;
            
            if (Settings.Harass.UseEIfManaWillBeRestored &&
                StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions, x => x.IsValidTargetCached(E.Range) && Damage.IsTargetKillableByRend(x) && 
                (Prediction.Health.GetPrediction(x, 250) > 15)).Count() >= 2)
            {
                E.Cast();
            }
            else if (
                StaticCacheProvider.GetMinions(CachedEntityType.CombinedAttackableMinions,
                    x => x.IsValidTargetCached(E.Range) && Damage.IsTargetKillableByRend(x) &&
                         (Prediction.Health.GetPrediction(x, 250) > 15)).Any())
            {
                E.Cast();
            }
        }
    }
}
