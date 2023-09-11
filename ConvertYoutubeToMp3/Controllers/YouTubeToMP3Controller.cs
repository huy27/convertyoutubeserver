using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ConvertYoutubeToMp3.Model;
using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

namespace ConvertYoutubeToMp3.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class YouTubeToMP3Controller : ControllerBase
    {
        private readonly YoutubeClient _youtube;

        public YouTubeToMP3Controller()
        {
            _youtube = new YoutubeClient();
        }

        [HttpGet]
        public async Task<IActionResult> ConvertToMP3(string url)
        {
            try
            {
                var videoManifest = await _youtube.Videos.Streams.GetManifestAsync(url);

                // Tìm kiếm luồng âm thanh tốt nhất để tải xuống
                var audioStreamInfo = videoManifest.GetAudioOnlyStreams().OrderBy(x => x.Bitrate).FirstOrDefault();

                if (audioStreamInfo == null)
                    throw new InvalidOperationException("Video không có luồng âm thanh.");

                // Lấy thông tin về kích thước file để đặt Content-Length header
                var fileSize = (long)audioStreamInfo.Size.Bytes;

                // Thiết lập Content-Type và Content-Disposition headers
                Response.Headers.Add("Content-Type", "audio/mpeg");
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{Guid.NewGuid().ToString()}.mp3\"");

                // Gửi status code 200 và kích thước file để báo hiệu rằng phản hồi sẽ được stream
                Response.StatusCode = (int)HttpStatusCode.OK;
                Response.ContentLength = fileSize;

                // Stream dữ liệu từ luồng âm thanh và gửi về client
                await using (var stream = await _youtube.Videos.Streams.GetAsync(audioStreamInfo))
                {
                    await stream.CopyToAsync(Response.Body);
                    await Response.Body.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            return new EmptyResult();
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetInfo(string videoUrl)
        {
            try
            {
                var video = await _youtube.Videos.GetAsync(videoUrl);
                if (video != null)
                {
                    var videoResponse = new VideoResponseModel
                    {
                        Id = video.Id,
                        Author = video.Author.ChannelTitle,
                        Description = video.Description,
                        Duration = video.Duration.ToString(),
                        Thumbnail = video.Thumbnails.FirstOrDefault().Url,
                        Title = video.Title,
                        UploadDate = video.UploadDate,
                        Url = video.Url,
                        ChannelUrl = video.Author.ChannelUrl
                    };
                    return StatusCode(200, videoResponse);
                }
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string value, int size)
        {
            try
            {
                var video = await _youtube.Search.GetVideosAsync(value).CollectAsync(size).ConfigureAwait(false);
                if (video != null && video.Any())
                {
                    var videoResponses = video.Select(x => new VideoResponseModel
                    {
                        Id = x.Id,
                        Author = x.Author.ChannelTitle,
                        Duration = x.Duration.ToString(),
                        Thumbnail = x.Thumbnails.FirstOrDefault().Url,
                        Title = x.Title,
                        Url = x.Url,
                        ChannelUrl = x.Author.ChannelUrl
                    }).ToList(); 
                    return StatusCode(200, videoResponses);
                }
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("searchByChannelUrl")]
        public async Task<IActionResult> SearchByChannelUrl(string channelUrl, int size)
        {
            try
            {
                var video = await _youtube.Channels.GetUploadsAsync(channelUrl).CollectAsync(size).ConfigureAwait(false);
                if (video != null && video.Any())
                {
                    var videoResponses = video.Select(x => new VideoResponseModel
                    {
                        Id = x.Id,
                        Author = x.Author.ChannelTitle,
                        Duration = x.Duration.ToString(),
                        Thumbnail = x.Thumbnails.FirstOrDefault().Url,
                        Title = x.Title,
                        Url = x.Url,
                        ChannelUrl = x.Author.ChannelUrl
                    }).ToList();
                    return StatusCode(200, videoResponses);
                }
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
