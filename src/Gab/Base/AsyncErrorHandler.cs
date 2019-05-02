using System;
using System.Diagnostics;

namespace Gab.Base
{
    public static class AsyncErrorHandler
    {
        public static void HandleException(Exception exception)
        {
            Debug.WriteLine(exception);          
        }
    }

}
