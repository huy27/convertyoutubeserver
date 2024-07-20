using System.Collections.Generic;
using System;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace ConvertYoutubeToMp3.Model
{
    public class VideoResponseModel
    {
        public VideoId Id { get; set; }

        public string Url { get; set; }

        public string Title { get; set; }

        /// <inheritdoc />
        public string Author { get; set; }

        public DateTimeOffset UploadDate { get; set; }

        public string Description { get; set; }

        public string? Duration { get; set; }

        public string Thumbnail { get; set; }
        public string ChannelUrl { get; set; }
    }
    public class VideoDetails
    {
        public string Status { get; set; }
        public string Mess { get; set; }
        public string CStatus { get; set; }
        public string Vid { get; set; }
        public string Title { get; set; }
        public string Ftype { get; set; }
        public string Fquality { get; set; }
        public string Dlink { get; set; }
    }

    public class ApiResponse
    {
        public string Status { get; set; }
        public string Mess { get; set; }
        public string Page { get; set; }
        public string Vid { get; set; }
        public string Extractor { get; set; }
        public string Title { get; set; }
        public int T { get; set; }
        public string A { get; set; }
        public Links Links { get; set; }
        public List<Related> Related { get; set; }
    }

    public class Links
    {
        public Dictionary<string, Quality> Mp4 { get; set; }
        public Dictionary<string, Quality> Mp3 { get; set; }
        public Dictionary<string, Quality> Other { get; set; }
    }

    public class Quality
    {
        public string Size { get; set; }
        public string F { get; set; }
        public string Q { get; set; }
        public string QText { get; set; }
        public string K { get; set; }
        public string Selected { get; set; } // Optional field for "auto" quality
    }

    public class Related
    {
        public string Title { get; set; }
        public List<object> Contents { get; set; } // Assuming contents is an array of objects
    }
}
