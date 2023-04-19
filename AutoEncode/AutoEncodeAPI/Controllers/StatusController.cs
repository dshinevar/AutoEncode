using AutoEncodeAPI.Pipe;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using Microsoft.AspNetCore.Mvc;
using IAELogger = AutoEncodeUtilities.Logger.ILogger;

namespace AutoEncodeAPI.Controllers
{
    [Route(ApiRouteConstants.StatusControllerBase)]
    [ApiController]
    public class StatusController : AutoEncodeControllerBase
    {
        public StatusController(IAELogger logger, IClientPipeManager clientPipeManager)
            : base(logger, clientPipeManager) { }

        [HttpGet]
        [Route("job-queue")]
        public ActionResult<List<EncodingJobData>> GetEncodingJobQueueCurrentState()
            => HandleRequest(ClientPipeManager.GetEncodingJobQueueAsync());

        [HttpGet]
        [Route("movie-source-files")]
        public ActionResult<Dictionary<string, List<VideoSourceData>>> GetMovieSourceFiles()
            => HandleRequest(ClientPipeManager.GetMovieSourceFilesAsync());

        [HttpGet]
        [Route("show-source-files")]
        public ActionResult<Dictionary<string, List<ShowSourceData>>> GetShowSourceFiles()
            => HandleRequest(ClientPipeManager.GetShowSourceFilesAsync());
    }
}
