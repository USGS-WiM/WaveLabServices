using System.Collections.Generic;
using WIM.Resources;
using Microsoft.AspNetCore.Mvc;
using System;
using WIM.Exceptions.Services;

namespace WaveLabServices.Controllers
{
    public class WaveLabControllerBase: WIM.Services.Controllers.ControllerBase
    {
        protected override IActionResult HandleException(Exception ex)
        {
            if (ex is WIM.Exceptions.Services.BadRequestException)
            {
                sm(ex.Message, MessageType.warning);
                return new BadRequestObjectResult(new Error(errorEnum.e_badRequest, ex.Message));
            }
            else if (ex is WIM.Exceptions.Services.NotFoundRequestException)
            {
                sm(ex.Message, MessageType.warning);
                return new NotFoundObjectResult(new Error(errorEnum.e_notFound, ex.Message));
            }
            else if (ex is WIM.Exceptions.Services.UnAuthorizedRequestException)
            {
                sm(ex.Message, MessageType.warning);
                return new UnauthorizedObjectResult(new Error(errorEnum.e_unauthorize, ex.Message));
            }
            else
            {
                sm(ex.Message, MessageType.error);
                return StatusCode(500, new Error(errorEnum.e_internalError, "An error occured while processing your request. "));
            }
        }        
    }
}
