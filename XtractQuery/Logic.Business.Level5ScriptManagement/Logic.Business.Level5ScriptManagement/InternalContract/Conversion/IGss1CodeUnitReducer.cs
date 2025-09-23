using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IGss1CodeUnitReducer
{
    void Reduce(MethodDeclarationSyntax method);
}