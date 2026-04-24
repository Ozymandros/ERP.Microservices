
using Microsoft.AspNetCore.Mvc;
using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Billing.API
{
    /// <summary>
    /// API controller for managing billing-related values and operations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        /// <summary>
        /// Retrieves a list of sample billing values.
        /// </summary>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }
        // ... add export-xlsx endpoint here if needed ...
    }
}
