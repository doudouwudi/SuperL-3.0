#region Licensing
// ---------------------------------------------------------------------
// <copyright file="ColorPicker.cs" company="EloBuddy">
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
using System.ComponentModel;
using System.Threading;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Rendering;
using SharpDX; 
using Color = System.Drawing.Color;

namespace Marksman_Master.Utils
{
    internal class ColorPicker : IDisposable
    {
        private Slider _slider1;
        private Slider _slider2;
        private Slider _slider3;
        private Slider _slider4;

        private PermaShow.Text _text;
        private bool _pickingColor;
        private bool _isMoving;
        private readonly string _uniqueId;
        private bool _posUpdated;

        internal delegate void OnColorChangeEvent(object sender, OnColorChangeArgs args);

        public event OnColorChangeEvent OnColorChange;
        
        public ColorBGRA Color { get; set; }
        public Vector2[] Position { get; set; } = {new Vector2(500, 200), new Vector2(500, 470)};
        public int Width { get; set; } = 200;

        public ColorPicker(string uniqueId, ColorBGRA defaultColor)
        {
            _uniqueId = uniqueId;

            Misc.PrintDebugMessage("Constructing ColorPicker !");

            Color = Bootstrap.SavedColorPickerData.ContainsKey(uniqueId) ? Bootstrap.SavedColorPickerData[uniqueId] : defaultColor;
        }

        public void Initialize(Color sliderColor)
        {
            if (_pickingColor)
                return;

            _text = new PermaShow.Text("Color Picker", 19, 222, 222, new ColorBGRA(255, 0, 102, 255));

            Drawing.OnEndScene += Drawing_OnEndScene;
            Game.OnWndProc += Game_OnWndProc;

            _slider1 = new Slider(0, Color.R, 255, new[] {new Vector2(425, 0), new Vector2(575, 300)},
                sliderColor);
            _slider2 = new Slider(0, Color.G, 255, new[] {new Vector2(425, 0), new Vector2(575, 340)},
                sliderColor);
            _slider3 = new Slider(0, Color.B, 255, new[] { new Vector2(425, 0), new Vector2(575, 380) },
                sliderColor);
            _slider4 = new Slider(0, Color.A, 255, new[] { new Vector2(425, 0), new Vector2(575, 420) },
                sliderColor);

            _pickingColor = true;
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (IsMouseOverSeparator())
            {
                switch (args.Msg)
                {
                    case (uint) WindowMessages.LeftButtonDown:
                        if (IsMouseOverSeparator())
                            _isMoving = true;
                        break;
                    case (uint) WindowMessages.LeftButtonUp:
                        _isMoving = false;
                        break;
                    default:
                        return;
                }
            }

            if (args.Msg != (uint) WindowMessages.LeftButtonDown)
                return;

            if (!IsMouseOverCancelButton())
            {
                if (!IsMouseOverConfirmButton())
                    return;

                Color = new ColorBGRA((byte) _slider1.Value, (byte) _slider2.Value, (byte) _slider3.Value, (byte)_slider4.Value);

                FileHandler.WriteToDataFile(_uniqueId, new ColorBGRA((byte)_slider1.Value, (byte)_slider2.Value, (byte)_slider3.Value, (byte)_slider4.Value));

                OnColorChange?.Invoke(this, new OnColorChangeArgs(Color));

                Unload();
            }
            else
            {
                Unload();
            }
        }
        
        private void Unload()
        {
            Drawing.OnEndScene -= Drawing_OnEndScene;
            Game.OnWndProc -= Game_OnWndProc;
            
            GC.Collect();
            _pickingColor = false;
            _posUpdated = false;
        }


