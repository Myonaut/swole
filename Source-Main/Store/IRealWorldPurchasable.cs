using Swole.User;

namespace Swole.Store
{
    public interface IRealWorldPurchasable : IPurchasable
    {

        public bool IsOwnedByUser();
        public bool IsOwnedByUser(ISwoleUser user);

    }
}
