using FluentCore.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FluentCore.Interface
{
    public interface IAuthlibInjector
    {
        string Url { get; set; }

        IEnumerable<string> GetArguments();

        Task<IEnumerable<string>> GetArgumentsAsync();

        JavaAgentModel GetJavaAgent();
    }
}
