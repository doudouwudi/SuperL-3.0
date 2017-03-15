#region Licensing
// ---------------------------------------------------------------------
// <copyright file="JungleClear.cs" company="EloBuddy">
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
namespace Marksman_Master.Plugins.Tristana.Modes
{
    using System;
    using System.Linq;
    using EloBuddy;
    using EloBuddy.SDK;
    using Utils;

    internal class JungleClear : Tristana
    {
        public static void Execute()
        {
            var jungleMinions =
                StaticCacheProvider.GetMinions(CachedEntityType.Monsters,
                    x => x.IsValidTarget(Player.Instance.GetAutoAttackRange())).ToList();

            if (!jungleMinions.Any())
                return;

            string[] allowedMonsters =
            {
                "SRU_Gromp", "SRU_Blue", "SRU_Red", "SRU_Razorbeak", "SRU_Krug", "SRU_Murkwolf", "Sru_Crab",
                "SRU_RiftHerald", "SRU_Dragon_Fire", "SRU_Dragon_Earth", "SRU_Dragon_Air", "SRU_Dragon_Elder",
                "SRU_Dragon_Water", "SRU_Baron"
            };

            if (Q.IsReady() && Settings.LaneClear.UseQInJungleClear &&
                (jungleMinions.Count(
                    x => allowedMonsters.Contains(x.BaseSkinName, StringComparer.CurrentCultureIgnoreCase)) >= 1))
            {
                Q.Cast();
            }

            if (!E.IsReady() || !Settings.LaneClear.UseEInJungleClear ||
                (Player.Instance.ManaPercent < Settings.LaneClear.MinManaE))
                return;

            var minion =
                jungleMinions.FirstOrDefault(
                    x => allowedMonsters.Contains(x.BaseSkinName, StringComparer.CurrentCultureIgnoreCase));

            if ((minion != null) && (minion.Health > Player.Instance.GetAutoAttackDamageCached(minion, true)*2))
            {
                E.Cast(minion);
            }
        }
    }
}
