using System.Collections.Generic;

namespace Swole 
{

    public interface IPackageDependent
    {

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null);
    
}

}
