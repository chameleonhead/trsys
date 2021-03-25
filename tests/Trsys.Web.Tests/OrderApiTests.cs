using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Trsys.Web.Data;

namespace Trsys.Web.Tests
{
    [TestClass]
    public class OrderApiTests
    {
        [TestMethod]
        public async Task GetApiOrders_should_return_ok_given_no_data_exists()
        {
            var server = new TestServer(new WebHostBuilder()
                .UseConfiguration(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build())
                .UseStartup<Startup>()
                .ConfigureServices(services => services.AddDbContext<TrsysContext>(options => options.UseInMemoryDatabase("test1"))));
            var client = server.CreateClient();
            var res = await client.GetAsync("/api/orders");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual("", await res.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task GetApiOrders_should_return_ok_and_single_entity_given_single_order_exists()
        {
            var server = new TestServer(new WebHostBuilder()
                .UseConfiguration(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build())
                .UseStartup<Startup>()
                .ConfigureServices(services => services.AddDbContext<TrsysContext>(options => options.UseInMemoryDatabase("test2"))));
            var client = server.CreateClient();
            var res = await client.PostAsync("/api/orders", new StringContent("1:USDJPY:0", Encoding.UTF8, "text/plain"));
            res.EnsureSuccessStatusCode();

            res = await client.GetAsync("/api/orders");
            Assert.AreEqual(HttpStatusCode.OK, res.StatusCode);
            Assert.AreEqual("1:USDJPY:0", await res.Content.ReadAsStringAsync());
        }
    }
}
