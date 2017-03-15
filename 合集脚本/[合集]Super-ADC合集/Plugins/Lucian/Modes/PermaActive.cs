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
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Lucian.Modes
{
    internal class PermaActive : Lucian
    {
        public static void Execute()
        {
            if (Q.IsReady() && !Player.Instance.IsRecalling() && !Player.Instance.Position.IsVectorUnderEnemyTower() && !Player.Instance.IsDashing())
            {
                foreach (
                    var enemy in StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, 
                            x => x.IsValidTarget(925) && ((Settings.Harass.UseQ && Settings.Harass.IsAutoHarassEnabledFor(x) && (Player.Instance.ManaPercent >= Settings.Harass.MinManaQ)) || (x.TotalHealthWithShields() <= Player.Instance.GetSpellDamageCached(x, SpellSlot.Q))))
                            .OrderByDescending(x => Player.Instance.GetSpellDamageCached(x, SpellSlot.Q)))
                {
                    if (enemy.IsValidTarget(Q.Range))
                    {
                        Q.Cast(enemy);
                        return;
                    }

                    if (!enemy.IsValidTarget(925) || !Settings.Combo.ExtendQOnMinions)
                        break;

                    var source = GetQExtendSource(enemy);

                    if (source != null)
                    {
                        Q.Cast(source);
                        return;
                    }
                }
            }

            if (!R.IsReady() || !Settings.Combo.UseR ||
                (Player.Instance.Spellbook.GetSpell(SpellSlot.R).Name != "LucianR"))
                return;

            var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);

            if ((target == null) || !Settings.Combo.RKeybind)
                return;

            var rPrediciton = R.GetPrediction(target);
            if (rPrediciton.HitChance >= HitChance.Medium)
            {
                R.Cast(rPrediciton.CastPosition);
            }
        }
    }
}