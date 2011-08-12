using System;
using Newtonsoft.Json;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace MangaStream
{
    [JsonObject(MemberSerialization.OptIn)]
    [Table]
    public class MangaModel : ModelBase
    {
        public MangaModel()
        {
            CreationTime = DateTime.Now;
        }

        /// <summary>
        /// Unique ID for a chapter in a manga series
        /// </summary>
        [JsonProperty(PropertyName = "manga_id")]
        [Column(CanBeNull = false, IsDbGenerated = false, IsPrimaryKey = true)]
        public string MangaId;

        /// <summary>
        /// Unique ID for a manga series
        /// </summary>
        [JsonProperty(PropertyName = "series_id")]
        [Column]
        public string SeriesId;

        /// <summary>
        /// Name of the manga series
        /// </summary>
        [JsonProperty(PropertyName = "series_name")]
        [Column]
        public string SeriesName;

        /// <summary>
        /// The chapter
        /// </summary>
        [JsonProperty(PropertyName = "chapter")]
        [Column]
        public string Chapter;

        /// <summary>
        /// Chapter Title
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        [Column]
        public string Title;

        /// <summary>
        /// Time when this data was downloaded to the device
        /// </summary>
        [Column]
        public DateTime CreationTime;

        private EntitySet<PageModel> _pages;
        /// <summary>
        /// Set of PageModels
        /// </summary>
        [JsonProperty(PropertyName = "pages")]
        [Association(Storage = "_pages", OtherKey = "MangaId", ThisKey = "MangaId")]
        public EntitySet<PageModel> Pages
        {
            get
            {
                return _pages;
            }
            set
            {
                _pages = value;
            }
        }
    }
}