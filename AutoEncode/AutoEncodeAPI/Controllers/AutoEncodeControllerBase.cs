using AutoEncodeAPI.Pipe;
using Microsoft.AspNetCore.Mvc;
using IAELogger = AutoEncodeUtilities.Logger.ILogger;

namespace AutoEncodeAPI.Controllers
{
    public abstract class AutoEncodeControllerBase : ControllerBase
    {
        // Really long timeout to ensure task ends API side
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(120);
        protected IClientPipeManager ClientPipeManager;
        protected IAELogger Logger;

        public AutoEncodeControllerBase(IAELogger logger, IClientPipeManager clientPipeManager)
        {
            Logger = logger;
            ClientPipeManager = clientPipeManager;
        }

        protected ActionResult<T> HandleRequest<T>(Task<T> task)
        {
            try
            {
                T result = TryWait(task);

                if (result is null)
                {
                    return BadRequest();
                }
                else
                {
                    return Ok(result);
                }
            }
            catch (TimeoutException tex)
            {
                Logger.LogException(tex, "Request timed out.", "AutoEncodeAPI", new { TaskException = task?.Exception ?? null });
                return StatusCode(StatusCodes.Status408RequestTimeout);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error handling request.", "AutoEncodeAPI", new { TaskException = task?.Exception ?? null });
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        protected T TryWait<T>(Task<T> task)
        {
            bool success = task.Wait(_timeout);

            if (success is true)
            {
                return task.Result;
            }
            else
            {
                throw new TimeoutException("Task timed out.");
            }
        }
    }
}
