namespace GameResources.Domain
{
    public static class GameResourcesStrings
    {

        public static string GetIconPath(ResourceId resourceId)
        {
            return $"GameResources[{resourceId}]";
        }

        public static string GetMapIconPath(ResourceId resourceId)
        {
            return $"MapResources/{resourceId}";
        }

        public static string GetCollectResourceIconPath(ResourceId resourceId)
        {
            if (resourceId == ResourceId.Wood)
            {
                return "Icons/Abiliteis/Collect_Axe";
            }
            if (resourceId == ResourceId.Stone || resourceId == ResourceId.Crystals)
            {
                return "Icons/Abiliteis/Collect_Pickaxe";
            }
            return "Icons/Placeholder";
        }

        public static string GetCollectResourceWarningIconPath(ResourceId resourceId)
        {
            if (resourceId == ResourceId.Wood)
            {
                return "Icons/Abiliteis/CollectWarning_Axe";
            }
            if (resourceId == ResourceId.Stone || resourceId == ResourceId.Crystals)
            {
                return "Icons/Abiliteis/CollectWarning_Pickaxe";
            }
            return "Icons/Placeholder";
        }

    }
}