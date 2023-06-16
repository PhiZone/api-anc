using Microsoft.AspNetCore.Mvc.ModelBinding;
using PhiZoneApi.Dtos;

namespace PhiZoneApi.Helpers
{
    public static class ModelErrorTranslator
    {

        public static List<ModelErrorDto> Translate(ModelStateDictionary modelState)
        {
            var list = new List<ModelErrorDto>();
            foreach (var key in modelState.Keys)
            {
                var errors = modelState[key].Errors;
                if (errors == null)
                {
                    continue;
                }

                var errorList = new List<string>();
                foreach (var error in errors)
                {
                    errorList.Add(error.ErrorMessage);
                }

                list.Add(new ModelErrorDto()
                {
                    Field = key,
                    Errors = errorList
                });
            }
            return list;
        }

    }
}
