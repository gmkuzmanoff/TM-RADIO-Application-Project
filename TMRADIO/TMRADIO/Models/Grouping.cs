using System.Collections.ObjectModel;

namespace TMRADIO.Models
{
    public class GroupedCollection<K, T> : ObservableCollection<T>
    {
        public GroupedCollection(K key, ObservableCollection<T> shows)
        {
            Key = key;
            foreach (var item in shows)
                this.Items.Add(item);
        }

        public K Key { get; set; }
    }
}
