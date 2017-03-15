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

using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Caitlyn.Modes
{
    internal class PermaActive : Caitlyn
    {
        public static void Execute()
        {
            if (Settings.Misc.EnableKillsteal && (Player.Instance.CountEnemyHeroesInRangeWithPrediction((int)Player.Instance.GetAutoAttackRange(), 1000) <= 1) && !IsPreAttack && Q.IsReady() && !Player.Instance.Position.IsVectorUnderEnemyTower())
            {
                foreach (
                    var target in
                        StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, 
                            x =>
                                x.IsValidTargetCached(Q.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield() &&
                                (x.TotalHealthWithShields() < Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)) &&
                                !((x.TotalHealthWithShields() < Player.Instance.GetAutoAttackDamageCached(x, true)) && Player.Instance.IsInAutoAttackRange(x))))
                {
                    Q.CastMinimumHitchance(target, 60);
                    break;
                }
            }

            if (!Settings.Combo.UseWOnImmobile || !W.IsReady())
                return;

            var immobileEnemies =
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x =>
                        x.IsValidTargetCached(W.Range) && !x.HasSpellShield() &&
                        (x.GetMovementBlockedDebuffDuration() > 1.5f)).ToList();

            foreach (var immobileEnemy in immobileEnemies)
            {
                if (!IsValidWCast(immobileEnemy.ServerPosition, 200, 0))
                    continue;

                W.Cast(immobileEnemy.ServerPosition);
                return;
            }

            var ga =
                ObjectManager.Get<Obj_GeneralParticleEmitter>()
                    .Where(
                        x =>
                            x.Name == "LifeAura.troy")
                    .ToList();

            if (ga.Any())
            {
                foreach (var owner in ga.Select(objGeneralParticleEmitter => StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => x.DistanceCached(objGeneralParticleEmitter) < 20).FirstOrDefault()).Where(owner => owner != null))
                {
                    if (!IsValidWCast(owner.ServerPosition, 200, 0))
                        continue;

                    W.Cast(owner.ServerPosition);
                    break;
                }
            }

            foreach (
                var enemy in
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x =>
                            x.IsValidTargetCached(W.Range) && x.Buffs.Any(
                                m => m.Name.Equals("zhonyasringshield", StringComparison.CurrentCultureIgnoreCase) ||
                                     m.Name.Equals("bardrstasis", StringComparison.CurrentCultureIgnoreCase))))
            {
                var buffTime =
                    enemy.Buffs.FirstOrDefault(
                        m => m.Name.Equals("zhonyasringshield", StringComparison.CurrentCultureIgnoreCase) ||
                             m.Name.Equals("bardrstasis", StringComparison.CurrentCultureIgnoreCase));

                if ((buffTime == null) || (buffTime.EndTime - Game.Time < 1.25f) || !IsValidWCast(enemy.ServerPosition, 200, 0))
                    continue;

                W.Cast(enemy.ServerPosition);
                break;
            }
        }
    }
}