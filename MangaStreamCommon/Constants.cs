using System;

namespace MangaStreamCommon
{
    public class Constants
    {
        // Service configuration
        public const string _seriesRequestTemplate = "?apikey={0}&call=get_series";
        public const string _latestRequestTemplate = "?apikey={0}&call=get_chapters_latest";
        public const string _chaptersRequestTemplate = "?apikey={0}&call=get_chapters_by_series&series_id={1}";
        public const string _chapterRequestTemplate = "?apikey={0}&call=get_chapter&manga_id={1}";

        // Serialization data locations
        public const string _DBConnectionString = "Data Source=isostore:/manga.sdf";

        // Isolated Storage file locations
        public const string _dataFileExt = ".data";
        public const string _dataFilePattern = "*.data";
        public const string _iconFileExt = ".ico";
        public const string _iconFilePattern = "*.ico";
        public const string _serializedSeriesFile = "Series";
        public const string _serializedLatestChaptersFile = "LatestChapters";
        public const string _serializedChaptersInSeriesFile = "ChaptersInSeries";
        public const string _serializedMangaFile = "Manga";

        // Settings keys
        public const string _latestMangaId = "LatestMangaId";

        public const string _downloadPath = "shared\\transfers\\";
    }
}
