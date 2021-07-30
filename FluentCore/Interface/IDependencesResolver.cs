using FluentCore.Model.Launch;
using System.Collections.Generic;

namespace FluentCore.Interface
{
    public interface IDependencesResolver
    {
        GameCore GameCore { get; set; }

        IEnumerable<IDependence> GetDependences();

        IEnumerable<IDependence> GetLostDependences();
    }
}
