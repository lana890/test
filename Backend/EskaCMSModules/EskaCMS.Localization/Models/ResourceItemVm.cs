namespace EskaCMS.Localization.ViewModel
{
    public class ResourceItemVm
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public long SiteCultureId { get; set; }

        public bool IsTranslated { get; set; }
    }
}
