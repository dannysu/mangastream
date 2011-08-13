using System;

namespace MangaStream
{
    public interface INavigable
    {
        INavigationService NavigationService { get; set; }
    }
}
