# XQ32/XSEQ/GSS1 Script Specification

## Introduction

This document describes the human-readable output from XtractQuery and its syntax.<br>
It also describes its relation to the instruction types read from the format, as per the format specification.

## Functions

Functions in the script are a finite sequence of instructions to be executed. Some instructions can jump to labels to immitate higher level programming concepts, like ``if ... else ...`` or loops. A function always has a name consisting of only lower- and uppercase letters and alphanumerical digits. A function can have up to 1000 parameters to pass into.<br>
```
MyFunction($param0, $param1, ...)
{
  ...
}
```

## Jumps

Jumps in the script are string literals suffixed by a colon `:` above an instruction. An instruction can have multiple labels. If called by any operation, that can execute a jump (see "Instructions" > "Jumps") the function will jump to that instruction and continue execution there.<br>
```
    if 1 goto "@000@"h;
"@000@":
"@001@":
    $local1 = log("Jumped here.");
```

## Comments

The script can contain single-line comments prefixed with `//` and multi-line comments encased in `/* Comment */`. Those comments will be ignored on compilation.<br>
```
// This is a comment
/* This is also
   a comment */
```

## Value notation

All literal values and variables have their own notation, so they can be faithfully re-compiled back into their base types (see "Argument" in the format specification).

| Basetype | Notation | Description |
| - | - | - |
| 1 | ```0```<br>```0x00000000``` | Describes a signed integer. |
| 2 | ```0h```<br>```0x00000000h``` | Describes an unsigned integer. The suffix 'h' was chosen, as this type is mostly used for checksum ('h'ash) values. |
| 2 | ```"Text"h``` | Describes a string, that is converted to a checksum and then processed and written like an unsigned integer. The suffix 'h' was chosen to infer a parity to unsigned integers and what they represent. Treated equivalently to the previous table entry by all instructions and functions. |
| 3 | ```1.0f``` | Describes a floating point value. The suffix 'f' stands for 'float'. |
| 4 | ```$variable1``` | Describes a value on the stack. Refer to "Variable notation" for their specific notation. |
| 8 | ```"Text"``` | Describes a string, that is written as is. It will NOT be converted to a checksum. |
| 10 | N/A | Describes a fixed size array initialised using instruction 530 and indexed via instruction 531. |

### Value type notation

This type notation is meant for **advanced users** only. Use at your own risk.

All literal values and variables can be suffixed by ``<[type]>`` to explicitly set the base type of the value to any integer instead of being derived from the notation as shown under "Value notation".<br>

## Variable notation

Variables have a fixed notation to declare their placement on the stack. (see "Variables" in the format specification)<br>
All variables start with the prefix '$', followed by a fixed term and number from 0 to 999 for placement on the stack.

Additionally, as of version 3.0.4, an optional variable name can be appended after the number portion of the variable.<br>
Everything after the number will be ignored for compilation and follows no specific syntax other than it has to append to the number portion and shouldn't start with a number itself.

| Notation | Description |
| - | - |
| ```$unk0``` | A value, that persists through multiple scripts. |
| ```$local0``` | A value, that persists only in the function it was set in. |
| ```$object0``` | A value, that persists only in the function it was set in. Frequently used for UI objects in some games. |
| ```$param0``` | A value, that exclusively holds input parameters to the function. |
| ```$global0``` | A value, that persists through multiple functions only in the script it was set in. |

## Instructions

#### Returns
| Type | Description |
| - | - |
| 10 | Yields back into the script engine to execute logic outside the script and continues the method at the next instruction.<br>```yield;``` |
| 11 | Returns from a function at that moment of execution. Can set the return value of the function.<br>```return;```<br>```return value;``` |
| 12 | Exits the script, that is currently executed, completely.<br>```exit;``` |

#### Calls
| Type | Description |
| - | - |
| 20 | Calls any function from any currently loaded script in the engine, either by name or by its CRC32-B or CRC16-X25 checksum.<br>```$local1 = call("MyOtherFunction"h, arg1, arg2, ...);```<br>```$local1 = call("MyOtherFunction", arg1, arg2, ...);```<br>```$local1 = MyOtherFunction(arg1, arg2, ...);``` |
| 21 | Alias for instruction 20; due to some hardcoding within XtractQuery it is currently *not* recommended to use this alias.                                                                                                                                                                                   |

