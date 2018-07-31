using System;

namespace PluralsightDurableFunctions
{
    using System.Configuration;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;

    public static class ProcessVideoActivities
    {
        [FunctionName("A_TranscodedVideo")]
        public static async Task<string> TranscodeVideo([ActivityTrigger] string inputVideo, TraceWriter log)
        {
            log.Info($"Transcoding {inputVideo}");

            // simulate doing the activity
            await Task.Delay(5000);

            return inputVideo.Contains("error") ? "error" : "transcoded.mp4";
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
            await Task.Delay(5000);

            return "thumbnail.png";
        }

        [FunctionName("A_PrependIntro")]
        public static async Task<string> PrependIntro([ActivityTrigger] string inputVideo, TraceWriter log)
        {
            log.Info($"Appending intro to video {inputVideo}");
            var introLocation = ConfigurationManager.AppSettings["IntroLocation"];

            // simulate doing the activity
            await Task.Delay(5000);

            return "withIntro.mp4";
        }

        [FunctionName("A_Cleanup")]
        public static async Task Cleanup([ActivityTrigger] string inputVideo, TraceWriter log)
        {
            log.Info($"Cleaning video {inputVideo}");
            // simulate doing the activity
            await Task.Delay(5000);
        }
    }
}
