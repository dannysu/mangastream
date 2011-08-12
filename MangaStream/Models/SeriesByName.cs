using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace MangaStream
{
    public class SeriesByName : List<SeriesInGroup>
    {
        private static readonly string Groups = "#abcdefghijklmnopqrstuvwxyz";

        private Dictionary<int, SeriesModel> _seriesLookup = new Dictionary<int, SeriesModel>();

        public SeriesByName(List<SeriesModel> series)
        {
            series.Sort(SeriesModel.CompareBySeriesName);

            Dictionary<string, SeriesInGroup> groups = new Dictionary<string, SeriesInGroup>();

            foreach (char c in Groups)
            {
                SeriesInGroup group = new SeriesInGroup(c.ToString());
                this.Add(group);
                groups[c.ToString()] = group;
            }

            foreach (SeriesModel viewModel in series)
            {
                groups[SeriesModel.GetSeriesNameKey(viewModel)].Add(viewModel);
            }
        }
    }
}
