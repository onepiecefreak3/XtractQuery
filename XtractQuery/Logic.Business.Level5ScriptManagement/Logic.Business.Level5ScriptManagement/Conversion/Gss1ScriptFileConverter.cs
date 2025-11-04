using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gss1;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion;

internal class Gss1ScriptFileConverter : IGss1ScriptFileConverter
{
    private readonly IMethodNameMapper _methodNameMapper;
    private readonly ILevel5SyntaxFactory _syntaxFactory;

    public Gss1ScriptFileConverter(IMethodNameMapper methodNameMapper, ILevel5SyntaxFactory syntaxFactory)
    {
        _methodNameMapper = methodNameMapper;
        _syntaxFactory = syntaxFactory;
    }

    public CodeUnitSyntax CreateCodeUnit(Gss1ScriptFile script)
    {
        IReadOnlyList<MethodDeclarationSyntax> methods = CreateMethodDeclarations(script);

        return new CodeUnitSyntax(methods);
    }

    private IReadOnlyList<MethodDeclarationSyntax> CreateMethodDeclarations(Gss1ScriptFile script)
    {
        var result = new List<MethodDeclarationSyntax>();

        foreach (ScriptFunction function in script.Functions)
            result.Add(CreateMethodDeclaration(function, script));

        return result;
    }

    private MethodDeclarationSyntax CreateMethodDeclaration(ScriptFunction function, Gss1ScriptFile script)
    {
        SyntaxToken identifier = _syntaxFactory.Identifier(function.Name);
        _ = CreateMethodDeclarationMetadataParameters(function.LocalCount, function.ObjectCount);
        var parameters = CreateMethodDeclarationParameters(function.ParameterCount);
        var body = CreateMethodDeclarationBody(function, script);

        return new MethodDeclarationSyntax(identifier, null, parameters, body);
    }

    private MethodDeclarationMetadataParametersSyntax CreateMethodDeclarationMetadataParameters(short localCount, short objectCount)
    {
        SyntaxToken relSmaller = _syntaxFactory.Token(SyntaxTokenKind.Smaller);
        var list = CreateMethodDeclarationMetadataParameterList(localCount, objectCount);
        SyntaxToken relBigger = _syntaxFactory.Token(SyntaxTokenKind.Greater);

        return new MethodDeclarationMetadataParametersSyntax(relSmaller, list, relBigger);
    }

    private MethodDeclarationMetadataParameterListSyntax CreateMethodDeclarationMetadataParameterList(short localCount, short objectCount)
    {
        var parameter1 = CreateNumericLiteralExpression(localCount);
        SyntaxToken comma = _syntaxFactory.Token(SyntaxTokenKind.Comma);
        var parameter2 = CreateNumericLiteralExpression(objectCount);

        return new MethodDeclarationMetadataParameterListSyntax(parameter1, comma, parameter2);
    }

    private MethodDeclarationParametersSyntax CreateMethodDeclarationParameters(int parameterCount)
    {
        SyntaxToken parenOpen = _syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parameters = CreateMethodDeclarationParameterList(parameterCount);
        SyntaxToken parenClose = _syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new MethodDeclarationParametersSyntax(parenOpen, parameters, parenClose);
    }

    private CommaSeparatedSyntaxList<VariableExpressionSyntax>? CreateMethodDeclarationParameterList(int parameterCount)
    {
        if (parameterCount <= 0)
            return null;

        var result = new List<VariableExpressionSyntax>();

        foreach (int parameter in Enumerable.Range(3000, parameterCount))
            result.Add(CreateVariableExpression((uint)parameter));

        return new CommaSeparatedSyntaxList<VariableExpressionSyntax>(result);
    }

    private MethodDeclarationBodySyntax CreateMethodDeclarationBody(ScriptFunction function, Gss1ScriptFile script)
    {
        SyntaxToken curlyOpen = _syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        var expressions = CreateStatements(function, script);
        SyntaxToken curlyClose = _syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new MethodDeclarationBodySyntax(curlyOpen, expressions, curlyClose);
    }

