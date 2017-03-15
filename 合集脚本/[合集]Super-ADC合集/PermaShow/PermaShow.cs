#region Licensing
// ---------------------------------------------------------------------
// <copyright file="PermaShow.cs" company="EloBuddy">
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

    using System.Linq;

    using EloBuddy;

    using EloBuddy.SDK;

    using EloBuddy.SDK.Menu;

    using SharpDX;

    using Interfaces;

    using Modules;

    using Values;

    using Color = System.Drawing.Color;

    internal class PermaShow : PermashowBase, IPermaShow
    {
        private readonly Vector2 _defaultPosition = new Vector2(190, 90);
        private Wrapper Wrapper { get; } = new Wrapper();

        public bool IsMoving { get; private set; }

        public Vector2 Position { get; set; }

        public ColorBGRA BackgroundColor => Wrapper.Bind<CreateMenuModule>().BackgroundColor;
        public ColorBGRA SeparatorColor => Wrapper.Bind<CreateMenuModule>().SeparatorColor;
        public ColorBGRA EnabledUnderlineColor => Wrapper.Bind<CreateMenuModule>().EnabledUnderlineColor;
        public ColorBGRA DisabledUnderlineColor => Wrapper.Bind<CreateMenuModule>().DisabledUnderlineColor;
        public ColorBGRA TextColor => Wrapper.Bind<CreateMenuModule>().TextColor;

        public uint PermaShowItemTextHeight { get; set; } = 14;

        public static Menu Menu { get; set; }
        
        public bool Enabled { get; set; }

        public int DefaultSpacing { get; set; }

        public int Opacity { get; set; }

        public PermaShow(string headerName, Vector2 pos)
        {
            Position = pos;

            Wrapper.Load();

            Wrapper.Bind<CreateMenuModule>().HeaderName = headerName;
            Wrapper.InvokeLoadMethodForAll();

            Wrapper.Bind<DataHandlerModule>().HeaderText = new Text(headerName, 19, (int)_defaultPosition.X, (int)_defaultPosition.Y, TextColor);

            Core.DelayAction(() =>
            {
                Wrapper.Bind<CreateMenuModule>().CreateMenu();

                Opacity = Wrapper.Bind<CreateMenuModule>().Opacity;
                DefaultSpacing = Wrapper.Bind<CreateMenuModule>().DefaultSpacing;
                Enabled = Wrapper.Bind<CreateMenuModule>().Enabled;
                
                Drawing.OnPreReset += Drawing_OnPreReset;
                Drawing.OnPostReset += Drawing_OnPostReset;
                Drawing.OnEndScene += Drawing_OnDraw;

                Game.OnTick += Game_OnTick;
                Game.OnWndProc += Game_OnWndProc;

                foreach (var permaShowItem in Wrapper.Bind<DataHandlerModule>().PermaShowItems)
                {
                    switch (permaShowItem.Value.GetType().Name)
                    {
                        case "BoolItem":
                            permaShowItem.Value.Get<BoolItem>().OnValueChange += (sender, b) => UpdatePositions();
                            break;
                        case "StringItem":
                            permaShowItem.Value.Get<StringItem>().OnValueChange += (sender, b) => UpdatePositions();
                            break;
                        case "MenuItem":
                            permaShowItem.Value.Get<MenuItem>().OnValueChange += (sender, b) => UpdatePositions();
                            break;
                        default:
                            continue;
                    }
                }
                UpdatePositions();

            }, 2500);
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (!Enabled)
                return;

            switch (args.Msg)
            {
                case (uint)WindowMessages.LeftButtonDown:
                    if (IsPositionOnPermaShow(Game.CursorPos2D))
                        IsMoving = true;
                    break;
                case (uint)WindowMessages.LeftButtonUp:
                    IsMoving = false;
                    break;
                default:
                    return;
            }
        }

        private void Game_OnTick(EventArgs args)
        {
            if (IsMoving)
            {
                Position = Game.CursorPos2D;
                UpdatePositions();
            }

            Opacity = Wrapper.Bind<CreateMenuModule>().Opacity;
            DefaultSpacing = Wrapper.Bind<CreateMenuModule>().DefaultSpacing;
            Enabled = Wrapper.Bind<CreateMenuModule>().Enabled;
        }

        private int GetMaxItemNameTextLength()
        {
            if (!Enabled || Opacity == 0)
                return 0;

            var itemNameTextLength = 0;

            foreach (var item in Wrapper.Bind<DataHandlerModule>().PermaShowItems)
            {
                var itemNameTextWidth = item.Key.ItemNameText.GetTextRectangle().Width;

                if (itemNameTextLength == 0 || itemNameTextLength < itemNameTextWidth)
                {
                    itemNameTextLength = itemNameTextWidth;
                }
            }
            return itemNameTextLength;
        }

        private int GetMaxItemValueTextLength()
        {
            if (!Enabled || Opacity == 0)
                return 0;

            var itemValueTextLength = 0;

            foreach (var item in Wrapper.Bind<DataHandlerModule>().PermaShowItems)
            {
                var itemValueTextWidth = item.Key.ItemValueText.GetTextRectangle().Width;

                if (itemValueTextLength == 0 || itemValueTextLength < itemValueTextWidth)
                {
                    itemValueTextLength = itemValueTextWidth;
                }
            }
            return itemValueTextLength;
        }

        private int GetMaxTextLength()
        {
            if (!Enabled || Opacity == 0)
                return 0;

            var a = (int)(GetMaxItemNameTextLength() + GetMaxItemValueTextLength() + DefaultSpacing * 1.25f);
            var b = Wrapper.Bind<DataHandlerModule>().HeaderText.GetTextRectangle().Width;

            return a > b ? a : b;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!Enabled || Opacity == 0)
                return;

            if (Drawing.Direct3DDevice.IsDisposed || CountItems() == 0)
                return;

            Draw();
        }

        private Color ToColor(ColorBGRA color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private void Draw()
        {
            var lastSeparator = Wrapper.Bind<DataHandlerModule>().Separators.Last();

            var width = Position.X + GetMaxTextLength() + DefaultSpacing*2 - Position.X;
            
            EloBuddy.SDK.Rendering.Line.DrawLine(ToColor(new ColorBGRA(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, (byte)Opacity)), width + 8, new Vector2(Position.X + width / 2, Position.Y), new Vector2(Position.X + width / 2, lastSeparator.Positions[0].Y + 5));
            
            //Drawing.DrawLine(new Vector2(Position.X + width/2, Position.Y), new Vector2(Position.X + width/2, lastSeparator.Positions[0].Y + 5), (int) width + 8, ToColor(new ColorBGRA(BackgroundColor.R, BackgroundColor.G, BackgroundColor.B, (byte) Opacity)));

            Wrapper.Bind<DataHandlerModule>().HeaderText.Font.DrawText(null, Wrapper.Bind<DataHandlerModule>().HeaderText.Message, (int) Position.X + DefaultSpacing, (int) Position.Y, new ColorBGRA(TextColor.R, TextColor.G, TextColor.B, (byte)Opacity));

            //Drawing.DrawLine(new Vector2(Position.X, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*1.15f), new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing*2, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*1.15f), 3, ToColor(new ColorBGRA(SeparatorColor.R, SeparatorColor.G, SeparatorColor.B, (byte) Opacity)));

            //Drawing.DrawLine(new Vector2(Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*1.85f), new Vector2(Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2, lastSeparator.Positions[0].Y), 2, ToColor(new ColorBGRA(SeparatorColor.R, SeparatorColor.G, SeparatorColor.B, (byte) Opacity)));

            EloBuddy.SDK.Rendering.Line.DrawLine(ToColor(new ColorBGRA(SeparatorColor.R, SeparatorColor.G, SeparatorColor.B, (byte)Opacity)), 3, new Vector2(Position.X, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height * 1.15f), new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing * 2, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height * 1.15f));


            EloBuddy.SDK.Rendering.Line.DrawLine(ToColor(new ColorBGRA(SeparatorColor.R, SeparatorColor.G, SeparatorColor.B, (byte)Opacity)),2, new Vector2(Position.X + GetMaxItemNameTextLength() + DefaultSpacing * 2, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height * 1.85f), new Vector2(Position.X + GetMaxItemNameTextLength() + DefaultSpacing * 2, lastSeparator.Positions[0].Y));

            foreach (var permaShowItem in Wrapper.Bind<DataHandlerModule>().PermaShowItems)
            {
                if (permaShowItem.Value.GetType() == typeof (MenuItem))
                {
                    permaShowItem.Key.ItemNameText.Color = new ColorBGRA(TextColor.R, TextColor.G, TextColor.B, (byte)Opacity);
                    permaShowItem.Key.ItemValueText.Color = new ColorBGRA(TextColor.R, TextColor.G, TextColor.B, (byte)Opacity);
                    permaShowItem.Key.ItemNameText.Message = permaShowItem.Value.Get<MenuItem>().ItemName;
                    permaShowItem.Key.ItemNameText.Draw();
                    permaShowItem.Key.ItemValueText.Message = permaShowItem.Value.Get<MenuItem>().Value
                        ? "[ ✓ ] Enabled"
                        : "[ X ] Disabled";
                    permaShowItem.Key.ItemValueText.Draw();
                } else if (permaShowItem.Value.GetType() == typeof(BoolItem))
                {
                    permaShowItem.Key.ItemNameText.Color = new ColorBGRA(TextColor.R, TextColor.G, TextColor.B, (byte)Opacity);
                    permaShowItem.Key.ItemValueText.Color = new ColorBGRA(TextColor.R, TextColor.G, TextColor.B, (byte)Opacity);
                    permaShowItem.Key.ItemNameText.Message = permaShowItem.Value.Get<BoolItem>().ItemName;
                    permaShowItem.Key.ItemNameText.Draw();
                    permaShowItem.Key.ItemValueText.Message = permaShowItem.Value.Get<BoolItem>().Value
                        ? "[ ✓ ] Enabled"
                        : "[ X ] Disabled";
                    permaShowItem.Key.ItemValueText.Draw();
                }
                else if (permaShowItem.Value.GetType() == typeof(StringItem))
                {
                    permaShowItem.Key.ItemNameText.Color = new ColorBGRA(TextColor.R, TextColor.G, TextColor.B, (byte)Opacity);
                    permaShowItem.Key.ItemValueText.Color = new ColorBGRA(TextColor.R, TextColor.G, TextColor.B, (byte)Opacity);
                    permaShowItem.Key.ItemNameText.Message = permaShowItem.Value.Get<StringItem>().ItemName;
                    permaShowItem.Key.ItemNameText.Draw();
                    permaShowItem.Key.ItemValueText.Message = permaShowItem.Value.Get<StringItem>().Value;
                    permaShowItem.Key.ItemValueText.Draw();
                }
            }

            foreach (var separator in Wrapper.Bind<DataHandlerModule>().Separators)
            {
                //Drawing.DrawLine(separator.Positions[0], separator.Positions[1], separator.Width, ToColor(new ColorBGRA(separator.Color.R, separator.Color.G, separator.Color.B, (byte)Opacity)));

                EloBuddy.SDK.Rendering.Line.DrawLine(ToColor(new ColorBGRA(separator.Color.R, separator.Color.G, separator.Color.B, (byte)Opacity)), separator.Width, separator.Positions[0], separator.Positions[1]);
            }

            foreach (var underline in Wrapper.Bind<DataHandlerModule>().Underlines)
            {
                //Drawing.DrawLine(underline.Positions[0], underline.Positions[1], underline.Width, ToColor(new ColorBGRA(underline.Color.R, underline.Color.G, underline.Color.B, (byte)Opacity)));

                EloBuddy.SDK.Rendering.Line.DrawLine(ToColor(new ColorBGRA(underline.Color.R, underline.Color.G, underline.Color.B, (byte)Opacity)), underline.Width, underline.Positions[0], underline.Positions[1]);
            }
        }

        private void UpdatePositions()
        {
            if(!Enabled || Opacity == 0)
                return;

            var itenNameXPosition = (int) Position.X + DefaultSpacing;
            var itemValueXPosition = (int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f);

            foreach (var re in Wrapper.Bind<DataHandlerModule>().PermaShowItems)
            {
                var index = Wrapper.Bind<DataHandlerModule>().PermaShowItems.Keys.ToList().IndexOf(re.Key);
                var lastItem = Wrapper.Bind<DataHandlerModule>().PermaShowItems.Last();
                var yPosition = index == 0
                    ? (int) (Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*2f)
                    : (int) (Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*2f) + (int) ((lastItem.Key.ItemNameText.Height + 10)*index);

                Wrapper.Bind<DataHandlerModule>().PermaShowItems.Keys.ToList()[index].ItemNameText.X = itenNameXPosition;
                Wrapper.Bind<DataHandlerModule>().PermaShowItems.Keys.ToList()[index].ItemNameText.Y = yPosition;
                Wrapper.Bind<DataHandlerModule>().PermaShowItems.Keys.ToList()[index].ItemValueText.X = itemValueXPosition;
                Wrapper.Bind<DataHandlerModule>().PermaShowItems.Keys.ToList()[index].ItemValueText.Y = yPosition;
            }

            foreach (var sep in Wrapper.Bind<DataHandlerModule>().Separators)
            {
                var index = Wrapper.Bind<DataHandlerModule>().Separators.IndexOf(sep);
                var xPositon = Position.X + GetMaxTextLength() + DefaultSpacing*2;
                var permashowItem = Wrapper.Bind<DataHandlerModule>().PermaShowItems.Keys.ToList();
                float yPosition;

                switch (index)
                {
                    case 0:
                        yPosition = Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*1.85f;
                        break;
                    case 1:
                        yPosition = Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*2f + permashowItem[0].ItemNameText.Height + 5;
                        break;
                    default:
                        yPosition = permashowItem[index - 2].ItemNameText.Y +
                                    permashowItem[index - 2].ItemNameText.Height + 10 +
                                    permashowItem[index - 2].ItemNameText.Height + 5;
                        break;
                }

                Wrapper.Bind<DataHandlerModule>().Separators[index].Positions = new[]
                {new Vector2(Position.X, yPosition), new Vector2(xPositon, yPosition)};
                Core.DelayAction(() => Wrapper.Bind<DataHandlerModule>().Separators[index].Color = SeparatorColor, 500);
            }

            foreach (var underline in Wrapper.Bind<DataHandlerModule>().Underlines)
            {
                var permaShowItem =
                    Wrapper.Bind<DataHandlerModule>().PermaShowItems.Where(
                        x => x.Value.GetType() == typeof (BoolItem) || x.Value.GetType() == typeof (MenuItem)).ToArray()
                        [Wrapper.Bind<DataHandlerModule>().Underlines.IndexOf(underline)];
                var x1Position = permaShowItem.Key.ItemValueText.X;
                var x2Position = itemValueXPosition + permaShowItem.Key.ItemValueText.GetTextRectangle().Width;
                var yPosition = permaShowItem.Key.ItemValueText.Y + permaShowItem.Key.ItemValueText.Height;

                if (permaShowItem.Value.GetType() == typeof (MenuItem))
                {
                    Wrapper.Bind<DataHandlerModule>().Underlines[Wrapper.Bind<DataHandlerModule>().Underlines.IndexOf(underline)].Color = permaShowItem.Value.Get<MenuItem>().Value
                        ? EnabledUnderlineColor
                        : DisabledUnderlineColor;
                }
                else if (permaShowItem.Value.GetType() == typeof (BoolItem))
                {
                    Wrapper.Bind<DataHandlerModule>().Underlines[Wrapper.Bind<DataHandlerModule>().Underlines.IndexOf(underline)].Color = permaShowItem.Value.Get<BoolItem>().Value
                        ? EnabledUnderlineColor
                        : DisabledUnderlineColor;
                }

                Wrapper.Bind<DataHandlerModule>().Underlines[Wrapper.Bind<DataHandlerModule>().Underlines.IndexOf(underline)].Positions = new[]
                {new Vector2(x1Position + 25, yPosition), new Vector2(x2Position, yPosition)};
            }
        }

        private int CountItems()
        {
            return Wrapper.Bind<DataHandlerModule>().PermaShowItems.Count;
        }


        internal override T AddItem<T>(string uniqueId, T value)
        {
            if (!Wrapper.Bind<DataHandlerModule>().PermaShowItems.Any())
            {
                if (value.GetType() == typeof(BoolItem))
                {
                    var data = value as BoolItem;

                    if (!string.IsNullOrEmpty(data?.ItemName) && PermaShowItemTextHeight > 1)
                    {
                        Wrapper.Bind<DataHandlerModule>().Separators.Add(new Separator
                        {
                            Color = new ColorBGRA(16, 29, 29, 255),
                            Positions = new[] { new Vector2(200, 130), new Vector2(500, 130) },
                            Width = 2
                        });
                        
                        var itemName = new Text(data.ItemName, PermaShowItemTextHeight, 215, 135,
                            new ColorBGRA(109, 101, 64, 255));

                        var itemValue = new Text(data.Value ? "[ ✓ ] Enabled" : "[ X ] Disabled", PermaShowItemTextHeight, 350, 135, new ColorBGRA(109, 101, 64, 255));

                        var item = new BoolItem(data.ItemName, data.Value);

                        Wrapper.Bind<DataHandlerModule>().PermaShowItems.Add(new ValueBase(uniqueId, itemName, itemValue, TextColor), item);

                        Wrapper.Bind<DataHandlerModule>().Underlines.Add(new Separator
                        {
                            Color = data.Value ? new ColorBGRA(173, 255, 47, 255) : new ColorBGRA(255, 0, 0, 255),
                            Positions = new[] { new Vector2(376, 150), new Vector2(350 + itemValue.GetTextRectangle().Width, 150) },
                            Width = 1
                        });

                        Wrapper.Bind<DataHandlerModule>().Separators.Add(new Separator
                        {
                            Color = new ColorBGRA(16, 29, 29, 255),
                            Positions = new[] { new Vector2(200, 155), new Vector2(500, 155) },
                            Width = 2
                        });
                        return (T)Convert.ChangeType(item, typeof(BoolItem));
                    }
                }
                if (value.GetType() == typeof(MenuItem))
                {
                    var data = value as MenuItem;

                    if (!string.IsNullOrEmpty(data?.ItemName) && PermaShowItemTextHeight > 1)
                    {
                        var menu = MenuManager.MenuValues;

                        var itemName = new Text(data.ItemName, PermaShowItemTextHeight, (int)Position.X + DefaultSpacing, (int)(Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height * 2f), TextColor);
                        var itemValue = new Text(menu[data.MenuItemName] ? "[ ✓ ] Enabled" : "[ X ] Disabled", PermaShowItemTextHeight, (int)(Position.X + itemName.GetTextRectangle().Width + DefaultSpacing * 2.5f), (int)(Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height * 2f), TextColor);

                        var item = new MenuItem(data.ItemName, data.MenuItemName);

                        Wrapper.Bind<DataHandlerModule>().PermaShowItems.Add(new ValueBase(uniqueId, itemName, itemValue, TextColor), item);

                        Wrapper.Bind<DataHandlerModule>().Separators.Add(new Separator
                        {
                            Color = SeparatorColor,
                            Positions = new[]
                            {
                                new Vector2(Position.X, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*1.85f), new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*1.85f)
                            },
                            Width = 2
                        });

                        Wrapper.Bind<DataHandlerModule>().Underlines.Add(new Separator
                        {
                            Color = menu[data.MenuItemName] ? EnabledUnderlineColor : DisabledUnderlineColor,
                            Positions = new[]
                            {
                                new Vector2((int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f), Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*2f + PermaShowItemTextHeight), new Vector2((int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f) + itemValue.GetTextRectangle().Width + 27, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*2f + PermaShowItemTextHeight)
                            },
                            Width = 1
                        });

                        Wrapper.Bind<DataHandlerModule>().Separators.Add(new Separator
                        {
                            Color = SeparatorColor,
                            Positions = new[]
                            {
                                new Vector2(Position.X, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*2f + PermaShowItemTextHeight + 5), new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing, Position.Y + Wrapper.Bind<DataHandlerModule>().HeaderText.Height*2f + PermaShowItemTextHeight + 5)
                            },
                            Width = 2
                        });
                        return (T)Convert.ChangeType(item, typeof(MenuItem));
                    }
                }
                if (value.GetType() == typeof(StringItem))
                {
                    var data = value as StringItem;

                    if (!string.IsNullOrEmpty(data?.ItemName) && PermaShowItemTextHeight > 1)
                    {
                        Wrapper.Bind<DataHandlerModule>().Separators.Add(new Separator
                        {
                            Color = SeparatorColor,
                            Positions = new[] { new Vector2(200, 130), new Vector2(500, 130) },
                            Width = 2
                        });

                        var itemName = new Text(data.ItemName, PermaShowItemTextHeight, 215, 135, TextColor);

                        var itemValue = new Text(data.Value, PermaShowItemTextHeight, 350, 135, TextColor);

                        var item = new StringItem(data.ItemName, data.Value);

                        Wrapper.Bind<DataHandlerModule>().PermaShowItems.Add(new ValueBase(uniqueId, itemName, itemValue, TextColor), item);

                        Wrapper.Bind<DataHandlerModule>().Separators.Add(new Separator
                        {
                            Color = SeparatorColor,
                            Positions = new[] { new Vector2(200, 155), new Vector2(500, 155) },
                            Width = 2
                        });
                        return (T)Convert.ChangeType(item, typeof(StringItem));
                    }
                }
            }
            else
            {
                if (value.GetType() == typeof(MenuItem))
                {
                    var data = value as MenuItem;

                    if (!string.IsNullOrEmpty(data?.ItemName) && PermaShowItemTextHeight > 1)
                    {
                        var menu = MenuManager.MenuValues;
                        var lastItem = Wrapper.Bind<DataHandlerModule>().PermaShowItems.Keys.Last();

                        var itemName = new Text(data.ItemName, PermaShowItemTextHeight, (int)Position.X + DefaultSpacing,
                            (int)(lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10), TextColor);
                        var itemValue = new Text(menu[data.MenuItemName] ? "[ ✓ ] Enabled" : "[ X ] Disabled",
                            PermaShowItemTextHeight,
                            (int)(Position.X + itemName.GetTextRectangle().Width + DefaultSpacing * 2.5f),
                            (int)(lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10), TextColor);

                        var item = new MenuItem(data.ItemName, data.MenuItemName);

                        Wrapper.Bind<DataHandlerModule>().PermaShowItems.Add(new ValueBase(uniqueId, itemName, itemValue, TextColor), item);

                        Wrapper.Bind<DataHandlerModule>().Underlines.Add(new Separator
                        {
                            Color = menu[data.MenuItemName] ? EnabledUnderlineColor : DisabledUnderlineColor,
                            Positions = new[]
                            {
                                new Vector2((int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f),
                                    (int) (lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10) + PermaShowItemTextHeight),
                                new Vector2(
                                    (int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f) +
                                    itemValue.GetTextRectangle().Width,
                                    (int) (lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10) + PermaShowItemTextHeight)
                            },
                            Width = 1
                        });

                        Wrapper.Bind<DataHandlerModule>().Separators.Add(new Separator
                        {
                            Color = SeparatorColor,
                            Positions = new[]
                                {
                                new Vector2(Position.X, (int) (lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10) + PermaShowItemTextHeight + 5), new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing, (int) (lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10) + PermaShowItemTextHeight + 5)
                            },
                            Width = 2
                        });
                        return (T)Convert.ChangeType(item, typeof(MenuItem));
                    }
                }
                if (value.GetType() == typeof(BoolItem))
                {
                    var data = value as BoolItem;

                    if (!string.IsNullOrEmpty(data?.ItemName) && PermaShowItemTextHeight > 1)
                    {
                        var lastItem = Wrapper.Bind<DataHandlerModule>().PermaShowItems.Keys.Last();

                        var itemName = new Text(data.ItemName, PermaShowItemTextHeight, (int)Position.X + DefaultSpacing, (int)(lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10), TextColor);
                        var itemValue = new Text(data.Value ? "[ ✓ ] Enabled" : "[ X ] Disabled", PermaShowItemTextHeight, (int)(Position.X + itemName.GetTextRectangle().Width + DefaultSpacing * 2.5f), (int)(lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10), TextColor);

                        var item = new BoolItem(data.ItemName, data.Value);

                        Wrapper.Bind<DataHandlerModule>().PermaShowItems.Add(new ValueBase(uniqueId, itemName, itemValue, TextColor), item);

                        Wrapper.Bind<DataHandlerModule>().Underlines.Add(new Separator
                        {
                            Color = data.Value ? EnabledUnderlineColor : DisabledUnderlineColor,
                            Positions = new[]
                            {
                                new Vector2((int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f), (int) (lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10) + PermaShowItemTextHeight), new Vector2((int) (Position.X + GetMaxItemNameTextLength() + DefaultSpacing*2.5f) + itemValue.GetTextRectangle().Width, (int) (lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10) + PermaShowItemTextHeight)
                            },
                            Width = 1
                        });

                        Wrapper.Bind<DataHandlerModule>().Separators.Add(new Separator
                        {
                            Positions = new[]
                            {
                                new Vector2(Position.X, (int) (lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10) + PermaShowItemTextHeight + 5), new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing, (int) (lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10) + PermaShowItemTextHeight + 5)
                            },
                            Width = 2
                        });
                        return (T)Convert.ChangeType(item, typeof(BoolItem));
                    }
                }
                else if (value.GetType() == typeof(StringItem))
                {
                    var data = value as StringItem;

                    if (!string.IsNullOrEmpty(data?.ItemName) && PermaShowItemTextHeight > 1)
                    {
                        var lastItem = Wrapper.Bind<DataHandlerModule>().PermaShowItems.Keys.Last();

                        var itemName = new Text(data.ItemName, PermaShowItemTextHeight, (int)Position.X + DefaultSpacing, (int)(lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10), TextColor);
                        var itemValue = new Text(data.Value, PermaShowItemTextHeight, (int)(Position.X + itemName.GetTextRectangle().Width + DefaultSpacing * 2.5f), (int)(lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10), TextColor);

                        var item = new StringItem(data.ItemName, data.Value);

                        Wrapper.Bind<DataHandlerModule>().PermaShowItems.Add(new ValueBase(uniqueId, itemName, itemValue, TextColor), item);

                        Wrapper.Bind<DataHandlerModule>().Separators.Add(new Separator
                        {
                            Color = SeparatorColor,
                            Positions = new[]
                            {
                                new Vector2(Position.X, (int) (lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10) + PermaShowItemTextHeight + 5), new Vector2(Position.X + GetMaxTextLength() + DefaultSpacing, (int) (lastItem.ItemNameText.Y + lastItem.ItemNameText.Height + 10) + PermaShowItemTextHeight + 5)
                            },
                            Width = 2
                        });
                        return (T)Convert.ChangeType(item, typeof(StringItem));
                    }
                }
            }
            return (T)(object)null;
        }

        private bool IsPositionOnPermaShow(Vector2 position)
        {
            if (!Enabled)
                return false;

            var lastSeparator = Wrapper.Bind<DataHandlerModule>().Separators.Last();

            return position.X >= Position.X && position.X <= Position.X + GetMaxTextLength() + DefaultSpacing * 2 && position.Y >= Position.Y && position.Y <= lastSeparator.Positions[0].Y + 5;
        }

        private static void Drawing_OnPostReset(EventArgs args)
        {
        }

        private static void Drawing_OnPreReset(EventArgs args)
        {
        }
    }
}