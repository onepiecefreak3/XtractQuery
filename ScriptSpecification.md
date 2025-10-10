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
| 1 | ```0``` | Describes a signed integer. |
| 2 | ```0h``` | Describes an unsigned integer. The suffix 'h' was chosen, as this type is mostly used for checksum ('h'ash) values. |
| 2 | ```"Text"h``` | Describes a string, that is converted to a checksum and then processed and written like an unsigned integer. The suffix 'h' was chosen to infer a parity to unsigned integers and what they represent. |
| 3 | ```1.0f``` | Describes a floating point value. The suffix 'f' stands for 'float'. |
| 4 | ```$variable1``` | Describes a value on the stack. Refer to "Variable notation" for their specific notation. |
| 8 | ```"Text"``` | Describes a string, that is written as is. It will NOT be converted to a checksum. |

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

#### Jumps
| Type | Description |
| - | - |
| 30 | If the value equates to ``true``, jump to the label in the current function.<br>```if 1 goto "Label1"h;```<br>```if $variable0 goto "Label1"h;``` |
| 31 | Unconditionally jump to the label in the current function.<br>```goto "Label1"h;``` |
| 33 | If the value equates to ``false``, jump to the label in the current function.<br>```if 0 goto "Label1"h;```<br>```if $variable0 goto "Label1"h;``` |

#### Basic assignments and operations
| Type | Description |
| - | - |
| 100 | Sets a literal value or variable to another variable.<br>```$local1 = 0;```<br>```$local1 = $local2;``` |
| 110 | Sets the bit complement of a literal value or variable to another variable.<br>```$local1 = ~0;```<br>```$local1 = ~$local2;``` |
| 112 | Sets the negation of a literal value or variable to another variable.<br>```$local1 = -0;```<br>```$local1 = -$local2;``` |
| 140 | Adds 1 to a variable and sets to another variable.<br>```$local1 = $local2 + 1;``` |
| 141 | Subtracts 1 from a variable and sets to another variable.<br>```$local1 = $local2 - 1;``` |
| 150 | Adds a literal value or variable to another variable and sets to another variable.<br>```$local1 = $local2 + 30;```<br>```$local1 = $local2 + $object1;``` |
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
| 130 | Compares if two literal values or variables are equal and to another variable.<br>```$local1 = 1 == 2;```<br>```$local1 = $local2 == 2;```<br>```$local1 = $local3 == $local4;``` |
| 131 | Compares if two literal values or variables are not equal and sets the result to another variable.<br>```$local1 = 1 != 2;```<br>```$local1 = $local2 != 2;```<br>```$local1 = $local3 != $local4;``` |
| 132 | Compares if one literal value or variable is greater or equal to another literal value or variable and sets to another variable.<br>```$local1 = 1 >= 2;```<br>```$local1 = $local2 >= 2;```<br>```$local1 = $local3 >= $local4;``` |
| 133 | Compares if one literal value or variable is smaller or equal to another literal value or variable and sets to another variable.<br>```$local1 = 1 <= 2;```<br>```$local1 = $local2 <= 2;```<br>```$local1 = $local3 <= $local4;``` |
| 134 | Compares if one literal value or variable is greater than another literal value or variable and sets to another variable.<br>```$local1 = 1 > 2;```<br>```$local1 = $local2 > 2;```<br>```$local1 = $local3 > $local4;``` |
| 135 | Compares if one literal value or variable is smaller than another literal value or variable and sets to another variable.<br>```$local1 = 1 < 2;```<br>```$local1 = $local2 < 2;```<br>```$local1 = $local3 < $local4;``` |

#### Booleans
| Type | Description |
| - | - |
| 120 | Converts a literal value or variable to boolean and negate it.<br>```$local1 = not $local2;``` |
| 121 | Converts two literal values or variables to boolean and boolean and them.<br>```$local1 = true && true;```<br>```$local1 = $local2 && true;```<br>```$local1 = $local2 && $local3;``` |
| 122 | Converts two literal values or variables to boolean and boolean or them.<br>```$local1 = true \|\| true;```<br>```$local1 = $local2 \|\| true;```<br>```$local1 = $local2 \|\| $local3;``` |

