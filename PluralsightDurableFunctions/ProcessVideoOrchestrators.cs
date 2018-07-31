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
            var transcodedLocation = await context.CallActivityAsync<string>("A_TranscodedVideo", videoLocation);
            var thumbnailLocation = await context.CallActivityAsync<string>("A_ExtractThumbnail", transcodedLocation);
            var withIntroLocation = await context.CallActivityAsync<string>("A_PrependIntro", transcodedLocation);

            return new
                {
                    Transcoded = transcodedLocation,
                    Thumbnail = thumbnailLocation,
                    WithIntro = withIntroLocation
                };
        }
    }
}