        private void Drawing_OnEndScene(EventArgs args)
        {
            if (_isMoving)
            {
                Position = new[]
                {
                    new Vector2(Game.CursorPos2D.X, Game.CursorPos2D.Y),
                    new Vector2(Game.CursorPos2D.X, Game.CursorPos2D.Y + 270)
                };
                _posUpdated = false;
            }

            Drawing.DrawLine(Position[0], Position[1], Width, System.Drawing.Color.FromArgb(255, 62, 59, 59));
            Drawing.DrawLine(new Vector2(Position[0].X - Width / 2f, Position[0].Y),
                new Vector2(Position[0].X + Width / 2f, Position[0].Y), 25, System.Drawing.Color.FromArgb(255, 4, 76, 61));
            Drawing.DrawLine(new Vector2(Position[0].X, Position[0].Y + 40),
                new Vector2(Position[0].X + 25, Position[0].Y + 40), 25, System.Drawing.Color.FromArgb(_slider4.Value, _slider1.Value, _slider2.Value, _slider3.Value));

            var fontPositionX = (int)(Position[0].X - Width / 2f + 25);
            var fontPositionY = (int)Position[0].Y - (int)_text.Height / 2;
            var sliderPositionX = (int)(Position[0].X + Width / 2f);

            _text.Font.DrawText(null, "Color Picker", fontPositionX, fontPositionY, new ColorBGRA(13, 168, 97, 255));
            _text.Font.DrawText(null, "Preview : ", fontPositionX, fontPositionY + 40, new ColorBGRA(109, 101, 64, 255));
            _text.Font.DrawText(null, "Red : " + _slider1.Value, fontPositionX, fontPositionY + 80, new ColorBGRA(109, 101, 64, 255));
            _text.Font.DrawText(null, "Green : " + _slider2.Value, fontPositionX, fontPositionY + 120, new ColorBGRA(109, 101, 64, 255));
            _text.Font.DrawText(null, "Blue : " + _slider3.Value, fontPositionX, fontPositionY + 160, new ColorBGRA(109, 101, 64, 255));
            _text.Font.DrawText(null, "Alpha : " + _slider4.Value, fontPositionX, fontPositionY + 200, new ColorBGRA(109, 101, 64, 255));

            _slider1.Positions = new[] { new Vector2(fontPositionX, fontPositionY + 110), new Vector2(sliderPositionX - 25, fontPositionY + 110) };
            _slider2.Positions = new[] { new Vector2(fontPositionX, fontPositionY + 150), new Vector2(sliderPositionX - 25, fontPositionY + 150) };
            _slider3.Positions = new[] { new Vector2(fontPositionX, fontPositionY + 190), new Vector2(sliderPositionX - 25, fontPositionY + 190) };
            _slider4.Positions = new[] { new Vector2(fontPositionX, fontPositionY + 230), new Vector2(sliderPositionX - 25, fontPositionY + 230) };

            if (!_posUpdated)
            {
                _slider1.UpdatePosition();
                _slider2.UpdatePosition();
                _slider3.UpdatePosition();
                _slider4.UpdatePosition();
            }

            _slider1.Draw();
            _slider2.Draw();
            _slider3.Draw();
            _slider4.Draw();

            Drawing.DrawLine(new Vector2(Position[0].X - Width / 2f + 25, fontPositionY + 255),
                new Vector2(Position[0].X - Width / 2f + Width / 2f, fontPositionY + 255), 25, System.Drawing.Color.FromArgb(IsMouseOverCancelButton() ? 255 : 150, 252, 113, 106));

            Drawing.DrawLine(new Vector2(Position[0].X - Width / 2f + Width / 2f, fontPositionY + 255),
                new Vector2(Position[0].X + Width / 2f - 25, fontPositionY + 255), 25, System.Drawing.Color.FromArgb(IsMouseOverConfirmButton() ? 255 : 150, 53, 188, 156));

            _text.Message = "Cancel";
            _text.Font.DrawText(null, "Cancel", (int)(Position[0].X - Width / 2f + 25) + +_text.GetTextRectangle().Width / 3, fontPositionY + 245,
                new ColorBGRA(255, 243, 242, IsMouseOverCancelButton() ? (byte)255 : (byte)150));
            _text.Message = "Confirm";
            _text.Font.DrawText(null, "Confirm", (int)(Position[0].X - Width / 2f + Width / 2f) + (int)(_text.GetTextRectangle().Width / 4.5f), fontPositionY + 245,
                new ColorBGRA(255, 243, 242, IsMouseOverConfirmButton() ? (byte)255 : (byte)150));

            _posUpdated = true;
        }

        private bool IsMouseOverSeparator()
        {
            var pos = Game.CursorPos2D;
            var posY = Position[0].Y;

            return pos.X >= Position[0].X - Width / 2f && pos.X <= Position[0].X + Width / 2f && pos.Y >= posY - 25 / 2f && pos.Y <= posY + 25 / 2f;
        }

        private bool IsMouseOverCancelButton()
        {
            var pos = Game.CursorPos2D;
            var posY = Position[0].Y - _text.Height / 2f + 250;

            return pos.X >= Position[0].X - Width / 2f + 25 && pos.X <= Position[0].X - Width / 2f + Width / 2f && pos.Y >= posY - 25 / 2f && pos.Y <= posY + 45 / 2f;
        }

        private bool IsMouseOverConfirmButton()
        {
            var pos = Game.CursorPos2D;
            var posY = Position[0].Y - _text.Height / 2f + 250;

            return pos.X >= Position[0].X - Width / 2f + Width / 2f && pos.X <= Position[0].X + Width / 2f - 25 && pos.Y >= posY - 25 / 2f && pos.Y <= posY + 45 / 2f;
        }


