using System.Collections.Generic;

namespace Space
{
    public interface IEntity<K> where K : notnull
    {
        public IEnumerable<K> GetGroups();
    }
}