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

namespace Marksman_Master.Plugins.Graves.Modes
{
    internal class JungleClear : Graves
    {
        public static void Execute()
        {
            var jungleMinions = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, 600).ToList();

            if (!jungleMinions.Any())
                return;

            if (E.IsReady() && Settings.LaneClear.UseEInJungleClear &&
                Player.Instance.ManaPercent >= Settings.LaneClear.MinManaE && GetAmmoCount < 2)
            {
                string[] allowedMonsters =
                {
                    "SRU_Gromp", "SRU_Blue", "SRU_Red", "SRU_Razorbeak", "SRU_Krug", "SRU_Murkwolf", "Sru_Crab",
                    "SRU_Crab",
                    "SRU_RiftHerald", "SRU_Dragon_Fire", "SRU_Dragon_Earth", "SRU_Dragon_Air", "SRU_Dragon_Elder",
                    "SRU_Dragon_Water", "SRU_Baron"
                };

                if (jungleMinions.Count > 1 || jungleMinions.Any(minion => allowedMonsters.Contains(minion.BaseSkinName) && minion.Health > Player.Instance.GetAutoAttackDamage(minion) * 2))
                {
                    E.Cast(Game.CursorPos.Distance(Player.Instance) > E.Range
                        ? Player.Instance.Position.Extend(Game.CursorPos, 420).To3D()
                        : Game.CursorPos);
                }
            }
            

            if (!Q.IsReady() || !Settings.LaneClear.UseQInJungleClear || (Player.Instance.ManaPercent < Settings.LaneClear.MinManaQ))
                return;

            var qMinions = jungleMinions.Where(x => x.IsValidTarget(Q.Range) && !Player.Instance.Position.IsWallBetween(x.Position)).ToList();
            
            if (!qMinions.Any())
                return;

            var last = 0;
            Obj_AI_Minion lastMinion = null;

            foreach (var minion in qMinions)
            {
                var area = new Geometry.Polygon.Rectangle(Player.Instance.Position,
                    Player.Instance.Position.Extend(minion, Q.Range).To3D(), Q.Width);

                var count = qMinions.Count(
                    x => new Geometry.Polygon.Circle(x.Position, x.BoundingRadius).Points.Any(k  =>
                        area.IsInside(k)));

                if (count <= last)
                    continue;

                last = count;
                lastMinion = minion;
            }

            if (last <= 0 || lastMinion == null)
                return;

            if (lastMinion.IsValidTarget(Q.Range))
            {
                Q.Cast(lastMinion);
            }
        }
    }
}