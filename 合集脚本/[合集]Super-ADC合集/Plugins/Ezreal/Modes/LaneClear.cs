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
using EloBuddy.SDK.Enumerations;
using Marksman_Master.Utils;

namespace Marksman_Master.Plugins.Ezreal.Modes
{
    internal class LaneClear : Ezreal
    {
        public static bool CanILaneClear()
        {
            return !Settings.LaneClear.EnableIfNoEnemies || (Player.Instance.CountEnemiesInRange(Settings.LaneClear.ScanRange) <= Settings.LaneClear.AllowedEnemies);
        }

        public static void Execute()
        {
            if (!Q.IsReady() || !Settings.LaneClear.UseQInLaneClear || Player.Instance.HasSheenBuff() ||
                (Player.Instance.ManaPercent < Settings.LaneClear.MinManaQ))
                return;

            var laneMinions = StaticCacheProvider.GetMinions(CachedEntityType.EnemyMinion, x => x.IsValidTargetCached(Q.Range)).ToList();

            if (!laneMinions.Any() || !CanILaneClear())
                return;

            switch (Settings.LaneClear.LaneClearQMode)
            {
                case 0: //last hit
                    foreach (var minion in
                        (from minion in laneMinions
                            where Q.GetPrediction(minion).HitChance >= HitChance.High
                            let health = Prediction.Health.GetPrediction(minion,
                                (int) ((minion.DistanceCached(Player.Instance) + Q.CastDelay)/Q.Speed*1000))
                            where (health > 30) && (health < Player.Instance.GetSpellDamageCached(minion, SpellSlot.Q))
                            select minion).Where(minion => !Orbwalker.GetTarget().IdEquals(minion) && !IsPreAttack))
                    {
                        if ((Player.Instance.GetAutoAttackDamageCached(minion, true) > minion.Health) &&
                            Player.Instance.IsInAutoAttackRange(minion))
                            return;

                        Q.Cast(minion);
                        return;
                    }
                    break;
                case 1: // push

                    if (Orbwalker.ShouldWait || IsPreAttack || Player.Instance.IsUnderTurret())
                        break;

                    var target = (from minion in laneMinions
                        where
                            (Q.GetPrediction(minion).HitChance >= HitChance.High) && !minion.IdEquals(Orbwalker.GetTarget()) &&
                            (minion.Health - Player.Instance.GetSpellDamageCached(minion, SpellSlot.Q) >= 20)
                            && !((Player.Instance.GetAutoAttackDamageCached(minion, true) > minion.Health) &&
                                 Player.Instance.IsInAutoAttackRange(minion))
                        select minion).OrderByDescending(x => x.Health).FirstOrDefault();

                    if (target == null)
                        return;

                    Q.Cast(target.ServerPosition);

                    break;
                default:
                    return;
            }
        }
    }
}