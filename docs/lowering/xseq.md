# XSEQ syntax lowering

Implementation: `XseqLowLevelCodeUnitConverter`.

XSEQ is the older Level5 compiler (before the switch to XQ32). Round-trip gold: `uniform` (XSEQ binary).

Related: [xq32.md](xq32.md) (later compiler), [gss1.md](gss1.md) (currently a copy of these rules).

---

## Control flow

Numeric jump labels are `@000@`, `@001@`, … (at least three digits). Existing labels in the source are never reused.

`true` in a condition becomes the integer `1`.

### If-then

Skip the body when the condition is false (`IfNotGoto`). Empty `else { }` is emitted as if-then.

High-level:

```
if $global39 {
    View3DChara<25>($global37);
}
```

Low-level:

```
	if not $global39 goto "@000@";
	$temp1 = View3DChara<25>($global37);
"@000@":
```

### If-else and `else if`

`IfNotGoto` ELSE, then-body, `goto` JOIN, ELSE, else-body, JOIN.

The **JOIN label is allocated after the then-body**, so nested loops/ifs consume numbers first (`if { while } else` → while `@002@`, join `@003@`).

High-level:

```
if $param0 == "on" {
    ...
    while (not sub5610($global18));
} else {
    sub5622($global18, $local2);
}
```

Low-level (spin before join):

```
	$temp1 = $param0 == "on";
	if not $temp1 goto "@001@";
	...
"@002@":
	$temp3 = sub5610($global18);
	$temp2 = not $temp3;
	if $temp2 goto "@002@";
	goto "@003@";
"@001@":
	$temp1 = sub5622($global18, $local2);
"@003@":
```

### If with `return` / `goto` in the then arm

Dest 1 stays live at the join; the **next if uses dest 2**.

High-level:

```
if not sub2010(1, $global36, $local1, $local2, $local3, $local4) {
    ...
    return 0;
}
if $global40 == $global36 {
    ...
    return 0;
}
```

Low-level:

```
	$temp2 = sub2010(1, $global36, $local1, $local2, $local3, $local4);
	$temp1 = not $temp2;
	if not $temp1 goto "@002@";
	...
	return 0;
"@002@":
	$temp2 = $global40 == $global36;
	if not $temp2 goto "@003@";
```

### Jump polarity (keep a NOT instruction)

Do **not** fold `if not (not x)` into `IfGoto`. XSEQ emits NOT, then `IfNotGoto`:

High-level:

```
if not $param0 {
    ...
}
```

Low-level:

```
	$temp1 = not $param0;
	if not $temp1 goto "@000@";
```

High-level:

```
if not sub2010(6) {
    sub2010(7);
}
```

Low-level:

```
	$temp2 = sub2010(6);
	$temp1 = not $temp2;
	if not $temp1 goto "@000@";
	$temp1 = sub2010(7);
"@000@":
```

`IfNotGoto` of a positive condition is still `if not $temp1 goto` without a preceding NOT when the condition is already a value in dest 1 (plain `if x {`).

### Empty / one-line `while`

Spin: label, then `IfGoto` of the condition on **dest 2**.

High-level:

```
while (sub2209($param0));
```

Low-level:

```
"@000@":
	$temp2 = sub2209($param0);
	if $temp2 goto "@000@";
```

High-level:

```
while (not sub5610($param0));
```

Low-level (NOT + `IfGoto` dest 2, not a folded `IfNotGoto`):

```
"@000@":
	$temp3 = sub5610($param0);
	$temp2 = not $temp3;
	if $temp2 goto "@000@";
```

### `while` with a body

Head, `IfNotGoto` exit (dest 1 unless string-`and`/`or` bumps dest — see below), body, `goto` head, exit.

`while (true)` → `if not 1 goto EXIT`.

### `do { } while (cond)`

Head, body, continue latch, `IfGoto` head (dest 2), exit.

### `for`

Same shape as XQ32: initializer, head, `IfNotGoto` exit, body, optional continue latch (only if the body uses `continue`), iterator, `goto` head, exit.

### `break` / `continue`

`goto` the current loop’s exit or continue label.

Unstructured `if goto` in the source uses **dest 2**. Unstructured `if not goto` uses dest 1.

### Logical string `and` / `or` as an if condition

