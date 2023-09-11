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
}
