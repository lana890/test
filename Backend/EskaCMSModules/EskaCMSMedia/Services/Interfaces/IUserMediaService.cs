using EskaCMS.Media.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Media.Services.Interfaces
{
    public interface IUserMediaService
    {
        public Task<bool> SaveUserMediaAsync(UserFilesVM userFilesVM);
        public Task<List<UserFileVM>> GetUserMedia(string userName);

        public Task<bool> DeleteUserFile(long Id);

    }
}
