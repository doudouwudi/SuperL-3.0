#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Misc.cs" company="EloBuddy">
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
using System.Text.RegularExpressions;
using EloBuddy;
using EloBuddy.Sandbox;
using EloBuddy.SDK;
using Marksman_Master.Interfaces;
using SharpDX;

namespace Marksman_Master.Utils
{
    internal static class Misc
    {
        internal static bool IsMe => SandboxConfig.Username == "gero";
        
        private static bool Debug => IsMe && (!Bootstrap.MenuLoaded || MenuManager.IsDebugEnabled);
        
        /// <summary>
        /// Last message tick
        /// </summary>
        private static float _lastMessageTick;

        /// <summary>
        /// Last Message String
        /// </summary>
        private static string _lastMessageString;

        /// <summary>
        /// Female champions list
        /// </summary>
        private static readonly List<string> FemaleChampions = new List<string>
        {
            "Ahri", "Akali", "Anivia", "Annie",
            "Ashe", "Caitlyn", "Cassiopeia", "Diana",
            "Elise", "Evelynn", "Fiora", "Illaoi",
            "Irelia", "Janna", "Jinx", "Kalista",
            "Karma", "Katarina", "Kayle", "Kindred",
            "Leblanc", "Leona", "Lissandra", "Lulu",
            "Lux", "MissFortune", "Morgana", "Nami",
            "Nidalee", "Orianna", "Poppy", "Quinn",
            "RekSai", "Riven", "Sejuani", "Shyvana",
            "Sivir", "Sona", "Soraka", "Syndra",
            "Tristana", "Vayne",  "Vi", "Zyra"
        };

        /// <summary>
        /// Prints info message
        /// </summary>
        /// <param name="message">The message string</param>
        public static void PrintDebugMessage(string message)
        {
            if(Debug)
                Console.WriteLine($"[DEBUG] {message}");
        }


        /// <summary>
        /// Prints info message
        /// </summary>
        /// <param name="message">The message</param>
        public static void PrintDebugMessage(object message)
        {
            if (Debug)
                Console.WriteLine($"[DEBUG] {message}");
        }

