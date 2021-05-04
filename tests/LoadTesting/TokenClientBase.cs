﻿using NBomber.Contracts;
using Serilog;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoadTesting
{
    public abstract class TokenClientBase
    {
        protected string SecretKey { get; }
        protected HttpClient Client { get; }

        private string secretToken;
        private bool isInit = false;
        private readonly SemaphoreSlim lockObject = new SemaphoreSlim(1);

        public TokenClientBase(string endpoint, string secretKey)
        {
            SecretKey = secretKey;
            Client = new HttpClient
            {
                BaseAddress = new Uri(endpoint)
            };
        }

        public async Task InitializeAsync()
        {
            var res = await Client.PostAsync("/api/token", new StringContent(SecretKey, Encoding.UTF8, "text/plain"));
            res.EnsureSuccessStatusCode();
            secretToken = await res.Content.ReadAsStringAsync();

            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Version", "20210331");
            Client.DefaultRequestHeaders.Add("X-Secret-Token", secretToken);
            isInit = true;
        }

        public async Task FinalizeAsync()
        {
            var res = await Client.PostAsync("/api/token" + secretToken + "release", new StringContent(SecretKey, Encoding.UTF8, "text/plain"));
            res.EnsureSuccessStatusCode();
            secretToken = null;
            Client.DefaultRequestHeaders.Clear();
            isInit = false;
        }

        public async Task<Response> ExecuteAsync()
        {
            EnsureInited();

            await lockObject.WaitAsync();
            try
            {
                return await OnExecuteAsync();
            }
            finally
            {
                lockObject.Release();
            }
        }

        private void EnsureInited()
        {
            if (!isInit) throw new InvalidOperationException("not initialized");
        }

        protected abstract Task<Response> OnExecuteAsync();
    }
}
