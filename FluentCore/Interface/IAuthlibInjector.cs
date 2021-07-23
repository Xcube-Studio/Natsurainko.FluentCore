using FluentCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
