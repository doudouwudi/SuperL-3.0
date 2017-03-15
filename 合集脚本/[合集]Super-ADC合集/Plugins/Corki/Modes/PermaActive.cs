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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Corki.Modes
{
    internal class PermaActive : Corki
    {
        public static void Execute()
        {
            foreach (
                var target in
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x => (x.HealthPercent <= 30) &&
                             !x.IsZombie && x.IsValidTargetCached(1500) && !x.HasSpellShield() && !x.HasUndyingBuffA()))
            {
                if (Q.IsReady() && target.IsValidTarget(Q.Range) && (Damage.GetSpellDamage(target, SpellSlot.Q) <= target.TotalHealthWithShields(true)))
                {
                    var qPrediction = Q.GetPrediction(target);
                    if (qPrediction.HitChance >= EloBuddy.SDK.Enumerations.HitChance.Medium)
                    {
                        Q.Cast(qPrediction.CastPosition);
                        return;
                    }
                }
                if (!R.IsReady() || !target.IsValidTarget(R.Range) || (Damage.GetSpellDamage(target, SpellSlot.R) > target.TotalHealthWithShields(true)))
                    continue;

                var rPrediction = R.GetPrediction(target);

                if (rPrediction.Collision && Settings.Combo.RAllowCollision)
                {
                    var first =
                        rPrediction.CollisionObjects.OrderBy(x => x.DistanceCached(Player.Instance)).FirstOrDefault();

                    if (first == null)
                        return;

                    var enemy =
                        GetCollisionObjects<Obj_AI_Base>(first).FirstOrDefault(x => x.NetworkId == target.NetworkId);

                    if (enemy == null)
                        continue;

                    R.Cast(first);
                    return;
                }

                if (rPrediction.HitChance < EloBuddy.SDK.Enumerations.HitChance.Medium)
                    continue;

                R.Cast(rPrediction.CastPosition);
                return;
            }
            
            if (!R.IsReady() || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) || !Settings.Misc.AutoHarassEnabled || Player.Instance.IsRecalling() ||
                (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Ammo < Settings.Misc.MinStacksToUseR))
                return;

            if (HasBigRMissile && !(HasBigRMissile && Settings.Misc.UseBigBomb))
                return;

            foreach (
                var target in
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        hero => Settings.Misc.IsAutoHarassEnabledFor(hero) &&
                                !hero.IsZombie && hero.IsValidTarget(R.Range) && !hero.HasSpellShield() &&
                                !hero.HasUndyingBuffA())
                        .OrderByDescending(TargetSelector.GetPriority))
            {
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
                else if (target.HealthPercent <= 50
                    ? prediction.HitChance >= EloBuddy.SDK.Enumerations.HitChance.Medium
                    : prediction.HitChance >= EloBuddy.SDK.Enumerations.HitChance.High)
                {
                    R.Cast(prediction.CastPosition);
                }
            }
        }
    }
}