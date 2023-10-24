using System;

namespace Swole.UI
{

    [Serializable, Flags]
    public enum FilterMode
    {

        None = 0, Newest = 1, Oldest = 2, Alphabetical = 4, Ascending = 8, Descending = 16

    }

}
