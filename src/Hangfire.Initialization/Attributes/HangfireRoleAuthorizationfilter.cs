using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace Hangfire.Initialization
{
    //var options = new DashboardOptions
    //{
    //    Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }
    //};
    public class HangfireRoleAuthorizationfilter : IDashboardAuthorizationFilter
    {
        public string[] Roles { get; set; }
        public bool AllowLocal { get; set; } = true;

        public HangfireRoleAuthorizationfilter(params string[] roles)
        {
            Roles = roles;
        }

        public bool Authorize(DashboardContext context)
        {
            var user = GetHttpContext(context).User;
            var isInRole = Roles.Any(role => user.IsInRole(role));
            return (AllowLocal && (context.Request.LocalIpAddress == "127.0.0.1" || context.Request.LocalIpAddress == "::1")) || isInRole;
        }

        private HttpContext GetHttpContext(DashboardContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var method = context.GetType().GetProperty("HttpContext");

            if (method == null)
            {
                throw new ArgumentException($"Context argument should have HttpContext property ");
            }

            return (HttpContext)method.GetValue(context);
        }

    }
}
