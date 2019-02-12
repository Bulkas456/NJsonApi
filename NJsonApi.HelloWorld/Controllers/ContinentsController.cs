using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NJsonApi.HelloWorld.Models;

namespace NJsonApi.HelloWorld.Controllers
{
    [Route("continents")]
    public class ContinentsController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<Continent> Get()
        {
            return StaticPersistentStore.Continents;
        }

        [HttpGet]
        [Route("{id}")]
        public Continent Get(int id)
        {
            try
            {
                return StaticPersistentStore.Continents.Single(w => w.Id == id);
            }
            catch
            {
                throw new NotSupportedException($"Not supported for id: '{id}'");
            }
        }
    }
}
