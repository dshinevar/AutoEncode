using AutoEncodeAPI.Pipe;
using Microsoft.AspNetCore.Mvc;

namespace AutoEncodeAPI.Controllers
{
    public abstract class AutoEncodeControllerBase : ControllerBase
    {
        protected IClientPipeManager ClientPipeManager;

        public AutoEncodeControllerBase(IClientPipeManager clientPipeManager)
        {
            ClientPipeManager = clientPipeManager;
        }

        protected ActionResult<T> HandleRequest<T>(Task<T> task, TimeSpan timeout)
        {
            try
            {
                T result = TryWait(task, timeout);

                if (result is null)
                {
                    return BadRequest();
                }
                else
                {
                    return Ok(result);
                }
            }
            catch (TimeoutException)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        protected T TryWait<T>(Task<T> task, TimeSpan timeout)
        {
            bool success = task.Wait(timeout);

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
