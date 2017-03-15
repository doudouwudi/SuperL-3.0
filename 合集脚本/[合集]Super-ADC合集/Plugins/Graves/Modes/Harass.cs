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
using System;
using EloBuddy;
using EloBuddy.SDK;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Graves.Modes
{
    internal class Harass : Graves
    {
        public static void Execute()
        {
            if (Q.IsReady() && Settings.Harass.UseQ && Player.Instance.ManaPercent >= Settings.Harass.MinManaQ)
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                if (target != null)
                {
                    var qPrediction = Q.GetPrediction(target);

                    if (qPrediction.HitChancePercent >= 75)
                    {
                        var distToPlayer =
                            Player.Instance.Position.Extend(qPrediction.CastPosition, 1000)
                                .CutVectorNearWall(1000)
                                .Distance(Player.Instance);

                        if (target.Distance(Player.Instance) < distToPlayer && !Player.Instance.Position.IsWallBetween(qPrediction.CastPosition))
                        {
                            Q.Cast(qPrediction.CastPosition);
                            Console.WriteLine("[DEBUG] Q cast on {0}", target.ChampionName);
                        }
                        else if (!Player.Instance.Position.IsWallBetween(qPrediction.CastPosition))
                        {
                            Q.Cast(qPrediction.CastPosition);
                            Console.WriteLine("[DEBUG] Q cast on {0} v2", target.ChampionName);
                        }
                    }
                }
            }
        }
    }
}
