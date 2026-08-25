# XtractQuery

## Description

A command line tool to de- and recompile .xq and .xs files from various 3DS games by Level5 and .cq, .lb, and .gds files from various NDS games by Level5.

It supports all known format specifications.

## Usage

Various options have to be set to properly use the command line tool.

| Option | Description |
| - | - |
| -h | Shows a help text explaining all the options listed here and examples on how to use use them. |
| -o | The operation to execute. Has to be followed by either:<br>`d` to decompress a script<br>`e` to extract a script to human readable code<br>`c` to create a scripot from human readable code |
| -t | The type of .xq, .cq, .lb, or .gds file to process. Is only necessary for operation `c`. Has to be followed by either:<br>`xq32`<br>`xseq`<br>`xscr`<br>`gss1`<br>`gsd1`<br>`gds` |
| -f | The file or directory to execute the operation on. |
| -l | The pointer length.<br>Valid lengths are `int`, `long`. Default is `int`.<br>The length is automatically detected when extracting.<br>This option will not have any effect on operation `e` and only on scripts of type `xq32`. |
| -e | The encoding to use for reading/writing string values.<br>Valid encodings are `sjis`, `utf8`. Default value is `sjis`. |
| -nc | If the file should use a compression layer.<br>This option is automatically detected when extracting.<br>This option will not have any effect on operation `e` and `d`. |
| -ns | If the file should emit high-level syntax.<br>This option will not have any effect on operation `c`. |

### Method name mapping

In the file `methodMapping.json` instruction types, that are not known by the program (see "Instructions" in the script specification), can be mapped to a human readable name.<br>
Since those unknown instructions are normally game specific logic, they have to be figured out by the user and added to the mapping for themselves.

If an unknown instruction type has no corresponding mapping, its name will be set to `subXXX`, where `XXX` is the instruction type.

This mapping is currently not used for GDS scripts.

### Reference scripts

Scripts can call methods from within themselves and other scripts currently loaded in the engine. Normally, those calls happen via the CRC32-B or CRC16-X25 of the function name to invoke them.<br>
To resolve those checksums back into human readable names, reference scripts can be placed in the folder `reference` next to the command line tool.

It is recommended to put every script of a game in the references to have the highest probability of properly resolving all checksums.<br>
However, there is no guarantee that a checksum will be resolved.

Only scripts of the same type as the processed script will be used for reference.

## Examples

To extract a script to human readable code:<br>
```XtractQuery.exe -o e -f Path/To/File.xq```

To create a XQ32 script from human readable code:<br>
```XtractQuery.exe -o c -t xq32 -f Path/To/File.txt```

To decompress the tables in a script (see "Compression" in format specification):<br>
```XtractQuery.exe -o d -f Path/To/File.xq```

## Disclaimer

Resolving and lowering high-level syntax structures and complex expressions were implemented by a supervised AI workflow utilizing Cursor Grok 4.5 Medium.<br>
The resolver and lowerer for those were also explicitly programmed to be single-line invocations to be toggled on or off by a simple command line parameter, if you do not want AI-generated code to run or prefer low-level instruction output.

Everything else in this project was programmed manually by a human.
