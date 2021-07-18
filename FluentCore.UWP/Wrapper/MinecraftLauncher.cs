using FluentCore.UWP.Interface;
using FluentCore.UWP.Model.Launch;
using FluentCore.UWP.Service.Local;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Wrapper
{
    /// <summary>
    /// Minecraft启动器 封装类
    /// </summary>
    public class MinecraftLauncher : ILauncher
    {
        /// <summary>
        /// 用游戏目录和启动配置信息来初始化启动器类
        /// </summary>
        /// <param name="root">.minecraft目录路径</param>
        /// <param name="config">启动配置信息</param>
        public MinecraftLauncher(string root,LaunchConfig config)
        {
            this.Root = root;
            this.LaunchConfig = config;
        }

        /// <summary>
        /// 启动器调用的进程容器
        /// </summary>
        public ProcessContainer ProcessContainer { get; private set; }

        /// <summary>
        /// 启动器启动时的配置信息
        /// </summary>
        public LaunchConfig LaunchConfig { get; set; }

        /// <summary>
        /// 游戏核心所在的根目录
        /// </summary>
        public string Root { get; private set; }

        /// <summary>
        /// 根据游戏核心id来启动游戏
        /// </summary>
        /// <param name="id"></param>
        public void Launch(string id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 等待游戏结束并取回启动结果
        /// </summary>
        /// <returns></returns>
        public async Task<LaunchConfig> WaitForResult()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 终止游戏 => 立即杀死进程
        /// </summary>
        public void Stop() => this.ProcessContainer.Kill();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (ProcessContainer != null)
                {
                    this.ProcessContainer.Dispose();
                    this.ProcessContainer = null;
                }

                this.LaunchConfig = null;
                this.Root = null;
            }
        }

        public static async Task<LaunchResult> LaunchAsync(GameCore core, LaunchConfig config)
        {
            throw new NotImplementedException();
        }

        public static LaunchResult Launch(GameCore core, LaunchConfig config) => LaunchAsync(core, config).GetAwaiter().GetResult();
    }
}
