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

namespace Marksman_Master.Plugins.Vayne.Modes
{
    internal class PermaActive : Vayne
    {
        public static void Execute()
        {
            if (!E.IsReady())
                return;

            if (Settings.Misc.EKs)
            {
                foreach (var enemy in
                    StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                        x => x.IsValidTargetCached(E.Range) && HasSilverDebuff(x) && (GetSilverDebuff(x).Count == 2) && Damage.IsKillableFrom3SilverStacks(x)))
                {
                    E.Cast(enemy);
                    return;
                }
            }

            if(!Settings.Combo.UseE || Player.Instance.IsRecalling() || (Settings.Misc.EMode != 0))
                return;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (Settings.Misc.ETargeting)
            {
                case 0:
                    var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

                    if ((target != null) && WillEStun(target))
                    {
                        E.Cast(target);
                    }
                    break;
                case 1:
                    foreach (var enemy in
                        StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                            x => x.IsValidTargetCached(E.Range) && WillEStun(x))
                            .OrderByDescending(TargetSelector.GetPriority))
                    {
                        E.Cast(enemy);
                    }
                    break;
            }
        }
    }
}
