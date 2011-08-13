using System;
using Newtonsoft.Json;
using System.Data.Linq.Mapping;

namespace MangaStreamCommon
{
    [JsonObject(MemberSerialization.OptIn)]
    [Table]
    public class MangaAbstractModel : ModelBase
    {
        public MangaAbstractModel()
        {
            CreationTime = DateTime.Now;
        }

        [Column(CanBeNull = false, IsDbGenerated = false, IsPrimaryKey = true)]
        public bool IsRecentChapter;

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

        private string _seriesName;
        /// <summary>
        /// Name of the manga series
        /// </summary>
        [JsonProperty(PropertyName = "series_name")]
        [Column]
        public string SeriesName
        {
            get
            {
                return _seriesName;
            }
            set
            {
                if (!value.Equals(_seriesName))
                {
                    _seriesName = value;
                    NotifyPropertyChanged("SeriesName");
                }
            }
        }

        private string _chapter;
        /// <summary>
        /// The chapter
        /// </summary>
        [JsonProperty(PropertyName = "chapter")]
        [Column]
        public string Chapter
        {
            get
            {
                return _chapter;
            }
            set
            {
                if (!value.Equals(_chapter))
                {
                    _chapter = value;
                    NotifyPropertyChanged("Chapter");
                }
            }
        }

        private string _title;
        /// <summary>
        /// Chapter Title
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        [Column]
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                if (!value.Equals(_title))
                {
                    _title = value;
                    NotifyPropertyChanged("Title");
                }
            }
        }

        /// <summary>
        /// Time when this data was downloaded to the device
        /// </summary>
        [Column]
        public DateTime CreationTime;
    }
}