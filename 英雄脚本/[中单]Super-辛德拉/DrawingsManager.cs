using System;
using EloBuddy;
using EloBuddy.SDK.Rendering;
using static Dark_Syndra.Menus;
using static Dark_Syndra.SpellsManager;
using System.Drawing;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK;

namespace Dark_Syndra

{
    internal class DrawingsManager
    {
        public static void InitializeDrawings()
        {
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            DamageIndicator.Init();
        }




        private static void Drawing_OnDraw(EventArgs args)
        {
            var readyDraw = DrawingsMenu["readyDraw"].Cast<CheckBox>().CurrentValue;
            var target = TargetSelector.GetTarget(SpellsManager.E.Range + 20000, DamageType.Mixed);
            //Drawings
            if (DrawingsMenu["qDraw"].Cast<CheckBox>().CurrentValue && readyDraw
                ? Q.IsReady()
                : DrawingsMenu["qDraw"].Cast<CheckBox>().CurrentValue)
                Circle.Draw(QColorSlide.GetSharpColor(), Q.Range, 1f, Player.Instance);

            if (DrawingsMenu["wDraw"].Cast<CheckBox>().CurrentValue && readyDraw
                ? W.IsReady()
                : DrawingsMenu["wDraw"].Cast<CheckBox>().CurrentValue)
                Circle.Draw(WColorSlide.GetSharpColor(), W.Range, 1f, Player.Instance);

            if (DrawingsMenu["eDraw"].Cast<CheckBox>().CurrentValue && readyDraw
                ? E.IsReady()
                : DrawingsMenu["eDraw"].Cast<CheckBox>().CurrentValue)
                Circle.Draw(EColorSlide.GetSharpColor(), E.Range, 1f, Player.Instance);

            if (DrawingsMenu["rDraw"].Cast<CheckBox>().CurrentValue && readyDraw
                ? R.IsReady()
                : DrawingsMenu["rDraw"].Cast<CheckBox>().CurrentValue)
                Circle.Draw(EColorSlide.GetSharpColor(), R.Range, 1f, Player.Instance);

            if (DrawingsMenu["qeDraw"].Cast<CheckBox>().CurrentValue && readyDraw
                ? Q.IsReady() && E.IsReady()
                : DrawingsMenu["qeDraw"].Cast<CheckBox>().CurrentValue)
                Circle.Draw(EColorSlide.GetSharpColor(), QE.Range, 1f, Player.Instance);

            if (target == null) return;
            if (!(target.Health <=
                  target.GetTotalDamage())) return;
            Drawing.DrawText(Drawing.WorldToScreen(target.Position).X - 60,
                Drawing.WorldToScreen(target.Position).Y + 10,
                Color.Gold, "Killable with Combo");
        }



        private static void Drawing_OnEndScene(EventArgs args)
        {
        }
    }

}