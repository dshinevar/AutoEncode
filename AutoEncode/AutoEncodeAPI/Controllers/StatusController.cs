using AutoEncodeAPI.Pipe;
using AutoEncodeUtilities;
using AutoEncodeUtilities.Data;
using Microsoft.AspNetCore.Mvc;

namespace AutoEncodeAPI.Controllers
{
    [Route(ApiRouteConstants.StatusControllerBase)]
    [ApiController]
    public class StatusController : AutoEncodeControllerBase
    {
        public StatusController(IClientPipeManager clientPipeManager)
            : base(clientPipeManager) { }

        [HttpGet]
        [Route("job-queue")]
        public ActionResult<List<EncodingJobData>> GetEncodingJobQueueCurrentState()
            => HandleRequest(ClientPipeManager.GetEncodingJobQueueAsync(), TimeSpan.FromSeconds(60));

        [HttpGet]
        [Route("movie-source-files")]
        public ActionResult<Dictionary<string, List<VideoSourceData>>> GetMovieSourceFiles()
            => HandleRequest(ClientPipeManager.GetMovieSourceFilesAsync(), TimeSpan.FromSeconds(60));

        [HttpGet]
        [Route("show-source-files")]
        public ActionResult<Dictionary<string, List<ShowSourceData>>> GetShowSourceFiles()
            => HandleRequest(ClientPipeManager.GetShowSourceFilesAsync(), TimeSpan.FromSeconds(60));
    }
}
