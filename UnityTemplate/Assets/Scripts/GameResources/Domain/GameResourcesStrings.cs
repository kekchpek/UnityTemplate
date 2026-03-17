namespace GameResources.Domain
{
    public static class GameResourcesStrings
    {
        public static string GetIconPath(ResourceId resourceId)
        {
            return $"GameResources/{resourceId}";
        }
    }
}