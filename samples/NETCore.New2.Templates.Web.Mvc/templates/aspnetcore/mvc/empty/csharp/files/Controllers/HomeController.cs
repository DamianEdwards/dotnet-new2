using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace $DefaultNamespace$
{
    [Route("/")]
    public class HomeController
    {
        [HttpGet]
        public string Index()
        {
            return "Hello, World!";
        }
    }
}
