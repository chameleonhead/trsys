﻿using NBomber;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LoadTesting
{

    class Program
    {
        const int COUNT_OF_CLIENTS = 100;
        const int LENGTH_OF_TEST_MINUTES = 3;
        const string ENDPOINT_URL = "https://localhost:5001";
        static readonly string[] ORDER_DATA = new[] {
            "1:USDJPY:0:1.0:1:1616746000",
            "1:USDJPY:0:1.0:1:1616746000@2:EURUSD:1:0.5:2:1616746100",
            "2:EURUSD:1:0.5:2:1616746100",
            "2:EURUSD:1:0.5:2:1616746100@3:CNYUSD:1:0.05:3:1616746200",
            "3:CNYUSD:1:0.05:3:1616746200",
            "3:CNYUSD:1:0.05:3:1616746200@4:GBPUSD:0:10.05:4:1616746300",
            "4:GBPUSD:0:10.05:4:1616746300"
        };

        static void Main(string[] _)
        {
            using var server = new ProcessRunner("dotnet", "Trsys.Web.dll");

            var secretTokens = GenerateSecretTokens(COUNT_OF_CLIENTS).Result;
            var feeds = Feed.CreateConstant("secret_keys", FeedData.FromSeq(secretTokens).ShuffleData());
            var random = new Random();
            var client = new HttpClient();
            client.BaseAddress = new Uri(ENDPOINT_URL);
            client.DefaultRequestHeaders.Add("Version", "20210331");
            client.DefaultRequestHeaders.Add("X-Secret-Token", secretTokens.FirstOrDefault());
            SetPublisherData(client, ORDER_DATA[0]).Wait();
            var started = DateTime.Now;
            var span = TimeSpan.FromMinutes(LENGTH_OF_TEST_MINUTES) / ORDER_DATA.Length;

            var step1 = Step.Create("publisher", feeds,
                async context =>
                {
                    if (random.Next(COUNT_OF_CLIENTS * 100) == 0)
                    {
                        var diff = DateTime.Now - started;
                        var orders = ORDER_DATA[(int)(diff / span)];
                        context.Logger.Debug($"Setting order {orders}");
                        await SetPublisherData(client, orders);
                    }
                    return Response.Ok();
                });
            var step2 = HttpStep.Create("subscriber", feeds,
                context =>
                {
                    return Http.CreateRequest("GET", ENDPOINT_URL + "/api/orders")
                        .WithHeader("Version", "20210331")
                        .WithHeader("X-Secret-Token", context.FeedItem)
                        .WithCheck(async res =>
                        {
                            if (!res.IsSuccessStatusCode)
                            {
                                return Response.Fail($"Not successful status code: {res.StatusCode}");
                            }
                            var responseText = await res.Content.ReadAsStringAsync();
                            if (Array.IndexOf(ORDER_DATA, responseText) == -1)
                            {
                                return Response.Fail($"Invalid response: {responseText}");
                            }
                            return Response.Ok();
                        });
                });

            var scenario = ScenarioBuilder
                .CreateScenario("pubsub", step1, step2)
                .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                .WithLoadSimulations(LoadSimulation.NewInjectPerSec(10 * COUNT_OF_CLIENTS, TimeSpan.FromMinutes(LENGTH_OF_TEST_MINUTES)));

            NBomberRunner
                .RegisterScenarios(scenario)
                .Run();
        }

        private static async Task<IEnumerable<string>> GenerateSecretTokens(int count)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(ENDPOINT_URL);
            await client.PostAsync("/login", new FormUrlEncodedContent(
                new KeyValuePair<string, string>[] {
                    KeyValuePair.Create("Username", "admin"),
                    KeyValuePair.Create("Password", "P@ssw0rd"),
                }));

            var secretKeys = await GetSecretKeysAsync(client);
            foreach (var secretKey in secretKeys)
            {
                await client.PostAsync($"/admin/keys/{secretKey}/revoke", new StringContent("", Encoding.UTF8, "text/plain"));
                await client.PostAsync($"/admin/keys/{secretKey}/delete", new StringContent("", Encoding.UTF8, "text/plain"));
            }

            for (var i = 0; i < count; i++)
            {
                await client.PostAsync("/admin/keys/new", new FormUrlEncodedContent(
                    new KeyValuePair<string, string>[] {
                        KeyValuePair.Create("KeyType", "3"),
                    }));
            }

            secretKeys = await GetSecretKeysAsync(client);

            var secretTokens = new List<string>();
            foreach (var secretKey in secretKeys)
            {
                await client.PostAsync($"/admin/keys/{secretKey}/approve", new StringContent("", Encoding.UTF8, "text/plain"));
                var res = await client.PostAsync("/api/token", new StringContent(secretKey, Encoding.UTF8, "text/plain"));
                res.EnsureSuccessStatusCode();
                secretTokens.Add(await res.Content.ReadAsStringAsync());
            }

            return secretTokens;
        }

        private static async Task SetPublisherData(HttpClient client, string data)
        {
            var res = await client.PostAsync("/api/orders", new StringContent(data, Encoding.UTF8, "text/plain"));
            res.EnsureSuccessStatusCode();
        }

        private static async Task<IEnumerable<string>> GetSecretKeysAsync(HttpClient client)
        {
            var secretKeys = new List<string>();
            var adminRes = await client.GetAsync("/admin");
            var html = await adminRes.Content.ReadAsStringAsync();
            var index = html.IndexOf("<table");
            if (index >= 0)
            {
                index = html.IndexOf("<span class=\"secret-key\"", index);
            }
            while (index >= 0)
            {
                var endIndex = html.IndexOf("</span>", index);
                if (endIndex < 0)
                {
                    break;
                }
                var secretKey = html.Substring(index + 25, endIndex - (index + 25));
                secretKeys.Add(secretKey);
                index = html.IndexOf("<span class=\"secret-key\">", index + 1);
            }
            return secretKeys;
        }
    }
}

