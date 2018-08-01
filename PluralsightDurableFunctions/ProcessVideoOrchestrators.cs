using System;

namespace PluralsightDurableFunctions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;

    public static class ProcessVideoOrchestrators
    {
        [FunctionName("O_ProcessVideo")]
        public static async Task<object> ProcessVideo(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            TraceWriter log)
        {
            var videoLocation = context.GetInput<string>();

            string transcodedLocation = null;
            string thumbnailLocation = null;
            string withIntroLocation = null;

            try
            {
                var bitRates = new[] { 1000, 2000, 3000, 4000 };
                var transcodeTasks = new List<Task<VideoFileInfo>>();

                foreach (var bitRate in bitRates)
                {
                    var info = new VideoFileInfo
                        {
                            Location = videoLocation,
                            BitRate = bitRate
                        };
                    var task = context.CallActivityAsync<VideoFileInfo>("A_TranscodedVideo", info);
                    transcodeTasks.Add(task);
                }

                var transcodedResults = await Task.WhenAll(transcodeTasks);
                transcodedLocation =
                    transcodedResults.OrderByDescending(x => x.BitRate).Select(x => x.Location).First();

                transcodedLocation = await context.CallActivityAsync<string>("A_TranscodedVideo", videoLocation);

                thumbnailLocation =
                    await context.CallActivityWithRetryAsync<string>(
                        "A_ExtractThumbnail",
                        new RetryOptions(TimeSpan.FromSeconds(5), 3)
                            {
                                Handle = ex => ex is InvalidOperationException
                            },
                        transcodedLocation);

                if (!context.IsReplaying)
                {
                    log.Info("Will call A_PrependIntro");
                }

                withIntroLocation = await context.CallActivityAsync<string>("A_PrependIntro", transcodedLocation);

                return new
                {
                    Transcoded = transcodedLocation,
                    Thumbnail = thumbnailLocation,
                    WithIntro = withIntroLocation
                };
            }
            catch (Exception ex)
            {
                if (!context.IsReplaying)
                {
                    log.Error("Caught an error from an activity " + ex.Message);
                }

                await context.CallActivityAsync<string>("A_Cleanup", videoLocation);

                return new
                {
                    Error = "Failed to process video",
                    ex.Message
                };
            }
        }
    }
}
