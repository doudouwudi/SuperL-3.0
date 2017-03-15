#region Licensing
// ---------------------------------------------------------------------
// <copyright file="LaneClear.cs" company="EloBuddy">
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
    internal class LaneClear : Graves
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies;
        }

        public static void Execute()
        {
            var laneMinions =
                EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, 1000).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            if (!Q.IsReady() || !Settings.LaneClear.UseQInLaneClear ||
                (Player.Instance.ManaPercent < Settings.LaneClear.MinManaQ) || Player.Instance.IsUnderTurret())
                return;

            var qMinions = laneMinions.Where(x => x.IsValidTarget(Q.Range) && !Player.Instance.Position.IsWallBetween(x.Position)).ToList();

            if (!qMinions.Any())
                return;

            var last = 0;
            Obj_AI_Minion lastMinion = null;

            foreach (var minion in qMinions)
            {
                var area = new Geometry.Polygon.Rectangle(Player.Instance.Position,
                    Player.Instance.Position.Extend(minion, Q.Range).To3D(), Q.Width);

                var count = qMinions.Count(
                    x => new Geometry.Polygon.Circle(x.Position, x.BoundingRadius).Points.Any(k =>
                        area.IsInside(k)));

                if (count <= last)
                    continue;

                last = count;
                lastMinion = minion;
            }

            if (last <= 0 || lastMinion == null)
                return;

            if (last >= Settings.LaneClear.MinMinionsHitQ && lastMinion.IsValidTarget(Q.Range))
            {
                Q.Cast(lastMinion);
            }
        }
    }
}