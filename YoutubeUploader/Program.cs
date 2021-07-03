using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

using OctoThumbnailGenerator;

namespace YoutubeUploader
{
    internal class Program
    {
        private static readonly ConcurrentDictionary<string, (int consoleTop, long fileSize)> consolePositions = new ConcurrentDictionary<string, (int, long)>();

        [STAThread]
        private static void Main(string[] args)
        {
            Console.WriteLine("YouTube Data API: Upload Video");
            Console.WriteLine("==============================");

            try
            {

                using var fd = new OpenFileDialog
                {
                    Multiselect = true,
                    Filter = "Video files|*.mp4"
                };
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var item in fd.FileNames)
                    {
                        var fileSize = new FileInfo(item).Length;
                        consolePositions.TryAdd(item, (Console.CursorTop, fileSize / 1024 / 1024));
                        Console.WriteLine(item.Substring(item.LastIndexOf("\\") + 1, item.Length - item.LastIndexOf("\\") - 1 - 4) + $": 0 MB of {fileSize / 1024 / 1024} MB sent.");
                    }
                    foreach (var item in fd.FileNames)
                    {
                        Run(item).Wait();
                    }
                    //Parallel.ForEach(fd.FileNames, (item) =>
                    //    {
                    //        Run(item).Wait();
                    //    });
                }
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        private static async Task Run(string fileName)
        {
            UserCredential credential;
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { YouTubeService.Scope.YoutubeUpload },
                    "susch19",
                    CancellationToken.None
                );
            }

            using var youTubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = Assembly.GetExecutingAssembly().GetName().Name,
                
            });

            var video = new Video
            {
                Snippet = new VideoSnippet
                {
                    Title = fileName.Substring(fileName.LastIndexOf("\\") + 1, fileName.Length - fileName.LastIndexOf("\\") - 1 - 4),
                    Description = File.ReadAllText("StandardDescription.txt"),
                    Tags = new string[] { "Livecoding", "Programmieren", "Develop", "Developing", "programming", "programm", ".NET", "CodeTalk", "OctoAwesome", "Teaching", "Tutorial", "Java", "Minecraft", "Multiplayer", "Game", "Gamedevelopment", "Spiel", "Spieleprogrammierung", "engine", "kommunikation", "Spaß", "Community", "OpenSource", "C#", "CSharp" },
                    CategoryId = "28" // See https://developers.google.com/youtube/v3/docs/videoCategories/list
                },
                Status = new VideoStatus
                {
                    PrivacyStatus = "private"
                },
            };


            var filePath = fileName;

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {

                var videosInsertRequest = youTubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += (p) => videosInsertRequest_ProgressChanged(p, video.Snippet.Title, fileName);
                videosInsertRequest.ResponseReceived += (v) => videosInsertRequest_ResponseReceived(v, fileName, youTubeService);

                await videosInsertRequest.UploadAsync();
            }
        }

        private static void videosInsertRequest_ProgressChanged(Google.Apis.Upload.IUploadProgress progress, string title, string fileName)
        {
            Console.CursorTop = consolePositions[fileName].consoleTop;
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    Console.WriteLine(title + $": {progress.BytesSent / 1024 / 1024} MB of {consolePositions[fileName].fileSize} MB sent.");
                    break;

                case UploadStatus.Failed:
                    Console.WriteLine("An error prevented the upload from completing.\n{0}", progress.Exception);
                    break;
            }
        }

        private static void videosInsertRequest_ResponseReceived(Video video, string fileName, YouTubeService youTubeService)
        {
            var smu = new ThumbnailsResource(youTubeService);
            Console.CursorTop = consolePositions[fileName].consoleTop;
            Console.WriteLine("{0} '{1}' was successfully uploaded.".PadRight(Console.WindowWidth, ' '), video.Snippet.Title, video.Id);
            video.ProcessingDetails = new VideoProcessingDetails();

            using (var stream = new MemoryStream())
            {
                OctoThumbnailGenerator.MainProgramm.GenerateThumbnail(int.Parse(fileName.Substring(fileName.LastIndexOf("[") + 4, fileName.LastIndexOf("]") - fileName.LastIndexOf("[") - 4)), stream);
                stream.Seek(0, SeekOrigin.Begin);
                
                var response = smu.Set(video.Id, stream, "image/png").Upload();
                if(response.Status == UploadStatus.Failed)
                    Console.WriteLine("{0} '{1}' Thumbnail set failed. {2}".PadRight(Console.WindowWidth, ' '), video.Snippet.Title, video.Id, response.Exception.Message);
            }
        }
    }
}
