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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Vayne.Modes
{
    internal class JungleClear : Vayne
    {
        public static void Execute()
        {
            var jungleMinions = StaticCacheProvider.GetMinions(CachedEntityType.Monsters, x => x.IsValidTargetCached(Player.Instance.GetAutoAttackRange())).ToList();

            if (!jungleMinions.Any())
                return;

            string[] allowedMonsters =
            {
                "SRU_Gromp", "SRU_Blue", "SRU_Red", "SRU_Razorbeak", "SRU_Krug", "SRU_Murkwolf", "Sru_Crab",
                "SRU_Crab"
            };

            if (E.IsReady() && Settings.LaneClear.UseE &&
                (Player.Instance.ManaPercent >= Settings.LaneClear.MinMana))
            {
                var entity =
                    jungleMinions.Find(
                        x =>
                            x.IsValidTargetCached(E.Range) &&
                            allowedMonsters.Any(name => string.Equals(name, x.BaseSkinName)) && WillEStun(x));

                if ((entity != null) && (entity.Health > Player.Instance.GetAutoAttackDamageCached(entity) * 2))
                {
                    E.Cast(entity);
                }
            }

            if (!IsPostAttack || !Q.IsReady() || (Orbwalker.LastTarget == null) || !Settings.LaneClear.UseQToJungleClear || (Player.Instance.ManaPercent < Settings.LaneClear.MinMana))
                return;

            if (!Player.Instance.Position.Extend(Game.CursorPos, 299)
                .IsInRangeCached(Orbwalker.LastTarget, Player.Instance.GetAutoAttackRange()))
                return;

            Q.Cast(Player.Instance.Position.Extend(Game.CursorPos, 285).To3D());
        }
    }
}
