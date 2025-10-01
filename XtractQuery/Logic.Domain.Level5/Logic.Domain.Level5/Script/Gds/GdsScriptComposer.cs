using Logic.Domain.Level5.Contract.Script.Gds;
using Logic.Domain.Level5.Contract.Script.Gds.DataClasses;
using Logic.Domain.Level5.Script.Gds.DataClasses;

namespace Logic.Domain.Level5.Script.Gds;

class GdsScriptComposer : IGdsScriptComposer
{
    public GdsArgument[] Compose(GdsScriptFile script)
    {
        GdsJumpTarget[] jumpTargets = CreateJumpTargets(script);

        return CreateArguments(script, jumpTargets);
    }

    private GdsArgument[] CreateArguments(GdsScriptFile script, GdsJumpTarget[] jumpTargets)
    {
        var result = new List<GdsArgument>();

        var offset = 0;
        foreach (GdsScriptInstruction instruction in script.Instructions)
        {
            switch (instruction.Type)
            {
                case 0:
                    result.Add(CreateArgument(offset, 0, (short)(int)instruction.Arguments[0].Value!));
                    offset += 4;

                    foreach (GdsScriptArgument argument in instruction.Arguments[1..])
                    {
                        switch (argument.Type)
                        {
                            case GdsScriptArgumentType.Jump:
                                var argumentJumpLabel = (string)argument.Value!;
                                GdsJumpTarget? argumentJumpTarget = jumpTargets.FirstOrDefault(x => x.Label == argumentJumpLabel);

                                if (argumentJumpTarget is null)
                                    throw new InvalidOperationException($"Could not determine target of jump for label {argumentJumpLabel}.");

                                result.Add(CreateArgument(offset, 6, argumentJumpTarget.Offset));
                                break;

                            default:
                                result.Add(CreateArgument(offset, (short)argument.Type, argument.Value));
                                break;
                        }

                        offset += CalculateArgumentSize(argument);
                    }

                    continue;

                case 7:
                    var jumpLabel = (string)instruction.Arguments[0].Value!;
                    GdsJumpTarget? jumpTarget = jumpTargets.FirstOrDefault(x => x.Label == jumpLabel);

                    if (jumpTarget is null)
                        throw new InvalidOperationException($"Could not determine target of jump for label {jumpLabel}.");

                    result.Add(CreateArgument(offset, 7, jumpTarget.Offset));
                    break;

                default:
                    result.Add(CreateArgument(offset, (short)instruction.Type, null));
                    break;
            }

            offset += CalculateInstructionSize(instruction);
        }

        return [.. result];
    }

    private static GdsJumpTarget[] CreateJumpTargets(GdsScriptFile script)
    {
        var result = new List<GdsJumpTarget>();

        var offset = 0;
        foreach (GdsScriptInstruction instruction in script.Instructions)
        {
            int instructionOffset = offset;
            int instructionLength = CalculateInstructionSize(instruction);

            offset += instructionLength;

            if (instruction.Jump is null)
                continue;

            result.Add(new GdsJumpTarget
            {
                Label = instruction.Jump.Label,
                Offset = instructionOffset
            });
        }

        return [.. result];
    }

    private static GdsArgument CreateArgument(int offset, short type, object? value)
    {
        return new GdsArgument
        {
            offset = offset,
            type = type,
            value = value
        };
    }

    private static int CalculateInstructionSize(GdsScriptInstruction instruction)
    {
        switch (instruction.Type)
        {
            case 0:
                int instructionLength = instruction.Arguments[1..].Sum(CalculateArgumentSize);
                return instructionLength + 4;

            case 7:
                return 6;

            case 8:
            case 9:
            case 11:
            case 12:
                return 2;

            default:
                throw new InvalidOperationException($"Unknown instruction type {instruction.Type}.");
        }
    }

    private static int CalculateArgumentSize(GdsScriptArgument argument)
    {
        switch (argument.Type)
        {
            case GdsScriptArgumentType.String:
                return ((string)argument.Value!).Length + 5;

            case GdsScriptArgumentType.Int:
            case GdsScriptArgumentType.UnsignedInt:
            case GdsScriptArgumentType.Jump:
                return 6;

            default:
                throw new InvalidOperationException($"Unknown argument type {argument.Type}.");
        }
    }
}