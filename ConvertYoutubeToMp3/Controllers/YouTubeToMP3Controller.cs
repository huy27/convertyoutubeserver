using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using ConvertYoutubeToMp3.Model;
using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Common;
using System.Text;
using Newtonsoft.Json;
using System.Web;
using System.Collections.Generic;

namespace ConvertYoutubeToMp3.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class YouTubeToMP3Controller : ControllerBase
    {
        private readonly YoutubeClient _youtube;
        private readonly IHttpClientFactory _httpClientFactory;

        public YouTubeToMP3Controller(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

        [HttpGet("convertToMP4")]
        public async Task<IActionResult> ConvertToMP4(string videoId, string quality = "auto")
        {
            try
            {
                var url = "https://www.y2mate.com/mates/convertV2/index";

                // Tạo HttpClient từ IHttpClientFactory
                var httpClient = _httpClientFactory.CreateClient();

                // Tạo nội dung yêu cầu
                var video = await GetLinkVideo(videoId);
                if (video == null || video.Values == null)
                    return BadRequest("Có lỗi khi chuyển sang mp4");
                var qualityVideo = video.Values.FirstOrDefault(x => x.Q.Equals(quality));

                var postData = $"vid={videoId}&k={Uri.EscapeDataString(qualityVideo.K)}";
                var content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");

                // Tính toán Content-Length
                var contentLength = Encoding.UTF8.GetByteCount(postData);
                content.Headers.ContentLength = contentLength;


                // Thêm header
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en,en-US;q=0.9,vi;q=0.8");
                httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.y2mate.com/youtube/rWMrrxphPkw");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\", \"Microsoft Edge\";v=\"126\"");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-dest", "empty");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-mode", "cors");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-site", "same-origin");
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-requested-with", "XMLHttpRequest");
                httpClient.DefaultRequestHeaders.Host = "www.y2mate.com";

                // Thực hiện POST request
                var response = await httpClient.PostAsync(url, content);

                // Đọc kết quả
                var responseContent = await response.Content.ReadAsStringAsync();
                var videoDetail = JsonConvert.DeserializeObject<VideoDetails>(responseContent);

                // Trả về kết quả dưới dạng JSON
                var qualities = video.Values.Select(x => x.Q).ToList();
                return Ok(new
                {
                    videoDetail,
                    qualities
                });
            }
            catch (Exception)
            {
                return BadRequest("Có lỗi khi chuyển sang mp4");
            }
            
        }

        private async Task<Dictionary<string, Quality>> GetLinkVideo(string videoId)
        {
            var url = "https://www.y2mate.com/mates/en948/analyzeV2/ajax";

            // Tạo HttpClient từ IHttpClientFactory
            var httpClient = _httpClientFactory.CreateClient();

            // Tạo nội dung yêu cầu
            var postData = $"k_query=https%3A%2F%2Fwww.youtubepp.com%2Fwatch%3Fv%3D{videoId}&k_page=home&hl=en&q_auto=0";
            var content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");

            // Tính toán Content-Length
            var contentLength = Encoding.UTF8.GetByteCount(postData);
            content.Headers.ContentLength = contentLength;


            // Thêm header
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en,en-US;q=0.9,vi;q=0.8");
            httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.y2mate.com/youtube/rWMrrxphPkw");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\", \"Microsoft Edge\";v=\"126\"");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-dest", "empty");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-mode", "cors");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-site", "same-origin");
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-requested-with", "XMLHttpRequest");
            httpClient.DefaultRequestHeaders.Host = "www.y2mate.com";

            // Thực hiện POST request
            var response = await httpClient.PostAsync(url, content);

            // Đọc kết quả
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseContent);
            if (apiResponse.Links == null || apiResponse.Links.Mp4 == null)
                return null;

            var video = apiResponse.Links.Mp4;

            // Trả về kết quả dưới dạng JSON
            return video;
        }
    }
}
