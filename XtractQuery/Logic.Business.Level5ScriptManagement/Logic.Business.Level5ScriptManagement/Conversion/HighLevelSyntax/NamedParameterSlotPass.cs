using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Compile-only: rewrites named method parameters to <c>$paramN</c> by declaration index.
/// Parameters take precedence over globals; name conflicts are recorded as warnings.
/// </summary>
internal class NamedParameterSlotPass(ILevel5SyntaxFactory syntaxFactory) : INamedParameterSlotPass
{
    private readonly List<NamedParameterGlobalConflictWarning> _warnings = [];

    public IReadOnlyList<NamedParameterGlobalConflictWarning> Warnings => _warnings;

    public CodeUnitSyntax Convert(CodeUnitSyntax tree)
    {
        _warnings.Clear();

        HashSet<string> globalNames = CollectDeclaredGlobalNames(tree);
        var members = new List<CodeUnitMemberSyntax>();

        foreach (CodeUnitMemberSyntax member in tree.Members)
        {
            if (member is MethodDeclarationSyntax method)
                members.Add(ConvertMethod(method, globalNames));
            else
                members.Add(member);
        }

        return new CodeUnitSyntax(members);
    }

    private MethodDeclarationSyntax ConvertMethod(MethodDeclarationSyntax method, HashSet<string> globalNames)
    {
        IReadOnlyList<VariableExpressionSyntax>? parameters = method.Parameters.Parameters?.Elements;
        if (parameters is null || parameters.Count == 0)
            return method;

        if (parameters.Count > VariableSlotClassifier.ParamSlotCount)
        {
            throw CreateException(
                $"Method \"{method.Identifier.Text}\" has {parameters.Count} parameters; at most {VariableSlotClassifier.ParamSlotCount} are allowed.",
                method.Location);
        }

        Dictionary<string, int> assignment = BuildParameterAssignment(method, parameters, globalNames);
        if (assignment.Count == 0)
            return method;

        RewriteParameterList(parameters, assignment);
        foreach (StatementSyntax statement in method.Body.Expressions)
            RewriteStatement(statement, assignment);

        return method;
    }

    private Dictionary<string, int> BuildParameterAssignment(
        MethodDeclarationSyntax method,
        IReadOnlyList<VariableExpressionSyntax> parameters,
        HashSet<string> globalNames)
    {
        var assignment = new Dictionary<string, int>(StringComparer.Ordinal);
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        string methodName = method.Identifier.Text;

        for (var index = 0; index < parameters.Count; index++)
        {
            VariableExpressionSyntax parameter = parameters[index];
            string text = parameter.Variable.Text;

            if (VariableSlotClassifier.TryGetExplicitParamSlot(text, out int typedSlot))
            {
                if (typedSlot != index)
                {
                    throw CreateException(
                        $"Parameter \"{text}\" is at index {index} but refers to slot {typedSlot}. Typed $paramN must match its declaration index.",
                        parameter.Location);
                }

                if (typedSlot is < 0 or >= VariableSlotClassifier.ParamSlotCount)
                {
                    throw CreateException(
                        $"Parameter slot {typedSlot} is out of range. Valid parameter slots are 0..{VariableSlotClassifier.ParamSlotCount - 1}.",
                        parameter.Location);
                }

                continue;
            }

            if (VariableSlotClassifier.TryGetTypedSlot(text, out string type, out _))
            {
                throw CreateException(
                    $"Parameter \"{text}\" uses typed slot kind \"{type}\"; only free-form names or $paramN are allowed in the parameter list.",
                    parameter.Location);
            }

            if (!VariableSlotClassifier.IsNamedVariable(text))
            {
                throw CreateException(
                    $"Invalid parameter name \"{text}\".",
                    parameter.Location);
            }

            if (!seenNames.Add(text))
            {
                throw CreateException(
                    $"Parameter \"{text}\" is declared more than once in method \"{methodName}\".",
                    parameter.Location);
            }

            if (globalNames.Contains(text))
            {
                _warnings.Add(new NamedParameterGlobalConflictWarning
                {
                    MethodName = methodName,
                    ParameterName = text,
                    ParameterIndex = index
                });
            }

            assignment[text] = index;
        }

        return assignment;
    }

    private static HashSet<string> CollectDeclaredGlobalNames(CodeUnitSyntax tree)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (CodeUnitMemberSyntax member in tree.Members)
        {
            if (member is not GlobalDeclarationStatementSyntax declaration)
                continue;

            foreach (VariableExpressionSyntax variable in declaration.Variables.Elements)
            {
                if (VariableSlotClassifier.IsNamedVariable(variable.Variable.Text))
                    names.Add(variable.Variable.Text);
            }
        }

