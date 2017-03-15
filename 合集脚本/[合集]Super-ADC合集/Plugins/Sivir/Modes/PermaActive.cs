#region Licensing
// ---------------------------------------------------------------------
// <copyright file="PermaActive.cs" company="EloBuddy">
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

namespace Marksman_Master.Plugins.Sivir.Modes
{
    using System.Linq;
    using EloBuddy;
    using EloBuddy.SDK;
    using Utils;

    internal class PermaActive : Sivir
    {
        public static void Execute()
        {
            if (!IsPreAttack && Q.IsReady() && Settings.Harass.AutoHarass)
            {
                foreach (var immobileEnemy in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x =>
                {
                    if (!x.IsValidTargetCached(Q.Range) || !x.IsImmobile())
                        return false;

                    var immobileDuration = x.GetMovementBlockedDebuffDuration();
                    var eta = x.DistanceCached(Player.Instance)/Q.Speed + .25f;

                    return immobileDuration > eta;
                })
                    .OrderByDescending(TargetSelector.GetPriority)
                    .Select(x => Q.GetPrediction(x))
                    .OrderByDescending(x => x.HitChancePercent))
                {
                    Q.Cast(immobileEnemy.CastPosition);
                    break;
                }
            }

            if (IsPreAttack || !Q.IsReady() || !Settings.Combo.UseQ)
                return;

            foreach (var qPrediction in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                x =>
                    x.IsValidTargetCached(Q.Range) &&
                    (x.TotalHealthWithShields() - IncomingDamage.GetIncomingDamage(x) <
                     Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)))
                .Select(target => Q.GetPrediction(target))
                .Where(qPrediction => qPrediction.HitChancePercent >= 60))
            {
                Q.Cast(qPrediction.CastPosition);
                break;
            }
        }
    }
}