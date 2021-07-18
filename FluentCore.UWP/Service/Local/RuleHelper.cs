using FluentCore.UWP.Model.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FluentCore.UWP.Service.Local
{
    public static class RuleHelper
    {
        public static bool Parser(IEnumerable<RuleModel> rules)
        {
            if (rules == null)
                return true;

            foreach(RuleModel model in rules)
            {
                if (model.System == null)
                    continue;

                if (model.Action == "allow")
                    switch (model.System["name"])
                    {
                        case "osx":
                            if (SystemConfiguration.Platform != OSPlatform.OSX)
                                return false;
                            break;
                    }
                else switch (model.System["name"])
                {
                    case "osx":
                        if (SystemConfiguration.Platform == OSPlatform.OSX)
                            return false;
                        break;
                }
            }

            return true;
        }
    }
}
