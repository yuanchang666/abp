using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Volo.CmsKit.Public.Pages;

namespace Volo.CmsKit.Public.Web.BackgroundServices
{
    public class PageRouteCacheUpdater : AsyncPeriodicBackgroundWorkerBase
    {
        public PageRouteCacheUpdater(
            AbpAsyncTimer timer,
            IServiceScopeFactory serviceScopeFactory
        ) : base(
            timer,
            serviceScopeFactory)
        {
            Timer.Period = 30000; //5 minutes
        }

        protected override Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            Console.WriteLine("Background runner started.");

            try
            {
                var pageService = workerContext
                    .ServiceProvider
                    .GetRequiredService<IPageAppService>();

                var memoryCache = workerContext
                    .ServiceProvider
                    .GetRequiredService<IMemoryCache>();

                var routes = new string[]
                {
                    "this-is-sample", "abp-framework", "how-to-customize"
                };

                memoryCache.Set("pageUrls", routes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return Task.CompletedTask;
        }
    }
}