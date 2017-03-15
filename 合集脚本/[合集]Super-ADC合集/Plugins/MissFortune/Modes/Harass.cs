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

namespace Marksman_Master.Plugins.MissFortune.Modes
{
    using Utils;

    internal class Harass : MissFortune
    {
        public static void Execute()
        {
            if (!Q.IsReady() || !Settings.Harass.UseQ || IsPreAttack ||
                !(Player.Instance.ManaPercent >= Settings.Harass.MinManaQ))
                return;

            var qTarget = TargetSelector.GetTarget(Q.Range + (Settings.Misc.BounceQFromMinions ? 420 : 0),
                DamageType.Physical);

            if (qTarget == null)
                return;

            var validUnkillable = Settings.Harass.UseQUnkillable &&
                                  (Player.Instance.ManaPercent >= Settings.Harass.MinManaQUnkillable);

            if (Settings.Misc.BounceQFromMinions)
            {
                var minion = GetQKillableMinion(qTarget);

                if (minion != null)
                {
                    Q.Cast(minion);
                }
                else if(validUnkillable)
                {
                    var unKillableMinion = GetQUnkillableMinion(qTarget);

                    if (unKillableMinion != null)
                    {
                        Q.Cast(unKillableMinion);
                    }
                }
                else if (qTarget.IsValidTargetCached(Q.Range))
                {
                    Q.Cast(qTarget);
                }
            }
            else if (qTarget.IsValidTargetCached(Q.Range))
            {
                Q.Cast(qTarget);
            }
        }
    }
}
