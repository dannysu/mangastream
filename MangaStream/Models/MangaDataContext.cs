using System.Data.Linq;

namespace MangaStream
{
    public class MangaDataContext : DataContext
    {
        // Pass the connection string to the base class.
        public MangaDataContext(string connectionString)
            : base(connectionString)
        {
        }

        public Table<SeriesModel> Series;

        public Table<MangaAbstractModel> Chapters;

        public Table<MangaModel> Manga;

        public Table<PageModel> Pages;

        public Table<ImageModel> Images;
    }
}