    private IReadOnlyList<StatementSyntax>? CreateStatements(ScriptFunction function, Gss1ScriptFile script)
    {
        if (function.InstructionCount <= 0)
            return null;

        if (script.Instructions.Count < function.InstructionIndex + function.InstructionCount)
            throw new InvalidOperationException($"Can't fetch all instructions ({script.Instructions.Count} >= {function.InstructionIndex + function.InstructionCount})");
        if (script.Jumps.Count < function.JumpIndex + function.JumpCount)
            throw new InvalidOperationException($"Can't fetch all jumps ({script.Jumps.Count} >= {function.JumpIndex + function.JumpCount})");

        IDictionary<int, ScriptJump[]> jumpLookup = script.Jumps
            .Skip(function.JumpIndex).Take(function.JumpCount)
            .GroupBy(j => j.InstructionIndex)
            .ToDictionary(j => j.Key, j => j.ToArray());

        var result = new List<StatementSyntax>();
        for (int i = function.InstructionIndex; i < function.InstructionIndex + function.InstructionCount; i++)
        {
            ScriptInstruction instruction = script.Instructions[i];

            if (jumpLookup.TryGetValue(i, out ScriptJump[]? jumps))
            {
                foreach (ScriptJump jump in jumps)
                    result.Add(CreateGotoLabelStatement(jump));
            }

            result.Add(CreateStatement(instruction, script));
        }

        int instructionEndIndex = function.InstructionIndex + function.InstructionCount;
        if (!jumpLookup.ContainsKey(instructionEndIndex))
            return result;

        if (jumpLookup.TryGetValue(instructionEndIndex, out ScriptJump[]? endJumps))
        {
            foreach (ScriptJump jump in endJumps)
                result.Add(CreateGotoLabelStatement(jump));
        }

        return result;
    }

    private GotoLabelStatementSyntax CreateGotoLabelStatement(ScriptJump jump)
    {
        var labelLiteral = CreateStringLiteralExpression(jump.Name);
        SyntaxToken colonToken = _syntaxFactory.Token(SyntaxTokenKind.Colon);

        return new GotoLabelStatementSyntax(labelLiteral, colonToken);
    }

    private StatementSyntax CreateStatement(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        switch (instruction.Type)
        {
            case 10:
                return CreateYieldStatement();

            case 11:
                return CreateReturnStatement(instruction, script);

            case 12:
                return CreateExitStatement();

            case 30:
                return CreateIfGotoStatement(instruction, script);

            case 31:
                return CreateGotoStatement(instruction, script);

            case 33:
                return CreateIfNotGotoStatement(instruction, script);

            case 240:
            case 241:
                return CreatePostfixUnaryStatement(instruction, script);

            default:
                return CreateAssignmentStatement(instruction, script);
        }
    }

    private IfGotoStatementSyntax CreateIfGotoStatement(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        SyntaxToken ifToken = _syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        var value = CreateValueExpression(script.Arguments[instruction.ArgumentIndex + 1]);
        var gotoStatement = CreateGotoStatement(instruction, script);

        return new IfGotoStatementSyntax(ifToken, value, gotoStatement);
    }

    private GotoStatementSyntax CreateGotoStatement(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        SyntaxToken gotoToken = _syntaxFactory.Token(SyntaxTokenKind.GotoKeyword);
        var value = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new GotoStatementSyntax(gotoToken, value, semicolon);
    }

    private IfNotGotoStatementSyntax CreateIfNotGotoStatement(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        SyntaxToken ifToken = _syntaxFactory.Token(SyntaxTokenKind.IfKeyword);
        var value = CreateValueExpression(script.Arguments[instruction.ArgumentIndex + 1]);
        var unaryExpression = CreateNotUnaryExpression(value);
        var gotoStatement = CreateGotoStatement(instruction, script);

        return new IfNotGotoStatementSyntax(ifToken, unaryExpression, gotoStatement);
    }

    private PostfixUnaryStatementSyntax CreatePostfixUnaryStatement(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        var expression = CreatePostfixUnaryExpression(instruction, script);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new PostfixUnaryStatementSyntax(expression, semicolon);
    }

