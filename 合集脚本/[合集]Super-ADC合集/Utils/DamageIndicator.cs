#region Licensing
// ---------------------------------------------------------------------
// <copyright file="DamageIndicator.cs" company="EloBuddy">
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
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace Marksman_Master.Utils
{
    internal static class DamageIndicator
    {
        public static Color Color { get; set; } = Color.Lime;
        public static Color JungleColor { get; set; } = Color.White;
        public static int DrawingRange { get; set; }
        public static bool IncludeJungleMobs { get; set; }

        internal delegate float DamageDelegateH(Obj_AI_Base unit);
        public static DamageDelegateH DamageDelegate { get; set; }

        public static void Initalize(Color color, int drawingRange = 1200)
        {
            Color = color;
            DrawingRange = drawingRange;

            Drawing.OnEndScene += DrawingOnEndScene;
        }

        public static void Initalize(Color color, bool includeJungleMobs, Color jungleColor, int drawingRange = 1200)
        {
            Color = color;
            IncludeJungleMobs = includeJungleMobs;
            DrawingRange = drawingRange;
            JungleColor = jungleColor;

            Drawing.OnEndScene += DrawingOnEndScene;
        }

        private static void DrawingOnEndScene(EventArgs args)
        {
            if (DamageDelegate == null)
                return;

            if (IncludeJungleMobs)
            {
                foreach (
                    var unit in
                        EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, DrawingRange)
                            .Where(x => x.IsValidTarget() && x.IsHPBarRendered && Drawing.WorldToScreen(x.Position).IsOnScreen()))
                {
                    if (DamageDelegate(unit) <= 0)
                        return;

                    int height;
                    int width;

                    int xOffset;
                    int yOffset;

                    if ((unit.Name.Contains("Blue") || unit.Name.Contains("Red")) && !unit.Name.Contains("Mini"))
                    {
                        height = 9;
                        width = 142;
                        xOffset = -4;
                        yOffset = 7;
                    }
                    else if (unit.Name.Contains("Dragon"))
                    {
                        height = 10;
                        width = 143;
                        xOffset = -4;
                        yOffset = 8;
                    }
                    else if (unit.Name.Contains("Baron"))
                    {
                        height = 12;
                        width = 191;
                        xOffset = -29;
                        yOffset = 6;
                    }
                    else if (unit.Name.Contains("Herald"))
                    {
                        height = 10;
                        width = 142;
                        xOffset = -4;
                        yOffset = 7;
                    }
                    else if ((unit.Name.Contains("Razorbeak") || unit.Name.Contains("Murkwolf")) &&
                             !unit.Name.Contains("Mini"))
                    {
                        width = 74;
                        height = 3;
                        xOffset = 30;
                        yOffset = 7;
                    }
                    else if (unit.Name.Contains("Krug") && !unit.Name.Contains("Mini"))
                    {
                        width = 80;
                        height = 3;
                        xOffset = 27;
                        yOffset = 7;
                    }
                    else if (unit.Name.Contains("Gromp"))
                    {
                        width = 86;
                        height = 3;
                        xOffset = 24;
                        yOffset = 6;
                    }
                    else if (unit.Name.Contains("Crab"))
                    {
                        width = 61;
                        height = 2;
                        xOffset = 36;
                        yOffset = 21;
                    }
                    else if (unit.Name.Contains("RedMini") || unit.Name.Contains("BlueMini") ||
                             unit.Name.Contains("RazorbeakMini"))
                    {
                        height = 2;
                        width = 49;
                        xOffset = 42;
                        yOffset = 6;
                    }
                    else if (unit.Name.Contains("KrugMini") || unit.Name.Contains("MurkwolfMini"))
                    {
                        height = 2;
                        width = 55;
                        xOffset = 39;
                        yOffset = 6;
                    }
                    else
                    {
                        continue;
                    }

                    var damageAfter = Math.Max(0, unit.Health - DamageDelegate(unit)) / unit.MaxHealth;
                    var currentHealth = unit.Health / unit.MaxHealth;

                    var start = unit.HPBarPosition.X + xOffset + (damageAfter * width);
                    var end = unit.HPBarPosition.X + xOffset + (currentHealth * width);

                    Drawing.DrawLine(start, unit.HPBarPosition.Y + yOffset, end, unit.HPBarPosition.Y + yOffset, height, System.Drawing.Color.FromArgb(JungleColor.A, JungleColor.R, JungleColor.G, JungleColor.B));
                }
            }


            foreach (var unit in
                StaticCacheProvider.GetChampions(CachedEntityType.EnemyHero,
                    index => index.IsHPBarRendered && index.IsValidTarget(DrawingRange) && index.Position.IsOnScreen()))
            {
                if (DamageDelegate(unit) <= 0)
                    return;

                const int height = 10;
                const int width = 106;

                int xOffset;
                int yOffset;

                switch (unit.Hero)
                {
                    case Champion.Annie:
                        xOffset = -9;
                        yOffset = -3;
                        break;
                    case Champion.Jhin:
                        xOffset = -9;
                        yOffset = -5;
                        break;
                    default:
                        xOffset = 2;
                        yOffset = 9;
                        break;

                }

                var damageAfter = Math.Max(0, unit.TotalHealthWithShields() - DamageDelegate(unit)) / unit.MaxHealth;
                var currentHealth = unit.TotalHealthWithShields() / unit.MaxHealth;

                var start = new Vector2(unit.HPBarPosition.X + xOffset + damageAfter * width, unit.HPBarPosition.Y + yOffset);
                var end = new Vector2(unit.HPBarPosition.X + currentHealth * width, unit.HPBarPosition.Y + yOffset);

                Line.DrawLine(System.Drawing.Color.FromArgb(Color.A, Color.R, Color.G, Color.B), height, start, end);
            }
        }
    }
}