using System;

namespace PluralsightDurableFunctions
{
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
            try
            {
                if (!context.IsReplaying)
                {
                    log.Info("Will call A_TranscodedVideo");
                }

                var transcodedLocation = await context.CallActivityAsync<string>("A_TranscodedVideo", videoLocation);

                if (!context.IsReplaying)
                {
                    log.Info("Will call A_ExtractThumbnail");
                }

                var thumbnailLocation =
                    await context.CallActivityAsync<string>("A_ExtractThumbnail", transcodedLocation);

                if (!context.IsReplaying)
                {
                    log.Info("Will call A_PrependIntro");
                }

                var withIntroLocation = await context.CallActivityAsync<string>("A_PrependIntro", transcodedLocation);

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
