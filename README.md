# XtractQuery

## Description
A command line tool to de- and recompile .xq files from various 3DS games by Level5.<br>
It supports all known format specifications.

## Usage

Various options have to be set to properly use the command line tool.

| Option | Description |
| - | - |
| -h | Shows a help text explaining all the options listed here and examples on how to use use them. |
| -o | The operation to execute. Has to be followed by either:<br>`d` to decompress a script<br>`e` to extract a script to human readable code<br>`c` to create a scripot from human readable code |
| -t | The type of .xq file to process. Is only necessary for operation `c`. Has to be followed by either:<br>`xq32`<br>`xseq` |
| -f | The file or directory to execute the operation on. |

### Method name mapping

In the file `methodMapping.json` instruction types, that are not known by the program (see "Instructions" in the script specification), can be mapped to a human readable name.<br>
Since those unknown instructions are normally game specific logic, they have to be figured out by the user and added to the mapping for themselves.

If an unknown instruction type has no corresponding mapping, its name will be set to `subXXX`, where `XXX` is the instruction type.

### Reference scripts

Scripts can call methods from within themselves and other scripts currently loaded in the engine. Normally, those calls happen via the CRC32/CRC16 of the function name to invoke them.<br>
To resolve those checksums back into human readable names, reference scripts can be placed in the folder `reference` next to the command line tool.

It is recommended to put every script of a game in the references to have the highest probability of properly resolving all checksums.<br>
However, there is no guarantee that a checksum will be resolved, so user action has to be taken.

## Examples

To extract a script to human readable code:<br>
```XtractQuery.exe -o e -f Path/To/File.xq```

To create a XQ32 script from human readable code:<br>
```XtractQuery.exe -o c -t xq32 -f Path/To/File.txt```

To decompress the tables in a script (see "Compression" in format specification):<br>
```XtractQuery.exe -o d -f Path/To/File.xq```
