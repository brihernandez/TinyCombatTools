using System.Collections.Generic;

namespace Falcon
{
    public class ModData : LoadableData
    {
        public string DisplayName = "";
        public string Description = "";
        public string ThumbnailPath = "";
        public string SteamPreviewPath = "";
        public List<string> Assets = new List<string>();
    }
}
