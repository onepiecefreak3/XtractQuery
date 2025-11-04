using Logic.Domain.Level5.Contract.DataClasses.Script.Gds;
using Logic.Domain.Level5.Contract.Script.Gds;
using Logic.Domain.Level5.DataClasses.Script.Gds;

namespace Logic.Domain.Level5.Script.Gds;

internal class GdsScriptParser(IGdsScriptReader reader) : IGdsScriptParser
{
    public GdsScriptFile Parse(Stream input)
    {
        GdsArgument[] arguments = reader.Read(input);

        return Parse(arguments);
    }

    public GdsScriptFile Parse(GdsArgument[] arguments)
    {
        var jumpTargets = ParseJumpTargets(arguments);

        return new GdsScriptFile
        {
            Instructions = [.. ParseInstructions(arguments, jumpTargets)]
        };
    }

    private IList<GdsScriptInstruction> ParseInstructions(GdsArgument[] arguments, GdsJumpTarget[] jumpTargets)
    {
        var result = new List<GdsScriptInstruction>();

        var parameters = new List<GdsScriptArgument>();

        GdsArgument? instruction = null;
        foreach (GdsArgument argument in arguments)
        {
            if (argument.type is >= 1 and <= 6)
            {
                if (instruction is null)
                    throw new InvalidOperationException("Script cannot start with an argument of type 1 to 6.");

                parameters.Add(CreateArgument(argument, jumpTargets));
                continue;
            }

            if (instruction is not null)
            {
                result.Add(CreateInstruction(instruction, [.. parameters], jumpTargets));

                parameters.Clear();
            }

            instruction = argument;

            switch (instruction.type)
            {
                case 0:
                    parameters.Add(CreateArgument(instruction.value, false));
                    break;

                case 7:
                    GdsJumpTarget jumpTarget = GetJumpTarget(instruction, jumpTargets);
                    parameters.Add(CreateArgument(jumpTarget.Label, true));
                    break;
            }
        }

        if (instruction is not null)
            result.Add(CreateInstruction(instruction, [.. parameters], jumpTargets));

        return result;
    }

    private GdsJumpTarget[] ParseJumpTargets(GdsArgument[] arguments)
    {
        var jumpsTargets = new List<GdsArgument>();

        Dictionary<int, GdsArgument> lookup = arguments.ToDictionary(x => x.offset, y => y);
        foreach (GdsArgument argument in arguments)
        {
            if (argument.type is not 6 and not 7)
                continue;

            if (!lookup.TryGetValue((int)argument.value!, out GdsArgument? jumpTarget))
                throw new InvalidOperationException($"Could not determine target of jump at position {argument.offset}.");

            jumpsTargets.Add(jumpTarget);
        }

        var result = new List<GdsJumpTarget>();

        var jumpIndex = 0;
        foreach (GdsArgument jumpTarget in jumpsTargets.OrderBy(j => j.offset))
        {
            result.Add(new GdsJumpTarget
            {
                Label = $"@{jumpIndex++:000}@",
                Offset = jumpTarget.offset
            });
        }

        return [.. result];
    }

    private static GdsScriptInstruction CreateInstruction(GdsArgument instruction, GdsScriptArgument[] parameters, GdsJumpTarget[] jumpTargets)
    {
        GdsJumpTarget? jumpTarget = jumpTargets.FirstOrDefault(x => x.Offset == instruction.offset);

        return new GdsScriptInstruction
        {
            Type = instruction.type,
            Arguments = parameters,
            Jump = jumpTarget is null ? null : new GdsScriptJump
            {
                Label = jumpTarget.Label
            }
        };
    }

    private static GdsScriptArgument CreateArgument(GdsArgument argument, GdsJumpTarget[] jumpTargets)
    {
        object? value = argument.value;
        if (argument.type is 6)
        {
            GdsJumpTarget jumpTarget = GetJumpTarget(argument, jumpTargets);
            value = jumpTarget.Label;
        }

        return new GdsScriptArgument
        {
            Type = argument.type switch
            {
                1 => GdsScriptArgumentType.Int,
                2 => GdsScriptArgumentType.Float,
                3 => GdsScriptArgumentType.String,
                6 => GdsScriptArgumentType.Jump,
                _ => throw new InvalidOperationException($"Unknown argument type {argument.type}.")
            },
            Value = value
        };
    }

    private static GdsScriptArgument CreateArgument(object? value, bool isJump)
    {
        return new GdsScriptArgument
        {
            Type = value switch
            {
                int => GdsScriptArgumentType.Int,
                short => GdsScriptArgumentType.Int,
                float => GdsScriptArgumentType.Float,
                string => isJump ? GdsScriptArgumentType.Jump : GdsScriptArgumentType.String,
                _ => throw new InvalidOperationException($"Unknown argument type {value?.GetType()}.")
            },
            Value = value is short shortValue ? (int)shortValue : value
        };
    }

    private static GdsJumpTarget GetJumpTarget(GdsArgument argument, GdsJumpTarget[] jumpTargets)
    {
        if (argument.value is not int intValue)
            throw new InvalidOperationException("Invalid argument type for jump target.");

        GdsJumpTarget? jumpTarget = jumpTargets.FirstOrDefault(x => x.Offset == intValue);

        if (jumpTarget is null)
            throw new InvalidOperationException($"Could not determine target of jump at position {argument.offset}.");

        return jumpTarget;
    }
}