#### Jumps
| Type | Description |
| - | - |
| 30 | If the value equates to ``true``, jump to the label in the current function.<br>```if 1 goto "Label1"h;```<br>```if $variable0 goto "Label1"h;``` |
| 31 | Unconditionally jump to the label in the current function.<br>If multiple labels are specified, the instruction jumps unconditionally to one random label.<br>```goto "Label1"h;```<br>```goto "Label1"h, "Labe2"h;``` |
| 32 | If the value equates to ``true``, jump to the label in the current function.<br>```if 0 goto "Label1"h;```<br>```if $variable0 goto "Label1"h;``` |
| 33 | If the negated value equates to ``true``, jump to the label in the current function.<br>Decompilation prefers the operator `not`.<br>Compilation accepts operators `not` and `!`.<br>```if not 0 goto "Label1"h;```<br>```if not $variable0 goto "Label1"h;``` |
> Note: Differences between instruction 30 and instruction 32 are unclear.

#### Basic assignments and operations
| Type | Description |
| - | - |
| 100 | Sets a literal value or variable to another variable.<br>```$local1 = 0;```<br>```$local1 = $local2;``` |
| 110 | Sets the bit complement of a literal value or variable to another variable.<br>```$local1 = ~0;```<br>```$local1 = ~$local2;``` |
| 111 | Coerces a literal value or variable to a number and sets to another variable.<br>Ints and floats remain unchanged. All other types coerce to `0`. <br>```$local1 = to_number("heya"); // 0```<br>```$local1 = to_number($local2);``` |
| 112 | Sets the negation of a literal value or variable to another variable.<br>```$local1 = -0;```<br>```$local1 = -$local2;``` |
| 140 | Adds 1 to a variable and sets to another variable.<br>```$local1 = $local2 + 1;``` |
| 141 | Subtracts 1 from a variable and sets to another variable.<br>```$local1 = $local2 - 1;``` |
| 150 | Adds a literal value or variable to another variable and sets to another variable. Returns `0` if a non-numeric type is used.<br>```$local1 = $local2 + 30;```<br>```$local1 = $local2 + $object1;```<br>```$local1 = "hi!" + 3; // 0 because "hi" isn't numeric``` |
| 151 | Subtracts a literal value or variable from another variable and sets to another variable.<br>```$local1 = $local2 - 30;```<br>```$local1 = $local2 - $object1;``` |
| 152 | Multiplies a literal value or variable with another variable and sets to another variable.<br>```$local1 = $local2 * 30;```<br>```$local1 = $local2 * $object1;``` |
| 153 | Divides a variable by a literal value or another variable and sets to another variable.<br>```$local1 = $local2 / 30;```<br>```$local1 = $local2 / $object1;``` |
| 154 | Modulates variable by a literal value or another variable and sets to another variable.<br>```$local1 = $local2 % 30;```<br>```$local1 = $local2 % $object1;``` |
| 240 | Increments a variable by 1.<br>```$local1++;``` |
| 241 | Decrements a variable by 1.<br>```$local1--;``` |
| 250 | Adds a literal value or variable to another variable.<br>```$local1 += 30;```<br>```$local1 += $object1;``` |
| 251 | Subtracts a literal value or variable from another variable.<br>```$local1 -= 30;```<br>```$local1 -= $object1;``` |
| 252 | Multiplies a literal value or variable to another variable.<br>```$local1 *= 30;```<br>```$local1 *= $object1;``` |
| 253 | Divides a variable by a literal value or variable.<br>```$local1 /= 30;```<br>```$local1 /= $object1;``` |
| 254 | Modulates a variable by a literal value or variable.<br>```$local1 %= 30;```<br>```$local1 % = $object1;``` |

#### Bit operations
| Type | Description |
| - | - |
| 160 | Bitwise and two literal values or variables and sets to another variable.<br>```$local1 = 33 & 32;```<br>```$local1 = $local2 & 32;```<br>```$local1 = $local2 & $local3;``` |
| 161 | Bitwise or two literal values or variables and sets to another variable.<br>```$local1 = 33 \| 32;```<br>```$local1 = $local2 \| 32;```<br>```$local1 = $local2 \| $local3;``` |
| 162 | Bitwise xor two literal values or variables and sets to another variable.<br>```$local1 = 33 ^ 32;```<br>```$local1 = $local2 ^ 32;```<br>```$local1 = $local2 ^ $local3;``` |
| 170 | Bitwise shift left a literal value or variable by another literal value or variable and sets to another variable.<br>```$local1 = 33 << 2;```<br>```$local1 = $local2 << 2;```<br>```$local1 = $local2 << $local3;``` |
| 171 | Bitwise shift right a literal value or variable by another literal value or variable and sets to another variable.<br>```$local1 = 33 >> 2;```<br>```$local1 = $local2 >> 2;```<br>```$local1 = $local2 >> $local3;``` |
| 260 | Bitwise and a variable with a literal value or another variable.<br>```$local1 &= 32;```<br>```$local1 &= $local3;``` |
| 261 | Bitwise or a variable with a literal value or another variable.<br>```$local1 \|= 32;```<br>```$local1 \|= $local3;``` |
| 262 | Bitwise xor a variable with a literal value or another variable.<br>```$local1 ^= 32;```<br>```$local1 ^= $local3;``` |
| 270 | Bitwise shift left a variable with a literal value or another variable.<br>```$local1 <<= 32;```<br>```$local1 <<= $local3;``` |
| 271 | Bitwise shift right a variable with a literal value or another variable.<br>```$local1 >>= 32;```<br>```$local1 >>= $local3;``` |

