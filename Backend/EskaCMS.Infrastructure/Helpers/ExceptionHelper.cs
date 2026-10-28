using System;
using System.Reflection;

namespace EskaCMS.Infrastructure.Helpers
{
    /// <summary>
    /// Some Eskae type-checking methods used internally.
    /// </summary>
    public static class ExceptionHelper
    {
        public static Exception ThrowDeepException(Exception Exc)
        {
            if(Exc == null)
            {
                return null;
            }
            else if(Exc.InnerException != null)
            {
                return ThrowDeepException(Exc.InnerException);
            }
            else
            {
                return Exc;
            }
        }


    }
}
