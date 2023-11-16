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
| -t | The type of .xq file to process. Is only necessary for operation `c`. Has to be followed by either:<br>xq32<br>xseq |
| -f | The file to execute the operation on. |

## Examples

To extract a script to human readable code:<br>
```XtractQuery.exe -o e -f Path/To/File.xq```

To create a XQ32 script from human readable code:<br>
```XtractQuery.exe -o c -t xq32 -f Path/To/File.txt```

To decompress the tables in a script (see "Compression" in format specification):<br>
```XtractQuery.exe -o d -f Path/To/File.xq```
