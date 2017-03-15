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

namespace Marksman_Master.Plugins.Corki.Modes
{
    internal class Combo : Corki
    {
        public static void Execute()
        {
            if (W.IsReady() && Settings.Combo.UseW && !HasPackagesBuff && (Player.Instance.CountEnemiesInRange(1500) == 1) && (Player.Instance.Mana > QMana[Q.Level] + WMana + EMana + RMana))
            {
                var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x =>
                        x.IsValidTargetCached(W.Range) && !x.HasUndyingBuffA() && !x.Position.IsVectorUnderEnemyTower() &&
                        (x.HealthPercent < Player.Instance.HealthPercent) && (x.Health < Damage.GetComboDamage(x, 2, 2)));

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

                if (target != null)
                {
                    W.Cast(Player.Instance.Position.Extend(target, 580).To3D());
                    return;
                }
            }

            if (Q.IsReady() && Settings.Combo.UseQ)
            {
                var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => x.IsValidTargetCached(Q.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield());

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Magical);

                if (target != null)
                {
                    var prediction = Q.GetPrediction(target);

                    if (prediction.HitChance >= HitChance.High)
                    {
                        Q.Cast(prediction.CastPosition);
                        return;
                    }
                }
            }

            if (E.IsReady() && Settings.Combo.UseE)
            {
                var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => x.IsValidTargetCached(500) && !x.HasUndyingBuffA());

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Mixed);

                if (target != null)
                {
                    E.Cast();
                    return;
                }
            }

            if (!R.IsReady() || !Settings.Combo.UseR || (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Ammo < Settings.Combo.MinStacksForR))
                return;
            {
                var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    x => x.IsValidTargetCached(R.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield());

                var target = TargetSelector.GetTarget(possibleTargets, DamageType.Magical);

                if (target == null)
                    return;

                var prediction = R.GetPrediction(target);

                if (prediction.Collision && Settings.Combo.RAllowCollision)
                {
                    var first =
                        prediction.CollisionObjects.OrderBy(x => x.DistanceCached(Player.Instance)).FirstOrDefault();

                    if (first == null)
                        return;

                    var enemy =
                        GetCollisionObjects<Obj_AI_Base>(first).FirstOrDefault(x => x.NetworkId == target.NetworkId);

                    if (enemy != null)
                    {
                        R.Cast(first);
                    }
                }
                else if (target.HealthPercent <= 50 ? prediction.HitChance >= HitChance.Medium
                    : prediction.HitChance >= HitChance.High)
                {
                    R.Cast(prediction.CastPosition);
                }
            }
        }
    }
}