        return names;
    }

    private void RewriteParameterList(
        IReadOnlyList<VariableExpressionSyntax> parameters,
        IReadOnlyDictionary<string, int> assignment)
    {
        foreach (VariableExpressionSyntax parameter in parameters)
            RewriteVariable(parameter, assignment);
    }

    private void RewriteStatement(StatementSyntax statement, IReadOnlyDictionary<string, int> assignment)
    {
        switch (statement)
        {
            case AssignmentStatementSyntax assignmentStatement:
                RewriteExpression(assignmentStatement.Left, assignment);
                RewriteExpression(assignmentStatement.Right, assignment);
                break;

            case IfGotoStatementSyntax ifGoto:
                RewriteExpression(ifGoto.Value, assignment);
                break;

            case IfNotGotoStatementSyntax ifNotGoto:
                RewriteExpression(ifNotGoto.Comparison, assignment);
                break;

            case ReturnStatementSyntax { ValueExpression: not null } returnStatement:
                RewriteExpression(returnStatement.ValueExpression, assignment);
                break;

            case PostfixUnaryStatementSyntax postfix:
                RewriteExpression(postfix.Expression, assignment);
                break;

            case MethodInvocationStatementSyntax invocation:
                RewriteInvocationParameters(invocation.Parameters, assignment);
                break;

            case GotoStatementSyntax gotoStatement:
                foreach (ValueExpressionSyntax target in gotoStatement.Targets.Elements)
                    RewriteExpression(target, assignment);
                break;

            case IfStatementSyntax ifStatement:
                RewriteExpression(ifStatement.Condition, assignment);
                foreach (StatementSyntax nested in ifStatement.Body.Statements)
                    RewriteStatement(nested, assignment);
                if (ifStatement.Else != null)
                    RewriteStatement(ifStatement.Else.Statement, assignment);
                break;

            case WhileStatementSyntax whileStatement:
                RewriteExpression(whileStatement.Condition, assignment);
                if (whileStatement.Body != null)
                {
                    foreach (StatementSyntax nested in whileStatement.Body.Statements)
                        RewriteStatement(nested, assignment);
                }
                break;

            case ForStatementSyntax forStatement:
                if (forStatement.Initializer != null)
                    RewriteStatement(forStatement.Initializer, assignment);
                RewriteExpression(forStatement.Condition, assignment);
                if (forStatement.Iterator != null)
                    RewriteStatement(forStatement.Iterator, assignment);
                foreach (StatementSyntax nested in forStatement.Body.Statements)
                    RewriteStatement(nested, assignment);
                break;

            case DoWhileStatementSyntax doWhile:
                RewriteExpression(doWhile.Condition, assignment);
                foreach (StatementSyntax nested in doWhile.Body.Statements)
                    RewriteStatement(nested, assignment);
                break;

            case BlockSyntax block:
                foreach (StatementSyntax nested in block.Statements)
                    RewriteStatement(nested, assignment);
                break;
        }
    }

    private void RewriteExpression(ExpressionSyntax expression, IReadOnlyDictionary<string, int> assignment)
    {
        switch (expression)
        {
            case VariableExpressionSyntax variable:
                RewriteVariable(variable, assignment);
                break;

            case ValueExpressionSyntax value:
                RewriteExpression(value.Value, assignment);
                break;

            case ParenthesizedExpressionSyntax parenthesized:
                RewriteExpression(parenthesized.Expression, assignment);
                break;

            case UnaryExpressionSyntax unary:
                RewriteExpression(unary.Value, assignment);
                break;

            case BinaryExpressionSyntax binary:
                RewriteExpression(binary.Left, assignment);
                RewriteExpression(binary.Right, assignment);
                break;

            case LogicalExpressionSyntax logical:
                RewriteExpression(logical.Left, assignment);
                RewriteExpression(logical.Right, assignment);
                break;

            case MethodInvocationExpressionSyntax invocation:
                RewriteInvocationParameters(invocation.Parameters, assignment);
                break;

            case PostfixUnaryExpressionSyntax postfix:
                RewriteExpression(postfix.Value, assignment);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                RewriteExpression(arrayIndex.Value, assignment);
                foreach (ArrayIndexerExpressionSyntax indexer in arrayIndex.Indexer)
                    RewriteExpression(indexer.Index, assignment);
                break;

            case ArrayInstantiationExpressionSyntax arrayInstantiation:
                foreach (ArrayIndexerExpressionSyntax indexer in arrayInstantiation.Indexer)
                    RewriteExpression(indexer.Index, assignment);
                break;

            case TypeCastValueExpressionSyntax typeCast:
                RewriteExpression(typeCast.Value, assignment);
                break;

            case SwitchExpressionSyntax switchExpression:
                RewriteExpression(switchExpression.Value, assignment);
                foreach (SwitchCaseExpressionSyntax @case in switchExpression.CaseBlock.Cases)
                    RewriteSwitchCase(@case, assignment);
                break;

            case AssignmentExpressionSyntax nestedAssignment:
                RewriteExpression(nestedAssignment.Left, assignment);
                RewriteExpression(nestedAssignment.Right, assignment);
                break;
        }
    }

    private void RewriteSwitchCase(SwitchCaseExpressionSyntax @case, IReadOnlyDictionary<string, int> assignment)
    {
        switch (@case)
        {
            case LiteralSwitchCaseExpressionSyntax literal:
                RewriteExpression(literal.CaseValue, assignment);
                RewriteExpression(literal.Value, assignment);
                break;

            case DefaultSwitchCaseExpressionSyntax defaultCase:
                RewriteExpression(defaultCase.Value, assignment);
                break;
        }
    }

    private void RewriteInvocationParameters(
        MethodInvocationParametersSyntax parameters,
        IReadOnlyDictionary<string, int> assignment)
    {
        if (parameters.ParameterList?.Elements is null)
            return;

        foreach (ExpressionSyntax parameter in parameters.ParameterList.Elements)
            RewriteExpression(parameter, assignment);
    }

    private void RewriteVariable(VariableExpressionSyntax variable, IReadOnlyDictionary<string, int> assignment)
    {
        string text = variable.Variable.Text;
        if (!assignment.TryGetValue(text, out int slot))
            return;

        SyntaxToken token = syntaxFactory.Variable("param", (uint)slot);
        if (variable.Variable.LeadingTrivia is { } leading)
            token = token.WithLeadingTrivia(leading.Text);
        if (variable.Variable.TrailingTrivia is { } trailing)
            token = token.WithTrailingTrivia(trailing.Text);

        variable.SetVariable(token, updatePositions: false);
    }

    private static Exception CreateException(string message, SyntaxLocation location)
    {
        return new InvalidOperationException($"{message} (Line {location.Line}, Column {location.Column})");
    }
}
