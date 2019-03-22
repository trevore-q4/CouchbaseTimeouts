using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Core;
using Couchbase.IO;
using Microsoft.AspNetCore.Mvc;

namespace CouchbaseTimeouts.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        const int NumKeys = 10000;

        private static int _counter;

        private readonly IBucket _bucket;
        private readonly Random _random = new Random();

        private static readonly List<string> _randomKeys = new List<string>();
        private static readonly List<string> _randomValues = new List<string>();

        static HomeController()
        {
            for (int i = 0; i < NumKeys; i++)
            {
                _randomKeys.Add(Guid.NewGuid().ToString());
                _randomValues.Add(Guid.NewGuid().ToString());
            }
        }

        public HomeController(IBucket bucket)
        {
            _bucket = bucket;
        }

        public async Task<IActionResult> Index()
        {
            var key = GetRandomKey();
            var startTime = DateTime.Now;
            Interlocked.Increment(ref _counter);
            var result = await _bucket.GetAsync<string>(key);
            Interlocked.Decrement(ref _counter);
            var elapsed = DateTime.Now - startTime;
            var secondsElapsed = (int)elapsed.TotalSeconds;

            if (result.Success)
            {
                if (secondsElapsed > 1)
                    Trace.WriteLine($"{DateTime.Now.ToString("mm:ss")} Successful GET took {secondsElapsed} seconds!");

                return Ok(result.Value);
            }

            if (result.Status == ResponseStatus.KeyNotFound)
            {
                result = await _bucket.UpsertAsync(key, GetRandomValue(), TimeSpan.FromSeconds(5));

                if (result.Success)
                {
                    return Ok("Key not found, successfully added");
                }
                else
                {
                    Trace.WriteLine($"{DateTime.Now.ToString("mm:ss")} Failed to upsert: {result.Status} {result.Message}");
                    return StatusCode(500, result.Message);
                }
            }

            Trace.WriteLine($"{DateTime.Now.ToString("mm:ss")} Server error after {secondsElapsed} seconds: {result.Status} {result.Message}");
            Trace.WriteLine($"{DateTime.Now.ToString("mm:ss")} Number of in-flight requests: {_counter}");

            return StatusCode(500, result.Message);
        }

        private string GetRandomKey()
        {
            var index = _random.Next(0, _randomKeys.Count - 1);
            return _randomKeys[index];
        }

        private string GetRandomValue()
        {
            var index = _random.Next(0, _randomValues.Count - 1);
            return _randomValues[index];
        }
    }
}
