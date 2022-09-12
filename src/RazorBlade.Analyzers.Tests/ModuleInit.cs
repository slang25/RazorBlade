using System.Runtime.CompilerServices;
using VerifyTests;

public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Enable();
        VerifyDiffPlex.Initialize();
    }
}
