using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace SAssemblies.Timers
{
    class Inhibitor
    {
        public static Menu.MenuItemSettings InhibitorTimer = new Menu.MenuItemSettings(typeof(Inhibitor));

        private static readonly Utility.Map GMap = Utility.Map.GetMap();
        private static InhibitorObject _inhibitors;
        private int lastGameUpdateTime = 0;

        public Inhibitor()
        {
            Game.OnUpdate += Game_OnGameUpdate;
            InitInhibitorObjects();
        }

        ~Inhibitor()
        {
            Game.OnUpdate -= Game_OnGameUpdate;
            _inhibitors = null;
        }

        public bool IsActive()
        {
            return Timer.Timers.GetActive() && InhibitorTimer.GetActive();
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            InhibitorTimer.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("TIMERS_INHIBITOR_MAIN"), "SAssembliesTimersInhibitor"));
            InhibitorTimer.MenuItems.Add(
                InhibitorTimer.Menu.AddItem(new MenuItem("SAssembliesTimersInhibitorSpeech", Language.GetString("GLOBAL_VOICE")).SetValue(false)));
            InhibitorTimer.MenuItems.Add(
                InhibitorTimer.Menu.AddItem(new MenuItem("SAssembliesTimersInhibitorActive", Language.GetString("GLOBAL_ACTIVE")).SetValue(false)));
            return InhibitorTimer;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive() || lastGameUpdateTime + new Random().Next(500, 1000) > Environment.TickCount)
                return;

            lastGameUpdateTime = Environment.TickCount;

            if (InhibitorTimer.GetActive())
            {
                if (_inhibitors == null || _inhibitors.Inhibitors == null)
                    return;
                foreach (InhibitorObject inhibitor in _inhibitors.Inhibitors)
                {
                    if (inhibitor.Obj.Health > 0)
                    {
                        inhibitor.Locked = false;
                        inhibitor.NextRespawnTime = 0;
                        inhibitor.Called = false;
                    }
                    else if (inhibitor.Obj.Health < 1 && inhibitor.Locked == false)
                    {
                        inhibitor.Locked = true;
                        inhibitor.NextRespawnTime = inhibitor.RespawnTime + (int)Game.ClockTime;
                    }
                }
            }

            /////

            if (InhibitorTimer.GetActive())
            {
                if (_inhibitors.Inhibitors == null)
                    return;
                foreach (InhibitorObject inhibitor in _inhibitors.Inhibitors)
                {
                    if (inhibitor.Locked)
                    {
                        if (inhibitor.NextRespawnTime <= 0)
                            continue;
                        int time = Timer.Timers.GetMenuItem("SAssembliesTimersRemindTime").GetValue<Slider>().Value;
                        if (!inhibitor.Called && inhibitor.NextRespawnTime - (int)Game.ClockTime <= time &&
                            inhibitor.NextRespawnTime - (int)Game.ClockTime >= time - 1)
                        {
                            inhibitor.Called = true;
                            Timer.PingAndCall("Inhibitor respawns in " + time + " seconds!", inhibitor.Obj.Position);
                            if (InhibitorTimer.GetMenuItem("SAssembliesTimersInhibitorSpeech").GetValue<bool>())
                            {
                                Speech.Speak("Inhibitor respawns in " + time + " seconds!");
                            }
                        }
                    }
                }
            }
        }

        public void InitInhibitorObjects()
        {
            _inhibitors = new InhibitorObject();
            foreach (Obj_BarracksDampener inhib in ObjectManager.Get<Obj_BarracksDampener>())
            {
                _inhibitors.Inhibitors.Add(new InhibitorObject(inhib));
            }
        }

        public class InhibitorObject
        {
            public bool Called;
            public List<InhibitorObject> Inhibitors;
            public bool Locked;
            public int NextRespawnTime;
            public Obj_BarracksDampener Obj;
            public int RespawnTime;
            public int SpawnTime;
            public Render.Text TextMinimap;
            public Render.Text TextMap;

            public InhibitorObject()
            {
                Inhibitors = new List<InhibitorObject>();
            }

            public InhibitorObject(Obj_BarracksDampener obj)
            {
                Obj = obj;
                SpawnTime = (int)Game.ClockTime;
                RespawnTime = 300;
                NextRespawnTime = 0;
                Locked = false;
                Called = false;
                TextMinimap = new Render.Text(0, 0, "", Timer.Timers.GetMenuItem("SAssembliesTimersTextScale").GetValue<Slider>().Value, new ColorBGRA(Color4.White));
                Timer.Timers.GetMenuItem("SAssembliesTimersTextScale").ValueChanged += InhibitorObject_ValueChanged;
                TextMinimap.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                TextMinimap.PositionUpdate = delegate
                {
                    if (Obj.Position.Length().Equals(0.0f))
                        return new Vector2(0, 0);
                    Vector2 sPos = Drawing.WorldToMinimap(Obj.Position);
                    return new Vector2(sPos.X, sPos.Y);
                };
                TextMinimap.VisibleCondition = sender =>
                {
                    return Timer.Timers.GetActive() && InhibitorTimer.GetActive() && NextRespawnTime > 0;
                };
                TextMinimap.OutLined = true;
                TextMinimap.Centered = true;
                TextMinimap.Add();
                TextMap = new Render.Text(0, 0, "", (int)(Timer.Timers.GetMenuItem("SAssembliesTimersTextScale").GetValue<Slider>().Value * 3.5), new ColorBGRA(Color4.White));
                TextMap.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                TextMap.PositionUpdate = delegate
                {
                    if (Obj.Position.Length().Equals(0.0f))
                        return new Vector2(0, 0);
                    Vector2 sPos = Drawing.WorldToScreen(Obj.Position);
                    return new Vector2(sPos.X, sPos.Y);
                };
                TextMap.VisibleCondition = sender =>
                {
                    return Timer.Timers.GetActive() && InhibitorTimer.GetActive() && NextRespawnTime > 0;
                };
                TextMap.OutLined = true;
                TextMap.Centered = true;
                TextMap.Add();
            }

            void InhibitorObject_ValueChanged(object sender, OnValueChangeEventArgs e)
            {
                TextMinimap.Remove();
                TextMinimap.TextFontDescription = new FontDescription
                {
                    FaceName = "Calibri",
                    Height = e.GetNewValue<Slider>().Value,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                };
                TextMinimap.Add();
                TextMap.Remove();
                TextMap.TextFontDescription = new FontDescription
                {
                    FaceName = "Calibri",
                    Height = e.GetNewValue<Slider>().Value,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                };
                TextMap.Add();
            }
        }
    }
}
