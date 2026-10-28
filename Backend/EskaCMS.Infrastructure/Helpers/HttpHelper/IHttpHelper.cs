using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Infrastructure.Helpers.HttpHelper
{
    public interface IHttpHelper
    {
        public Task<string> Get(RequestWithFormDataVM Model);
        public Task<string> Get(GetRequestVM Model);
        public Task<string> Get(string URL, string BaseUrl = "");

        public Task<string> Delete(GetRequestVM Model);

        public Task<string> Delete(string URL, string BaseUrl = "");

        public Task<string> Post(PostRequestVM Model);
        public Task<string> Post(RequestWithFormDataVM Model);

        public Task<string> Put(PostRequestVM Model);

    }
}
