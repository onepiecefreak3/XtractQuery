XQ32/XSEQ Format Specification

-- Basics

XQ32 (and in extension XSEQ) is a script format used in 3DS Level5 games to script any sort of flow or high level behaviour.
It is used to describe menu construction and behaviour, as well as environment management, and other similar tasks.
The main motivation for this script format seems to be to elevate as much logic as possible out of the compiled executable of the game.
It may have been helpful in debugging and hot switching behaviour in development.

-- Datatypes

These are the data types of data units as they will apear in this documentation.
byte - 1 byte
short/ushort - 2 bytes
int/uint - 4 bytes
float - 4 bytes
long/ulong - 8 bytes
string[n] - an ASCII string; not null-terminated; n denotes the length

-- Description

This script format defines 4 tables and a string blob to store an arbitrary amount of methods of arbitrary length, callable through the games engine or itself.
It is unknown what exactly the entry point into the script is or if it is even a set method to invoke the scripts logic.

-- Compression

Each table is compressed by Level5's own container specification, and borrows pre-existing compression schemes by Nintendo from their SDK.
Read the first 4 bytes of a table in little endian as an int. It assumes the name 'methodSize'.
Decompress the table from the 4th byte until the end, according to the method portion of 'methodSize'.

The value 'methodSize' i structured as follows:
method = methodSize & 0x7
decompressedSize = methodSize >> 3

The methods map to the following compressions:
0 - No compression
1 - LZ10
2 - Huffman 4Bit
3 - Huffman 8Bit
4 - RLE
5 - ZLib
6-7 - Not used

-- Structure

All values are read as little endian. All values are read consecutively in one structure, if not stated otherwise.
A structure member can have a constant value, since they wouldn't make sense logically otherwise or are inferable by static analysis.

- Header (same in both)
string[4] magic = "XQ32"
short functionCount
short functionOffset = 0x20 >> 2
short jumpOffset
short jumpCount
short instructionOffset
short instructionCount
short argumentOffset
short argumentcount
short globalVariableCount
short stringOffset

The header declares the start of each of the tables and the string blob. It also defines the number of entries each of those tables have.

- Xq32Function
int nameOffset
uint crc32
short instructionOffset
short instructionEndOffset
short jumpOffset
short jumpCount
short localVariableCount
short objectVariableCount
int parameterCount

- XseqFunction
int nameOffset
ushort crc16
short instructionOffset
short instructionEndOffset
short jumpOffset
short jumpCount
short localVariableCount
short objectVariableCount
short parameterCount

The function structure declares one function of the script.
It declares the instructions and jumps used in this function, by giving the offset into the instruction and jump table and how many of them to read for this function.
It declares the parameter count, the amount of values that are passed into the method from an external caller.
It declares the amount of local and object values used in the function. Those values are used to allocate memory for the stack used in the function.
It declares its own name, by giving the offset into the string blob and its CRC32/CRC16 checksum.

- Xq32Jump
int nameOffset
uint crc32
int instructionIndex

- XseqJump
int nameOffset
ushort crc16
short instructionIndex

The jump structure declares one jump to another instruction inside the instruction table by index.
It declares its own name, by giving the offset into the string blob and its CRC32/CRC16 checksum.
A function invokes a jump by checksum or name. It's common to use the checksum for performance.
A jump can only be performed in the current function. Jumps referenced outside the function may not be executed.

- Instruction (same in both)
short argOffset
short argCount
short returnParameter
short instructionType
int zero = 0

The instruction structure delcares one instruction in a function of the script.
It declares the arguments used in this instruction, by giving the offset into the argument table and how many of them to read for this instruction.
It writes the result into the stack value indexed by 'returnParameter'.
It defines its functionality by indexing it with 'instructionType'.

'instructionType's are defined by the engine the script is executed with and can define operations such as, but not limited to:
	Mathematical operations
	Conditional jumps
	System methods, such as format
	More complex logic (usually instructions in the 1000+ range)

- Argument (same in both)
int type
uint value

The argument structure declares one argument in an instruction.
It declares its data type and the corresponding value.
The data types are (by taking the 4 LSB of 'type'):
1 - int - A signed integer value
2 - uint - An unsigned integer value
3 - float - A floating point value
4 - int - A value pointing to one index on the scripts stack (See "Stack" in the script specification for more information)
8 - string - A pointer into the string table and corresponding to a null-terminated string