using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Windows.Media.Imaging;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace MangaStream
{
    [JsonObject(MemberSerialization.OptIn)]
    [Table]
    public class SeriesModel : ModelBase
    {
        public SeriesModel()
        {
            _iconImage = null;
            CreationTime = DateTime.Now;
        }

        /// <summary>
        /// Unique ID for a manga series
        /// </summary>
        [JsonProperty(PropertyName = "series_id")]
        [Column(CanBeNull = false, IsDbGenerated = false, IsPrimaryKey = true)]
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

        /// <summary>
        /// Icon URL
        /// </summary>
        [JsonProperty(PropertyName = "icon")]
        [Column]
        public string Icon;

        /// <summary>
        /// Time when this data was downloaded to the device
        /// </summary>
        [Column]
        public DateTime CreationTime;

        private BitmapImage _iconImage;
        /// <summary>
        /// Actual bitmap image of the icon
        /// </summary>
        public BitmapImage IconImage
        {
            get
            {
                return _iconImage;
            }

            set
            {
                if (value != _iconImage)
                {
                    _iconImage = value;
                    NotifyPropertyChanged("IconImage");
                }
            }
        }

        public static string GetSeriesNameKey(SeriesModel viewModel)
        {
            char key = char.ToLower(viewModel.SeriesName[0]);

            if (key < 'a' || key > 'z')
            {
                key = '#';
            }

            return key.ToString();
        }

        public static int CompareBySeriesName(object obj1, object obj2)
        {
            SeriesModel manga1 = (SeriesModel)obj1;
            SeriesModel manga2 = (SeriesModel)obj2;

            return manga1.SeriesName.CompareTo(manga2.SeriesName);
        }
    }
}