# XQ32/XSEQ Format Specification

## Introduction

XQ32 (and in extension XSEQ) is a script format used in 3DS Level5 games to script any sort of flow or high level behaviour.
It is used to describe menu construction and behaviour, as well as environment management, and other similar tasks.
The main motivation for this script format seems to be to elevate as much logic as possible out of the compiled executable of the game.
It may have been helpful in debugging and hot switching behaviour in development.

## Datatypes

These are the data types of data units as they will appear in this documentation.

| Type | Size in bytes | Notes |
| - | - | - |
| short | 2 | |
| ushort | 2 | Unsigned |
| int | 4 | |
| uint | 4 | Unsigned |
| float | 4 | |
| string[n] | n | Encoded as ASCII, if not stated otherwise |

## Container

This script format defines 4 tables and a string blob to store an arbitrary amount of methods of arbitrary length, callable through the games engine or the script itself.
It is unknown what exactly the entry point into the script is or if it is even a set method to invoke the scripts logic.

## Compression

Each table is compressed by Level5's own container specification, and borrows pre-existing compression schemes by Nintendo from their SDK.
Read the first 4 bytes of a table in little endian as an int. It assumes the name 'methodSize'.
Decompress the table from the 4th byte onwards until the end, according to the method portion of 'methodSize'.

The value 'methodSize' is structured as follows:
method = methodSize & 0x7
decompressedSize = methodSize >> 3

The methods map to the following compressions:
| Method | Compression |
| - | - |
| 0 | None |
| 1 | LZ10 |
| 2 | Huffman 4Bit |
| 3 | Huffman 8Bit |
| 4 | RLE |
| 5 | ZLib |
| 6 | - |
| 7 | - |

## Structure

All values are read as little endian. All values are read consecutively in one structure, if not stated otherwise.
A structure member can have a constant value, since they wouldn't make sense logically otherwise or are inferable by static analysis.

### Header

The header declares the start of each table and the string blob. It also defines the number of entries each of those tables have.
The absolute offset to a table or the string blob can be calculated by left shifting the read value by 2.

| Datatype | Name | Default value |
| - | - | - |
| string[4] | magic | "XQ32" |
| short | functionCount | |
| short | functionOffset | 0x20 >> 2 |
| short | jumpOffset | |
| short | jumpCount | |
| short | instructionOffset | |
| short | instructionCount | |
| short | argumentOffset | |
| short | argumentCount | |
| short | globalVariableCount | |
| short | stringOffset | |

### Function

The function structure declares one function of the script.
It declares the instructions and jumps used in this function, by giving the offset into the instruction and jump table and how many of them to read for this function.
It declares the parameter count, the amount of values that are passed into the method from an external caller.
It declares the amount of local and object values used in the function. Those values are used to allocate memory for the stack used in the function.
It declares its own name, by giving the offset into the string blob and its CRC32/CRC16 checksum.

#### Xq32Function

| Datatype | Name |
| - | - |
| int | nameOffset |
| uint | crc32 |
| short | instructionIndex |
| short | instructionEndIndex |
| short | jumpIndex |
| short | jumpCount |
| short | localVariableCount |
| short | objectVariableCount |
| int | parameterCount |

#### XseqFunction

| Datatype | Name |
| - | - |
| int | nameOffset |
| ushort | crc16 |
| short | instructionIndex |
| short | instructionEndIndex |
| short | jumpIndex |
| short | jumpCount |
| short | localVariableCount |
| short | objectVariableCount |
| int | parameterCount |

### Jump

The jump structure declares one jump to another instruction inside the instruction table by index.
It declares its own name, by giving the offset into the string blob and its CRC32/CRC16 checksum.
A function invokes a jump by checksum or name. It's common to use the checksum for performance.
A jump can only be performed in the current function. Jumps referenced outside the function may not be executed.

#### Xq32Jump

| Datatype | Name |
| - | - |
| int | nameOffset |
| uint | crc32 |
| int | instructionIndex |

#### XseqJump

| Datatype | Name |
| - | - |
| int | nameOffset |
| ushort | crc16 |
| int | instructionIndex |

### Instruction

The instruction structure delcares one instruction in a function of the script.
It declares the arguments used in this instruction, by giving the offset into the argument table and how many of them to read for this instruction.
It writes the result into the stack value indexed by 'targetVariable' (see "Variables").
The logic to execute is defined by 'instructionType' (see "Instructions" in the script specification).

| Datatype | Name |
| - | - |
| short | argumentIndex |
| short | argumentCount |
| short | targetVariable |
| short | instructionType |
| int | zero |

### Argument

The argument structure declares one argument in an instruction.
It declares its data type and the corresponding value.
The base type of an argument can be calculated by taking its 4 least significant bits.

| Datatype | Name |
| - | - |
| int | type |
| uint | value |

Base types of arguments typically found in scripts:
| Basetype | Datatype | Notes |
| - | - | - |
| 1 | int | A signed integer value |
| 2 | uint | An unsigned integer value |
| 3 | float | A floating point value |
| 4 | int | An index to a stack value (see "Variables") |
| 8 | int | An absolute offset into the string blob. Normally SJIS-encoded. Null-terminated. |

## Variables

Variables represent values on the stack. There are multiple stack regions when a script is executed, each with their own implications.
There are generally up to 1000 slots per stack region.

| Start | End | Description |
| - | - | - |
| 0 | 999 | Values, that persist through multiple scripts. Can contain any data, including primitive values and arrays. |
| 1000 | 1999 | Values, that persist only in the function they were set in. 1000 is reserved as the return value for a function. Mostly used for primitive values. |
| 2000 | 2999 | Values, that persist only in the function they were set in. Can contain any data, including primitive values and arrays. |
| 3000 | 3999 | Values, that exclusively hold the input parameters to a function. Can contain any data, including primitive values and arrays. |
| 4000 | 4999 | Values, that persist through multiple functions only in the script they were set in. Can contain any data, including primitive values and arrays. |
