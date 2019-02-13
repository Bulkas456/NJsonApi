using System.Collections.Generic;
using System.Web.Http;
using System.Linq;
using NJsonApi.Common.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using NJsonApi.HelloWorld.Models;
using System;

namespace NJsonApi.HelloWorld.Controllers
{
    [Route("worlds")]
    public class WorldsController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<World> Get()
        {
            return StaticPersistentStore.Worlds;
        }

        [HttpGet]
        [Route("{id}")]
        public World Get([FromRoute]int id)
        {
            try
            {
                return StaticPersistentStore.Worlds.Single(w => w.Id == id);
            }
            catch
            {
                throw new NotSupportedException($"Not supported for id: '{id}'");
            }
        }

        [HttpPost]
        public World Post([FromBody]World worldDelta)
        {
            var world = worldDelta;//.ToObject();
            world.Id = StaticPersistentStore.Worlds.Max(w => w.Id) + 1;
            StaticPersistentStore.Worlds.Add(world);
            return world;
        }

        [HttpPut]
        [Route("{id}")]
        public World Put([FromBody]Delta<World> worldDelta, [FromRoute]int id)
        {
            var world = Get(id);
            worldDelta.Apply(world);
            return world;
        }
    }
}
