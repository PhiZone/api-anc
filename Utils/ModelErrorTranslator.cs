using Microsoft.AspNetCore.Mvc.ModelBinding;
using PhiZoneApi.Dtos.Responses;

namespace PhiZoneApi.Utils;

public static class ModelErrorTranslator
{
    public static List<ModelErrorDto> Translate(ModelStateDictionary modelState)
    {
        return (from key in modelState.Keys
            let entry = modelState[key]
            where entry != null && entry.Errors.Count >= 1
            select new ModelErrorDto
            {
                Field = key, Errors = entry.Errors.Select(error => error.ErrorMessage).ToList()
            }).ToList();
    }
}