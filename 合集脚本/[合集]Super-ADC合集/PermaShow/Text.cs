#region Licensing
// ---------------------------------------------------------------------
// <copyright file="Text.cs" company="EloBuddy">
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
namespace Marksman_Master.PermaShow
{
    using System;

    using EloBuddy;

    using SharpDX;
    
    using SharpDX.Direct3D9;

    using Interfaces;

    using Font = SharpDX.Direct3D9.Font;

    internal sealed class Text : IDisposable, IDrawable
    {
        public uint Height { get; set; }
        public Font Font { get; set; }
        public ColorBGRA Color { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Message { get; set; }

        public Text(string message, uint height, int x, int y, ColorBGRA color, bool italic = false,
            bool bold = false, FontQuality quality = FontQuality.Antialiased)
        {
            Height = height;
            Color = color;
            X = x;
            Y = y;
            Message = message;

            Font = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Gill Sans MT Pro Medium",
                    Height = (int) height,
                    Quality = quality,
                    Italic = italic,
                    Weight = bold ? FontWeight.Bold : FontWeight.Regular
                });

            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
        }

        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            Dispose();
        }

        public Text(string message, string faceName, uint height, int x, int y, ColorBGRA color,
            bool italic = false, bool bold = false, FontQuality quality = FontQuality.Antialiased)
        {
            Height = height;
            Color = color;
            X = x;
            Y = y;
            Message = message;

            Font = new Font(Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = faceName,
                    Height = (int) height,
                    Quality = quality,
                    Italic = italic,
                    Weight = bold ? FontWeight.Bold : FontWeight.Regular
                });

            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
        }

        ~Text()
        {
            Dispose();
        }

        public void Dispose()
        {
            Font?.Dispose();
            GC.SuppressFinalize(this);

            Drawing.OnPreReset -= Drawing_OnPreReset;
            Drawing.OnPostReset -= Drawing_OnPostReset;
            AppDomain.CurrentDomain.DomainUnload -= CurrentDomain_DomainUnload;
        }
        
        public void Draw()
        {
            if (Font == null || Font.IsDisposed || Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed || Height < 1 || string.IsNullOrEmpty(Message))
                return;

            //using (Font)
            {
                Font?.DrawText(null, Message, X, Y, Color);
            }
        }

        public Rectangle GetTextRectangle()
        {
            return Font?.MeasureText(null, Message, FontDrawFlags.Right) ?? Rectangle.Empty;
        }

        private void Drawing_OnPostReset(EventArgs args)
        {
            Dispose();
            Font?.OnResetDevice();
        }

        private void Drawing_OnPreReset(EventArgs args)
        {
            Dispose();
            Font?.OnLostDevice();
        }
    }
}