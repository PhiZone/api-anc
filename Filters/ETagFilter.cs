using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace PhiZoneApi.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ETagFilter : ActionFilterAttribute
{
    private readonly string _secret;

    public ETagFilter(IConfiguration config)
    {
        _secret = config["Secret"]!;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext executingContext,
        ActionExecutionDelegate next)
    {
        var request = executingContext.HttpContext.Request;
        var executedContext = await next();
        var response = executedContext.HttpContext.Response;
        if (request.Method == HttpMethod.Get.Method && response.StatusCode == StatusCodes.Status200OK)
        {
            Validate(executedContext);
        }
    }

    private void Validate(ActionExecutedContext executedContext)
    {
        if (executedContext.Result == null)
        {
            return;
        }

        var request = executedContext.HttpContext.Request;
        var response = executedContext.HttpContext.Response;
        var result = (executedContext.Result as ObjectResult)!.Value;
        var eTag = ComputeETag(result);
        if (request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var header))
        {
            var incomingETag = header.ToString();
            if (incomingETag == eTag)
            {
                executedContext.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
            }
        }

        response.Headers.Add(HeaderNames.ETag, new[] { eTag });
    }

    private string ComputeETag(object? value)
    {
        var serialized = JsonConvert.SerializeObject(value);
        var bytes = KeyDerivation.Pbkdf2(serialized, Encoding.UTF8.GetBytes(_secret), KeyDerivationPrf.HMACSHA512, 8192,
            32);
        return Convert.ToBase64String(bytes);
    }
}