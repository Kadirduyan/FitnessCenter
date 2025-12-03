using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Antiforgery;

namespace FitnessCenter.Filters
{
    public class AntiforgeryExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            // Tüm exception'ları yakala ve Login sayfasına yönlendir
            if (context.Exception is AntiforgeryValidationException ||
                context.Exception is Microsoft.AspNetCore.Http.BadHttpRequestException ||
                context.Exception.Message.Contains("The required anti-forgery form field") ||
                context.Exception.Message.Contains("The anti-forgery token was meant for user") ||
                context.HttpContext.Response.StatusCode == 400)
            {
                // Login sayfasına yönlendir
                context.Result = new RedirectResult("/Account/Login");
                context.ExceptionHandled = true;
            }
        }
    }
}

