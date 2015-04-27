using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace SAssemblies.Timers
{
    class Timer
    {
        public static Menu.MenuItemSettings Timers = new Menu.MenuItemSettings();

        private Timer()
        {

        }

        ~Timer()
        {
            
        }

        private static void SetupMainMenu()
        {
            var menu = new LeagueSharp.Common.Menu("STimers", "SAssembliesSTimers", true);
            SetupMenu(menu);
            menu.AddToMainMenu();
        }

        public static Menu.MenuItemSettings SetupMenu(LeagueSharp.Common.Menu menu)
        {
            Language.SetLanguage();
            Timers.Menu = menu.AddSubMenu(new LeagueSharp.Common.Menu(Language.GetString("TIMERS_TIMER_MAIN"), "SAssembliesTimers"));
            Timers.MenuItems.Add(
                Timers.Menu.AddItem(new MenuItem("SAssembliesTimersTextScale", Language.GetString("TIMERS_TIMER_SCALE")).SetValue(new Slider(12, 8, 20))));
            Timers.MenuItems.Add(Timers.Menu.AddItem(new MenuItem("SAssembliesTimersActive", Language.GetString("GLOBAL_ACTIVE")).SetValue(true)));
            return Timers;
        }

        private String AlignTime(float endTime)
        {
            if (!float.IsInfinity(endTime) && !float.IsNaN(endTime))
            {
                var m = (float)Math.Floor(endTime / 60);
                var s = (float)Math.Ceiling(endTime % 60);
                String ms = (s < 10 ? m + ":0" + s : m + ":" + s);
                return ms;
            }
            return "";
        }

        public static bool PingAndCall(String text, Vector3 pos, bool call = true, bool ping = true)
        {
            if (ping)
            {
                for (int i = 0; i < Timers.GetMenuItem("SAssembliesTimersPingTimes").GetValue<Slider>().Value; i++)
                {
                    GamePacket gPacketT;
                    if (Timers.GetMenuItem("SAssembliesTimersLocalPing").GetValue<bool>())
                    {
                        gPacketT =
                            Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(pos[0], pos[1], 0, 0,
                                Packet.PingType.Normal));
                        gPacketT.Process();
                    }
                    else if (!Timers.GetMenuItem("SAssembliesTimersLocalPing").GetValue<bool>() &&
                             Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsServerChatPingActive")
                                 .GetValue<bool>())
                    {
                        gPacketT = Packet.C2S.Ping.Encoded(new Packet.C2S.Ping.Struct(pos.X, pos.Y));
                        gPacketT.Send();
                    }
                }
            }
            if (call)
            {
                if (Timers.GetMenuItem("SAssembliesTimersChatChoice").GetValue<StringList>().SelectedIndex == 1)
                {
                    Game.PrintChat(text);
                }
                else if (Timers.GetMenuItem("SAssembliesTimersChatChoice").GetValue<StringList>().SelectedIndex == 2 &&
                         Menu.GlobalSettings.GetMenuItem("SAssembliesGlobalSettingsServerChatPingActive").GetValue<bool>())
                {
                    Game.Say(text);
                }
            }
            return true;
        }
    }
}
