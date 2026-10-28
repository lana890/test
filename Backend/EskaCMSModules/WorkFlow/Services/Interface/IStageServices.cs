using EskaCMS.WorkFlow.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.WorkFlow.Services.Interface
{
    public interface IStageServices
    {

        Task<bool> CreateStage(CreateStageDto model);
        Task<bool> UpdateStage(EditStageDto model);
        Task<bool> DeleteStage(long Id);

        Task<bool> EditMultipleOrderStage(List<StageOrderDto> stages);
    }
}
