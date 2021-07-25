using FluentCore.Interface;
using FluentCore.Model.Game;
using FluentCore.Model.Launch;
using FluentCore.Service.Local;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FluentCore.Service.Component.Launch
{
    public class CoreLocator : ICoreLocator
    {
        public CoreLocator(string root)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                throw new ArgumentException("无效的参数");

            this.Root = root;
        }

        public string Root { get; set; }

        public IEnumerable<GameCore> GetAllGameCores()
        {
            foreach (DirectoryInfo info in new DirectoryInfo(PathHelper.GetVersionsFolder(this.Root)).GetDirectories())
            {
                GameCore core = GetGameCoreFromId(info.Name);
                if (core != null)
                    yield return core;
            }
        }

        public IEnumerable<CoreModel> GetAllCoreModels()
        {
            foreach(DirectoryInfo info in new DirectoryInfo(PathHelper.GetVersionsFolder(this.Root)).GetDirectories())
            {
                CoreModel model = GetCoreModelFromId(info.Name);
                if (model != null)
                    yield return model;
            }
        }

        public GameCore GetGameCoreFromId(string id)
        {
            CoreModel coreModel = GetCoreModelFromId(id);
            if (coreModel == null)
                return null;

            string mainJar = $"{PathHelper.GetVersionFolder(this.Root, id)}{PathHelper.X}{id}.jar";

            List<Native> natives = new List<Native>();
            List<Library> libraries = new List<Library>();

            StringBuilder bArg = new StringBuilder();
            StringBuilder fArg = new StringBuilder();

            if (coreModel.InheritsFrom != null)
            {
                mainJar = $"{PathHelper.GetVersionFolder(this.Root, coreModel.InheritsFrom)}{PathHelper.X}{coreModel.InheritsFrom}.jar";
                coreModel = MergeInheritsFromCoreWithRaw(coreModel, GetCoreModelFromId(coreModel.InheritsFrom));
            }

            foreach (Library library in coreModel.Libraries)
                if (RuleHelper.Parser(library.Rules))
                    if (library.Natives != null)
                        natives.Add(new Native(library));
                    else libraries.Add(library);

            if (coreModel.MinecraftArguments != null)
                bArg.Append($"{coreModel.MinecraftArguments}");

            if (coreModel.Arguments != null && coreModel.Arguments.Game != null)
                foreach (object obj in coreModel.Arguments.Game)
                    if (!obj.ToString().Contains("rules"))
                        bArg.Append($" {obj}");

            if (coreModel.Arguments != null && coreModel.Arguments.Jvm != null)
                //fix for forge 1.17
                for (int i = 0; i < coreModel.Arguments.Jvm.Count; i++)
                {
                    object obj = coreModel.Arguments.Jvm[i];

                    if (!obj.ToString().Contains("rules"))
                    {
                        if (obj.ToString().Contains("-DlibraryDirectory"))
                        {
                            fArg.Append($" {obj.ToString().Replace("${library_directory}", this.Root.Contains(" ") ? $"\"{PathHelper.GetLibrariesFolder(this.Root)}\"" : PathHelper.GetLibrariesFolder(this.Root))}");
                            continue;
                        };

                        if (obj.ToString().Contains("${library_directory}"))
                        {
                            string value = obj.ToString().Replace("/", PathHelper.X).Replace("${library_directory}", PathHelper.GetLibrariesFolder(this.Root));
                            value = this.Root.Contains(" ") ? $"\"{value}\"" : value;
                            fArg.Append($" {value}");
                            continue;
                        };

                        fArg.Append($" {obj}");
                    }
                }

            return new GameCore
            {
                AsstesIndex = coreModel.AssetIndex,
                BehindArguments = bArg.ToString().Replace("  ", " "),
                Downloads = coreModel.Downloads,
                FrontArguments = fArg.ToString().Replace("  ", " "),
                Id = coreModel.Id,
                //Logging = coreModel.Logging,
                Libraries = libraries,
                MainClass = coreModel.MainClass,
                MainJar = mainJar,
                Natives = natives,
                Root = this.Root,
                Type = coreModel.Type
            };
        }

        public CoreModel GetCoreModelFromId(string id)
        {
            var info = new DirectoryInfo(PathHelper.GetVersionFolder(this.Root, id));
            var file = new FileInfo($"{PathHelper.GetVersionFolder(this.Root, id)}{PathHelper.X}{id}.json");

            if (info.Exists && file.Exists)
                try { return JsonConvert.DeserializeObject<CoreModel>(File.ReadAllText(file.FullName)); } catch { Console.WriteLine("Error in GetCoreModelFromId(string id)"); }

            return null;
        }

        public static CoreModel MergeInheritsFromCoreWithRaw(CoreModel raw, CoreModel inheritsFrom)
        {
            if (raw.Arguments != null)
            {
                raw.Arguments.Game = raw.Arguments.Game.Union(inheritsFrom.Arguments.Game).ToList();
                raw.Arguments.Jvm = raw.Arguments.Jvm.Concat(inheritsFrom.Arguments.Jvm).ToList();
            }
            raw.AssetIndex = inheritsFrom.AssetIndex;
            raw.Assets = inheritsFrom.Assets;
            raw.Downloads = inheritsFrom.Downloads;
            raw.JavaVersion = inheritsFrom.JavaVersion;
            raw.Libraries = raw.Libraries.Union(inheritsFrom.Libraries);
            raw.Type = inheritsFrom.Type;
            raw.InheritsFrom = null;

            return raw;
        }
    }
}
