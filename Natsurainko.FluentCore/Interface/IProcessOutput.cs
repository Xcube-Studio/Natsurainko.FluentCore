using System;
using System.Collections.Generic;
using System.Text;

namespace Natsurainko.FluentCore.Interface
{
    public interface IProcessOutput
    {
        string Raw { get; }

        string GetPrintValue();

        void Print();
    }
}
