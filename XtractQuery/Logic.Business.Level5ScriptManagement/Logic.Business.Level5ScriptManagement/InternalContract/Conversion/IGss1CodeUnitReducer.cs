using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IGss1CodeUnitReducer
{
    void Reduce(MethodDeclarationSyntax method);
}