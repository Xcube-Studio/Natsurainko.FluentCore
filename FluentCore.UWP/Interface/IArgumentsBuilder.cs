using FluentCore.UWP.Model.Launch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Interface
{
    public interface IArgumentsBuilder
    {
        GameCore GameCore { get; set; }

        string BulidArguments(bool withJavaPath = false);

        string GetFrontArguments();

        string GetBehindArguments();

        string GetClasspath();
    }
}
