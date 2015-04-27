using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using ClipperLib;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.GameFiles.AirClient;
using LeagueSharp.GameFiles.GameClient;
using LeagueSharp.GameFiles.Tools;
using SAwareness.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using Config = LeagueSharp.Common.Config;
using Font = SharpDX.Direct3D9.Font;
using Rectangle = SharpDX.Rectangle;

//Erstelle ein Thread, rufe darin die sich im event eingeschrieben methoden auf

namespace SAwareness
{
    class Menu
    {
        public static MenuItemSettings GlobalSettings = new MenuItemSettings();

        public class MenuItemSettings
        {
            public bool ForceDisable;
            public dynamic Item;
            public LeagueSharp.Common.Menu Menu;
            public List<MenuItem> MenuItems = new List<MenuItem>();
            public String Name;
            public List<MenuItemSettings> SubMenus = new List<MenuItemSettings>();
            public Type Type;

            public MenuItemSettings(Type type, dynamic item)
            {
                Type = type;
                Item = item;
            }

            public MenuItemSettings(dynamic item)
            {
                Item = item;
            }

            public MenuItemSettings(Type type)
            {
                Type = type;
            }

            public MenuItemSettings(String name)
            {
                Name = name;
            }

            public MenuItemSettings()
            {
            }

            public MenuItemSettings AddMenuItemSettings(String displayName, String name)
            {
                SubMenus.Add(new MenuItemSettings(name));
                MenuItemSettings tempSettings = GetMenuSettings(name);
                if (tempSettings == null)
                {
                    throw new NullReferenceException(name + " not found");
                }
                tempSettings.Menu = Menu.AddSubMenu(new LeagueSharp.Common.Menu(displayName, name));
                return tempSettings;
            }

            public bool GetActive()
            {
                if (Menu == null)
                    return false;
                foreach (MenuItem item in Menu.Items)
                {
                    if (item.DisplayName == Language.GetString("GLOBAL_ACTIVE"))
                    {
                        if (item.GetValue<bool>())
                        {
                            return true;
                        }
                        return false;
                    }
                }
                return false;
            }

            public void SetActive(bool active)
            {
                if (Menu == null)
                    return;
                foreach (MenuItem item in Menu.Items)
                {
                    if (item.DisplayName == Language.GetString("GLOBAL_ACTIVE"))
                    {
                        item.SetValue(active);
                        return;
                    }
                }
            }

            public MenuItem GetMenuItem(String menuName)
            {
                if (Menu == null)
                    return null;
                foreach (MenuItem item in Menu.Items)
                {
                    if (item.Name == menuName)
                    {
                        return item;
                    }
                }
                return null;
            }

            public LeagueSharp.Common.Menu GetSubMenu(String menuName)
            {
                if (Menu == null)
                    return null;
                return Menu.SubMenu(menuName);
            }

            public MenuItemSettings GetMenuSettings(String name)
            {
                foreach (MenuItemSettings menu in SubMenus)
                {
                    if (menu.Name.Contains(name))
                        return menu;
                }
                return null;
            }
        }

        //public static MenuItemSettings  = new MenuItemSettings();
    }

    internal static class Log
    {
        public static String File = "C:\\SAwareness.log";
        public static String Prefix = "Packet";

        public static void LogString(String text, String file = null, String prefix = null)
        {
            switch (text)
            {
                case "missile":
                case "DrawFX":
                case "Mfx_pcm_mis.troy":
                case "Mfx_bcm_tar.troy":
                case "Mfx_bcm_mis.troy":
                case "Mfx_pcm_tar.troy":
                    return;
            }
            LogWrite(text, file, prefix);
        }

        private static void LogGamePacket(GamePacket result, String file = null, String prefix = null)
        {
            byte[] b = new byte[result.Size()];
            long size = result.Size();
            int cur = 0;
            while (cur < size - 1)
            {
                b[cur] = result.ReadByte(cur);
                cur++;
            }
            LogPacket(b, file, prefix);
        }

        public static void LogPacket(byte[] data, String file = null, String prefix = null)
        {
            if (!(data[0].ToHexString().Equals("AE") || data[0].ToHexString().Equals("29") || data[0].ToHexString().Equals("1A") || data[0].ToHexString().Equals("34") || data[0].ToHexString().Equals("6E") || data[0].ToHexString().Equals("85") || data[0].ToHexString().Equals("C4") || data[0].ToHexString().Equals("61") || data[0].ToHexString().Equals("38") || data[0].ToHexString().Equals("FE")))
            LogWrite(BitConverter.ToString(data), file, prefix);
        }

        private static void LogWrite(String text, String file = null, String prefix = null)
        {
            if (text == null)
                return;
            if (file == null)
                file = File;
            if (prefix == null)
                prefix = Prefix;
            using (var stream = new StreamWriter(file, true))
            {
                stream.WriteLine(prefix + "@" + Game.ClockTime + ": " + text);
            }
        }
    }

    internal static class Common
    {
        public static bool IsOnScreen(Vector3 vector)
        {
            Vector2 screen = Drawing.WorldToScreen(vector);
            if (screen[0] < 0 || screen[0] > Drawing.Width || screen[1] < 0 || screen[1] > Drawing.Height)
                return false;
            return true;
        }

        public static bool IsOnScreen(Vector2 vector)
        {
            Vector2 screen = vector;
            if (screen[0] < 0 || screen[0] > Drawing.Width || screen[1] < 0 || screen[1] > Drawing.Height)
                return false;
            return true;
        }

        public static Size ScaleSize(this Size size, float scale, Vector2 mainPos = default(Vector2))
        {
            size.Height = (int) (((size.Height - mainPos.Y)*scale) + mainPos.Y);
            size.Width = (int) (((size.Width - mainPos.X)*scale) + mainPos.X);
            return size;
        }

        public static bool IsInside(Vector2 mousePos, Size windowPos, int width, int height)
        {
            return Utils.IsUnderRectangle(mousePos, windowPos.Width, windowPos.Height, width, height);
        }

        public static bool IsInside(Vector2 mousePos, Vector2 windowPos, int width, int height)
        {
            return Utils.IsUnderRectangle(mousePos, windowPos.X, windowPos.Y, width, height);
        }
    }

    internal class Downloader
    {
        public delegate void DownloadFinished(object sender, DlEventArgs args);

        public static String Host = "https://github.com/Screeder/SAwareness/raw/master/Sprites/SAwareness/";
        public static String Path = "CHAMP/";

        private readonly List<Files> _downloadQueue = new List<Files>();
        public event DownloadFinished DownloadFileFinished;

        public void AddDownload(String hostFile, String localFile)
        {
            _downloadQueue.Add(new Files(hostFile, localFile));
        }

        public void StartDownload()
        {
            StartDownloadInternal();
        }

        private async Task StartDownloadInternal()
        {
            var webClient = new WebClient();
            var tasks = new List<DlTask>();
            foreach (Files files in _downloadQueue)
            {
                Task t = webClient.DownloadFileTaskAsync(new Uri(Host + Path + files.OnlineFile), files.OfflineFile);
                tasks.Add(new DlTask(files, t));
            }
            foreach (DlTask task in tasks)
            {
                await task.Task;
                tasks.Remove(task);
                OnFinished(new DlEventArgs(task.Files));
            }
        }

        protected virtual void OnFinished(DlEventArgs args)
        {
            if (DownloadFileFinished != null)
                DownloadFileFinished(this, args);
        }

        public static void DownloadFile(String hostfile, String localfile)
        {
            var webClient = new WebClient();
            webClient.DownloadFile(Host + Path + hostfile, localfile);
        }

        public class DlEventArgs : EventArgs
        {
            public Files DlFiles;

            public DlEventArgs(Files files)
            {
                DlFiles = files;
            }
        }

        private struct DlTask
        {
            public readonly Files Files;
            public readonly Task Task;

            public DlTask(Files files, Task task)
            {
                Files = files;
                Task = task;
            }
        }

        public struct Files
        {
            public String OfflineFile;
            public String OnlineFile;