    private PostfixUnaryExpressionSyntax CreatePostfixUnaryExpression(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        ValueExpressionSyntax returnValue = CreateValueExpression((uint)instruction.ReturnParameter);

        ExpressionSyntax value = returnValue;
        if (instruction.ArgumentCount > 0)
            value = CreateArrayIndexExpression(returnValue, script.Arguments.Skip(instruction.ArgumentIndex).Take(instruction.ArgumentCount).ToArray());

        SyntaxToken operation;
        switch (instruction.Type)
        {
            case 240:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Increment);
                break;

            case 241:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Decrement);
                break;

            default:
                throw new InvalidOperationException("Unknown postfix unary expression.");
        }

        return new PostfixUnaryExpressionSyntax(value, operation);
    }

    private YieldStatementSyntax CreateYieldStatement()
    {
        SyntaxToken yield = _syntaxFactory.Token(SyntaxTokenKind.YieldKeyword);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new YieldStatementSyntax(yield, semicolon);
    }

    private ReturnStatementSyntax CreateReturnStatement(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        SyntaxToken returnToken = _syntaxFactory.Token(SyntaxTokenKind.ReturnKeyword);
        ValueExpressionSyntax? valueExpression = null;
        if (instruction.ArgumentCount > 0)
            valueExpression = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ReturnStatementSyntax(returnToken, valueExpression, semicolon);
    }

    private ExitStatementSyntax CreateExitStatement()
    {
        SyntaxToken exit = _syntaxFactory.Token(SyntaxTokenKind.ExitKeyword);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ExitStatementSyntax(exit, semicolon);
    }

    private AssignmentStatementSyntax CreateAssignmentStatement(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        ValueExpressionSyntax leftValue = CreateValueExpression((uint)instruction.ReturnParameter);
        ExpressionSyntax left = leftValue;

        switch (instruction.Type)
        {
            case 100:
            case 250:
            case 251:
            case 252:
            case 253:
            case 254:
            case 260:
            case 261:
            case 262:
            case 270:
            case 271:
                if (instruction.ArgumentCount > 1)
                {
                    var indexes3 = script.Arguments.Skip(instruction.ArgumentIndex + 1).Take(instruction.ArgumentCount - 1).ToArray();
                    left = CreateArrayIndexExpression(leftValue, indexes3);
                }
                break;
        }

        SyntaxToken equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.EqualsSign);
        SyntaxToken semicolon = _syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        ExpressionSyntax right;
        switch (instruction.Type)
        {
            case 100:
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 110:
            case 112:
            case 120:
                right = CreateUnaryExpression(instruction, script);
                break;

            case 121:
            case 122:
                right = CreateLogicalExpression(instruction, script);
                break;

            case 130:
            case 131:
            case 132:
            case 133:
            case 134:
            case 135:
            case 140:
            case 141:
            case 150:
            case 151:
            case 152:
            case 153:
            case 154:
            case 160:
            case 161:
            case 162:
            case 170:
            case 171:
                right = CreateBinaryExpression(instruction, script);
                break;

            case 250:
                equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.PlusEquals);
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 251:
                equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.MinusEquals);
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 252:
                equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.MulEquals);
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 253:
                equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.DivEquals);
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 254:
                equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.ModEquals);
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 260:
                equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.AndEquals);
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 261:
                equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.OrEquals);
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 262:
                equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.XorEquals);
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 270:
                equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.LeftShiftEquals);
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 271:
                equalsOperator = _syntaxFactory.Token(SyntaxTokenKind.RightShiftEquals);
                right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
                break;

            case 511:
                SyntaxToken intToken = _syntaxFactory.Token(SyntaxTokenKind.IntKeyword);
                var castValue1 = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);

                right = CreateTypeCastValueExpression(castValue1, intToken);
                break;

            case 512:
                SyntaxToken boolToken = _syntaxFactory.Token(SyntaxTokenKind.BoolKeyword);
                var castValue2 = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);

                right = CreateTypeCastValueExpression(castValue2, boolToken);
                break;

            case 513:
                SyntaxToken floatToken = _syntaxFactory.Token(SyntaxTokenKind.FloatKeyword);
                var castValue3 = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);

                right = CreateTypeCastValueExpression(castValue3, floatToken);
                break;

            case 523:
                right = CreateSwitchExpression(instruction, script);
                break;

            case 530:
                var indexes1 = script.Arguments.Skip(instruction.ArgumentIndex).Take(instruction.ArgumentCount).ToArray();
                right = CreateArrayInstantiationExpression(indexes1);
                break;

            case 531:
                var indexes2 = script.Arguments.Skip(instruction.ArgumentIndex + 1).Take(instruction.ArgumentCount - 1).ToArray();
                right = CreateArrayIndexExpression(script.Arguments[instruction.ArgumentIndex], indexes2);
                break;

            default:
                right = CreateMethodInvocationExpression(instruction, script);
                break;
        }

        return new AssignmentStatementSyntax(left, equalsOperator, right, semicolon);
    }

    private TypeCastValueExpressionSyntax CreateTypeCastValueExpression(ValueExpressionSyntax value, SyntaxToken type)
    {
        var typeCast = CreateTypeCastExpression(type);

        return new TypeCastValueExpressionSyntax(typeCast, value);
    }

    private TypeCastExpressionSyntax CreateTypeCastExpression(SyntaxToken type)
    {
        SyntaxToken parenOpen = _syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        SyntaxToken parenClose = _syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new TypeCastExpressionSyntax(parenOpen, type, parenClose);
    }

    private SwitchExpressionSyntax CreateSwitchExpression(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        var switchValue = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
        SyntaxToken switchToken = _syntaxFactory.Token(SyntaxTokenKind.SwitchKeyword);
        var block = CreateSwitchBlockExpression(instruction, script);

        return new SwitchExpressionSyntax(switchValue, switchToken, block);
    }

    private SwitchBlockExpressionSyntax CreateSwitchBlockExpression(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        SyntaxToken curlyOpen = _syntaxFactory.Token(SyntaxTokenKind.CurlyOpen);
        var cases = CreateSwitchCaseExpressions(instruction, script);
        SyntaxToken curlyClose = _syntaxFactory.Token(SyntaxTokenKind.CurlyClose);

        return new SwitchBlockExpressionSyntax(curlyOpen, cases, curlyClose);
    }

    private IReadOnlyList<SwitchCaseExpressionSyntax> CreateSwitchCaseExpressions(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        if (instruction.ArgumentCount <= 1)
            throw new InvalidOperationException("Invalid amount of arguments for switch case expression.");

        var result = new List<SwitchCaseExpressionSyntax>();

        for (var i = 2; i < instruction.ArgumentCount; i += 2)
        {
            if (i + 1 >= instruction.ArgumentCount)
                throw new InvalidOperationException("Invalid amount of arguments for switch case expression.");

            result.Add(CreateLiteralSwitchCaseExpression(script.Arguments[instruction.ArgumentIndex + i], script.Arguments[instruction.ArgumentIndex + i + 1]));
        }

        result.Add(CreateDefaultSwitchCaseExpression(script.Arguments[instruction.ArgumentIndex + 1]));

        return result;
    }

    private LiteralSwitchCaseExpressionSyntax CreateLiteralSwitchCaseExpression(ScriptArgument literal, ScriptArgument argument)
    {
        var caseValue = CreateValueExpression(literal);
        SyntaxToken arrowRight = _syntaxFactory.Token(SyntaxTokenKind.ArrowRight);
        var value = CreateValueExpression(argument);

        return new LiteralSwitchCaseExpressionSyntax(caseValue, arrowRight, value);
    }

    private DefaultSwitchCaseExpressionSyntax CreateDefaultSwitchCaseExpression(ScriptArgument argument)
    {
        SyntaxToken underscore = _syntaxFactory.Token(SyntaxTokenKind.Underscore);
        SyntaxToken arrowRight = _syntaxFactory.Token(SyntaxTokenKind.ArrowRight);
        var value = CreateValueExpression(argument);

        return new DefaultSwitchCaseExpressionSyntax(underscore, arrowRight, value);
    }

    private UnaryExpressionSyntax CreateUnaryExpression(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        var value = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);

        switch (instruction.Type)
        {
            case 110:
                return CreateComplementUnaryExpression(value);

            case 112:
                return CreateNegateUnaryExpression(value);

            case 120:
                return CreateNotUnaryExpression(value);

            default:
                throw new InvalidOperationException("Unknown unary expression.");
        }
    }

    private UnaryExpressionSyntax CreateComplementUnaryExpression(ValueExpressionSyntax value)
    {
        SyntaxToken operation = _syntaxFactory.Token(SyntaxTokenKind.Complement);

        return new UnaryExpressionSyntax(operation, value);
    }

    private UnaryExpressionSyntax CreateNotUnaryExpression(ValueExpressionSyntax value)
    {
        SyntaxToken operation = _syntaxFactory.Token(SyntaxTokenKind.NotKeyword);

        return new UnaryExpressionSyntax(operation, value);
    }

    private UnaryExpressionSyntax CreateNegateUnaryExpression(ValueExpressionSyntax value)
    {
        SyntaxToken operation = _syntaxFactory.Token(SyntaxTokenKind.Minus);

        return new UnaryExpressionSyntax(operation, value);
    }

    private LogicalExpressionSyntax CreateLogicalExpression(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        var left = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);
        var right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex + 1]);

        SyntaxToken operation;
        switch (instruction.Type)
        {
            case 121:
                operation = _syntaxFactory.Token(SyntaxTokenKind.AndKeyword);
                break;

            case 122:
                operation = _syntaxFactory.Token(SyntaxTokenKind.OrKeyword);
                break;

            default:
                throw new InvalidOperationException("Unknown logical expression.");
        }

        return new LogicalExpressionSyntax(left, operation, right);
    }

    private BinaryExpressionSyntax CreateBinaryExpression(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        var left = CreateValueExpression(script.Arguments[instruction.ArgumentIndex]);

        SyntaxToken operation;
        switch (instruction.Type)
        {
            case 140:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Plus);
                return new BinaryExpressionSyntax(left, operation, CreateValueExpression(1, ScriptArgumentType.Int));

            case 141:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Minus);
                return new BinaryExpressionSyntax(left, operation, CreateValueExpression(1, ScriptArgumentType.Int));
        }

        var right = CreateValueExpression(script.Arguments[instruction.ArgumentIndex + 1]);

        switch (instruction.Type)
        {
            case 130:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Equals);
                break;

            case 131:
                operation = _syntaxFactory.Token(SyntaxTokenKind.NotEquals);
                break;

            case 132:
                operation = _syntaxFactory.Token(SyntaxTokenKind.GreaterEquals);
                break;

            case 133:
                operation = _syntaxFactory.Token(SyntaxTokenKind.SmallerEquals);
                break;

            case 134:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Greater);
                break;

            case 135:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Smaller);
                break;

            case 150:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Plus);
                break;

            case 151:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Minus);
                break;

            case 152:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Mul);
                break;

            case 153:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Div);
                break;

            case 154:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Mod);
                break;

            case 160:
                operation = _syntaxFactory.Token(SyntaxTokenKind.And);
                break;

            case 161:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Or);
                break;

            case 162:
                operation = _syntaxFactory.Token(SyntaxTokenKind.Xor);
                break;

            case 170:
                operation = _syntaxFactory.Token(SyntaxTokenKind.LeftShift);
                break;

            case 171:
                operation = _syntaxFactory.Token(SyntaxTokenKind.RightShift);
                break;

            default:
                throw new InvalidOperationException("Unknown binary expression.");
        }

        return new BinaryExpressionSyntax(left, operation, right);
    }

    private ArrayInstantiationExpressionSyntax CreateArrayInstantiationExpression(IList<ScriptArgument> indexes)
    {
        SyntaxToken newToken = _syntaxFactory.Token(SyntaxTokenKind.NewKeyword);
        var indexers = CreateArrayIndexerExpressions(indexes);

        return new ArrayInstantiationExpressionSyntax(newToken, indexers);
    }

    private ArrayIndexExpressionSyntax CreateArrayIndexExpression(ScriptArgument array, IList<ScriptArgument> indexes)
    {
        ValueExpressionSyntax arrayValue = CreateValueExpression(array);
        return CreateArrayIndexExpression(arrayValue, indexes);
    }

    private ArrayIndexExpressionSyntax CreateArrayIndexExpression(ValueExpressionSyntax arrayVariable, IList<ScriptArgument> indexes)
    {
        return new ArrayIndexExpressionSyntax(arrayVariable, CreateArrayIndexerExpressions(indexes));
    }

    private IReadOnlyList<ArrayIndexerExpressionSyntax> CreateArrayIndexerExpressions(IList<ScriptArgument> indexes)
    {
        var result = new List<ArrayIndexerExpressionSyntax>();

        foreach (var index in indexes)
            result.Add(CreateArrayIndexerExpression(index));

        return result;
    }

    private ArrayIndexerExpressionSyntax CreateArrayIndexerExpression(ScriptArgument argument)
    {
        SyntaxToken bracketOpen = _syntaxFactory.Token(SyntaxTokenKind.BracketOpen);
        SyntaxToken bracketClose = _syntaxFactory.Token(SyntaxTokenKind.BracketClose);

        ValueExpressionSyntax value = CreateValueExpression(argument);

        return new ArrayIndexerExpressionSyntax(bracketOpen, value, bracketClose);
    }

    private MethodInvocationExpressionSyntax CreateMethodInvocationExpression(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        SyntaxToken identifier = CreateMethodNameIdentifier(instruction, script);
        var metadata = CreateMethodInvocationMetadata(instruction, script);
        var parameters = CreateMethodInvocationExpressionParameters(instruction, script);

        return new MethodInvocationExpressionSyntax(identifier, metadata, parameters);
    }

    private SyntaxToken CreateMethodNameIdentifier(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        if (IsMethodNameTransfer(instruction, script))
            return _syntaxFactory.Identifier((string)script.Arguments[instruction.ArgumentIndex].Value);

        if (_methodNameMapper.MapsInstructionType(instruction.Type))
        {
            string mappedMethod = _methodNameMapper.GetMethodName(instruction.Type);
            return _syntaxFactory.Identifier(mappedMethod);
        }

        return _syntaxFactory.Identifier($"sub{instruction.Type}");
    }

    private MethodInvocationMetadataSyntax? CreateMethodInvocationMetadata(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        if (!IsMethodNameTransfer(instruction, script))
            return null;

        if (instruction.ArgumentIndex >= script.Arguments.Count ||
            script.Arguments[instruction.ArgumentIndex].RawArgumentType < 0)
            return null;

        SyntaxToken relSmaller = _syntaxFactory.Token(SyntaxTokenKind.Smaller);
        var value = CreateLiteralExpression(script.Arguments[instruction.ArgumentIndex].RawArgumentType, ScriptArgumentType.Int);
        SyntaxToken relBigger = _syntaxFactory.Token(SyntaxTokenKind.Greater);

        return new MethodInvocationMetadataSyntax(relSmaller, value, relBigger);
    }

    private MethodInvocationParametersSyntax CreateMethodInvocationExpressionParameters(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        SyntaxToken parenOpen = _syntaxFactory.Token(SyntaxTokenKind.ParenOpen);
        var parameterList = CreateValueList(instruction, script);
        SyntaxToken parenClose = _syntaxFactory.Token(SyntaxTokenKind.ParenClose);

        return new MethodInvocationParametersSyntax(parenOpen, parameterList, parenClose);
    }

    private CommaSeparatedSyntaxList<ValueExpressionSyntax>? CreateValueList(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        int argumentCount = instruction.ArgumentCount;
        int argumentIndex = instruction.ArgumentIndex;

        if (IsMethodNameTransfer(instruction, script))
        {
            argumentIndex++;
            argumentCount--;
        }

        if (argumentCount <= 0)
            return null;

        if (script.Arguments.Count < argumentIndex + argumentCount)
            throw new InvalidOperationException($"Can't fetch all arguments ({script.Arguments.Count} >= {argumentIndex + argumentCount})");

        var result = new List<ValueExpressionSyntax>();
        for (int i = argumentIndex; i < argumentIndex + argumentCount; i++)
            result.Add(CreateValueExpression(script.Arguments[i]));

        return new CommaSeparatedSyntaxList<ValueExpressionSyntax>(result);
    }

    private ValueExpressionSyntax CreateValueExpression(ScriptArgument argument)
    {
        return CreateValueExpression(argument.Value, argument.Type, argument.RawArgumentType);
    }

    private ValueExpressionSyntax CreateValueExpression(uint variableSlot)
    {
        return CreateValueExpression(variableSlot, ScriptArgumentType.Variable);
    }

    private ValueExpressionSyntax CreateValueExpression(object value, ScriptArgumentType argumentType, int rawArgumentType = -1)
    {
        ExpressionSyntax parameter = CreateArgumentExpression(value, argumentType);

        ValueMetadataParametersSyntax? parameters = null;
        if (rawArgumentType >= 0)
            parameters = CreateValueMetadataParameters(rawArgumentType);

        return new ValueExpressionSyntax(parameter, parameters);
    }

    private ValueMetadataParametersSyntax CreateValueMetadataParameters(int rawArgumentType)
    {
        SyntaxToken relSmaller = _syntaxFactory.Token(SyntaxTokenKind.Smaller);
        var metadataParameter = CreateNumericLiteralExpression(rawArgumentType);
        SyntaxToken relBigger = _syntaxFactory.Token(SyntaxTokenKind.Greater);

        return new ValueMetadataParametersSyntax(relSmaller, metadataParameter, relBigger);
    }

    private ExpressionSyntax CreateArgumentExpression(object value, ScriptArgumentType argumentType)
    {
        switch (argumentType)
        {
            case ScriptArgumentType.Variable:
                return CreateVariableExpression((uint)value);

            default:
                return CreateLiteralExpression(value, argumentType);
        }
    }

    private LiteralExpressionSyntax CreateLiteralExpression(object value, ScriptArgumentType argumentType)
    {
        switch (argumentType)
        {
            case ScriptArgumentType.Int:
                return CreateNumericLiteralExpression((int)value);

            case ScriptArgumentType.StringHash:
                if (value is string stringValue)
                    return CreateHashStringExpression(stringValue);

                return CreateHashNumericLiteral((uint)value);

            case ScriptArgumentType.Float:
                return CreateFloatingNumericLiteralExpression((float)value);

            case ScriptArgumentType.String:
                return CreateStringLiteralExpression((string)value);

            default:
                throw new InvalidOperationException($"Unknown argument type {argumentType}.");
        }
    }

    private VariableExpressionSyntax CreateVariableExpression(uint variableSlot)
    {
        // 5000+ ?
        // Values 4000+ are script global values
        // Values 3000+ are input parameters to the function
        // 2000+ ?
        // Values 1000+ are function local values
        // 0000+ ?

        if (variableSlot is >= 0 and <= 999)
            return new VariableExpressionSyntax(_syntaxFactory.Variable("unk", variableSlot));
        if (variableSlot is >= 1000 and <= 1999)
            return new VariableExpressionSyntax(_syntaxFactory.Variable("local", variableSlot - 1000));
        if (variableSlot is >= 2000 and <= 2999)
            return new VariableExpressionSyntax(_syntaxFactory.Variable("object", variableSlot - 2000));
        if (variableSlot is >= 3000 and <= 3999)
            return new VariableExpressionSyntax(_syntaxFactory.Variable("param", variableSlot - 3000));
        if (variableSlot is >= 4000 and <= 4999)
            return new VariableExpressionSyntax(_syntaxFactory.Variable("global", variableSlot - 4000));

        throw new InvalidOperationException($"Unknown variable slot {variableSlot}.");
    }

    private LiteralExpressionSyntax CreateNumericLiteralExpression(int value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.NumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateHashStringExpression(string value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.HashStringLiteral(value));
    }

    private LiteralExpressionSyntax CreateHashNumericLiteral(uint value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.HashNumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateFloatingNumericLiteralExpression(float value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.FloatingNumericLiteral(value));
    }

    private LiteralExpressionSyntax CreateStringLiteralExpression(string value)
    {
        return new LiteralExpressionSyntax(_syntaxFactory.StringLiteral(value));
    }

    private bool IsMethodNameTransfer(ScriptInstruction instruction, Gss1ScriptFile script)
    {
        if (instruction.Type != 20)
            return false;

        if (instruction.ArgumentCount <= 0)
            return false;

        return script.Arguments[instruction.ArgumentIndex].Value is string;
    }
}