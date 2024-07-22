using System;
using System.Reflection;

namespace EmamiInboundMail.Console
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var types = Assembly.GetAssembly(typeof(EmamiInboundMail.Service.Program)).GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var loaderExceptions = ex.LoaderExceptions;

            }
            catch (Exception ex)
            {

            }
        }
    }
}
