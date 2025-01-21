using System;

namespace Swole
{

    [Serializable]
    public struct ContentInfo
    {

        public string name;

        public string author;

        public string creationDate;

        public string lastEditDate;

        public string description;

        public static bool operator ==(ContentInfo infoA, ContentInfo infoB)
        {
            if (infoA.name != infoB.name) return false;
            if (infoA.author != infoB.author) return false;
            if (infoA.creationDate != infoB.creationDate) return false;
            if (infoA.lastEditDate != infoB.lastEditDate) return false;
            if (infoA.description != infoB.description) return false;

            return true;
        }

        public static bool operator !=(ContentInfo infoA, ContentInfo infoB) => !(infoA == infoB);

        public override bool Equals(object obj)
        {
            if (obj is ContentInfo info) return info == this; 
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }

}
