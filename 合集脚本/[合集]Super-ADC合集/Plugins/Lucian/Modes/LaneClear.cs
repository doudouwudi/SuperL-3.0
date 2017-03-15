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

namespace Marksman_Master.Plugins.Lucian.Modes
{
    internal class LaneClear : Lucian
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || (Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies);
        }

        public static void Execute()
        {
            var laneMinions = StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion, x => x.IsValidTarget(1100)).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            if (!Q.IsReady() || !Settings.LaneClear.UseQInLaneClear || HasPassiveBuff || Player.Instance.HasSheenBuff() ||
                (Player.Instance.ManaPercent < Settings.LaneClear.MinManaQ) || (laneMinions.Count <= 1))
                return;

            foreach (var objAiMinion in from objAiMinion in laneMinions
                let rectangle = new Geometry.Polygon.Rectangle(Player.Instance.Position,
                    Player.Instance.Position.Extend(objAiMinion, Player.Instance.Distance(objAiMinion) > 900 ? 900 - Player.Instance.Distance(objAiMinion) : 900).To3D(), 20)
                let count = laneMinions.Count(minion => new Geometry.Polygon.Circle(minion.Position, objAiMinion.BoundingRadius).Points.Any(rectangle.IsInside))
                where count >= Settings.LaneClear.MinMinionsHitQ
                select objAiMinion)
            {
                Q.Cast(objAiMinion);
            }
        }
    }
}