#### Comparisons
| Type | Description |
| - | - |
| 130 | Compares if two literal values or variables are equal and sets to another variable.<br>```$local1 = 1 == 2;```<br>```$local1 = $local2 == 2;```<br>```$local1 = $local3 == $local4;``` |
| 131 | Compares if two literal values or variables are not equal and sets the result to another variable.<br>```$local1 = 1 != 2;```<br>```$local1 = $local2 != 2;```<br>```$local1 = $local3 != $local4;``` |
| 132 | Compares if one literal value or variable is greater or equal to another literal value or variable and sets to another variable. Lexographically compares strings. <br>```$local1 = 1 >= 2;```<br>```$local1 = $local2 >= 2;```<br>```$local1 = $local3 >= $local4;``` |
| 133 | Compares if one literal value or variable is smaller or equal to another literal value or variable and sets to another variable. Lexographically compares strings. <br>```$local1 = 1 <= 2;```<br>```$local1 = $local2 <= 2;```<br>```$local1 = $local3 <= $local4;``` |
| 134 | Compares if one literal value or variable is greater than another literal value or variable and sets to another variable. Lexographically compares strings. <br>```$local1 = 1 > 2;```<br>```$local1 = $local2 > 2;```<br>```$local1 = $local3 > $local4;``` |
| 135 | Compares if one literal value or variable is smaller than another literal value or variable and sets to another variable. Lexographically compares strings. <br>```$local1 = 1 < 2;```<br>```$local1 = $local2 < 2;```<br>```$local1 = $local3 < $local4;``` |

#### Booleans
| Type | Description |
| - | - |
| 120 | Coerces a literal value or variable to boolean and negates it.<br>Decompilation prefers the operator `not`.<br>Compilation accepts operators `not` and `!`.<br>```$local1 = not $local2;``` |
| 121 | Coerces two literal values or variables to boolean and runs a logical AND operation on them.<br>Decompilation prefers the operator `and`.<br>Compilation accepts operators `and` and `&&`.<br>```$local1 = 1 and 1;```<br>```$local1 = $local2 and 1;```<br>```$local1 = $local2 and $local3;``` |
| 122 | Coerces two literal values or variables to boolean and runs a logical OR operation on them.<br>Decompilation prefers the operator `or`.<br>Compilation accepts operators `or` and `||`.<br>```$local1 = 1 or 1;```<br>```$local1 = $local2 or 1;```<br>```$local1 = $local2 or $local3;``` |

#### Strings
| Type | Description |
| - | - |
| 500 | Was originally used to log a message in developement. Is a no-op in published games.<br>```$local1 = log("This is a message. $local2 = ", $local2);``` |
| 501 | Formats a string using C format specifiers (e.g., %s, %d). Use %% to escape percent signs, '-' for left alignment, numbers for minimum field width etc. Supports up to 999 placeholders per call. Note: %c returns a corrupted string on an input of several chars. <br>```$local1 = format("$local2 is equal to %s and $local3 is %s.", $local2, $local3);``` |
| 502 | Converts a UTF-8 encoded string into a UTF-16 (wide) encoded string.<br>```$local1 = utf8_to_wide("hi");``` |
| 503 | Gets the first n chars of a string. Crashes on a negative end position. Resets the variable it returns to on an end position of `0`. <br>```$local1 = substring("This message", 4); // returns "This" ``` |

#### Arrays
| Type | Description |
| - | - |
| 530 | Creates a new multi-dimensional array.<br>```$local1 = new[2];```<br>```$local1 = new[2][1];``` |
| 531 | Gets a reference to the indexed element in an array. Non-numeric indexes get coerced to `0`. <br>```$local1 = $local2[0]; // accesses index 0 of $local2```<br>```$local1 = $local2["hi"]; // equivalent to $local1 = $local2[0];``` |

The array index notation can be used in all shorthand assignments of type 240 - 271. They can not be directly used in operations of type 110 - 171. You need to use operation 531 to get an array element and set it to another variable to use them in those operations.

