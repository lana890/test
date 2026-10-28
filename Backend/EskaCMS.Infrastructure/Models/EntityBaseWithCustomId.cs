using EskaCommerce.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EskaCMS.Infrastructure.Models
{
    public abstract class EntityBaseWithCustomId : ValidatableObject, IEntityWithCustomId
    {
    }
}
