﻿using System.IO;
using DiscourseAPIClient;
using DiscourseAutoApprove.ServiceInterface;
using DiscourseAutoApprove.ServiceModel;
using Funq;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.Logging;
using ServiceStack.Logging.EventLog;
using ServiceStack.Razor;
using ServiceStack.Validation;

namespace DiscourseAutoApprove
{
    public class AppHost : AppHostBase
    {
        /// <summary>
        /// Default constructor.
        /// Base constructor requires a name and assembly to locate web service classes. 
        /// </summary>
        public AppHost()
            : base("DiscourseAutoApprove", typeof(SyncAccountServices).Assembly)
        {
            var customSettings = new FileInfo(@"~/appsettings.txt".MapHostAbsolutePath());
            AppSettings = customSettings.Exists
                ? (IAppSettings)new TextFileSettings(customSettings.FullName)
                : new AppSettings();
        }

        /// <summary>
        /// Application specific configuration
        /// This method should initialize any IoC resources utilized by your web service classes.
        /// </summary>
        /// <param name="container"></param>
        public override void Configure(Container container)
        {
            //Config examples
            //this.Plugins.Add(new PostmanFeature());
            //this.Plugins.Add(new CorsFeature());

            SetConfig(new HostConfig
            {
                DebugMode = AppSettings.Get("DebugMode", false),
                AddRedirectParamsToQueryString = true
            });

            LogManager.LogFactory = new EventLogFactory("DiscourseAutoApprover","Application");

            Plugins.Add(new RazorFormat());
            Plugins.Add(new ValidationFeature());
            container.Register(AppSettings);

            var client = new DiscourseClient(
                AppSettings.Get("DiscourseRemoteUrl", ""),
                AppSettings.Get("DiscourseAdminApiKey", ""),
                AppSettings.Get("DiscourseAdminUserName", ""));
            client.Login(AppSettings.Get("DiscourseAdminUserName", ""), AppSettings.Get("DiscourseAdminPassword", ""));
            container.Register<IDiscourseClient>(client);

            var serviceStackAccountClient = new ServiceStackAccountClient(AppSettings.GetString("CheckSubscriptionUrl"));
            container.Register<IServiceStackAccountClient>(serviceStackAccountClient);
        }
    }

    public class SyncSingleUserByEmailValidator : AbstractValidator<SyncSingleUserByEmail>
    {
        public SyncSingleUserByEmailValidator()
        {
            RuleFor(x => x.Email).EmailAddress();
        }
    }
}