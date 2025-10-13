#if NET8_0_OR_GREATER
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;


internal static class LoadContextUtils
{
    // Shared ALC instance for override mode
    private static AssemblyLoadContext sharedALC;

    /// <summary>
    /// Get the AssemblyLoadContext for DynamoCore.
    /// </summary>
    /// <param name="returnSharedALC">If true, returns a shared collectible Assembly load context.
    /// This is useful for testing how Dynamo behaves in a non default ALC.</param>
    internal static AssemblyLoadContext GetDynamoCoreLoadContext(bool returnSharedALC = false)
    {
        if (returnSharedALC)
        {
            // Create a shared collectible ALC if not already created
            if (sharedALC == null)
            {
                sharedALC = new AssemblyLoadContext("DynamoTestingSharedALC");
            }
            return sharedALC;
        }

        return AssemblyLoadContext.GetLoadContext(typeof(LoadContextUtils).Assembly);
    }

    internal static Assembly LoadIntoALCFromPathOrFile(string input, AssemblyLoadContext alc = null)
    {
        alc ??= AssemblyLoadContext.Default;
        if ((input.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
             input.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) &&
            File.Exists(input))
        {
            return alc.LoadFromAssemblyPath(Path.GetFullPath(input));
        }
        else
        {
            return alc.LoadFromAssemblyName(new AssemblyName(input));
        }
    }

    //TODO does it make sense to add a load helper here that always uses the DynamoCoreLoadContext?
    // or some kind of validaton to check that we dont leak types into the default load context?
    //until we actually onboard into Revit addin isolation - the dynamo load context IS the default...
    //so to test we might need a way in sandbox to create a separate testing dynamo load context.
}
#endif
