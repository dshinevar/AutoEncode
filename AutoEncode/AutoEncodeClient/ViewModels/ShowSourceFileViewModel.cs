using AutoEncodeUtilities.Data;
using System.Collections.Generic;

namespace AutoEncodeClient.ViewModels
{
    public class ShowSourceFileViewModel
    {
        public string ShowName { get; set; }

        public List<SeasonSourceFileViewModel> Seasons { get; set; } = [];
    }

    public class SeasonSourceFileViewModel
    {
        public string Season => $"Season {SeasonInt}";

        public int SeasonInt { get; set; }

        public List<ShowSourceFileData> Episodes { get; set; } = [];
    }
}