            public Files(String onlineFile, String offlineFile)
            {
                OnlineFile = onlineFile;
                OfflineFile = offlineFile;
            }
        }
    }

    public class RafLoader
    {
        private static bool loading = false;
        private static bool loaded = false;
        public static float LastLoadTime = 0;

        public enum ImageList
        {
            None,
            Champion,
            ChampionCircle,
            SpellPassive,
            SpellQ,
            SpellW,
            SpellE,
            SpellR,
            SpellSummoner1,
            SpellSummoner2
        }

        static RafLoader()
        {
            //new Thread(InitLoader).Start();
        }

        public static void InitLoader()
        {
            if (!loading && !loaded)
            {
                loading = true;
                AirGeneratedContent.Init();
            }
        }

        private static bool IsLoaded()
        {
            if (AirGeneratedContent.Items != null)
            {
                loading = false;
                loaded = true;
                return true;
            }
            return false;
        }

        private static string SmiteType(String name)
        {
            switch (name)
            {
                case "s5_summonersmiteplayerganker":
                    return "smite_blue.dds";
                    break;

                case "s5_summonersmiteduel":
                    return "smite_red.dds";
                    break;

                case "s5_summonersmitequick":
                    return "smite_silver.dds";
                    break;

                case "itemsmiteaoe":
                    return "smite_purple.dds";
                    break;

                case "summonersmite":
                    return "summoner_smite.dds";
                    break;

                default:
                    return null;
            }
        }

        private static String TemporaryIniBinFix(String name)
        {
            switch (name.ToLower())
            {
                //case "chogath":
                //    return "greenterror";
                //    break;

                //case "orianna":
                //    return "oriana";
                //    break;

                default:
                    return name;
            }
        }

        private static byte[] GetFileContent(String fileName, String optionalDirName = "")
        {
            if (fileName == null)
                return null;
            fileName = fileName.ToLower();
            foreach (var file in Archives.Files)
            {
                if (file.Key.Contains(fileName) && file.Key.Contains(optionalDirName))
                {
                    return file.Value.GetLastContent();
                }
            }
            return null;
        }

        private static bool FileExists(String fileName, String optionalDirName = "")
        {
            if (fileName == null)
                return false;
            fileName = fileName.ToLower();
            foreach (var file in Archives.Files)
            {
                if (file.Key.Contains(fileName) && file.Key.Contains(optionalDirName))
                {
                    return true;
                }
            }
            return false;
        }

