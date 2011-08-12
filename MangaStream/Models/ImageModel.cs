using Newtonsoft.Json;
using System.Data.Linq.Mapping;

namespace MangaStream
{
    [JsonObject(MemberSerialization.OptIn)]
    [Table]
    public class ImageModel : ModelBase
    {
        /// <summary>
        /// Unique ID for a chapter in a manga series
        /// </summary>
        [Column(CanBeNull = false, IsDbGenerated = false, IsPrimaryKey = true)]
        public string MangaId;

        /// <summary>
        /// Page number
        /// </summary>
        /// <returns></returns>
        [Column(CanBeNull = false, IsDbGenerated = false, IsPrimaryKey = true)]
        public int Page;

        /// <summary>
        /// URL for the image
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        [Column(CanBeNull = false, IsDbGenerated = false, IsPrimaryKey = true)]
        public string Url;

        /// <summary>
        /// Width of this image segment
        /// </summary>
        [JsonProperty(PropertyName = "width")]
        [JsonConverter(typeof(IntegerConverter))]
        [Column]
        public int Width;

        /// <summary>
        /// Height of this image segment
        /// </summary>
        [JsonProperty(PropertyName = "height")]
        [JsonConverter(typeof(IntegerConverter))]
        [Column]
        public int Height;

        /// <summary>
        /// Top offset of this image segment
        /// </summary>
        [JsonProperty(PropertyName = "top")]
        [JsonConverter(typeof(IntegerConverter))]
        [Column]
        public int Top;

        /// <summary>
        /// Left offset of this image segment
        /// </summary>
        [JsonProperty(PropertyName = "left")]
        [JsonConverter(typeof(IntegerConverter))]
        [Column]
        public int Left;

        /// <summary>
        /// Z-index of this image segment
        /// </summary>
        [JsonProperty(PropertyName = "zindex")]
        [JsonConverter(typeof(IntegerConverter))]
        [Column]
        public int ZIndex;
    }
}