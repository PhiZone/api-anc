using Microsoft.AspNetCore.Mvc.ModelBinding;
using PhiZoneApi.Dtos;

namespace PhiZoneApi.Utils;

public static class ModelErrorTranslator
{
    public static List<ModelErrorDto> Translate(ModelStateDictionary modelState)
    {
        var list = new List<ModelErrorDto>();
        foreach (var key in modelState.Keys)
        {
            var errors = modelState[key];
            if (errors == null || errors.Errors.Count < 1) continue;

            var errorList = errors.Errors.Select(error => error.ErrorMessage).ToList();

            list.Add(new ModelErrorDto { Field = key, Errors = errorList });
        }

        return list;
    }
}