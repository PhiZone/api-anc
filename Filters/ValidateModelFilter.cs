using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos;
using PhiZoneApi.Enums;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Filters;

public class ValidateModelFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
            context.Result = new BadRequestObjectResult(new ResponseDto<object>
            {
                Status = ResponseStatus.ErrorDetailed,
                Code = ResponseCodes.DataInvalid,
                Errors = ModelErrorTranslator.Translate(context.ModelState)
            });
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}