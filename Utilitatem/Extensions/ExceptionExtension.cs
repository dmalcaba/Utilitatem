using System.Collections.Generic;

namespace System
{
    public static class ExceptionExtension
    {
        /// <summary>
        /// Usage:
        /// Console.WriteLine("Error {0}", string.Join(Environment.NewLine, ex.GetInnerExceptions().Select(x => x.Message)));
        /// </summary>
        public static IEnumerable<Exception> GetInnerExceptions(this Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException("ex");
            }

            var innerException = ex;
            do
            {
                yield return innerException;
                innerException = innerException.InnerException;
            }
            while (innerException != null);
        }
    }
}