        public static byte[] GetImage(String baseSkinName, ImageList list, String optionalName = "")
        {
            if (!IsLoaded())
                return null;
            var champion = AirGeneratedContent.Champions[baseSkinName];
            if (champion == null)
            {
                Console.Write("SAwareness: Can not get champion: " + baseSkinName);
                return null;
            }
            String imageStr = null;
            String category = "";
            Dictionary<uint, object> inibin = null;
            switch (list)
            {
                case ImageList.None:
                    imageStr = optionalName;
                    break;

                case ImageList.Champion:
                    category = "character";
                    if (FileExists(TemporaryIniBinFix(champion.skinName) + "_square", category))
                    {
                        imageStr = TemporaryIniBinFix(champion.skinName) + "_square";
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        imageStr = GetIniBinOptions(inibin, "iconsquare");
                    }
                    break;

                case ImageList.ChampionCircle:
                    category = "character";
                    if (FileExists(TemporaryIniBinFix(champion.skinName) + "_circle", category))
                    {
                        imageStr = TemporaryIniBinFix(champion.skinName) + "_circle";
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        imageStr = GetIniBinOptions(inibin, "iconcircle");
                    }
                    break;

                case ImageList.SpellPassive:
                    category = "character";
                    if (FileExists(champion.passiveIcon, category))
                    {
                        imageStr = champion.passiveIcon;
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        imageStr = GetIniBinOptions(inibin, "iconpassive");
                    }
                    break;

                case ImageList.SpellQ:
                    category = "character";
                    if (FileExists(champion.abilityIcon1, category))
                    {
                        imageStr = champion.abilityIcon1;
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    else
                    {
                        category = "spell";
                        if (FileExists(champion.abilityIcon1, category))
                        {
                            imageStr = champion.abilityIcon1;
                        }
                        if (imageStr != null)
                        {
                            break;
                        }
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        inibin = GetIniBin(GetIniBinOptions(inibin, "spell1"));
                        if (inibin != null)
                        {
                            category = "";
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    break;

                case ImageList.SpellW:
                    category = "character";
                    if (FileExists(champion.abilityIcon2, category))
                    {
                        imageStr = champion.abilityIcon2;
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    else
                    {
                        category = "spell";
                        if (FileExists(champion.abilityIcon2, category))
                        {
                            imageStr = champion.abilityIcon2;
                        }
                        if (imageStr != null)
                        {
                            break;
                        }
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        inibin = GetIniBin(GetIniBinOptions(inibin, "spell2"));
                        if (inibin != null)
                        {
                            category = "";
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    break;

                case ImageList.SpellE:
                    category = "character";
                    if (FileExists(champion.abilityIcon3, category))
                    {
                        imageStr = champion.abilityIcon3;
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    else
                    {
                        category = "spell";
                        if (FileExists(champion.abilityIcon3, category))
                        {
                            imageStr = champion.abilityIcon3;
                        }
                        if (imageStr != null)
                        {
                            break;
                        }
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        inibin = GetIniBin(GetIniBinOptions(inibin, "spell3"));
                        if (inibin != null)
                        {
                            category = "";
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    break;

                case ImageList.SpellR:
                    category = "character";
                    if (FileExists(champion.abilityIcon4, category))
                    {
                        imageStr = champion.abilityIcon4;
                    }
                    if (imageStr != null)
                    {
                        break;
                    }
                    else
                    {
                        category = "spell";
                        if (FileExists(champion.abilityIcon4, category))
                        {
                            imageStr = champion.abilityIcon4;
                        }
                        if (imageStr != null)
                        {
                            break;
                        }
                    }
                    inibin = GetIniBin(champion.skinName);
                    if (inibin != null)
                    {
                        inibin = GetIniBin(GetIniBinOptions(inibin, "spell4"));
                        if (inibin != null)
                        {
                            category = "";
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    break;

                case ImageList.SpellSummoner1:
                    category = "spells";
                    if (SmiteType(optionalName) == null)
                    {
                        inibin = GetIniBin(AirGeneratedContent.Spells[optionalName].name);
                        if (inibin != null)
                        {
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    else
                    {
                        imageStr = SmiteType(optionalName);
                    }
                    break;

                case ImageList.SpellSummoner2:
                    category = "spells";
                    if (SmiteType(optionalName) == null)
                    {
                        inibin = GetIniBin(AirGeneratedContent.Spells[optionalName].name);
                        if (inibin != null)
                        {
                            imageStr = GetIniBinOptions(inibin, "inventoryicon");
                        }
                    }
                    else
                    {
                        imageStr = SmiteType(optionalName);
                    }
                    break;

                default:
                    imageStr = "";
                    break;
            }
            LastLoadTime = Environment.TickCount;
            return GetFileContent(imageStr, category);
        }

        private static String GetIniBinOptions(Dictionary<uint, object> inibinValues, String inibinFile)
        {
            foreach (var value in inibinValues)
            {
                if (IniBinKeys.GetKey(value.Key).ToLower().Contains(inibinFile))
                {
                    if (GetFileContent(value.Value.ToString()) != null)
                    {
                        return value.Value.ToString();
                    }
                }
            }
            return null;
        }

        public static Dictionary<uint, object> GetIniBin(String baseSkinName)
        {
            byte[] bInibin = GetFileContent("/" + baseSkinName + ".inibin");
            return IniBinReader.GetValues(bInibin);
        }
    }

    public static class SpriteHelperNew
    {
        public enum TextureType
        {
            Default,
            Summoner,
            Item
        }

        public enum DownloadType
        {
            Champion,
            Spell,
            Summoner,
            Item
        }

        private static Downloader _downloader = new Downloader();
        public static readonly Dictionary<String, byte[]> MyResources = new Dictionary<String, byte[]>();

        //private static List<SpriteRef> Sprites = new List<SpriteRef>();

        static SpriteHelperNew()
        {
            ResourceSet resourceSet = Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            foreach (DictionaryEntry entry in resourceSet)
            {
                var conv = entry.Value as Bitmap;
                if (conv != null)
                {
                    MyResources.Add(entry.Key.ToString().ToLower(), (byte[])new ImageConverter().ConvertTo((Bitmap)entry.Value, typeof(byte[])));
                }
                else
                {
                    MyResources.Add(entry.Key.ToString().ToLower(), (byte[])entry.Value);
                }
            }
        }

        private static Dictionary<String, Bitmap> cachedMaps = new Dictionary<string, Bitmap>();

        public static String ConvertNames(String name)
        {
            if (name.ToLower().Contains("smite"))
            {
                return "SummonerSmite";
            }
            switch (name)
            {
                case "viw":
                    return "ViW";

                case "zedult":
                    return "ZedUlt";

                case "vayneinquisition":
                    return "VayneInquisition";

                case "reksaie":
                    return "RekSaiE";

                default:
                    return name;
            }
        }

        public static void DownloadImageOpGg(string name, String subFolder)
        {
            WebRequest request = null;
            WebRequest requestSize = null;
            request =
            WebRequest.Create("http://ss.op.gg/images/profile_icons/" + name);
            requestSize =
            WebRequest.Create("http://ss.op.gg/images/profile_icons/" + name);
            requestSize.Method = "HEAD";
            if (request == null || requestSize == null)
                return;
            try
            {
                long fileSize = 0;
                using (var resp = (HttpWebResponse)requestSize.GetResponse())
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        fileSize = resp.ContentLength;
                    }
                }
                if (fileSize == GetFileSize(name, subFolder))
                    return;
                Stream responseStream;
                using (var response = (HttpWebResponse)request.GetResponse())
                    if (response.StatusCode == HttpStatusCode.OK)
                        using (responseStream = response.GetResponseStream())
                        {
                            if (responseStream != null)
                            {
                                using (var memoryStream = new MemoryStream())
                                {
                                    responseStream.CopyTo(memoryStream);
                                    SaveImage(name, memoryStream.ToArray(), subFolder);
                                }
                            }
                        }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
            }
        }

        public static void DownloadImageRiot(string name, DownloadType type, String subFolder)
        {
            String version = "";
            try
            {
                String json = new WebClient().DownloadString("http://ddragon.leagueoflegends.com/realms/euw.json");
                version = (string)new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(json)["v"];
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
                return;
            }
            WebRequest request = null;
            WebRequest requestSize = null;
            name = ConvertNames(name);
            if (type == DownloadType.Champion)
            {
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/champion/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/champion/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Spell)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Summoner)
            {
                //summonerexhaust
                if (name.Contains("summonerodingarrison"))
                    name = "SummonerOdinGarrison";
                else
                    name = name[0].ToString().ToUpper() + name.Substring(1, 7) + name[8].ToString().ToUpper() + name.Substring(9, name.Length - 9);
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Item)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            if (request == null || requestSize == null)
                return;
            try
            {
                long fileSize = 0;
                using (WebResponse resp = requestSize.GetResponse())
                {
                    fileSize = resp.ContentLength;
                }
                if (fileSize == GetFileSize(name, subFolder))
                    return;
                Stream responseStream;
                using (WebResponse response = request.GetResponse())
                using (responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            responseStream.CopyTo(memoryStream);
                            SaveImage(name, memoryStream.ToArray(), subFolder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
            }
        }

        private static int GetFileSize(String name, String subFolder)
        {
            int size = 0;
            string loc = Path.Combine(new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp", "Assemblies", "cache",
                "SAwareness", subFolder, name  + ".png"
            });
            try
            {
                byte[] bitmap = File.ReadAllBytes(loc);
                size = bitmap.Length;
            }
            catch (Exception)
            {

            }
            return size;
        }

        public static void SaveImage(String name, /*Bitmap*/byte[] bitmap, String subFolder)
        {
            string loc = Path.Combine(new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp", "Assemblies", "cache",
                "SAwareness", subFolder, name  + ".png"
            });
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp",
                        "Assemblies", "cache", "SAwareness", subFolder));
            File.WriteAllBytes(loc, bitmap/*(byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]))*/);
        }

        public static void LoadTexture(String name, ref SpriteInfoNew spriteInfo, String subFolder)
        {
            if (spriteInfo == null)
                spriteInfo = new SpriteInfoNew();
            Byte[] bitmap = null;
            name = ConvertNames(name);
            string loc = Path.Combine(new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp", "Assemblies", "cache",
                "SAwareness", subFolder, name  + ".png"
            });
            try
            {
                bitmap = File.ReadAllBytes(loc);
                spriteInfo.Bitmap = (Bitmap)new ImageConverter().ConvertFrom(bitmap);
                spriteInfo.Sprite = new Render.Sprite(bitmap, new Vector2(0, 0));
                spriteInfo.DownloadFinished = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot load file: {0}, Exception: {1}", name, ex);
            }
        }

        public static void LoadTexture(String name, ref Texture texture, DownloadType type)
        {
            //_rafList.SearchFileEntries();
        }

        public static void LoadTexture(String name, ref Texture texture, TextureType type)
        {
            if ((type == TextureType.Default || type == TextureType.Summoner) && MyResources.ContainsKey(name.ToLower()))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice, MyResources[name.ToLower()]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Summoner && MyResources.ContainsKey(name.ToLower().Remove(name.Length - 1)))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice,
                        MyResources[name.ToLower().Remove(name.Length - 1)]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Item && MyResources.ContainsKey(name.ToLower().Insert(0, "_")))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice, MyResources[name.ToLower().Insert(0, "_")]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else
            {
                Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
            }
        }

        public static void LoadTexture(String name, ref SpriteInfoNew texture, TextureType type)
        {
            if (texture == null)
                texture = new SpriteInfoNew();
            Bitmap bmp;
            if ((type == TextureType.Default || type == TextureType.Summoner) && MyResources.ContainsKey(name.ToLower()))
            {
                try
                {
                    using (var ms = new MemoryStream(MyResources[name.ToLower()]))
                    {
                        bmp = new Bitmap(ms);
                    }
                    texture.Bitmap = (Bitmap)bmp.Clone();
                    texture.Sprite = new Render.Sprite(bmp, new Vector2(0, 0));
                    texture.DownloadFinished = true;
                    //texture.Sprite.UpdateTextureBitmap(bmp);
                    //texture = new Render.Sprite(bmp, new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfoNew();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Summoner && MyResources.ContainsKey(name.ToLower().Remove(name.Length - 1)))
            {
                try
                {
                    //texture = new Render.Sprite((Bitmap)Resources.ResourceManager.GetObject(name.ToLower().Remove(name.Length - 1)), new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfoNew();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Item && MyResources.ContainsKey(name.ToLower().Insert(0, "_")))
            {
                try
                {
                    //texture = new Render.Sprite((Bitmap)Resources.ResourceManager.GetObject(name.ToLower().Insert(0, "_")), new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfoNew();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else
            {
                if (texture == null)
                {
                    texture = new SpriteInfoNew();
                    texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                }
                Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
            }
        }

        public static void LoadTexture(Bitmap map, ref Render.Sprite texture)
        {
            if (texture == null)
                texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
            texture.UpdateTextureBitmap(map);
        }

        public static bool LoadTexture(String name, ref Render.Sprite texture, DownloadType type)
        {
            try
            {
                Bitmap map = null;
                if (!cachedMaps.ContainsKey(name))
                {
                    //map = DownloadImageRiot(name, type);
                    cachedMaps.Add(name, (Bitmap)map.Clone());
                }
                else
                {
                    map = new Bitmap((Bitmap)cachedMaps[name].Clone());
                }
                if (map == null)
                {
                    texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
                    return false;
                }
                texture = new Render.Sprite(map, new Vector2(0, 0));
                //texture.UpdateTextureBitmap(map);
                return true;
                //texture = new Render.Sprite(map, new Vector2(0, 0));
            }
            catch (Exception ex)
            {
                Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                return false;
            }
        }

        public class SpriteInfoNew : IDisposable
        {
            public enum OVD
            {
                Small,
                Big
            }

            public Render.Sprite Sprite;
            public Bitmap Bitmap;
            public bool DownloadFinished = false;
            public bool LoadingFinished = false;
            public OVD Mode = OVD.Small;

            public void Dispose()
            {
                if (Sprite != null)
                    Sprite.Dispose();

                if (Bitmap != null)
                    Bitmap.Dispose();

            }

            ~SpriteInfoNew()
            {
                Dispose();
            }
        }
    }

    public static class SpriteHelper
    {
        public enum TextureType
        {
            Default,
            Summoner,
            Item
        }

        public enum DownloadType
        {
            Champion,
            Spell,
            Summoner,
            Item
        }

        private static Downloader _downloader = new Downloader();
        public static readonly Dictionary<String, byte[]> MyResources = new Dictionary<String, byte[]>();

        //private static List<SpriteRef> Sprites = new List<SpriteRef>();

        static SpriteHelper()
        {
            ResourceSet resourceSet = Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            foreach (DictionaryEntry entry in resourceSet)
            {
                var conv = entry.Value as Bitmap;
                if (conv != null)
                {
                    MyResources.Add(entry.Key.ToString().ToLower(), (byte[])new ImageConverter().ConvertTo((Bitmap)entry.Value, typeof(byte[])));
                }
                else
                {
                    MyResources.Add(entry.Key.ToString().ToLower(), (byte[])entry.Value);
                }
            }
        }

        private static Dictionary<String, Bitmap> cachedMaps = new Dictionary<string, Bitmap>();

        public static String ConvertNames(String name)
        {
            if (name.ToLower().Contains("smite"))
            {
                return "SummonerSmite";
            }
            switch (name)
            {
                case "viw":
                    return "ViW";
                    
                case "zedult":
                    return "ZedUlt";

                case "vayneinquisition":
                    return "VayneInquisition";

                case "reksaie":
                    return "RekSaiE";

                case "dravenspinning":
                    return "DravenSpinning";

                default:
                    return name;
            }
        }

        public static void DownloadImageOpGg(string name, String subFolder)
        {
            WebRequest request = null;
            WebRequest requestSize = null;
            request =
            WebRequest.Create("http://ss.op.gg/images/profile_icons/" + name);
            requestSize =
            WebRequest.Create("http://ss.op.gg/images/profile_icons/" + name);
            requestSize.Method = "HEAD";
            if (request == null || requestSize == null)
                return;
            try
            {
                long fileSize = 0;
                using (var resp = (HttpWebResponse)requestSize.GetResponse())
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        fileSize = resp.ContentLength;
                    }
                }
                if (fileSize == GetFileSize(name, subFolder))
                    return;
                Stream responseStream;
                using (var response = (HttpWebResponse)request.GetResponse())
                if(response.StatusCode == HttpStatusCode.OK)
                using (responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            responseStream.CopyTo(memoryStream);
                            SaveImage(name, memoryStream.ToArray(), subFolder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
            }
        }

        public static void DownloadImageRiot(string name, DownloadType type, String subFolder)
        {
            String version = "";
            try
            {
                String json = new WebClient().DownloadString("http://ddragon.leagueoflegends.com/realms/euw.json");
                version = (string)new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(json)["v"];
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
                return;
            }
            WebRequest request = null;
            WebRequest requestSize = null;
            name = ConvertNames(name);
            if (type == DownloadType.Champion)
            {
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/champion/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/champion/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Spell)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Summoner)
            {
                //summonerexhaust
                if (name.Contains("summonerodingarrison"))
                    name = "SummonerOdinGarrison";
                else
                    name = name[0].ToString().ToUpper() + name.Substring(1, 7) + name[8].ToString().ToUpper() + name.Substring(9, name.Length - 9);
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            else if (type == DownloadType.Item)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
                requestSize.Method = "HEAD";
            }
            if (request == null || requestSize == null)
                return;
            try
            {
                long fileSize = 0;
                using (WebResponse resp = requestSize.GetResponse())
                {
                    fileSize = resp.ContentLength;
                }
                if (fileSize == GetFileSize(name, subFolder))
                    return;
                Stream responseStream;
                using (WebResponse response = request.GetResponse())
                using (responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            responseStream.CopyTo(memoryStream);
                            SaveImage(name, memoryStream.ToArray(), subFolder);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot download file: {0}, Exception: {1}", name, ex);
            }
        }

        private static int GetFileSize(String name, String subFolder)
        {
            int size = 0;
            string loc = Path.Combine(new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp", "Assemblies", "cache",
                "SAwareness", subFolder, name  + ".png"
            });
            try
            {
                byte[] bitmap = File.ReadAllBytes(loc);
                size = bitmap.Length;
            }
            catch (Exception)
            {
                
            }
            return size;
        }

        public static void SaveImage(String name, /*Bitmap*/byte[] bitmap, String subFolder)
        {
            string loc = Path.Combine(new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp", "Assemblies", "cache",
                "SAwareness", subFolder, name  + ".png"
            });
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp",
                        "Assemblies", "cache", "SAwareness", subFolder));
            File.WriteAllBytes(loc, bitmap/*(byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]))*/);
        }

        public static void LoadTexture(String name, ref SpriteInfo spriteInfo, String subFolder)
        {
            if (spriteInfo == null)
                spriteInfo = new SpriteInfo();
            Byte[] bitmap = null;
            name = ConvertNames(name);
            string loc = Path.Combine(new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeagueSharp", "Assemblies", "cache",
                "SAwareness", subFolder, name  + ".png"
            });
            try
            {
                bitmap = File.ReadAllBytes(loc);
                spriteInfo.Bitmap = (Bitmap)new ImageConverter().ConvertFrom(bitmap);
                spriteInfo.Sprite = new Render.Sprite(bitmap, new Vector2(0, 0));
                spriteInfo.DownloadFinished = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot load file: {0}, Exception: {1}", name, ex);
            }
        }

        public static void LoadTexture(String name, ref SpriteInfo spriteInfo, String optionalName, RafLoader.ImageList list)
        {
            if (spriteInfo == null)
                spriteInfo = new SpriteInfo();
            Byte[] bitmap = null;
            bitmap = RafLoader.GetImage(name, list, optionalName);
            try
            {
                if(bitmap == null)
                    throw new Exception("Picture not available!");
                Texture tex = Texture.FromMemory(Drawing.Direct3DDevice, bitmap);
                spriteInfo.Sprite = new Render.Sprite(tex, new Vector2(0, 0));
                spriteInfo.Bitmap = spriteInfo.Sprite.Bitmap;
                spriteInfo.DownloadFinished = true;
                tex.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot load file: {0}, Exception: {1}", name, ex);
            }
        }

        public static void LoadTexture(String name, ref Texture texture, TextureType type)
        {
            if ((type == TextureType.Default || type == TextureType.Summoner) && MyResources.ContainsKey(name.ToLower()))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice, MyResources[name.ToLower()]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Summoner && MyResources.ContainsKey(name.ToLower().Remove(name.Length - 1)))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice,
                        MyResources[name.ToLower().Remove(name.Length - 1)]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Item && MyResources.ContainsKey(name.ToLower().Insert(0, "_")))
            {
                try
                {
                    texture = Texture.FromMemory(Drawing.Direct3DDevice, MyResources[name.ToLower().Insert(0, "_")]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else
            {
                Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
            }
        }

        public static void LoadTexture(String name, ref SpriteInfo texture, TextureType type)
        {
            if (texture == null)
                texture = new SpriteInfo();
            Bitmap bmp;
            if ((type == TextureType.Default || type == TextureType.Summoner) && MyResources.ContainsKey(name.ToLower()))
            {
                try
                {
                    using (var ms = new MemoryStream(MyResources[name.ToLower()]))
                    {
                        bmp = new Bitmap(ms);
                    }
                    texture.Bitmap = (Bitmap)bmp.Clone();
                    texture.Sprite = new Render.Sprite(bmp, new Vector2(0, 0));
                    texture.DownloadFinished = true;
                    //texture.Sprite.UpdateTextureBitmap(bmp);
                    //texture = new Render.Sprite(bmp, new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfo();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Summoner && MyResources.ContainsKey(name.ToLower().Remove(name.Length - 1)))
            {
                try
                {
                    //texture = new Render.Sprite((Bitmap)Resources.ResourceManager.GetObject(name.ToLower().Remove(name.Length - 1)), new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfo();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else if (type == TextureType.Item && MyResources.ContainsKey(name.ToLower().Insert(0, "_")))
            {
                try
                {
                    //texture = new Render.Sprite((Bitmap)Resources.ResourceManager.GetObject(name.ToLower().Insert(0, "_")), new Vector2(0, 0));
                }
                catch (Exception ex)
                {
                    if (texture == null)
                    {
                        texture = new SpriteInfo();
                        texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    }
                    Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                }
            }
            else
            {
                if (texture == null)
                {
                    texture = new SpriteInfo();
                    texture.Sprite = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                }
                Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
            }
        }

        public static void LoadTexture(Bitmap map, ref Render.Sprite texture)
        {
            if (texture == null)
                texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
            texture.UpdateTextureBitmap(map);
        }

        public static bool LoadTexture(String name, ref Render.Sprite texture, DownloadType type)
        {
            try
            {
                Bitmap map = null;
                if (!cachedMaps.ContainsKey(name))
                {
                    //map = DownloadImageRiot(name, type);
                    cachedMaps.Add(name, (Bitmap)map.Clone());
                }
                else
                {
                    map = new Bitmap((Bitmap)cachedMaps[name].Clone());
                }
                if (map == null)
                {
                    texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
                    return false;
                }
                texture = new Render.Sprite(map, new Vector2(0, 0));
                //texture.UpdateTextureBitmap(map);
                return true;
                //texture = new Render.Sprite(map, new Vector2(0, 0));
            }
            catch (Exception ex)
            {
                Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                return false;
            }
        }

        public async static Task<SpriteInfo> LoadTextureAsync(String name, SpriteInfo texture, DownloadType type)
        {
            try
            {
                if (texture == null)
                    texture = new SpriteInfo();
                Render.Sprite tex = texture.Sprite;
                LoadTextureAsyncInternal(name, () => texture, x => texture = x, type);
                //texture.Sprite = tex;
                texture.LoadingFinished = true;
                return texture;
                //texture = new Render.Sprite(map, new Vector2(0, 0));
            }
            catch (Exception ex)
            {
                Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                return new SpriteInfo();
            }
        }

        public async static Task<Bitmap> DownloadImageAsync(string name, DownloadType type)
        {
            String json = new WebClient().DownloadString("http://ddragon.leagueoflegends.com/realms/euw.json");
            String version = (string)new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(json)["v"];
            WebRequest request = null;
            if (type == DownloadType.Champion)
            {
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/champion/" + name + ".png");
            }
            else if (type == DownloadType.Spell)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
            }
            else if (type == DownloadType.Summoner)
            {
                //summonerexhaust
                if (name.Contains("summonerodingarrison"))
                    name = "SummonerOdinGarrison";
                else
                    name = name[0].ToString().ToUpper() + name.Substring(1, 7) + name[8].ToString().ToUpper() + name.Substring(9, name.Length - 9);
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
            }
            else if (type == DownloadType.Item)
            {
                //http://ddragon.leagueoflegends.com/cdn/4.20.1/img/spell/AhriFoxFire.png
                request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + version + "/img/spell/" + name + ".png");
            }
            if (request == null)
                return null;
            try
            {
                Stream responseStream;
                Task<WebResponse> reqA = request.GetResponseAsync();
                using (WebResponse response = await reqA) //Crash with AsyncRequest
                using (responseStream = response.GetResponseStream())
                {
                    return responseStream != null ? new Bitmap(responseStream) : null;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("SAwarness: Couldn't load texture: " + name + "\n Ex: " + ex);
                return null;
            }
        }

        private static void LoadTextureAsyncInternal(String name, Func<SpriteInfo> getTexture, Action<SpriteInfo> setTexture, DownloadType type)
        {
            try
            {
                SpriteInfo spriteInfo = getTexture();
                Render.Sprite texture;
                Bitmap map;
                if (!cachedMaps.ContainsKey(name))
                {
                    Task<Bitmap> bitmap = DownloadImageAsync(name, type);
                    if (bitmap == null || bitmap.Result == null || bitmap.Status == TaskStatus.Faulted)
                    {
                        texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                        Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
                        spriteInfo.Sprite = texture;
                        setTexture(spriteInfo);
                        throw new Exception();
                    }
                    map = bitmap.Result; //Change to Async to make it Async, currently crashing through loading is not thread safe.
                    //Bitmap map = await bitmap;
                    cachedMaps.Add(name, (Bitmap)map.Clone());
                }
                else
                {
                    map = new Bitmap((Bitmap)cachedMaps[name].Clone());
                }
                if (map == null)
                {
                    texture = new Render.Sprite(MyResources["questionmark"], new Vector2(0, 0));
                    spriteInfo.Sprite = texture;
                    setTexture(spriteInfo);
                    Console.WriteLine("SAwarness: " + name + " is missing. Please inform Screeder!");
                    throw new Exception();
                }
                spriteInfo.Bitmap = (Bitmap)map.Clone();
                texture = new Render.Sprite(map, new Vector2(0, 0));
                spriteInfo.DownloadFinished = true;
                spriteInfo.Sprite = texture;

                setTexture(spriteInfo);
                //texture = new Render.Sprite(map, new Vector2(0, 0));
            }
            catch (Exception ex)
            {
                Console.WriteLine("SAwarness: Could not load async " + name + ".");
            }
        }

        public class SpriteInfo : IDisposable
        {
            public enum OVD
            {
                Small,
                Big
            }

            public Render.Sprite Sprite;
            public Bitmap Bitmap;
            public bool DownloadFinished = false;
            public bool LoadingFinished = false;
            public OVD Mode = OVD.Small;

            public void Dispose()
            {
                if (Sprite != null)
                    Sprite.Dispose();

                if (Bitmap != null)
                    Bitmap.Dispose();

            }

            ~SpriteInfo()
            {
                Dispose();
            }
        }
    }

    internal static class Speech
    {
        private static Dictionary<int, SpeechSynthesizer> tts = new Dictionary<int, SpeechSynthesizer>();

        static Speech()
        {
            for (int i = 0; i < 4; i++)
            {
                tts.Add(i, new SpeechSynthesizer());
            }

            ReadOnlyCollection<InstalledVoice> list = tts[0].GetInstalledVoices();
            String strVoice = "";
            foreach (var voice in list)
            {
                if (voice.VoiceInfo.Culture.EnglishName.Contains("English") && voice.Enabled)
                {
                    strVoice = voice.VoiceInfo.Name;
                }
            }

            foreach (KeyValuePair<int, SpeechSynthesizer> speech in tts)
            {
                if (!strVoice.Equals(""))
                {
                    speech.Value.SelectVoice(strVoice);
                }
            }
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
        }

        private static void CurrentDomainOnDomainUnload(object sender, EventArgs e)
        {
            foreach (var speech in tts)
            {
                if (speech.Value.State != SynthesizerState.Ready)
                {
                    speech.Value.SpeakAsyncCancelAll();
                }
                speech.Value.Dispose();
            }
        }

        public static void Speak(String text)
        {
            bool speaking = false;
            foreach (var speech in tts)
            {
                if (speech.Value.State == SynthesizerState.Ready && !speaking)
                {
                    if (speech.Value.Volume !=
                        Menu.GlobalSettings.GetMenuItem("SAwarenessGlobalSettingsVoiceVolume")
                            .GetValue<Slider>()
                            .Value)
                    {
                        speech.Value.Volume =
                            Menu.GlobalSettings.GetMenuItem("SAwarenessGlobalSettingsVoiceVolume")
                                .GetValue<Slider>()
                                .Value;
                    }
                    speaking = true;
                    new Thread(() =>
                    {
                        try
                        {
                            speech.Value.Speak(text);
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }).Start();
                }
                else if (speech.Value.State != SynthesizerState.Ready)
                {
                    speech.Value.Pause();
                    speech.Value.Volume = 1;
                    speech.Value.Resume();
                }
            }
        }
    }

    internal static class DirectXDrawer
    {
        private static void InternalRender(Vector3 target)
        {
            //Drawing.Direct3DDevice.SetTransform(TransformState.World, Matrix.Translation(target));
            //Drawing.Direct3DDevice.SetTransform(TransformState.View, Drawing.View);
            //Drawing.Direct3DDevice.SetTransform(TransformState.Projection, Drawing.Projection);

            Drawing.Direct3DDevice.VertexShader = null;
            Drawing.Direct3DDevice.PixelShader = null;
            Drawing.Direct3DDevice.SetRenderState(RenderState.AlphaBlendEnable, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.BlendOperation, BlendOperation.Add);
            Drawing.Direct3DDevice.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
            Drawing.Direct3DDevice.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
            Drawing.Direct3DDevice.SetRenderState(RenderState.Lighting, 0);
            Drawing.Direct3DDevice.SetRenderState(RenderState.ZEnable, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.AntialiasedLineEnable, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.Clipping, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.EnableAdaptiveTessellation, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.MultisampleAntialias, true);
            Drawing.Direct3DDevice.SetRenderState(RenderState.ShadeMode, ShadeMode.Gouraud);
            Drawing.Direct3DDevice.SetTexture(0, null);
            Drawing.Direct3DDevice.SetRenderState(RenderState.CullMode, Cull.None);
        }

        public static void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            var vertices = new PositionColored[2];
            vertices[0] = new PositionColored(Vector3.Zero, color.ToArgb());
            from = from.SwitchYZ();
            to = to.SwitchYZ();
            vertices[1] = new PositionColored(to - from, color.ToArgb());

            InternalRender(from);

            Drawing.Direct3DDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices.Length/2, vertices);
        }

        public static void DrawLine(Line line, Vector3 from, Vector3 to, ColorBGRA color, Size size = default(Size),
            float[] scale = null, float rotation = 0.0f)
        {
            if (line != null)
            {
                from = from.SwitchYZ();
                to = to.SwitchYZ();
                Matrix nMatrix = (scale != null ? Matrix.Scaling(scale[0], scale[1], 0) : Matrix.Scaling(1))*
                                 Matrix.RotationZ(rotation)*Matrix.Translation(from);
                Vector3[] vec = {from, to};
                line.DrawTransform(vec, nMatrix, color);
            }
        }

        public static void DrawText(Font font, String text, Size size, SharpDX.Color color)
        {
            DrawText(font, text, size.Width, size.Height, color);
        }


        //TODO: Too many drawtext for shadowtext, need another method fps issues
        public static void DrawText(Font font, String text, int posX, int posY, SharpDX.Color color)
        {
            if (font == null || font.IsDisposed)
            {
                throw new SharpDXException("");
            }
            Rectangle rec = font.MeasureText(null, text, FontDrawFlags.Center);
            //font.DrawText(null, text, posX + 1 + rec.X, posY, Color.Black);
            font.DrawText(null, text, posX + 1 + rec.X, posY + 1, SharpDX.Color.Black);
            font.DrawText(null, text, posX + rec.X, posY + 1, SharpDX.Color.Black);
            //font.DrawText(null, text, posX - 1 + rec.X, posY, Color.Black);
            font.DrawText(null, text, posX - 1 + rec.X, posY - 1, SharpDX.Color.Black);
            font.DrawText(null, text, posX + rec.X, posY - 1, SharpDX.Color.Black);
            font.DrawText(null, text, posX + rec.X, posY, color);
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, float[] scale = null,
            float rotation = 0.0f)
        {
            DrawSprite(sprite, texture, size, SharpDX.Color.White, scale, rotation);
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, SharpDX.Color color,
            float[] scale = null,
            float rotation = 0.0f)
        {
            if (sprite != null && !sprite.IsDisposed && texture != null && !texture.IsDisposed)
            {
                Matrix matrix = sprite.Transform;
                Matrix nMatrix = (scale != null ? Matrix.Scaling(scale[0], scale[1], 0) : Matrix.Scaling(1))*
                                 Matrix.RotationZ(rotation)*Matrix.Translation(size.Width, size.Height, 0);
                sprite.Transform = nMatrix;
                Matrix mT = Drawing.Direct3DDevice.GetTransform(TransformState.World);

                //InternalRender(mT.TranslationVector);
                if (Common.IsOnScreen(new Vector2(size.Width, size.Height)))
                    sprite.Draw(texture, color);
                sprite.Transform = matrix;
            }
        }

        public static void DrawTransformSprite(Sprite sprite, Texture texture, SharpDX.Color color, Size size,
            float[] scale,
            float rotation, Rectangle? spriteResize)
        {
            if (sprite != null && texture != null)
            {
                Matrix matrix = sprite.Transform;
                Matrix nMatrix = Matrix.Scaling(scale[0], scale[1], 0)*Matrix.RotationZ(rotation)*
                                 Matrix.Translation(size.Width, size.Height, 0);
                sprite.Transform = nMatrix;
                sprite.Draw(texture, color);
                sprite.Transform = matrix;
            }
        }

        public static void DrawTransformedSprite(Sprite sprite, Texture texture, Rectangle spriteResize,
            SharpDX.Color color)
        {
            if (sprite != null && texture != null)
            {
                sprite.Draw(texture, color);
            }
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, SharpDX.Color color,
            Rectangle? spriteResize)
        {
            if (sprite != null && texture != null)
            {
                sprite.Draw(texture, color, spriteResize, new Vector3(size.Width, size.Height, 0));
            }
        }

        public static void DrawSprite(Sprite sprite, Texture texture, Size size, SharpDX.Color color)
        {
            if (sprite != null && texture != null)
            {
                DrawSprite(sprite, texture, size, color, null);
            }
        }

        public struct PositionColored
        {
            public static readonly int Stride = Vector3.SizeInBytes + sizeof (int);

            public int Color;
            public Vector3 Position;

            public PositionColored(Vector3 pos, int col)
            {
                Position = pos;
                Color = col;
            }
        }
    }

    static class MapPositions
    {

        public enum Region
        {
            Unknown,
            TopLeftOuterJungle,
            TopLeftInnerJungle,
            TopRightOuterJungle,
            TopRightInnerJungle,
            BottomLeftOuterJungle,
            BottomLeftInnerJungle,
            BottomRightOuterJungle,
            BottomRightInnerJungle,
            TopOuterRiver,
            TopInnerRiver,
            BottomOuterRiver,
            BottomInnerRiver,
            LeftMidLane,
            CenterMidLane,
            RightMidLane,
            LeftBotLane,
            CenterBotLane,
            RightBotLane,
            LeftTopLane,
            CenterTopLane,
            RightTopLane,

            TopLane,
            BotLane,
            MidLane,
            Lane,

            BlueInnerJungle,
            BlueOuterJungle,
            BlueLeftJungle,
            BlueRightJungle,
            RedInnerJungle,
            RedOuterJungle,
            RedLeftJungle,
            RedRightJungle,
            BlueJungle,
            RedJungle,
            Jungle,

            BottomRiver,
            TopRiver,
            InnerRiver,
            OuterRiver,
            River,

            Base,
            BlueBase,
            RedBase,
        }

        static readonly Dictionary<Region, List<Geometry.Polygon>> _regions = new Dictionary<Region, List<Geometry.Polygon>>();

        static MapPositions() //Positions by BestAkaliAfrica (xTeKillax)
        {
            _regions.Add(Region.TopLeftOuterJungle, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                    new IntPoint(1770, 5001), 
                    new IntPoint(2084, 11596), 
                    new IntPoint(3421, 9782), 
                    new IntPoint(3841, 9305), 
                    new IntPoint(4703, 8844), 
                    new IntPoint(6345, 7451), 
                    new IntPoint(3518, 4587)
                }
            }.ToPolygons());

            _regions.Add(Region.TopLeftInnerJungle, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                    new IntPoint(3274, 5106), 
                    new IntPoint(2071, 5398), 
                    new IntPoint(2088, 10702), 
                    new IntPoint(2878, 10382), 
                    new IntPoint(3289, 9293), 
                    new IntPoint(5589, 7887)
                }
            }.ToPolygons());

            _regions.Add(Region.TopOuterRiver , new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                    new IntPoint(6427, 7629), 
                    new IntPoint(4693, 8805), 
                    new IntPoint(3427, 9600), 
                    new IntPoint(2410, 11629), 
                    new IntPoint(3006, 12325), 
                    new IntPoint(7340, 8331)
                }
            }.ToPolygons());

            _regions.Add(Region.TopInnerRiver, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(6217, 8077), 
                new IntPoint(5287, 8507), 
                new IntPoint(4440, 8988), 
                new IntPoint(3408, 9699), 
                new IntPoint(2667, 11359), 
                new IntPoint(3227, 11953), 
                new IntPoint(6886, 8668)
            }
            }.ToPolygons());

            _regions.Add(Region.TopRightOuterJungle, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(7417, 8209), 
                new IntPoint(5629, 9663), 
                new IntPoint(5425, 11054), 
                new IntPoint(4078, 11153), 
                new IntPoint(3111, 12709), 
                new IntPoint(6631, 12986), 
                new IntPoint(9777, 12970), 
                new IntPoint(10290, 11155)
            }
            }.ToPolygons());

