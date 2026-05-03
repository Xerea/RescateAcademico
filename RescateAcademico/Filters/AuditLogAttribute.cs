using Microsoft.AspNetCore.Mvc.Filters;
using RescateAcademico.Data;
using RescateAcademico.Models;

namespace RescateAcademico.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class AuditLogAttribute : Attribute
    {
        public string Accion { get; set; } = "";
        public string Tabla { get; set; } = "";
    }

    public class AuditLogFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;

        public AuditLogFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var auditAttr = context.ActionDescriptor.EndpointMetadata
                .OfType<AuditLogAttribute>()
                .FirstOrDefault();

            if (auditAttr == null)
            {
                await next();
                return;
            }

            var resultContext = await next();

            // Only log if action succeeded (no exception and not a bad result)
            if (resultContext.Exception != null && !resultContext.ExceptionHandled)
                return;

            var httpContext = context.HttpContext;
            var user = httpContext.User;
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var userEmail = user.Identity?.IsAuthenticated == true
                ? user.Identity.Name
                : httpContext.Request.Headers["X-Forwarded-Email"].FirstOrDefault() ?? "anonymous";

            var routeValues = context.RouteData.Values;
            var controller = routeValues["controller"]?.ToString() ?? "";
            var action = routeValues["action"]?.ToString() ?? "";
            var id = routeValues["id"]?.ToString() 
                ?? context.HttpContext.Request.Form["id"].FirstOrDefault() 
                ?? context.HttpContext.Request.Query["id"].FirstOrDefault() 
                ?? "";

            var descripcion = $"{controller}/{action}";
            if (!string.IsNullOrEmpty(id))
                descripcion += $" (Id: {id})";

            var log = new BitacoraLog
            {
                UsuarioId = userId,
                UsuarioEmail = userEmail,
                Accion = string.IsNullOrEmpty(auditAttr.Accion) ? action : auditAttr.Accion,
                TablaAfectada = string.IsNullOrEmpty(auditAttr.Tabla) ? controller : auditAttr.Tabla,
                RegistroNuevo = descripcion,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString() 
                    ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault(),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                FechaHora = DateTime.Now
            };

            _context.BitacoraLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
