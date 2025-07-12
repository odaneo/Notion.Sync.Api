namespace Notion.Sync.Api.Common
{
    public static class ManyToManySyncHelper
    {
        public static void SyncManyToMany<T>(
          ICollection<T> existingItems,
          IEnumerable<string> newIds,
          Func<T, string> idSelector,
          Func<string, T> createNewItem)
        {
            var existingIds = existingItems.Select(idSelector).ToHashSet();
            var newIdsSet = newIds.ToHashSet();

            var toRemove = existingItems.Where(x => !newIdsSet.Contains(idSelector(x))).ToList();
            foreach (var item in toRemove)
            {
                existingItems.Remove(item);
            }

            var toAdd = newIdsSet.Except(existingIds).ToList();
            foreach (var id in toAdd)
            {
                existingItems.Add(createNewItem(id));
            }
        }
    }
}
