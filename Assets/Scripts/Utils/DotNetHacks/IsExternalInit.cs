
using System.ComponentModel;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    // this is just a hack to hide errors: https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
    [EditorBrowsable(EditorBrowsableState.Never)]
    // ReSharper disable once UnusedMember.Global
    public class IsExternalInit { }
}
