using System;
using System.Linq;
using System.Threading;

namespace PluralsightDurableFunctions
{
    using System.Collections.Generic;
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
                var transcodedResults =
                    await context.CallSubOrchestratorAsync<VideoFileInfo[]>("O_TranscodedVideo", videoLocation);

                var transcodedLocation =
                    transcodedResults.OrderByDescending(x => x.BitRate).Select(x => x.Location).First();
                var thumbnailLocation = await context.CallActivityWithRetryAsync<string>(
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

                var withIntroLocation = await context.CallActivityAsync<string>("A_PrependIntro", transcodedLocation);

                await context.CallActivityAsync<string>("A_SendApprovalRequestEmail", new ApprovalInfo
                    {
                        OrchestrationId = context.InstanceId,
                        VideoLocation = withIntroLocation
                    });

                string approvalResult;
                using (var cts = new CancellationTokenSource())
                {
                    var timeoutAt = context.CurrentUtcDateTime.AddSeconds(30);
                    var timerTask = context.CreateTimer(timeoutAt, cts.Token);
                    var approvalTask = context.WaitForExternalEvent<string>(Constants.ApprovalResultEventName);
                    var winner = await Task.WhenAny(timerTask, approvalTask);

                    if (winner == approvalTask)
                    {
                        approvalResult = approvalTask.Result;
                        cts.Cancel();
                    }
                    else
                    {
                        approvalResult = "Timed out";

                    }
                }

                if (approvalResult == "Approved")
                {
                    await context.CallActivityAsync("A_PublishVideo", withIntroLocation);
                }
                else
                {
                    await context.CallActivityAsync("A_RejectVideo", withIntroLocation);
                }

                return new
                {
                    Transcoded = transcodedLocation,
                    Thumbnail = thumbnailLocation,
                    WithIntro = withIntroLocation,
                    ApprovalResult = approvalResult
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

        [FunctionName("O_TranscodedVideo")]
        public static async Task<VideoFileInfo[]> TranscodeVideo(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            TraceWriter log)
        {
            var videoLocation = context.GetInput<string>();
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
            return transcodedResults;            
        }
        
        [FunctionName("O_PeriodicTask")]
        public static async Task<int> PeriodicTask(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            TraceWriter log)
        {
            var timesRun = context.GetInput<int>();
            timesRun++;

            if (!context.IsReplaying)
            {
                log.Info($"Starting the PeriodicTask activity {context.InstanceId}, {timesRun}");
            }

            await context.CallActivityAsync("A_PeriodicActivity", timesRun);
            var nextRun = context.CurrentUtcDateTime.AddSeconds(30);
            await context.CreateTimer(nextRun, CancellationToken.None);
            context.ContinueAsNew(timesRun);
            return timesRun;
        }
    }
}
