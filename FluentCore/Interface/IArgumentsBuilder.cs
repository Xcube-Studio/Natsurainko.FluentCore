using FluentCore.Model.Launch;

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
