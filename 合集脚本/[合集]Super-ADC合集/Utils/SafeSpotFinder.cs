#region Licensing
// ---------------------------------------------------------------------
// <copyright file="SafeSpotFinder.cs" company="EloBuddy">
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
using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy.SDK;
using SharpDX;

namespace Marksman_Master.Utils
{
    internal class SafeSpotFinder
    {
        public static IEnumerable<Vector2> PointsInRange(Vector2 start, float range, float step = 200, int quality = 50)
        {
            for (var i = step; i <= range; i += step)
            {
                var circle = new Geometry.Polygon.Circle(start, range, (int) Misc.GetNumberInRangeFromProcent(step/range*100, quality/6f, quality));

                foreach (var vector2 in circle.Points)
                {
                    yield return start.Extend(vector2, i);
                }
            }
        }

        /// <summary>
        /// Gets the safe position
        /// </summary>
        /// <param name="start">Start vector</param>
        /// <param name="maxDistance">Max distance from start vector</param>
        /// <param name="enemyScanRangge">Max distance from start to enemies</param>
        /// <param name="enemyRange">Range of an enemy</param>
        /// <returns>Safe position</returns>
        public static Dictionary<Vector2, int> GetSafePosition(Vector2 start, float maxDistance, float enemyScanRangge, float enemyRange)
        {
            var list = new Dictionary<Vector2, int>();

            try
            {
                var sortedChampions = StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero, unit => !unit.IsDead && unit.Distance(start) <= enemyScanRangge)
                        .OrderBy(unit => unit.HealthPercent)
                        .ToList(); //DangerLevel from lowest

                var pointsInRange = PointsInRange(start, maxDistance);

                var inRange = pointsInRange as IList<Vector2> ?? pointsInRange.ToList();

                if (!sortedChampions.Any())
                {
                    var dic = new Dictionary<Vector2, int>();
                    foreach (var pos in inRange.Where(pos => !dic.Keys.Contains(pos)))
                    {
                        dic.Add(pos, 0);
                    }
                    return dic;
                }

                foreach (var location in inRange)
                {
                    if (location.Distance(start) > maxDistance)
                        continue;

                    foreach (var sortedChampion in sortedChampions)
                    {
                        if (location.IsInRangeCached(sortedChampion, enemyRange)) // location is inside enemy range
                        {
                            var index = sortedChampions.FindIndex(p => p == sortedChampion);

                            if (index > sortedChampions.Count && index != 0)
                            {
                                if (!list.ContainsKey(location))
                                    list.Add(location, index + 1 + location.CountEnemiesInRangeCached(enemyRange - location.Distance(start)));
                            } else if (!list.ContainsKey(location))
                                list.Add(location, 1 + location.CountEnemiesInRangeCached(enemyRange - location.Distance(start)));
                        } else if (!list.ContainsKey(location))
                            list.Add(location, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return list;
        }

        private static IEnumerable<float> ValuesBetween(float start, float end, float step = 1)
        {
            for (var i = 0f; i <= end; i += step)
            {
               yield return start + i;
            }
        }
    }
}