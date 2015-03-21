﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscourseAPIClient;
using DiscourseAutoApprove.ServiceInterface;
using DiscourseAutoApprove.ServiceModel;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Support;
using ServiceStack.Testing;

namespace DiscourseAutoApprove.Tests
{
    public class DiscourseClientTests
    {
        private readonly ServiceStackHost appHost;

        public DiscourseClientTests()
        {
            appHost = new BasicAppHost(typeof(MyServices).Assembly)
            {
                ConfigureContainer = ConfigureAppHost
            };
            SeedAppSettings();
            appHost.Init();
        }

        private void SeedAppSettings()
        {
            var customSettings = new FileInfo(@"~/../appsettings.txt".MapHostAbsolutePath());
            var appSettings = new TextFileSettings(customSettings.FullName);
            foreach (var key in appSettings.GetAllKeys())
            {
                appHost.AppSettings.Set(key,appSettings.Get(key));
            }
        }

        private void ConfigureAppHost(Container container)
        {
            LogManager.LogFactory = new InMemoryLogFactory();
            container.Register(appHost.AppSettings);
            container.Register<IDiscourseClient>(
                new DiscourseClient(
                    appHost.AppSettings.GetString("DiscourseRemoteUrl"),
                    appHost.AppSettings.GetString("DiscourseAdminApiKey"),
                    appHost.AppSettings.GetString("DiscourseAdminUserName")));
            container.Resolve<IDiscourseClient>().Login(
                appHost.AppSettings.GetString("DiscourseAdminUserName"), 
                appHost.AppSettings.GetString("DiscourseAdminPassword"));
            container.Register<IServiceStackAccountClient>(new MockServiceStackAccountClient());
        }

        [Test]
        public void TestSuspendAndUnsuspendUser()
        {
            var discourseClient = appHost.Resolve<IDiscourseClient>();
            discourseClient.AdminSuspendUser(8, 1, "This is a test");
            var testUser = discourseClient.AdminGetUsers().Where(x => x.Username == "testuser").FirstNonDefault();
            Assert.That(testUser != null);
            Assert.That(testUser.Id == 8);
            Assert.That(testUser.Suspended == true);

            discourseClient.AdminUnsuspendUser(8);
            testUser = discourseClient.AdminGetUsers().Where(x => x.Username == "testuser").FirstNonDefault();
            Assert.That(testUser != null);
            Assert.That(testUser.Id == 8);
            Assert.That(testUser.Suspended == null || testUser.Suspended == false);
        }

        [Test]
        public void TestAdminGetUsers()
        {
            var discourseClient = appHost.Resolve<IDiscourseClient>();
            var testUser = discourseClient.AdminGetUsers().Where(x => x.Username == "testuser").FirstNonDefault();
            Assert.That(testUser != null);
            Assert.That(testUser.Id == 8);
        }
    }
}