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

using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using Marksman_Master.Utils;
using SharpDX;

namespace Marksman_Master.Plugins.Vayne.Modes
{
    internal class Harass : Vayne
    {
        public static void Execute()
        {
            if (IsPostAttack && Q.IsReady() && Settings.Harass.UseQ &&
                Player.Instance.ManaPercent >= Settings.Harass.MinManaToUseQ)
            {
                var enemies = Player.Instance.CountEnemiesInRangeCached(1300);
                var target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 300, DamageType.Physical);
                var position = Vector3.Zero;

                if (!Settings.Misc.QSafetyChecks)
                {
                    if (!Player.Instance.Position.Extend(Game.CursorPos, 300).To3D().IsVectorUnderEnemyTower())
                    {
                        Q.Cast(Player.Instance.Position.Extend(Game.CursorPos, 285).To3D());
                    }
                }
                else
                {
                    switch (Settings.Misc.QMode)
                    {
                        case 1:
                            switch (enemies)
                            {
                                case 0:
                                {
                                    if (
                                        !Player.Instance.Position.Extend(Game.CursorPos, 285)
                                            .To3D()
                                            .IsVectorUnderEnemyTower())
                                    {
                                        position = Player.Instance.Position.Extend(Game.CursorPos, 285).To3D();
                                    }
                                }
                                    break;
                                case 1:
                                {
                                    if (target != null && Player.Instance.HealthPercent > 50 &&
                                        target.HealthPercent < 30)
                                    {
                                        if (
                                            !Player.Instance.Position.Extend(Game.CursorPos, 285)
                                                .To3D()
                                                .IsVectorUnderEnemyTower())
                                        {
                                            Misc.PrintDebugMessage("1v1 Game.CursorPos");
                                            position = Player.Instance.Position.Extend(Game.CursorPos, 285).To3D();
                                        }
                                    }
                                    else if (target != null)
                                    {
                                        var list =
                                            SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 900,
                                                900,
                                                target.IsMelee
                                                    ? target.GetAutoAttackRange()*2
                                                    : target.GetAutoAttackRange())
                                                .Where(x => !x.Key.To3D().IsVectorUnderEnemyTower())
                                                .Select(source => source.Key)
                                                .ToList();

                                        if (list.Any())
                                        {
                                            Misc.PrintDebugMessage("1v1 found positions");
                                            position =
                                                Misc.SortVectorsByDistance(list, target.Position.To2D())[0].To3D();
                                        }
                                        Misc.PrintDebugMessage("1v1 not found positions...");
                                    }
                                }
                                    break;
                                case 2:
                                {
                                    var enemy =
                                        EntityManager.Heroes.Enemies.Where(
                                            x => !x.IsDead && x.Distance(Player.Instance) < 1300)
                                            .OrderBy(x => x.Distance(Player.Instance)).ToArray();

                                    List<Vector2> list;
                                    if (Player.Instance.HealthPercent > 50 && enemy[0].HealthPercent < 30 &&
                                        (enemy[1].Distance(Player.Instance) > 600 || enemy[1].HealthPercent < 25))
                                    {
                                        list =
                                            SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 900, 1300,
                                                300)
                                                .Where(x => x.Value == 0 && !x.Key.To3D().IsVectorUnderEnemyTower())
                                                .Select(source => source.Key)
                                                .ToList();
                                        Misc.PrintDebugMessage("2v1 main if");
                                    }
                                    else
                                    {
                                        list =
                                            SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 900,
                                                Player.Instance.CountEnemiesInRange(800) >= 1 ? 800 : 1300,
                                                Player.Instance.CountEnemiesInRange(800) >= 1 ? 400 : 450)
                                                .Where(x => x.Value < 2 && !x.Key.To3D().IsVectorUnderEnemyTower())
                                                .Select(source => source.Key)
                                                .ToList();
                                        Misc.PrintDebugMessage("2v1 else .. ");
                                    }
                                    if (list.Any())
                                    {

                                        Misc.PrintDebugMessage("2v1 found positions");
                                        position = Misc.SortVectorsByDistance(list, target.Position.To2D())[0].To3D();
                                    }
                                    Misc.PrintDebugMessage("2v1 not found positions");
                                }
                                    break;
                                case 3:
                                {
                                    var list =
                                        SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 900, 1300, 550)
                                            .Where(x => x.Value < 2 && !x.Key.To3D().IsVectorUnderEnemyTower())
                                            .Select(source => source.Key)
                                            .ToList();

                                    if (list.Any())
                                    {
                                        Misc.PrintDebugMessage("3v1 found positions ");
                                        position = Misc.SortVectorsByDistance(list, target.Position.To2D())[0].To3D();
                                    }
                                    else
                                    {
                                        Misc.PrintDebugMessage("3v1 not found positions ");
                                        var list2 =
                                            SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 900, 1300,
                                                450)
                                                .Where(x => !x.Key.To3D().IsVectorUnderEnemyTower())
                                                .Select(source => source.Key)
                                                .ToList();
                                        var closest =
                                            EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(900))
                                                .OrderBy(x => x.Distance(Player.Instance));

