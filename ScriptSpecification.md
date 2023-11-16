# XQ32/XSEQ Script Specification

## Introduction

This document describes the human-readable output from XtractQuery and its syntax.
It also describes its relation to the instruction types read from the format, as per the format specification.

## Functions
TODO

## Jumps
TODO

## Comments
TODO

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

Variables have a fixed notation to declare their placement on the stack. (see "Variables" in the format specification)
All variables start with the prefix '$', followed by a fixed term and number from 0 to 999 for placement on the stack.

| Notation | Description |
| - | - |
| ```$unk0``` | A value, that persists through multiple scripts. |
| ```$local0``` | A value, that persists only in the function it was set in. |
| ```$object0``` | A value, that persists only in the function it was set in. |
| ```$param0``` | A value, that exclusively holds input parameters to the function. |
| ```$global0``` | A value, that persists through multiple functions only in the script it was set in. |

## Instructions

#### Returns
| Type | Description |
| - | - |
| 10 | Returns from a function at that moment of execution. Can set the return value of the function.<br>```return;```<br>```return value;``` |
| 11 | Yields back into the script engine to execute logic outside the script and continues the method at the next instruction.<br>```yield;``` |
| 12 | Exits the script, that is currently executed, completely.<br>```exit;``` |

#### Calls
| Type | Description |
| - | - |
| 20 | Calls any function from any currently loaded script in the engine, either by name or by its CRC32/CRC16 checksum.<br>```$local1 = call("MyOtherFunction"h, arg1, arg2, ...);```<br>```$local1 = call("MyOtherFunction", arg1, arg2, ...);```<br>```$local1 = MyOtherFunction(arg1, arg2, ...);``` |

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
TODO: 130, 131, 132, 133, 134, 135

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
| 503 | Get a substring from another string.<br>```$local1 = substring("This message", 5);``` |

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
| 511 | Casts a literal value or variable to int.<br>```$local1 = (int)$local2;``` |
| 512 | Casts a literal value or variable to bool.<br>```$local1 = (bool)$local2;``` |
| 513 | Casts a literal value or variable to float.<br>```$local1 = (float)$local2;``` |

#### Math
TODO: 520, 521, 522, 600, 601, 602, 603, 604, 605, 606, 607, 610, 611, 612, 620, 621, 622

#### Advanced operations
| Type | Description |
| - | - |
| 510 | Gets the amount of parameters into the function.<br>```$local1 = parameter_count();``` |
| 523 | Remaps the value of a variable to another literal value or variable or sets a default.<br><pre>$local1 = switch $local2<br>{<br>    1 => 99<br>    2 => $object1<br>    3 => $local3<br>    _ => 0<br>}</pre> |
