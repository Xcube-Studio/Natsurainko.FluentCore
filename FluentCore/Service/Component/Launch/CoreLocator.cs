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
            List<GameCore> cores = new List<GameCore>();

            foreach (DirectoryInfo info in new DirectoryInfo(PathHelper.GetVersionsFolder(this.Root)).GetDirectories())
            {
                GameCore core = GetGameCoreFromId(info.Name);
                if (core != null)
                    cores.Add(core);
            }

            return cores;
        }

        public IEnumerable<CoreModel> GetAllCoreModels()
        {
            List<CoreModel> models = new List<CoreModel>();

            foreach(DirectoryInfo info in new DirectoryInfo(PathHelper.GetVersionsFolder(this.Root)).GetDirectories())
            {
                CoreModel model = GetCoreModelFromId(info.Name);
                if (model != null)
                    models.Add(model);
            }

            return models;
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
                coreModel = MergeInheritsFromCoreWithRaw(coreModel, GetCoreModelFromId(coreModel.InheritsFrom));
                mainJar = $"{PathHelper.GetVersionFolder(this.Root, coreModel.InheritsFrom)}{PathHelper.X}{coreModel.InheritsFrom}.jar";
            }

            foreach (Library library in coreModel.Libraries)
                if (RuleHelper.Parser(library.Rules))
                    if (library.Natives != null)
                        natives.Add(new Native(library));
                    else libraries.Add(library);

            if (coreModel.MinecraftArguments != null)
                bArg.Append($"{coreModel.MinecraftArguments}");

            if (coreModel.Arguments != null)
                foreach (object obj in coreModel.Arguments.Game)
                    if (!obj.ToString().Contains("rules"))
                        bArg.Append($" {obj}");

            if (coreModel.Arguments != null)
                foreach (object obj in coreModel.Arguments.Jvm)
                    if (!obj.ToString().Contains("rules"))
                        fArg.Append($" {obj}");

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
            DirectoryInfo info = new(PathHelper.GetVersionFolder(this.Root, id));
            FileInfo file = new($"{PathHelper.GetVersionFolder(this.Root, id)}{PathHelper.X}{id}.json");

            if (info.Exists && file.Exists)
                try { return JsonConvert.DeserializeObject<CoreModel>(File.ReadAllText(file.FullName)); } catch { Console.WriteLine("Error in GetCoreModelFromId(string id)"); }

            return null;
        }

        public static CoreModel MergeInheritsFromCoreWithRaw(CoreModel raw, CoreModel inheritsFrom)
        {
            if (raw.Arguments != null)
            {
                raw.Arguments.Game = raw.Arguments.Game.Union(inheritsFrom.Arguments.Game);
                raw.Arguments.Jvm = raw.Arguments.Jvm.Union(inheritsFrom.Arguments.Jvm);
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