#### Strings
| Type | Description |
| - | - |
| 500 | Was originally used to log a message in developement. Is a no-op in published games.<br>```$local1 = log("This is a message.");``` |
| 501 | Formats a string with placeholder values.<br>```$local1 = format("Formatted message %s", $local2);``` |
| 503 | Get a substring from another string. Crashes on negative numbers. <br>```$local1 = substring("This message", 5);``` |

#### Arrays
| Type | Description |
| - | - |
| 530 | Creates a new multi-dimensional array.<br>```$local1 = new[2];```<br>```$local1 = new[2][1];``` |
| 531 | Gets a reference to the indexed element in an array.<br>```$local1 = $local2[0];```<br>```$local1 = $local2[2];``` |

The array index notation can be used in all shorthand assignments of type 240 - 271. They can not be directly used in operations of type 110 - 171. You need to use operation 531 to get an array element and set it to another variable to use them in those operations.

#### Type
| Type | Description |
| - | - |
| 40 | Gets the base type of a variable.<br>```$local1 = typeof($local2);``` |
| 511 | Casts a literal value or variable to int. Truncates floats and returns `0` for strings. <br>```$local1 = (int)$local2;``` |
| 512 | Casts a literal value or variable to bool. Non-Zero ints and floats become `true`, and arrays also become `true` and strings are always coerced to `false`. <br>```$local1 = (bool)$local2;``` |
| 513 | Casts a literal value or variable to float. Ints and floats coerce as you'd expect while other types become `0.0f`. <br>```$local1 = (float)$local2;``` |

#### Math
| Type | Description |
| - | - |
| 600 | Gets the absolute representation of a literal value or variable and sets to another variable.<br>```$local1 = math_abs($local2);``` |
| 601 | Gets the square root of a literal value or variable and sets to another variable.<br>```$local1 = math_sqrt($local2);``` |
| 602 | Rounds a literal value or variable to the next integer towards negative infinity and sets to another variable.<br>```$local1 = math_floor($local2);``` |
| 603 | Rounds a literal value or variable to the nearest integer and sets to another variable. .5 and upwards rounds towards positive infinity.<br>```$local1 = math_round($local2);``` |
| 604 | Rounds a literal value or variable to the next integer towards positive infinity and sets to another variable.<br>```$local1 = math_ceiling($local2);``` |
| 605 | Gets the smaller of two literal values or variables and sets to another variable.<br>```$local1 = math_min($local2, $local3);``` |
| 606 | Gets the greater of two literal values or variables and sets to another variable.<br>```$local1 = math_max($local2, $local3);``` |
| 607 | Clamps the first literal value or variable between a minimum and maximum literal value or variable and sets to another variable.<br>```$local1 = math_clamp($local2, $local3, $local4);``` |
| 610 | Gets the sin of a literal value or variable and sets to another variable.<br>```$local1 = math_sin($local2);``` |
| 611 | Gets the cos of a literal value or variable and sets to another variable.<br>```$local1 = math_cos($local2);``` |
| 612 | Gets the tan of a literal value or variable and sets to another variable.<br>```$local1 = math_tan($local2);``` |
| 620 | Gets the inverse tan of a literal value or variable and sets to another variable.<br>```$local1 = math_asin($local2);``` |
| 621 | Gets the inverse tan of a literal value or variable and sets to another variable.<br>```$local1 = math_acos($local2);``` |
| 622 | Gets the inverse tan of a literal value or variable and sets to another variable.<br>```$local1 = math_atan($local2);``` |

#### Advanced operations
| Type | Description |
| - | - |
| 510 | Gets the amount of parameters into the function.<br>```$local1 = parameter_count();``` |
| 520 | Gets a random value from 0 to a maximum defined by a literal value or variable and sets to another variable.<br>```$local1 = random(5);```<br>```$local1 = random($local2);``` |
| 521 | Gets the CRC32 from a literal value or variable and sets to another variable.<br>```$local1 = crc32($local2);``` |
| 522 | Gets the CRC16 from a literal value or variable and sets to another variable.<br>```$local1 = crc16($local2);``` |
| 523 | Remaps the value of a variable to another literal value or variable or sets a default.<br><pre>$local1 = switch $local2<br>{<br>    1 => 99<br>    2 => $object1<br>    3 => $local3<br>    _ => 0<br>}</pre> |
