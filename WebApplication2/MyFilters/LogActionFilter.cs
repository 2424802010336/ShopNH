using System.Diagnostics;
using System.Web.Mvc;

namespace WebApplication2.MyFilters
{
    public class LogActionFilter : ActionFilterAttribute
    {
        // Chạy TRƯỚC khi Action thực thi
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Debug.WriteLine("Người dùng đang truy cập vào: " + filterContext.ActionDescriptor.ActionName);
            base.OnActionExecuting(filterContext);
        }
    }
}