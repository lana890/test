using System.Linq;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace EskaCMS.Core.Areas.Core.Controllers
{
    [Area("Core")]
    [Authorize]
    [Route("api/entity-types")]
    public class EntityTypeApiController : Controller
    {
        //private readonly IRepositoryWithTypedId<EntityType, string> _entityTypeRepository;

        //public EntityTypeApiController(IRepositoryWithTypedId<EntityType, string> entityTypeRepository)
        //{
        //    _entityTypeRepository = entityTypeRepository;
        //}

        //[HttpGet("menuable")]
        //public IActionResult GetMenuable()
        //{
        //    var entityTypes = _entityTypeRepository.Query()
        //        .Where(x => x.IsMenuable)
        //        .Select(x => new
        //        {
        //            x.Id,
        //            x.Name
        //        });

        //    return Ok(entityTypes);
        //}
    }
}
