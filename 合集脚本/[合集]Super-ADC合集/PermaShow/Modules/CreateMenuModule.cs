#region Licensing
// ---------------------------------------------------------------------
// <copyright file="CreateMenuModule.cs" company="EloBuddy">
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
namespace Marksman_Master.PermaShow.Modules
{
    using EloBuddy.SDK.Menu;

    using EloBuddy.SDK.Menu.Values;

    using SharpDX;

    using Utils;

    internal sealed class CreateMenuModule : Wrapper
    {
        public bool Enabled
        {
            get
            {
                return Menu?["Enable"] != null && Menu["Enable"].Cast<CheckBox>().CurrentValue;
            }
            set
            {
                if (Menu?["Enable"] == null)
                    return;

                Menu["Enable"].Cast<CheckBox>().CurrentValue = value;
            }
        }

        public int DefaultSpacing
        {
            get { return Menu?["Spacing"]?.Cast<Slider>().CurrentValue ?? 0; }
            set
            {
                if (Menu?["Spacing"] == null)
                    return;

                Menu["Spacing"].Cast<Slider>().CurrentValue = value;
            }
        }

        public int Opacity
        {
            get { return Menu?["Opacity"]?.Cast<Slider>().CurrentValue ?? 0; }
            set
            {
                if (Menu?["Opacity"] == null)
                    return;

                Menu["Opacity"].Cast<Slider>().CurrentValue = value;
            }
        }

        public Menu Menu { get; private set; }

        public string HeaderName { get; set; }
        
        private ColorPicker[] ColorPicker { get; } = new ColorPicker[5];

        public ColorBGRA BackgroundColor => ColorPicker[0].Color;
        public ColorBGRA SeparatorColor => ColorPicker[1].Color;
        public ColorBGRA EnabledUnderlineColor => ColorPicker[2].Color;
        public ColorBGRA DisabledUnderlineColor => ColorPicker[3].Color;
        public ColorBGRA TextColor => ColorPicker[4].Color;

        public override void Load()
        {
            ColorPicker[0] = new ColorPicker(HeaderName + "." + "BackgroundColor", new ColorBGRA(14, 19, 20, 215));
            ColorPicker[1] = new ColorPicker(HeaderName + "." + "SeparatorColor", new ColorBGRA(16, 29, 29, 255));
            ColorPicker[2] = new ColorPicker(HeaderName + "." + "EnabledUnderlineColor",new ColorBGRA(173, 255, 47, 255));
            ColorPicker[3] = new ColorPicker(HeaderName + "." + "DisabledUnderlineColor", new ColorBGRA(255, 0, 0, 255));
            ColorPicker[4] = new ColorPicker(HeaderName + "." + "TextColor", new ColorBGRA(109, 101, 64, 255));
        }

        public void CreateMenu()
        {
            Menu = MenuManager.Menu.AddSubMenu(HeaderName, HeaderName);

            Menu.Add("Enable", new CheckBox("Enable PermaShow", false));
            Menu.Add("Spacing", new Slider("Spacing", 25, 10, 50));
            Menu.Add("Opacity", new Slider("Opacity", 255, 0, 255));
            
            Menu.AddSeparator(5);
            Menu.AddLabel("Colors settings : ");

            Menu.Add("BackgroundColor", new CheckBox("Change background color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[0].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            Menu.Add("BackgroundColorReset", new CheckBox("Reset background color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                FileHandler.WriteToDataFile(HeaderName + "." + "BackgroundColor", new ColorBGRA(14, 19, 20, 215));

                ColorPicker[0].Color = new ColorBGRA(14, 19, 20, 215);
                a.CurrentValue = false;
            };
            Menu.AddSeparator(2);

            Menu.Add("SeparatorColor", new CheckBox("Change separator color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[1].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            Menu.Add("SeparatorColorReset", new CheckBox("Reset separator color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                FileHandler.WriteToDataFile(HeaderName + "." + "SeparatorColor", new ColorBGRA(16, 29, 29, 255));

                ColorPicker[1].Color = new ColorBGRA(16, 29, 29, 255);
                a.CurrentValue = false;
            };
            Menu.AddSeparator(2);

            Menu.Add("EnabledUnderlineColor", new CheckBox("Change enabled underline color", false)).OnValueChange +=
                (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[2].Initialize(System.Drawing.Color.Aquamarine);
                    a.CurrentValue = false;
                };
            Menu.Add("EnabledUnderlineColorReset", new CheckBox("Reset enabled underline color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                FileHandler.WriteToDataFile(HeaderName + "." + "EnabledUnderlineColor", new ColorBGRA(173, 255, 47, 255));

                ColorPicker[2].Color = new ColorBGRA(173, 255, 47, 255);

                a.CurrentValue = false;
            };
            Menu.AddSeparator(2);

            Menu.Add("DisabledUnderlineColor", new CheckBox("Change disabled underline color", false)).OnValueChange +=
                (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    ColorPicker[3].Initialize(System.Drawing.Color.Aquamarine);
                    a.CurrentValue = false;
                };
            Menu.Add("DisabledUnderlineColorReset", new CheckBox("Reset disabled underline color", false)).OnValueChange += (a, b) =>
                {
                    if (!b.NewValue)
                        return;

                    FileHandler.WriteToDataFile(HeaderName + "." + "DisabledUnderlineColor", new ColorBGRA(255, 0, 0, 255));

                    ColorPicker[3].Color = new ColorBGRA(255, 0, 0, 255);

                    a.CurrentValue = false;
                };
            Menu.AddSeparator(2);

            Menu.Add("TextColor", new CheckBox("Change text color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                ColorPicker[4].Initialize(System.Drawing.Color.Aquamarine);
                a.CurrentValue = false;
            };
            Menu.Add("TextColorReset", new CheckBox("Reset text color", false)).OnValueChange += (a, b) =>
            {
                if (!b.NewValue)
                    return;

                FileHandler.WriteToDataFile(HeaderName + "." + "TextColor", new ColorBGRA(109, 101, 64, 255));

                ColorPicker[4].Color = new ColorBGRA(109, 101, 64, 255);

                a.CurrentValue = false;
            };
        }
    }
}