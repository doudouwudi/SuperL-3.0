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
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Caitlyn.Modes
{
    internal class Harass : Caitlyn
    {
        public static void Execute()
        {
            if (!Settings.Harass.UseQ || !Q.IsReady() || (Player.Instance.ManaPercent < Settings.Harass.MinManaQ) ||
                Player.Instance.Position.IsVectorUnderEnemyTower() || HasAutoAttackRangeBuffOnChamp || Player.Instance.CountEnemiesInRange(BasicAttackRange) != 0)
                return;

            var possibleTargets =
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x => x.IsValidTargetCached(Q.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield());

            var qTarget = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

            if (qTarget != null)
            {
                Q.CastMinimumHitchance(qTarget, HitChance.High);
            }
        }
    }
}
