using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public IActionResult Index()
        {
            var key = GetRandomKey();
            var startTime = DateTime.Now;
            var result = _bucket.Get<string>(key);
            var elapsed = DateTime.Now - startTime;
            var secondsElapsed = (int)elapsed.TotalSeconds;

            if (result.Success)
            {
                if (secondsElapsed > 1)
                    Trace.WriteLine($"Successful GET took {secondsElapsed} seconds!");

                return Ok(result.Value);
            }

            if (result.Status == ResponseStatus.KeyNotFound)
            {
                result = _bucket.Upsert(key, GetRandomValue(), TimeSpan.FromMinutes(5));

                if (result.Success)
                {
                    return Ok("Key not found, successfully added");
                }
                else
                {
                    Trace.WriteLine($"Failed to upsert: {result.Message}");
                    return StatusCode(500, result.Message);
                }
            }

            Trace.WriteLine($"Server error after {secondsElapsed} seconds: {result.Message}");

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
