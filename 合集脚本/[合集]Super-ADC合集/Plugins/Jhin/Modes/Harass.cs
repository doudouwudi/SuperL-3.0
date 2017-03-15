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
using EloBuddy.SDK.Enumerations;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Jhin.Modes
{
    internal class Harass : Jhin
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Harass.UseQ && Player.Instance.ManaPercent >= Settings.Harass.MinManaQ)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                if (target != null && !target.HasSpellShield() && !target.HasUndyingBuffA())
                {
                    Q.Cast(target);
                }
            }

            if (!W.IsReady() || !Settings.Harass.UseW || !(Player.Instance.ManaPercent >= Settings.Harass.MinManaW) ||
                Player.Instance.CountEnemiesInRangeCached(500) >= 2)
                return;
            
            var enemy = TargetSelector.GetTarget(W.Range, DamageType.Physical);

            if (enemy == null || enemy.HasSpellShield() || enemy.HasUndyingBuffA())
                return;

            var count =
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => Prediction.Position.PredictUnitPosition(x, 1000).DistanceCached(Player.Instance) < 400).Count();
            if (count >= 2)
                return;

            var wPrediction = W.GetPrediction(enemy);

            if (wPrediction.HitChance >= HitChance.High)
            {
                W.Cast(wPrediction.CastPosition);
            }
        }
    }
}
