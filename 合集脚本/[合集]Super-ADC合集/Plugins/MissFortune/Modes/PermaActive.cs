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
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.MissFortune.Modes
{
    internal class PermaActive : MissFortune
    {
        public static void Execute()
        {
            if (Settings.Misc.EnableKillsteal)
            {
                if (Q.IsReady())
                {
                    foreach (
                        var enemy in
                            StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                                x =>
                                    x.IsValidTargetCached(Q.Range + (Settings.Misc.BounceQFromMinions ? 420 : 0)) && !x.HasUndyingBuffA() && !x.HasSpellShield()))
                    {
                        var damage = Player.Instance.GetSpellDamageCached(enemy, SpellSlot.Q);

                        if (Settings.Misc.BounceQFromMinions)
                        {
                            var minion = GetQKillableMinion(enemy);
                            var minion2 = GetQUnkillableMinion(enemy);

                            if ((minion != null) && (damage*1.5f >= enemy.TotalHealthWithShields()))
                            {
                                Q.Cast(minion);
                                return;
                            }

                            if ((minion2 != null) && (damage >= enemy.TotalHealthWithShields()))
                            {
                                Q.Cast(minion);
                                return;
                            }

                            if (!enemy.IsValidTargetCached(Q.Range) || (damage < enemy.TotalHealthWithShields()))
                                continue;

                            Q.Cast(enemy);
                            return;
                        }

                        if (!enemy.IsValidTargetCached(Q.Range) || (damage < enemy.TotalHealthWithShields()))
                            continue;

                        Q.Cast(enemy);
                        return;
                    }
                }
                if (E.IsReady())
                {
                    foreach (
                        var enemy in
                            StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                                x =>
                                    x.IsValidTargetCached(E.Range) && !x.HasUndyingBuffA() && !x.HasSpellShield() &&
                                    (x.TotalHealthWithShields() < Player.Instance.GetSpellDamageCached(x, SpellSlot.E)) &&
                                    (E.GetPrediction(x).HitChance == HitChance.High)))
                    {
                        E.CastMinimumHitchance(enemy, HitChance.High);
                        return;
                    }
                }
            }

            if (Q.IsReady() && Settings.Misc.AutoHarassQ &&
                (Player.Instance.ManaPercent >= Settings.Misc.AutoHarassQMinMana))
            {
                foreach (var enemy in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, x=> x.IsValidTargetCached(Q.Range + (Settings.Misc.BounceQFromMinions ?  420 : 0)) && Settings.Misc.IsAutoHarassEnabledFor(x)))
                {
                    if (Settings.Misc.BounceQFromMinions)
                    {
                        var minion = GetQMinion(enemy);
                        if (minion != null)
                        {
                            Q.Cast(minion);
                            return;
                        }
                        if (enemy.IsValidTargetCached(Q.Range))
                        {
                            Q.Cast(enemy);
                            return;
                        }
                    }

                    if (!enemy.IsValidTargetCached(Q.Range))
                        continue;

                    Q.Cast(enemy);
                    return;
                }
            }

            if (!R.IsReady() || !Settings.Combo.UseR)
                return;

            var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);

            if ((target == null) || !Settings.Combo.SemiAutoRKeybind)
                return;

            var rPrediciton = R.GetPrediction(target);

            if (rPrediciton.HitChancePercent >= 65)
            {
                R.Cast(rPrediciton.CastPosition);
            }
        }
    }
}