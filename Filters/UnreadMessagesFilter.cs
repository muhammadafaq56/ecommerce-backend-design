using ECommerceApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Filters
{
    public class UnreadMessagesFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;

        public UnreadMessagesFilter(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var routeData = context.RouteData.Values;

            if (routeData["area"]?.ToString() == "Admin")
            {
                var count = await _context.ContactMessages
                    .CountAsync(m => !m.IsReplied);

                if (context.Controller is Controller controller)
                {
                    controller.ViewBag.UnreadMessages = count;
                }
            }

            await next();
        }
    }
}