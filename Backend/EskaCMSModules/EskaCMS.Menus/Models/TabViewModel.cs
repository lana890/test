namespace EskaCMS.Menus.Models
{

    public class TabViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long? ThumbnailId { get; set; }
        public string ThumbnailURL { get; set; }
        public string Slug { get; set; }
    }

}
