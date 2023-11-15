-- Stack

The stack is split in different regions, depending on the usage of the value.
Values 0 - 999 are currently unknown, but seem to be placed in the script execution environment and may be usable by any script at any time.
Value 1000 is a special value on the function stack, which may represent the return value of the function.
Value 1001 - 1999 are function local values, which may only be referenced by the current function.
	Additionally, they normally hold primitive values, instead of instanced objects or arrays.
Values 2000 - 2999 are function local objects, which may only be referenced by the current function.
	They contain primitive values, arrays, or instanced objects. They are slightly less performant, due to the instructions applied to them.
Values 3000 - 3999 are function parameters, which may only be referenced by the current function.
	They hold the objects passed into the function by invoking it. If you know they contain a primitive value,
	it is adviced to set them to a function local value before using them to increase performance.
Values 4000 - 4999 are script local objects, which may only be referenced by any function in the current script.
	They contain primitive values, arrays, or instanced objects. They are slightly less performant, due to the instructions applied to them.

-- Operations

- Returns
10, 11, 12

- Calls
20

- Jumps
30, 31, 33

- Basic assignments and operations
100, 110, 112, 150, 151, 152, 153, 154, 250, 251, 252, 253, 254

- Bit operations
160, 161, 162, 170, 171, 260, 261, 262, 270, 271

- Comparisons
130, 131, 132, 133, 134, 135

- Booleans
120, 121, 122

- Strings
500, 501, 503

- Arrays
530, 531, general assignment

- Type
40, 511, 512, 513

- Math
520, 521, 522, 600, 601, 602, 603, 604, 605, 606, 607, 610, 611, 612, 620, 621, 622

- Advanced operations
510, 523