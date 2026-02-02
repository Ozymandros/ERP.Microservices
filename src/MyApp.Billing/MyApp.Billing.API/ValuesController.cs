
using Microsoft.AspNetCore.Mvc;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Billing.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET: api/<ValuesController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }
        // ... add export-xlsx endpoint here if needed ...
    }
}
