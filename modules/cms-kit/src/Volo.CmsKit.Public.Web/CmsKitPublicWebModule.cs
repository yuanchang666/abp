using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.GlobalFeatures;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;
using Volo.CmsKit.GlobalFeatures;
using Volo.CmsKit.Localization;
using Volo.CmsKit.Public.Pages;
using Volo.CmsKit.Public.Web.BackgroundServices;
using Volo.CmsKit.Public.Web.Menus;
using Volo.CmsKit.Web;

namespace Volo.CmsKit.Public.Web
{
    [DependsOn(
        typeof(CmsKitPublicHttpApiModule),
        typeof(CmsKitCommonWebModule),
        typeof(AbpBackgroundWorkersModule)
    )]
    public class CmsKitPublicWebModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
            {
                options.AddAssemblyResource(
                    typeof(CmsKitResource),
                    typeof(CmsKitPublicWebModule).Assembly,
                    typeof(CmsKitPublicApplicationContractsModule).Assembly,
                    typeof(CmsKitCommonApplicationContractsModule).Assembly
                );
            });

            PreConfigure<IMvcBuilder>(mvcBuilder =>
            {
                mvcBuilder.AddApplicationPartIfNotExists(typeof(CmsKitPublicWebModule).Assembly);
            });
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpNavigationOptions>(options =>
            {
                options.MenuContributors.Add(new CmsKitPublicMenuContributor());
            });

            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.AddEmbedded<CmsKitPublicWebModule>("Volo.CmsKit.Public.Web");
            });

            context.Services.AddAutoMapperObjectMapper<CmsKitPublicWebModule>();

            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<CmsKitPublicWebModule>(validate: true);
            });
            
            Configure<RouteOptions>(options =>
            {
                options.ConstraintMap.Add("page",typeof(PageRouteConstraint));
            });
            
        }

        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            if (GlobalFeatureManager.Instance.IsEnabled<PagesFeature>())
            {
                Configure<RazorPagesOptions>(options =>
                {
                    options.Conventions.AddPageRoute("/CmsKit/Pages/Index", @"{*pageUrl:page}");
                });
            }
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            context.AddBackgroundWorker<PageRouteCacheUpdater>();
        }
    }

    public class PageRouteConstraint :  IRouteConstraint
    {
        private readonly IPageAppService pageAppService;
        private readonly IMemoryCache _memoryCache;

        public PageRouteConstraint(IPageAppService pageAppService,IMemoryCache memoryCache)
        {
            this.pageAppService = pageAppService;
            _memoryCache = memoryCache;
        }
        
        public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values,
            RouteDirection routeDirection)
        {
          
            if (routeDirection == RouteDirection.IncomingRequest)
            {
                if (values.TryGetValue("pageUrl",out var url) && url!=null)
                {
                    Console.WriteLine(url.ToString());
                    var routeString = url.ToString();
                    if (!routeString.IsNullOrEmpty())
                    {
                        var routes = _memoryCache.Get<string[]>("pageUrls");
                        Console.WriteLine("Routes List:"+string.Join(",",routes));
                        
                        return routes.Contains(url.ToString());
                    }

                    return false;
                }
            }

            return false;
        }
    }
}
