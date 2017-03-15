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

namespace Marksman_Master.Plugins.Twitch.Modes
{
    internal class Harass : Twitch
    {
        public static void Execute()
        {
            if (W.IsReady() && Settings.Harass.UseW && Player.Instance.ManaPercent >= Settings.Harass.MinManaToUseW)
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                if (target != null && !target.HasSpellShield() && Damage.CountEStacks(target) <= 4)
                {
                    var pred = W.GetPrediction(target);

                    if (pred.HitChancePercent >= 70)
                    {
                        W.Cast(pred.CastPosition);
                    }
                }
            }
            if (E.IsReady() && Settings.Harass.UseE && Player.Instance.ManaPercent >= Settings.Harass.EMinMana)
            {
                if (Settings.Harass.TwoEnemiesMin)
                {
                    var count =
                        StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                            unit => !unit.IsZombie && unit.IsValidTargetCached(E.Range) && HasDeadlyVenomBuff(unit)).Count();

                    if (count >= 2 &&
                         StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                            unit => !unit.IsZombie && unit.IsValidTargetCached(E.Range) &&
                                Damage.CountEStacks(unit) >= Settings.Harass.EMinStacks).Any())
                    {
                        E.Cast();
                    }
                }
                else
                {
                    if (StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                            unit => !unit.IsZombie && unit.IsValidTargetCached(E.Range) &&
                                Damage.CountEStacks(unit) >= Settings.Harass.EMinStacks).Any())
                    {
                        E.Cast();
                    }
                }
            }
        }
    }
}