        /// <summary>
        /// Prints info message
        /// </summary>
        /// <param name="message">The message string</param>
        /// <param name="possibleFlood">Set to true if there is a flood possibility</param>
        public static void PrintInfoMessage(string message, bool possibleFlood = true)
        {
            if (FemaleChampions.Any(key => message.Contains(key)))
            {
                message = Regex.Replace(message, "he", "she", RegexOptions.IgnoreCase);
                message = Regex.Replace(message, "him", "her", RegexOptions.IgnoreCase);
                message = Regex.Replace(message, "his", "hers", RegexOptions.IgnoreCase);
            }

            message = Regex.Replace(message, "<c>", "<font color=\"#5DC421\">", RegexOptions.IgnoreCase);
            message = Regex.Replace(message, "</c>", "</font>", RegexOptions.IgnoreCase);

            message = Regex.Replace(message, "<blue>", "<font color=\"#0A9CCC\">", RegexOptions.IgnoreCase);
            message = Regex.Replace(message, "</blue>", "</font>", RegexOptions.IgnoreCase);

            message = Regex.Replace(message, "<red>", "<font color=\"#FA002E\">", RegexOptions.IgnoreCase);
            message = Regex.Replace(message, "</red>", "</font>", RegexOptions.IgnoreCase); 

             message = Regex.Replace(message, "<in>", $"<font color=\"{ (SandboxConfig.IsBuddy ? "#BF1B49" : "#1BBF91")}\">", RegexOptions.IgnoreCase);
            message = Regex.Replace(message, "</in>", "</font>", RegexOptions.IgnoreCase);
            
            if (possibleFlood && _lastMessageTick + 500 > Game.Time * 1000 && _lastMessageString == message)
                return;

            _lastMessageTick = Game.Time * 1000;
            _lastMessageString = message;
            
            Chat.Print($"<font size=\"21\"><font color=\"{ (SandboxConfig.IsBuddy ? "#BF1B49" : "#1BBF91")}\"><b>[Marksman Master]</b></font> {message}</font>");
        }

        public static void UseItem(this IItem item, Obj_AI_Base target = null)
        {
            if (item == null || item.ItemId == 0)
                return;
            
            var myItem = new Item((int)item.ItemId, item.Range);

            if (item.ItemId == ItemIds.HealthPotion)
            {
                if(!myItem.IsOwned())
                    myItem = new Item((int)ItemIds.Biscuit, item.Range);
            }

            if (!myItem.IsOwned() || !myItem.IsReady())
                return;

            if(target == null)
                myItem.Cast();
            else
            {
                myItem.Cast(target);
            }

            if(!myItem.IsOwned())
                Activator.Activator.UnLoadItem((int) item.ItemId);
        }

        public static void UseItem(this IItem item, Vector3? position)
        {
            if (item == null || item.ItemId == 0)
                return;

            var myItem = new Item((int) item.ItemId, item.Range);

            if (item.ItemId == ItemIds.HealthPotion)
            {
                if (!myItem.IsOwned())
                    myItem = new Item((int)ItemIds.Biscuit, item.Range);
            }

            if (!myItem.IsOwned() || !myItem.IsReady())
                return;

            if (!position.HasValue)
                myItem.Cast();
            else
            {
                myItem.Cast(position.Value);
            }

            if (!myItem.IsOwned())
                Activator.Activator.UnLoadItem((int)item.ItemId);
        }

        public static Item ToItem(this IItem item)
        {
            return new Item((int) item.ItemId, item.Range);
        }

        public static void DrawRectangle(Vector3 start, Vector3 end, float width, float thickness, System.Drawing.Color color, RectangleDrawingFlags drawingFlags = RectangleDrawingFlags.All)
        {
            if (drawingFlags.HasFlag(RectangleDrawingFlags.Fill))
            {
                EloBuddy.SDK.Rendering.Line.DrawLine(color, width, start, end);
                return;
            }

            var direction = (end - start).Normalized().To2D().Perpendicular().To3D();

            var points = new List<Vector3>
            {
                start - direction * width,
                start + direction * width,
                end - direction * width,
                end + direction * width
            };

            if (drawingFlags.HasFlag(RectangleDrawingFlags.Side) || drawingFlags.HasFlag(RectangleDrawingFlags.All))
            {
                EloBuddy.SDK.Rendering.Line.DrawLine(color, thickness, points[0], points[2]);
                EloBuddy.SDK.Rendering.Line.DrawLine(color, thickness, points[1], points[3]);
            }

            if (drawingFlags.HasFlag(RectangleDrawingFlags.Bottom) || drawingFlags.HasFlag(RectangleDrawingFlags.All))
            {
                EloBuddy.SDK.Rendering.Line.DrawLine(color, thickness, points[1], points[0]);
            }

            if (drawingFlags.HasFlag(RectangleDrawingFlags.Top) || drawingFlags.HasFlag(RectangleDrawingFlags.All))
            {
                EloBuddy.SDK.Rendering.Line.DrawLine(color, thickness, points[2], points[3]);
            }
        }

        public static void DrawArrowHead(Vector3 start, Vector3 end, int angleDegrees, float width, float thickness, System.Drawing.Color color)
        {
            EloBuddy.SDK.Rendering.Line.DrawLine(color, thickness, end, end + (start - end).To2D().Normalized().Rotated(-angleDegrees * (float)Math.PI / 180).To3DWorld() * width);
            EloBuddy.SDK.Rendering.Line.DrawLine(color, thickness, end, end + (start - end).To2D().Normalized().Rotated(angleDegrees * (float)Math.PI / 180).To3DWorld() * width);
        }

        public static bool IsVectorUnderEnemyTower(this Vector3 vector)
        {
            return EntityManager.Turrets.Enemies.Any(x => x.IsValidTarget(900, true, vector));
        }

        public static bool IsWallBetween(this Vector3 start, Vector3 end, int step = 25)
        {
            if(start == Vector3.Zero || end == Vector3.Zero || step < 1)
                throw new ArgumentException("Invalid argument");

            try
            {
                for (var i = 0; i < start.DistanceCached(end); i += step)
                {
                    if (start.Extend(end, i).IsWall())
                    {
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            return false;
        }

        public static Vector2 CutVectorNearWall(this Vector2 from, int range, int step = 25)
        {
            var distance = Player.Instance.Position.DistanceCached(from);

            var x = from.Shorten(Player.Instance.Position.To2D(), distance);

            var output = new Vector2();

            for (var i = 0; i < range; i += step)
            {
                var vec = Player.Instance.Position.Extend(x, i);

                if (!vec.IsWall())
                    continue;

                output = vec;

                break;
            }
            return output;
        }

        public static Vector3 CutVectorNearWall(this Vector3 from, int range, int step = 25)
        {
            var distance = Player.Instance.Position.DistanceCached(from);

            var x = from.To2D().Shorten(Player.Instance.Position.To2D(), distance);

            var output = new Vector2();

            for (var i = 0; i < range; i += step)
            {
                var vec = Player.Instance.Position.Extend(x, i);

                if (!vec.IsWall())
                    continue;

                output = vec;

                break;
            }
            return output.To3D();
        }

        public static Vector2[] SortVectorsByDistance(Vector2[] array, Vector2 point)
        {
            if (array.Length == 1)
                return array;

            for (var i = 0; i < array.Length; i++)
            {
                for (var j = i + 1; j < array.Length; j++)
                {
                    if (!(array[i].DistanceCached(point) > array[j].DistanceCached(point)))
                        continue;

                    var temporary = array[i];

                    array[i] = array[j];
                    array[j] = temporary;
                }
            }
            return array;
        }

        public static List<Vector2> SortVectorsByDistance(List<Vector2> list, Vector2 point)
        {
            if (list.Count == 1)
                return list;

            for (var i = 0; i < list.Count; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    if (!(list[i].DistanceCached(point) > list[j].DistanceCached(point)))
                        continue;

                    var temporary = list[i];

                    list[i] = list[j];
                    list[j] = temporary;
                }
            }
            return list;
        }

        public static Vector2[] SortVectorsByDistanceDescending(Vector2[] array, Vector2 point)
        {
            if (array.Length == 1)
                return array;

            for (var i = 0; i < array.Length; i++)
            {
                for (var j = i + 1; j < array.Length; j++)
                {
                    if (!(array[i].DistanceCached(point) < array[j].DistanceCached(point)))
                        continue;

                    var temporary = array[i];

                    array[i] = array[j];
                    array[j] = temporary;
                }
            }
            return array;
        }

        public static List<Vector2> SortVectorsByDistanceDescending(List<Vector2> list, Vector2 point)
        {
            if (list.Count == 1)
                return list;

            for (var i = 0; i < list.Count; i++)
            {
                for (var j = i + 1; j < list.Count; j++)
                {
                    if (!(list[i].DistanceCached(point) < list[j].DistanceCached(point)))
                        continue;

                    var temporary = list[i];

                    list[i] = list[j];
                    list[j] = temporary;
                }
            }
            return list;
        }

        public static System.Drawing.Color ColorFromHsv(double hue, double saturation, double brightness)
        {
            if (hue > 360 || hue < 0 || saturation > 1 || saturation < 0 || brightness > 1 || brightness < 0)
                throw new ArgumentOutOfRangeException();

            var chroma = brightness * saturation;
            var x = chroma * (1 - Math.Abs(hue / 60f % 2 - 1));
            var m = brightness - chroma;
            ColorObject color;

            if (0 <= hue && 60 > hue)
                color = new ColorObject(chroma, x, 0);
            else if (60 <= hue && 120 > hue)
                color = new ColorObject(x, chroma, 0);
            else if (120 <= hue && 180 > hue)
                color = new ColorObject(0, chroma, x);
            else if (180 <= hue && 240 > hue)
                color = new ColorObject(0, x, chroma);
            else if (240 <= hue && 300 > hue)
                color = new ColorObject(x, 0, chroma);
            else if (300 <= hue && 360 > hue)
                color = new ColorObject(chroma, 0, x);
            else
                color = new ColorObject(0, 0, 0);

            color.R = (color.R + m) * 255;
            color.G = (color.G + m) * 255;
            color.B = (color.B + m) * 255;

            return System.Drawing.Color.FromArgb(255, Convert.ToInt32(color.R), Convert.ToInt32(color.G), Convert.ToInt32(color.B));
        }

        public static System.Drawing.Color ColorFromHsv(this HsvColor c)
        {
            var hue = c.Hue;
            var saturation = c.Saturation;
            var brightness = c.Value;

            if (hue > 360 || hue < 0 || saturation > 1 || saturation < 0 || brightness > 1 || brightness < 0)
                throw new ArgumentOutOfRangeException();

            var chroma = brightness * saturation;
            var x = chroma * (1 - Math.Abs(hue / 60f % 2 - 1));
            var m = brightness - chroma;
            ColorObject color;

            if (0 <= hue && 60 > hue)
                color = new ColorObject(chroma, x, 0);
            else if (60 <= hue && 120 > hue)
                color = new ColorObject(x, chroma, 0);
            else if (120 <= hue && 180 > hue)
                color = new ColorObject(0, chroma, x);
            else if (180 <= hue && 240 > hue)
                color = new ColorObject(0, x, chroma);
            else if (240 <= hue && 300 > hue)
                color = new ColorObject(x, 0, chroma);
            else if (300 <= hue && 360 > hue)
                color = new ColorObject(chroma, 0, x);
            else
                color = new ColorObject(0, 0, 0);

            color.R = (color.R + m) * 255;
            color.G = (color.G + m) * 255;
            color.B = (color.B + m) * 255;

            return System.Drawing.Color.FromArgb(255, Convert.ToInt32(color.R), Convert.ToInt32(color.G), Convert.ToInt32(color.B));
        }

        // ReSharper disable once InconsistentNaming
        public static System.Drawing.Color ColorFromColorBGRA(this ColorBGRA color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        // ReSharper disable once InconsistentNaming
        public static Color SharpDxFromBGRA(this ColorBGRA color)
        {
            return color;
        }

        // ReSharper disable once InconsistentNaming
        public static ColorBGRA BGRAfromSharpDx(this Color color)
        {
            return color;
        }

        public static System.Drawing.Color ColorFromSharpDx(this Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static Color SharpDxColorFromHsv(double hue, double saturation, double brightness)
        {
            if (hue > 360 || hue < 0 || saturation > 1 || saturation < 0 || brightness > 1 || brightness < 0)
                throw new ArgumentOutOfRangeException();

            var chroma = brightness * saturation;
            var x = chroma * (1 - Math.Abs(hue / 60f % 2 - 1));
            var m = brightness - chroma;
            ColorObject color;

            if (0 <= hue && 60 > hue)
                color = new ColorObject(chroma, x, 0);
            else if (60 <= hue && 120 > hue)
                color = new ColorObject(x, chroma, 0);
            else if (120 <= hue && 180 > hue)
                color = new ColorObject(0, chroma, x);
            else if (180 <= hue && 240 > hue)
                color = new ColorObject(0, x, chroma);
            else if (240 <= hue && 300 > hue)
                color = new ColorObject(x, 0, chroma);
            else if (300 <= hue && 360 > hue)
                color = new ColorObject(chroma, 0, x);
            else
                color = new ColorObject(0, 0, 0);

            color.R = (color.R + m) * 255;
            color.G = (color.G + m) * 255;
            color.B = (color.B + m) * 255;

            return new Color(Convert.ToInt32(color.R), Convert.ToInt32(color.G), Convert.ToInt32(color.B), 255);
        }

        public static Color SharpDxColorFromHsv(this HsvColor c)
        {
            var hue = c.Hue;
            var saturation = c.Saturation;
            var brightness = c.Value;

            if (hue > 360 || hue < 0 || saturation > 1 || saturation < 0 || brightness > 1 || brightness < 0)
                throw new ArgumentOutOfRangeException();

            var chroma = brightness * saturation;
            var x = chroma * (1 - Math.Abs(hue / 60f % 2 - 1));
            var m = brightness - chroma;
            ColorObject color;

            if (0 <= hue && 60 > hue)
                color = new ColorObject(chroma, x, 0);
            else if (60 <= hue && 120 > hue)
                color = new ColorObject(x, chroma, 0);
            else if (120 <= hue && 180 > hue)
                color = new ColorObject(0, chroma, x);
            else if (180 <= hue && 240 > hue)
                color = new ColorObject(0, x, chroma);
            else if (240 <= hue && 300 > hue)
                color = new ColorObject(x, 0, chroma);
            else if (300 <= hue && 360 > hue)
                color = new ColorObject(chroma, 0, x);
            else
                color = new ColorObject(0, 0, 0);

            color.R = (color.R + m) * 255;
            color.G = (color.G + m) * 255;
            color.B = (color.B + m) * 255;

            return new Color(Convert.ToInt32(color.R), Convert.ToInt32(color.G), Convert.ToInt32(color.B), 255);
        }

        public static double GetNumberInRangeFromProcent(double percent, double min, double max)
        {
            return (percent / 100f * (min - max) - min) * -1;
        }

        public static double GetProcentFromNumberRange(double value, double min, double max)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException();

            return (min - value) / (min - max) * 100f;
        }

        public static HsvColor ToHsv(this System.Drawing.Color color)
        {
            var r = color.R / 255d;
            var g = color.G / 255d;
            var b = color.B / 255d;

            var cMax = Math.Max(r, Math.Max(g, b));
            var cMin = Math.Min(r, Math.Min(g, b));

            var delta = cMax - cMin;
            double hue = 0;
            double saturation = 0;

            if (Math.Abs(cMax - r) < 0.01)
            {
                hue = 60 * (((g - b) / delta) % 6);
            }
            else if (Math.Abs(cMax - g) < 0.01)
            {
                hue = 60 * (((b - r) / delta) + 2);
            }
            else if (Math.Abs(cMax - b) < 0.01)
            {
                hue = 60 * (((r - g) / delta) + 4);
            }

            if (Math.Abs(cMax) < 0.01)
            {
                saturation = 0;
            }
            else if (Math.Abs(cMax) > 0.01)
            {
                saturation = delta / cMax;
            }
            return new HsvColor(hue, saturation, cMax);
        }

        public static HsvColor ToHsv(this Color color)
        {
            var r = color.R / 255d;
            var g = color.G / 255d;
            var b = color.B / 255d;

            var cMax = Math.Max(r, Math.Max(g, b));
            var cMin = Math.Min(r, Math.Min(g, b));

            var delta = cMax - cMin;
            double hue = 0;
            double saturation = 0;

            if (Math.Abs(cMax - r) < 0.01)
            {
                hue = 60 * (((g - b) / delta) % 6);
            }
            else if (Math.Abs(cMax - g) < 0.01)
            {
                hue = 60 * (((b - r) / delta) + 2);
            }
            else if (Math.Abs(cMax - b) < 0.01)
            {
                hue = 60 * (((r - g) / delta) + 4);
            }

            if (Math.Abs(cMax) < 0.01)
            {
                saturation = 0;
            }
            else if (Math.Abs(cMax) > 0.01)
            {
                saturation = delta / cMax;
            }
            return new HsvColor(hue, saturation, cMax);
        }


        private struct ColorObject
        {
            private double _r;
            private double _g;
            private double _b;

            public double R
            {
                get
                {
                    if (_r > 255)
                    {
                        return 255;
                    }
                    return _r < 0 ? 0 : _r;
                }
                set
                {
                    if (value > 255)
                    {
                        _r = 255;
                    }
                    else if (value < 0)
                    {
                        _r = 0;
                    } else _r = value;
                }
            }

            public double G
            {
                get
                {
                    if (_g > 255)
                    {
                        return 255;
                    }
                    return _g < 0 ? 0 : _g;
                }
                set
                {
                    if (value > 255)
                    {
                        _g = 255;
                    }
                    else if (value < 0)
                    {
                        _g = 0;
                    } else _g = value;
                }
            }

            public double B
            {
                get
                {
                    if (_b > 255)
                    {
                        return 255;
                    }
                    return _b < 0 ? 0 : _b;
                }
                set
                {
                    if (value > 255)
                    {
                        _b = 255;
                    }
                    else if (value < 0)
                    {
                        _b = 0;
                    } else _b = value;
                }
            }

            public ColorObject(double r, double g, double b)
            {
                _r = r;
                _g = g;
                _b = b;
            }
        }

        internal struct HsvColor
        {
            private double _h;
            private double _s;
            private double _v;

            public double Hue
            {
                get
                {
                    if (_h > 360)
                    {
                        return 360;
                    }
                    return _h < 0 ? 0 : _h;
                }
                set
                {
                    if (value > 360)
                    {
                        _h = 360;
                    }
                    else if (value < 0)
                    {
                        _h = 0;
                    } else _h = value;
                }
            }

            public double Saturation
            {
                get
                {
                    if (_s > 1)
                    {
                        return 1;
                    }
                    return _s < 0 ? 0 : _s;
                }
                set
                {
                    if (value > 1)
                    {
                        _s = 1;
                    }
                    else if (value < 0)
                    {
                        _s = 0;
                    } else _s = value;
                }
            }

            public double Value
            {
                get
                {
                    if (_v > 1)
                    {
                        return 1;
                    }
                    return _v < 0 ? 0 : _v;
                }
                set
                {
                    if (value > 1)
                    {
                        _v = 1;
                    }
                    else if (value < 0)
                    {
                        _v = 0;
                    } else _v = value;
                }
            }

            public HsvColor(double hue, double saturation, double value)
            {
                _h = hue;
                _s = saturation;
                _v = value;
            }

            public override string ToString()
            {
                return "HsvColor [H=" + _h + ", S=" + _s + ", V=" + _v + "]";
            }
        }
    }
}