        internal class Slider
        {
            private bool _isMoving;
            private Vector2[] _position;

            public int MinValue { get; }
            public int MaxValue { get; }

            private int _value;

            public int Value
            {
                get
                {
                    if (_value > MaxValue)
                    {
                        return MaxValue;
                    }
                    return _value < MinValue ? MinValue : _value;
                }
                set
                {
                    if (value > MaxValue)
                        _value = MaxValue;
                    _value = value < MinValue ? MinValue : value;
                }
            }

            public Vector2[] Positions { get; set; }
            public Color Color { get; }

            private Thread _thread;

            public Slider(int minValue, int value, int maxValue, Vector2[] positions, Color color)
            {
                MinValue = minValue;
                MaxValue = maxValue;
                Positions = positions;
                Color = color;
                _value = value;

                using (var backgroundWorker = new BackgroundWorker())
                {
                    Misc.PrintDebugMessage("Color Picker : Setting Values...");

                    backgroundWorker.DoWork += (sender, args) =>
                    {
                        _thread = new Thread(() => SetValueThread(minValue, value, maxValue, positions))
                        {
                            IsBackground = true
                        };
                        _thread.Start();
                    };
                    backgroundWorker.RunWorkerCompleted += (sender, args) =>
                    {
                        Misc.PrintDebugMessage("Color Picker : Values has been set.");
                    };

                    backgroundWorker.RunWorkerAsync();
                }
                Game.OnWndProc += GameOnWndProc;
            }

            private void SetValueThread(double minValue, double value, double maxValue, IList<Vector2> positions)
            {
                var pos = Misc.GetNumberInRangeFromProcent(Misc.GetProcentFromNumberRange(value, minValue, maxValue), positions[0].X, positions[1].X);

                _position = new[]
                {
                    new Vector2((float)pos-5, positions[0].Y),
                    new Vector2((float)pos+5, positions[0].Y)
                };
            }

            private void GameOnWndProc(WndEventArgs args)
            {
                switch (args.Msg)
                {
                    case (uint) WindowMessages.LeftButtonDown:
                        if (IsPositionOnSliderBox(Game.CursorPos2D))
                            _isMoving = true;
                        break;
                    case (uint) WindowMessages.LeftButtonUp:
                        _isMoving = false;
                        break;
                    default:
                        return;
                }
            }

            private bool IsPositionOnSliderBox(Vector2 pos)
            {
                return pos.X >= _position[0].X && pos.X <= _position[1].X && pos.Y >= Positions[0].Y - 6 &&
                       pos.Y <= Positions[0].Y + 6;
            }

            public void UpdatePosition()
            {
                SetValueThread(MinValue, _value, MaxValue, Positions);
            }

            public void Draw()
            {
                if (_thread.IsAlive)
                    return;

                if (_isMoving)
                {
                    float positionX;

                    if (Game.CursorPos2D.X < Positions[0].X)
                        positionX = Positions[0].X;
                    else if (Game.CursorPos2D.X > Positions[1].X)
                        positionX = Positions[1].X;
                    else
                        positionX = Game.CursorPos2D.X;

                    _position = new[]
                    {new Vector2(positionX - 5, Positions[0].Y), new Vector2(positionX + 5, Positions[0].Y)};
                }

                Line.DrawLine(Color, new Vector2(Positions[0].X, Positions[0].Y),new Vector2(_position[1].X, Positions[0].Y));
                Line.DrawLine(Color.AliceBlue, new Vector2(_position[1].X-5, Positions[0].Y), new Vector2(Positions[1].X, Positions[0].Y));
                Line.DrawLine(Color, 12, _position);

                //var value = (Positions[0].X - _position[1].X+5)/(Positions[0].X - Positions[1].X);
                /* _value = (int)((MaxValue - MinValue)*(Math.Pow(Math.E, value) - Math.Pow(Math.E, -value))/
                            (Math.Pow(Math.E, 1.0f) - Math.Pow(Math.E, -1.0f)) + MinValue);*/

                _value = (int) ((MaxValue - MinValue) * Misc.GetProcentFromNumberRange(_position[0].X + 5, Positions[0].X, Positions[1].X) / 100);
            }
        }

        public void Dispose()
        {
            _text.Dispose();
        }
    }

    internal class OnColorChangeArgs
    {
        public ColorBGRA Color { get; set; }

        public OnColorChangeArgs(ColorBGRA color)
        {
            Color = color;
        }
    }
}