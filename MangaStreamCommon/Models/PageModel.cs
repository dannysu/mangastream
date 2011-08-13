using Newtonsoft.Json;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace MangaStreamCommon
{
    [JsonObject(MemberSerialization.OptIn)]
    [Table]
    public class PageModel : ModelBase
    {
        /// <summary>
        /// Unique ID for a chapter in a manga series
        /// </summary>
        [Column(CanBeNull = false, IsDbGenerated = false, IsPrimaryKey = true)]
        public string MangaId;

        /// <summary>
        /// Page number
        /// </summary>
        [JsonProperty(PropertyName = "page")]
        [JsonConverter(typeof(IntegerConverter))]
        [Column(CanBeNull = false, IsDbGenerated = false, IsPrimaryKey = true)]
        public int Page;

        private EntitySet<ImageModel> _images;
        /// <summary>
        /// Set of ImageModels
        /// </summary>
        [JsonProperty(PropertyName = "images")]
        [Association(Storage = "_images", OtherKey = "MangaId,Page", ThisKey = "MangaId,Page")]
        public EntitySet<ImageModel> Images
        {
            get
            {
                return _images;
            }
            set
            {
                _images = value;
            }
        }
    }
}