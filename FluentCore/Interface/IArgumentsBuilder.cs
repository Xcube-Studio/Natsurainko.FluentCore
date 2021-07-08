using FluentCore.Model.Launch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.Interface
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
