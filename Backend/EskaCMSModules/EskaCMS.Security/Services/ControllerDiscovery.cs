
using EskaCMS.Infrastructure.Data;
using EskaCMS.Security.DynamicAuthorization.Core;
using EskaCMS.Security.DynamicAuthorization.Models;
using EskaCMS.Security.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Security;
using EskaCMS.Entities.Entity.Menus;
using Microsoft.AspNetCore.Mvc.Routing;

namespace EskaCMS.Core.DynamicAuthorization.Core
{
    public class ControllerDiscovery : IControllerDiscovery
    {
        private List<ControllerInfoVM> _mvcControllers;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private IRepositoryWithTypedId<ControllerInfo, string> _ControllerInfoRepository;

        public ControllerDiscovery(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
            IRepositoryWithTypedId<ControllerInfo, string> ControllerInfoRepository)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _ControllerInfoRepository = ControllerInfoRepository;
        }

        public async Task<IEnumerable<ControllerInfoVM>> GetControllers()
        {
            if (_mvcControllers != null)
                return _mvcControllers;

            _mvcControllers = new List<ControllerInfoVM>();
            var items = _actionDescriptorCollectionProvider
                .ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Select(descriptor => descriptor)
                .GroupBy(descriptor => descriptor.ControllerTypeInfo.FullName)
                .ToList();

            foreach (var actionDescriptors in items)
            {
                if (!actionDescriptors.Any())
                    continue;

                var actionDescriptor = actionDescriptors.First();
                var controllerTypeInfo = actionDescriptor.ControllerTypeInfo;
                var currentController = new ControllerInfoVM()
                {
                    AreaName = controllerTypeInfo.GetCustomAttribute<AreaAttribute>()?.RouteValue,
                    DisplayName = controllerTypeInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName,
                    Name = actionDescriptor.ControllerName,
                    Path = controllerTypeInfo.GetCustomAttribute<RouteAttribute>()?.Template

                };

                var actions = new List<ActionInfoVM>();
                foreach (var descriptor in actionDescriptors.GroupBy(a => a.ActionName).Select(g => g.First()))
                {
                    var methodInfo = descriptor.MethodInfo;

                    if (IsProtectedAction(controllerTypeInfo, methodInfo))
                    {


                        actions.Add(new ActionInfoVM()
                        {
                            ControllerId = currentController.Id,
                            Name = descriptor.ActionName,
                            DisplayName = methodInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName,
                            ActionName = methodInfo.GetCustomAttributes<RouteAttribute>()?.FirstOrDefault()?.Template,
                            FullApiPath = methodInfo.GetCustomAttributes<HttpGetAttribute>()?.FirstOrDefault()?.Template ??
                                         methodInfo.GetCustomAttributes<HttpPostAttribute>()?.FirstOrDefault()?.Template ??
                                        methodInfo.GetCustomAttributes<HttpPutAttribute>()?.FirstOrDefault()?.Template ??
                                          methodInfo.GetCustomAttributes<HttpDeleteAttribute>()?.FirstOrDefault()?.Template ??
                                          methodInfo.GetCustomAttributes<RouteAttribute>()?.FirstOrDefault()?.Template,

                            HttpMethod = methodInfo.GetCustomAttributes(typeof(HttpMethodAttribute), true).FirstOrDefault().GetType().Name,
                            ActionParam = descriptor.Parameters.Select(p => new MethodParams
                            {
                                Key = p.Name,
                                Type = p.ParameterType.Name,
                                ParamDetails = p.ParameterType.GetRuntimeFields().Select(c => new ParamDetails
                                {
                                    Name = c.Name,
                                    Type = c.FieldType.Name,
                                }
                                ).ToList(),

                            }).ToList()
                        });
                    }
                }

                if (actions.Any())
                {
                    currentController.Actions = actions;
                    _mvcControllers.Add(currentController);
                }
            }

            return _mvcControllers;
        }

        private static bool IsProtectedAction(MemberInfo controllerTypeInfo, MemberInfo actionMethodInfo)
        {
            if (actionMethodInfo.GetCustomAttribute<AllowAnonymousAttribute>(true) != null)
                return false;

            if (controllerTypeInfo.GetCustomAttribute<AuthorizeAttribute>(true) != null)
                return true;

            if (actionMethodInfo.GetCustomAttribute<AuthorizeAttribute>(true) != null)
                return true;

            return false;
        }
    }
}