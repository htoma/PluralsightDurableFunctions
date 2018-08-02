using System;

namespace PluralsightDurableFunctions
{
    using System.Configuration;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;

    public static class ProcessVideoActivities
    {
        private const int Delay = 1000;

        [FunctionName("A_TranscodedVideo")]
        public static async Task<VideoFileInfo> TranscodeVideo([ActivityTrigger] VideoFileInfo inputVideo,
            TraceWriter log)
        {
            log.Info($"Transcoding {inputVideo.Location} to {inputVideo.BitRate}");

            // simulate doing the activity
            await Task.Delay(Delay);

            var transcodedLocation =
                $"{Path.GetFileNameWithoutExtension(inputVideo.Location)}-{inputVideo.BitRate}kbps.mp4";
            return new VideoFileInfo
            {
                Location = transcodedLocation,
                BitRate = inputVideo.BitRate
            };
        }

        [FunctionName("A_ExtractThumbnail")]
        public static async Task<string> ExtractThumbnail([ActivityTrigger] string inputVideo, TraceWriter log)
        {
            log.Info($"Extracting thumbnail {inputVideo}");

            if (inputVideo.Contains("error"))
            {
                throw new InvalidOperationException("Testing exception handling");
            }

            // simulate doing the activity
            await Task.Delay(Delay);

            return "thumbnail.png";
        }

        [FunctionName("A_PrependIntro")]
        public static async Task<string> PrependIntro([ActivityTrigger] string inputVideo, TraceWriter log)
        {
            log.Info($"Appending intro to video {inputVideo}");
            var introLocation = ConfigurationManager.AppSettings["IntroLocation"];

            // simulate doing the activity
            await Task.Delay(Delay);

            return "withIntro.mp4";
        }

        [FunctionName("A_Cleanup")]
        public static async Task Cleanup([ActivityTrigger] string inputVideo, TraceWriter log)
        {
            log.Info($"Cleaning video {inputVideo}");
            // simulate doing the activity
            await Task.Delay(Delay);
        }

        [FunctionName("A_SendApprovalRequestEmail")]
        public static async Task SendApprovalRequestEmail([ActivityTrigger] string inputVideo, TraceWriter log)
        {
            log.Info($"Requesting approval for {inputVideo}");

            // simulate sending an email
            await Task.Delay(Delay);
        }

        [FunctionName("A_PublishVideo")]
        public static async Task PublishVideo([ActivityTrigger] string inputVideo, TraceWriter log)
        {
            log.Info($"Publishing {inputVideo}");

            // simulate publishing
            await Task.Delay(Delay);
        }

        [FunctionName("A_RejectVideo")]
        public static async Task RejectVideo([ActivityTrigger] string inputVideo, TraceWriter log)
        {
            log.Info($"Rejecting {inputVideo}");

            // simulate rejecting video
            await Task.Delay(Delay);
        }
    }
}
