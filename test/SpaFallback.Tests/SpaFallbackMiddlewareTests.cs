using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Hellang.Middleware.SpaFallback;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace SpaFallback.Tests
{
    public class SpaFallbackMiddlewareTests
    {
        private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "StaticFiles");

        [Fact]
        public async Task StartedResponse_DoesNotFallback()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/dotnet-bot.png");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var image = await response.Content.ReadAsStreamAsync();

                Assert.Equal(61510, image.Length);
            }
        }

        [Fact]
        public async Task NotStartedResponse_DoesNotFallback()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/no-content");

                var content = await response.Content.ReadAsStreamAsync();

                Assert.Equal(0, content.Length);
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
        }

        [Fact]
        public async Task NoFallback_Throws()
        {
            using (var server = CreateServer(requestPath: "/static"))
            using (var client = server.CreateClient())
            {
                await Assert.ThrowsAsync<SpaFallbackException>(() => client.GetAsync("/no-fallback"));
            }
        }

        [Fact]
        public async Task Soft404_DoesNotFallback()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/soft-404");

                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task NonExistingPath_DoesFallback()
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/does/not/exist");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                var page = await response.Content.ReadAsStringAsync();

                Assert.Contains("This is the index file.", page);
            }
        }

        [Theory]
        [InlineData("PUT")]
        [InlineData("POST")]
        [InlineData("HEAD")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        [InlineData("OPTIONS")]
        public async Task NonGetRequest_DoesNotFallback(string method)
        {
            using (var server = CreateServer())
            using (var client = server.CreateClient())
            {
                var httpMethod = new HttpMethod(method);

                var request = new HttpRequestMessage(httpMethod, "/does/not/exist");

                var response = await client.SendAsync(request);

                var content = await response.Content.ReadAsStreamAsync();

                if (content.CanSeek)
                {
                    // ASP.NET Core 3.0 returns a ResponseBodyReaderStream which isn't seekable.
                    Assert.Equal(0, content.Length);
                }

                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(true, HttpStatusCode.OK)]
        [InlineData(false, HttpStatusCode.NotFound)]
        public async Task NonExistingFileName_ReturnsCorrectResponse(bool allowFileExtensions, HttpStatusCode expectedStatusCode)
        {
            using (var server = CreateServer(allowFileExtensions: allowFileExtensions))
            using (var client = server.CreateClient())
            {
                var response = await client.GetAsync("/non-existing.file");

                Assert.Equal(expectedStatusCode, response.StatusCode);

                if (allowFileExtensions)
                {
                    var page = await response.Content.ReadAsStringAsync();

                    Assert.Contains("This is the index file.", page);
                }
            }
        }

        [Fact]
        public void CallingUseSpaFallbackWithoutCallingAddSpaFallback_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new TestServer(new WebHostBuilder().Configure(x => x.UseSpaFallback())));
        }

        private static TestServer CreateServer(string requestPath = null, bool allowFileExtensions = false)
        {
            var config = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("webroot", ".")
            };

            var options = new StaticFileOptions
            {
                RequestPath = requestPath,
                FileProvider = new PhysicalFileProvider(Folder)
            };

            var builder = new WebHostBuilder()
                .ConfigureAppConfiguration(x => x.AddInMemoryCollection(config))
                .ConfigureServices(x => x.AddSpaFallback(y => y.AllowFileExtensions = allowFileExtensions))
                .Configure(x => x.UseSpaFallback().UseStaticFiles(options).Use(HandleRoutes));

            return new TestServer(builder);
        }

        private static Task HandleRoutes(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path.StartsWithSegments("/soft-404"))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            if (context.Request.Path.StartsWithSegments("/no-content"))
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            }

            return next();
        }
    }
}