#### Type
| Type | Description |
| - | - |
| 40 | Gets the base type of a variable. Base types can be found at the start of the specification. <br>```$local1 = typeof($local2);``` |
| 511 | Casts a literal value or variable to int. Truncates floats. Other types coerce to `0`. <br>```$local1 = (int)$local2;``` |
| 512 | Casts a literal value or variable to bool. `0`, `0.0f` and strings coerce to `false`. All other values coerce to `true`. <br>```$local1 = (bool)$local2;``` |
| 513 | Casts a literal value or variable to float. Non-numeric values coerce to `0.0f`. <br>```$local1 = (float)$local2;``` |

#### Math
| Type | Description |
| - | - |
| 600 | Gets the absolute representation of a literal value or variable and sets to another variable. Non-numeric values coerce to `0`. <br>```$local1 = math_abs($local2);``` |
| 601 | Gets the square root of a literal value or variable and sets to another variable.<br>```$local1 = math_sqrt($local2);``` |
| 602 | Rounds a literal value or variable to the next integer towards negative infinity and sets to another variable.<br>```$local1 = math_floor($local2);``` |
| 603 | Rounds a literal value or variable to the nearest integer and sets to another variable. .5 and upwards rounds towards positive infinity.<br>```$local1 = math_round($local2);``` |
| 604 | Rounds a literal value or variable to the next integer towards positive infinity and sets to another variable.<br>```$local1 = math_ceiling($local2);``` |
| 605 | Gets the smaller of two literal values or variables and sets to another variable.<br>```$local1 = math_min($local2, $local3);``` |
| 606 | Gets the greater of two literal values or variables and sets to another variable.<br>```$local1 = math_max($local2, $local3);``` |
| 607 | Clamps the first literal value or variable between a minimum and maximum literal value or variable and sets to another variable.<br>```$local1 = math_clamp($local2, $local3, $local4);``` |
| 608 | Normalizes an angle (in radians) to the range `[-π, π]` and sets to another variable.<br>```$local1 = math_normalize_angle($local2);``` |
| 610 | Gets the sin of a literal value or variable and sets to another variable.<br>```$local1 = math_sin($local2);``` |
| 611 | Gets the cos of a literal value or variable and sets to another variable.<br>```$local1 = math_cos($local2);``` |
| 612 | Gets the tan of a literal value or variable and sets to another variable.<br>```$local1 = math_tan($local2);``` |
| 620 | Gets the inverse sin of a literal value or variable and sets to another variable.<br>```$local1 = math_asin($local2);``` |
| 621 | Gets the inverse cos of a literal value or variable and sets to another variable.<br>```$local1 = math_acos($local2);``` |
| 622 | Gets the inverse tan of a literal value or variable and sets to another variable.<br>```$local1 = math_atan($local2);``` |
| 623 | Gets the arctangent of two values (y and x) considering the correct quadrant and sets to another variable.<br>```$local1 = math_atan2($localY, $localX);``` |
| 630 | Raises a base value to the power of an exponent and sets to another variable. Allows fractional bases and exponents.<br>```$local1 = math_powf($local2, $local3);``` |
| 631 | Gets `e` (Euler’s number) raised to the power of a value and sets to another variable.<br>```$local1 = math_exp($local2);``` |

#### Advanced operations
| Type | Description |
| - | - |
| 510 | Gets the amount of parameters into the function.<br>```$local1 = parameter_count();``` |
| 520 | Gets a random value from 0 to a maximum defined by a literal value or variable and sets it to another variable.<br> The results for each data type are listed below:<br><ul><li>Integer: Returns a random integer in the range 0 to input - 1.</li><li>Float: Returns a random float in the range 0.0 to input.</li><li>Empty: Returns a random float in the range 0.0 - 1.0.</li><li>Other: Clears the variable it returns to.</li></ul>```$local1 = random(5); // can return an int from 0-4```<br>```$local2 = random(5f); // can return a float from 0.0-5.0```<br>```$local2 = random(); // can return a float from 0.0-1.0```<br>```$local2 = random("hi"); // clears $local2``` | <!-- no <br> needed; due to the list block-->
| 521 | Gets the CRC32-B checksum of a literal value or variable and sets to another variable. Arrays return `0`. <br>```$local1 = crc32($local2);``` |
| 522 | Gets the CRC16-X25 checksum of a literal value or variable and sets to another variable. Arrays return `0`. <br>```$local1 = crc16($local2);``` |
| 523 | Remaps the value of a variable to another literal value or variable or sets a default.<br><pre>$local1 = switch $local2<br>{<br>    1 => 99<br>    2 => $object1<br>    3 => $local3<br>    _ => 0<br>}</pre> |