                                        if (list2.Any())
                                        {
                                            Misc.PrintDebugMessage("3v1 found positions else ");
                                            position =
                                                Misc.SortVectorsByDistanceDescending(list2,
                                                    closest.First().Position.To2D())
                                                    [0].To3D();
                                        }
                                    }
                                }
                                    break;
                                case 4:
                                {
                                    var list =
                                        SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 900, 1300, 650)
                                            .Where(x => x.Value <= 1 && !x.Key.To3D().IsVectorUnderEnemyTower())
                                            .Select(source => source.Key)
                                            .ToList();

                                    if (list.Any())
                                        position =
                                            Misc.SortVectorsByDistanceDescending(list, target.Position.To2D())[0].To3D();
                                    else
                                    {
                                        var list2 =
                                            SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 900, 1300,
                                                550)
                                                .Where(x => !x.Key.To3D().IsVectorUnderEnemyTower())
                                                .Select(source => source.Key)
                                                .ToList();
                                        var closest =
                                            EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(1300))
                                                .OrderBy(x => x.Distance(Player.Instance));

                                        if (list2.Any())
                                            position =
                                                Misc.SortVectorsByDistanceDescending(list2,
                                                    closest.First().Position.To2D())
                                                    [0].To3D();
                                    }
                                }
                                    break;
                                default:
                                {
                                    var list =
                                        SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 900, 1300, 750)
                                            .Where(x => x.Value <= 1 && !x.Key.To3D().IsVectorUnderEnemyTower())
                                            .Select(source => source.Key)
                                            .ToList();

                                    if (list.Any())
                                        position =
                                            Misc.SortVectorsByDistanceDescending(list, target.Position.To2D())[0].To3D();
                                    else
                                    {
                                        var list2 =
                                            SafeSpotFinder.GetSafePosition(Player.Instance.Position.To2D(), 900, 1300,
                                                550)
                                                .Where(x => !x.Key.To3D().IsVectorUnderEnemyTower())
                                                .Select(source => source.Key)
                                                .ToList();
                                        var closest =
                                            EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(1300))
                                                .OrderBy(x => x.Distance(Player.Instance));

                                        if (list2.Any())
                                            position =
                                                Misc.SortVectorsByDistanceDescending(list2,
                                                    closest.First().Position.To2D())
                                                    [0].To3D();
                                    }
                                }
                                    break;
                            }
                            if (position != Vector3.Zero && EntityManager.Heroes.Enemies.Any(x => x.IsValidTarget(900)))
                                Q.Cast(position.Distance(Player.Instance) > Q.Range ? Player.Instance.Position.Extend(position, Q.Range).To3D() : position);
                            break;
                        case 0:
                            var pos = Player.Instance.Position.Extend(Game.CursorPos, 299).To3D();

                            if (!pos.IsVectorUnderEnemyTower())
                            {
                                if (target != null)
                                {
                                    if (target.HealthPercent + 15 < Player.Instance.HealthPercent)
                                    {
                                        if (target.IsMelee &&
                                            !pos.IsInRange(Prediction.Position.PredictUnitPosition(target, 850),
                                                target.GetAutoAttackRange() + 150))
                                        {
                                            Q.Cast(pos);
                                        }
                                        else if (!target.IsMelee)
                                        {
                                            Q.Cast(pos);
                                        }
                                    }
                                    else if (enemies == 2 && Player.Instance.CountAlliesInRange(850) >= 1)
                                    {
                                        Q.Cast(pos);
                                    }
                                    else if (enemies >= 2)
                                    {
                                        if (
                                            !EntityManager.Heroes.Enemies.Any(
                                                x =>
                                                    pos.IsInRange(Prediction.Position.PredictUnitPosition(x, 850),
                                                        x.IsMelee
                                                            ? x.GetAutoAttackRange() + 150
                                                            : x.GetAutoAttackRange())))
                                        {
                                            Q.Cast(pos);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}