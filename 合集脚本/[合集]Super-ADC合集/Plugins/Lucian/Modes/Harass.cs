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

namespace Marksman_Master.Plugins.Lucian.Modes
{
    using Utils;

    internal class Harass : Lucian
    {
        public static void Execute()
        {
            if (!Q.IsReady() || !Settings.Harass.UseQ || (Player.Instance.ManaPercent < Settings.Harass.MinManaQ))
                return;

            var possibleTargets = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                x =>
                    x.IsValidTarget(925) && !x.HasSpellShield() && !x.HasUndyingBuffA() &&
                    Settings.Harass.IsAutoHarassEnabledFor(x));

            var target = TargetSelector.GetTarget(possibleTargets, DamageType.Physical);

            if (target == null)
                return;

            if (target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
                return;
            }

            if (!target.IsValidTarget(925) || !Settings.Combo.ExtendQOnMinions)
                return;

            var source = GetQExtendSource(target);

            if (source == null)
                return;

            Q.Cast(source);
        }
    }
}