If the condition is `and`/`or` of two `==` / `!=` tests against a string literal, the if uses **dest 2** (comparisons land in `$temp3`/`$temp4`, combine in `$temp2`).

High-level:

```
if $global18 != "" and $global19 != "" {
```

Low-level:

```
	$temp3 = $global18 != "";
	$temp4 = $global19 != "";
	$temp2 = $temp3 and $temp4;
	if not $temp2 goto "@000@";
```

---

## `$temp` allocation

Slots still named `$tempN` in the high-level source are skipped (`TempSlotFrame`).

### Result dests

| Construct | Dest |
|---|---|
| Discarded call / return / `IfNotGoto` | `$temp1` |
| `IfGoto` (including empty-while spin) | `$temp2` |
| Named assign | dest 0 (children from `$temp1`, subject to packed reserve) |
| Postfix `++` / `--` statement | dest 0 |

### Discarded call keeps dest 1 live (`NamedDestReserve`)

After `$temp1 = call(...)`, dest 1 is reserved for the next **named-dest** packed expression:

- Complex operand on the **left** may overwrite `$temp1`.
- Leaf left + complex **right** starts at `$temp2`.
- A following `$local = $local` copy keeps the reserve sticky until a packed expression consumes it.

High-level (right complex after a call):

```
sub2010(...);
$local0 = $local0 - (3 - 1);
```

Low-level:

```
	$temp1 = sub2010(...);
	$temp2 = 3 - 1;
	$local0 = $local0 - $temp2;
```

High-level (left complex after a call — dest 1 reused):

```
sub2010(...);
$local0 = ($global36 - $global38) % 5;
```

Low-level:

```
	$temp1 = sub2010(...);
	$temp1 = $global36 - $global38;
	$local0 = $temp1 % 5;
```

### Calls: positional argument holes

Argument `i` of a call with result dest `D` uses `$temp{D+i+1}`, including holes for leaf arguments.

High-level:

```
sub5622($param0, $param1, 0x00000008h | 0x00000004h);
```

Low-level (`|` is argument index 2 → dest 4):

```
	$temp4 = 0x00000008h | 0x00000004h;
	$temp1 = sub5622($param0, $param1, $temp4);
```

High-level:

```
sub2010(411, $param3, 0, $global13 - $local9, ...);
```

Low-level (that argument is slot 5):

```
	$temp5 = $global13 - $local9;
	$temp1 = sub2010(411, $param3, 0, $temp5, ...);
```

### Binaries / logicals with a temp result: positional holes

Left operand dest `max(dest+1, reservedEnd+1)`, right is the next slot. Leaves still occupy the hole even if they do not spill.

High-level:

```
if $param0 == 5 or $param0 == 15 {
```

Low-level:

```
	$temp2 = $param0 == 5;
	$temp3 = $param0 == 15;
	$temp1 = $temp2 or $temp3;
	if not $temp1 goto ...
```

High-level:

```
if $global42 != $local4 or $global45 {
```

Low-level:

```
	$temp2 = $global42 != $local4;
	$temp1 = $temp2 or $global45;
	if not $temp1 goto ...
```

### Binaries / logicals with a named dest: pack complex operands only

No hole for a leaf left-hand side (`$local7 = $local7 + sub13659($local2)` → `$temp1 = sub13659(...); $local7 = $local7 + $temp1`), unless dest 1 is reserved after a discarded call (see above).

### Unary `not` / casts

Child dest is `dest+1` (operand stack), not the result dest.

High-level:

```
if not sub2010(305, "ハイポリ") and not sub2010(305, "ハイポリ2") {
```

Low-level:

```
	$temp4 = sub2010(305, "ハイポリ");
	$temp2 = not $temp4;
	$temp4 = sub2010(305, "ハイポリ2");
	$temp3 = not $temp4;
	$temp1 = $temp2 and $temp3;
	if not $temp1 goto "@000@";
```

### Array indexes

Index `i` uses `$temp{dest+i+1}` (positional). Array stores with a complex RHS spill to a temp first. Compound assigns always spill a value RHS.

### Chain assignment

Only `=`. Inner assign first, then copy through dest 1 (same as XQ32).

### Switch expression

Switched value uses `FirstOperandDest` (typically dest+1), then the switch writes dest.

### `yield` / `exit` / `return`

Pass through. `return <expr>` evaluates into `$temp1`.