            _regions.Add(Region.TopRightInnerJungle, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(7129, 9365), 
                new IntPoint(6319, 10046), 
                new IntPoint(5794, 10160), 
                new IntPoint(5435, 11144), 
                new IntPoint(4507, 11371), 
                new IntPoint(3916, 12150), 
                new IntPoint(7202, 12168), 
                new IntPoint(9002, 12524), 
                new IntPoint(9122, 10553), 
                new IntPoint(8205, 9990), 
                new IntPoint(8021, 9111)
            }
            }.ToPolygons());

            _regions.Add(Region.BottomLeftOuterJungle, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(4485, 3800), 
                new IntPoint(7368, 6600), 
                new IntPoint(9245, 5131), 
                new IntPoint(9247, 3949), 
                new IntPoint(10707, 3730), 
                new IntPoint(11388, 1980), 
                new IntPoint(10492, 1801), 
                new IntPoint(4938, 1780)
            }
            }.ToPolygons());

            _regions.Add(Region.BottomLeftInnerJungle, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(5132, 2358), 
                new IntPoint(4963, 3448), 
                new IntPoint(6850, 5663), 
                new IntPoint(7499, 5798), 
                new IntPoint(9151, 4810), 
                new IntPoint(9254, 4056), 
                new IntPoint(10663, 3012), 
                new IntPoint(10421, 2489)
            }
            }.ToPolygons());

            _regions.Add(Region.BottomOuterRiver, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(11752, 2728), 
                new IntPoint(9485, 3968), 
                new IntPoint(9072, 5126), 
                new IntPoint(8449, 5828), 
                new IntPoint(7462, 6567), 
                new IntPoint(8327, 7223), 
                new IntPoint(9692, 6463), 
                new IntPoint(10907, 5673),
                new IntPoint(12552, 3442)
            }
            }.ToPolygons());

            _regions.Add(Region.BottomInnerRiver, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(11236, 3200), 
                new IntPoint(10513, 4361), 
                new IntPoint(9961, 3480), 
                new IntPoint(9110, 4326), 
                new IntPoint(9455, 5250), 
                new IntPoint(7947, 6202), 
                new IntPoint(8742, 6731), 
                new IntPoint(10137, 6099), 
                new IntPoint(11429, 5293), 
                new IntPoint(12349, 3902)
            }
            }.ToPolygons());

            _regions.Add(Region.BottomRightOuterJungle, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(13014, 4103), 
                new IntPoint(12029, 4416), 
                new IntPoint(11447, 5317), 
                new IntPoint(8192, 7207), 
                new IntPoint(11118, 10396), 
                new IntPoint(13061, 9911)
            }
            }.ToPolygons());

            _regions.Add(Region.BottomRightInnerJungle, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(12491, 4049), 
                new IntPoint(11457, 5246), 
                new IntPoint(11553, 5671), 
                new IntPoint(10388, 6316), 
                new IntPoint(8881, 7164), 
                new IntPoint(11362, 9869), 
                new IntPoint(12550, 9567), 
                new IntPoint(12585, 6884), 
                new IntPoint(12956, 6405)
            }
            }.ToPolygons());

            _regions.Add(Region.LeftMidLane, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(3297, 4261), 
                new IntPoint(5930, 6897), 
                new IntPoint(6895, 6141), 
                new IntPoint(4112, 3575)
            }
            }.ToPolygons());

            _regions.Add(Region.CenterMidLane, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(5930, 6897), 
                new IntPoint(7987, 8832), 
                new IntPoint(9112, 7958), 
                new IntPoint(6895, 6141)
            }
            }.ToPolygons());

            _regions.Add(Region.RightMidLane, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(9112, 7958), 
                new IntPoint(7987, 8832), 
                new IntPoint(10631, 11341), 
                new IntPoint(11361, 10869)
            }
            }.ToPolygons());

            _regions.Add(Region.LeftBotLane, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(4502, 492), 
                new IntPoint(4486, 1784), 
                new IntPoint(11218, 1953), 
                new IntPoint(12183, 485)
            }
            }.ToPolygons());

            _regions.Add(Region.CenterBotLane, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(12183, 485), 
                new IntPoint(11218, 1953), 
                new IntPoint(12552, 3442), 
                new IntPoint(14283, 2620)
            }
            }.ToPolygons());

            _regions.Add(Region.RightBotLane, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(14283, 2620), 
                new IntPoint(12552, 3442), 
                new IntPoint(12997, 3971), 
                new IntPoint(13048, 10432), 
                new IntPoint(14580, 10329)
            }
            }.ToPolygons());

            _regions.Add(Region.LeftTopLane, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(23, 4744), 
                new IntPoint(104, 12521), 
                new IntPoint(1967, 11326), 
                new IntPoint(1719, 4564)
            }
            }.ToPolygons());

            _regions.Add(Region.CenterTopLane, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(104, 12521), 
                new IntPoint(3332, 14683), 
                new IntPoint(3620, 12813), 
                new IntPoint(1967, 11326)
            }
            }.ToPolygons());

            _regions.Add(Region.RightTopLane, new List<List<IntPoint>>
            {
                new List<IntPoint>()
                {
                new IntPoint(3620, 12813), 
                new IntPoint(3332, 14683), 
                new IntPoint(10295, 14390), 
                new IntPoint(10261, 13162), 
                new IntPoint(4284, 13087)
            }
            }.ToPolygons());

            _regions.Add(Region.BottomRiver, JoinPolygonLists(_regions[Region.BottomOuterRiver], _regions[Region.BottomInnerRiver]));
            _regions.Add(Region.TopRiver, JoinPolygonLists(_regions[Region.TopOuterRiver], _regions[Region.TopInnerRiver]));
            _regions.Add(Region.InnerRiver, JoinPolygonLists(_regions[Region.BottomInnerRiver], _regions[Region.TopInnerRiver]));
            _regions.Add(Region.OuterRiver, JoinPolygonLists(_regions[Region.BottomOuterRiver], _regions[Region.TopOuterRiver]));
            _regions.Add(Region.River, JoinPolygonLists(_regions[Region.TopRiver], _regions[Region.BottomRiver]));

            _regions.Add(Region.TopLane, JoinPolygonLists(_regions[Region.LeftTopLane], _regions[Region.CenterTopLane], _regions[Region.RightTopLane]));
            _regions.Add(Region.MidLane, JoinPolygonLists(_regions[Region.LeftMidLane], _regions[Region.CenterMidLane], _regions[Region.RightMidLane]));
            _regions.Add(Region.BotLane, JoinPolygonLists(_regions[Region.LeftBotLane], _regions[Region.CenterBotLane], _regions[Region.RightBotLane]));
            _regions.Add(Region.Lane, JoinPolygonLists(_regions[Region.TopLane], _regions[Region.MidLane], _regions[Region.BotLane]));

            _regions.Add(Region.BlueInnerJungle, JoinPolygonLists(_regions[Region.BottomLeftInnerJungle], _regions[Region.BottomRightInnerJungle]));
            _regions.Add(Region.BlueOuterJungle, JoinPolygonLists(_regions[Region.BottomLeftOuterJungle], _regions[Region.BottomRightOuterJungle]));
            _regions.Add(Region.BlueLeftJungle, JoinPolygonLists(_regions[Region.BottomLeftInnerJungle], _regions[Region.BottomLeftOuterJungle]));
            _regions.Add(Region.BlueRightJungle, JoinPolygonLists(_regions[Region.BottomRightInnerJungle], _regions[Region.BottomRightOuterJungle]));
            _regions.Add(Region.RedInnerJungle, JoinPolygonLists(_regions[Region.TopLeftInnerJungle], _regions[Region.TopRightInnerJungle]));
            _regions.Add(Region.RedOuterJungle, JoinPolygonLists(_regions[Region.TopLeftOuterJungle], _regions[Region.TopRightOuterJungle]));
            _regions.Add(Region.RedLeftJungle, JoinPolygonLists(_regions[Region.TopLeftInnerJungle], _regions[Region.TopLeftOuterJungle]));
            _regions.Add(Region.RedRightJungle, JoinPolygonLists(_regions[Region.TopRightInnerJungle], _regions[Region.TopRightOuterJungle]));
            _regions.Add(Region.BlueJungle, JoinPolygonLists(_regions[Region.BlueLeftJungle], _regions[Region.BlueRightJungle],
                                                             _regions[Region.BlueInnerJungle], _regions[Region.BlueOuterJungle]));
            _regions.Add(Region.RedJungle, JoinPolygonLists(_regions[Region.RedLeftJungle], _regions[Region.RedRightJungle],
                                                            _regions[Region.RedInnerJungle], _regions[Region.RedOuterJungle]));
            _regions.Add(Region.Jungle, JoinPolygonLists(_regions[Region.BlueJungle], _regions[Region.RedJungle]));

            _regions.Add(Region.Base, JoinPolygonLists(_regions[Region.Lane], _regions[Region.Jungle], _regions[Region.River]));
            _regions.Add(Region.BlueBase, _regions[Region.Base]);
            _regions.Add(Region.RedBase, _regions[Region.Base]);

            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            //DrawDebug();
        }

        private static void DrawDebug()
        {
            Region pos = Region.Unknown;
            foreach (var regionList in _regions)
            {
                foreach (var region in regionList.Value)
                {
                    region.Draw(Color.Aqua);
                    if (region.IsInside(ObjectManager.Player.ServerPosition))
                    {
                        pos = regionList.Key;
                    }
                }
            }
            Drawing.DrawText(Drawing.Width, Drawing.Height - 200, Color.Aqua, pos.ToString());
        }

        private static List<Geometry.Polygon> JoinPolygonLists(params List<Geometry.Polygon>[] lists)
        {
            List<Geometry.Polygon> list = new List<Geometry.Polygon>();
            foreach (var polygonList in lists)
            {
                foreach (var polygon in polygonList)
                {
                    list.Add(polygon);
                }
            }
            return list;
        }

        public static Region GetRegion(Vector2 point)
        {
            foreach (var regionList in _regions)
            {
                foreach (var region in regionList.Value)
                {
                    if (region.IsInside(point))
                    {
                        return regionList.Key;
                    }
                }
            }
            return Region.Unknown;
        }

        public static bool IsInRegion(Vector2 point, Region region)
        {
            foreach (var regionList in _regions)
            {
                foreach (var regionPos in regionList.Value)
                {
                    if (regionPos.IsInside(point))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }

    static class ThreadHelper
    {

        static ThreadEventHelper[] _threadHelpers = new ThreadEventHelper[10];
        static Thread[] _threads = new Thread[10];
        private static bool _cancelThread;
        private static int _lastUsed = 0;

        static ThreadHelper()
        {
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
            for (int i = 0; i < _threads.Length; i++)
            {
                _threads[i] = new Thread(CallEvent);
                _threads[i].Start(i);
            }
        }

        private static void CurrentDomainOnDomainUnload(Object obj, EventArgs args)
        {
            _cancelThread = true;
        }

        private static void CallEvent(object id)
        {
            while (!_cancelThread)
            {
                Thread.Sleep(1);
                if (_threadHelpers[(int) id] != null)
                {
                    _threadHelpers[(int)id].OnCall();
                }
            }
        }

        public static ThreadEventHelper GetInstance()
        {
            if (_threadHelpers[_lastUsed] == null)
            {
                _threadHelpers[_lastUsed] = new ThreadEventHelper();
            }
            return _threadHelpers[_lastUsed];
        }

        public class ThreadEventHelper 
        {

            //For later usage maybe
            public class ThreadHelperEventArgs : EventArgs
            {

            }

            public event EventHandler<EventArgs> Called; 

            public void OnCall()
            {
                var target = Called;

                if (target != null)
                {
                    target(this, new EventArgs());
                }
            }
        }
    }

    static class Language
    {

        private static System.Resources.ResourceManager resMgr;
        private static System.Resources.ResourceManager resMgrAlt;

        public static void UpdateLanguage(string langID)
        {
            try
            {
                resMgr = new System.Resources.ResourceManager("SAwareness.Resources.TRANSLATIONS.Translation-" + langID, typeof(Program).Assembly);
                resMgrAlt = new System.Resources.ResourceManager("SAwareness.Resources.TRANSLATIONS.Translation-en-US", typeof(Program).Assembly);
            }
            catch (Exception)
            {
                resMgr = new System.Resources.ResourceManager("SAwareness.Resources.TRANSLATIONS.Translation-en-US", typeof(Program).Assembly);
                resMgrAlt = resMgr;
            }
        }
 
        public static string GetString(String pattern)
        {
            try
            {
                if (resMgr == null || resMgr.GetString(pattern) == null || resMgr.GetString(pattern).Equals(""))
                {
                    return resMgrAlt.GetString(pattern) ?? "";
                }
                return resMgr.GetString(pattern);
            }
            catch (Exception)
            {
                try
                {
                    return resMgrAlt.GetString(pattern);
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        public static void SetLanguage()
        {
            switch (Config.SelectedLanguage)
            {
                case "Arabic":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ar-SA");
                    break;

                case "Chinese":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
                    break;

                case "Dutch":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("nl-NL");
                    break;

                case "English":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    break;

                case "French":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");
                    break;

                case "German":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
                    break;

                case "Greek":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("el-GR");
                    break;

                case "Italian":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("it-IT");
                    break;

                case "Korean":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ko-KR");
                    break;

                case "Polish":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("pl-PL");
                    break;

                case "Portuguese":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("pt-PT");
                    break;

                case "Romanian":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ro-RO");
                    break;

                case "Russian":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
                    break;

                case "Spanish":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("es-ES");
                    break;

                case "Swedish":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("sv-SE");
                    break;

                case "Thai":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("th-TH");
                    break;

                case "Turkish":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("tr-TR");
                    break;

                case "Vietnamese":
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("vi-VN");
                    break;

                default:
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    break;
            }
            UpdateLanguage(Thread.CurrentThread.CurrentUICulture.Name);
        }
    }
}