# GSS1 syntax lowering

Implementation: `Gss1LowLevelCodeUnitConverter`.

GSS1 currently uses the **same lowering rules as XSEQ**. The converter is a copy of `XseqLowLevelCodeUnitConverter` until GSS1 samples require a split.

When those samples appear, this file should diverge; until then the XSEQ document is the source of truth: [xseq.md](xseq.md).

Related: [xq32.md](xq32.md) (later XQ32 compiler).

---

## Status

| Area | GSS1 vs XSEQ |
|---|---|
| Jump polarity (keep NOT + `IfNotGoto`) | Same |
| `IfGoto` dest 2 (empty `while`, leftover `if goto`) | Same |
| JOIN allocated after then-body | Same |
| Dest 2 after `if { return }` | Same |
| String `and`/`or` if-condition dest 2 | Same |
| Call arguments: positional `$temp{D+i+1}` | Same |
| Temp-dest binaries: positional holes | Same |
| Named-dest binaries: pack complex operands | Same |
| Dest 1 live after discarded call | Same |

The rest of this file repeats the XSEQ rules and examples so GSS1 has a complete list without opening xseq.md.

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

Do **not** fold `if not (not x)` into `IfGoto`. GSS1/XSEQ emits NOT, then `IfNotGoto`:

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

Low-level:

```
"@000@":
	$temp3 = sub5610($param0);
	$temp2 = not $temp3;
	if $temp2 goto "@000@";
```

### `while` with a body

Head, `IfNotGoto` exit, body, `goto` head, exit. `while (true)` → `if not 1 goto EXIT`.

### `do { } while (cond)`

Head, body, continue latch, `IfGoto` head (dest 2), exit.

### `for`

Initializer, head, `IfNotGoto` exit, body, optional continue latch (only if the body uses `continue`), iterator, `goto` head, exit.

### `break` / `continue`

`goto` the current loop’s exit or continue label.

Unstructured `if goto` uses dest 2. Unstructured `if not goto` uses dest 1.

### Logical string `and` / `or` as an if condition

`and`/`or` of two `==` / `!=` tests against a string literal uses **dest 2**.

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

After `$temp1 = call(...)`, dest 1 is reserved for the next named-dest packed expression: complex **left** may overwrite `$temp1`; leaf left + complex **right** starts at `$temp2`. A `$local = $local` copy keeps the reserve sticky.

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

### Calls: positional argument holes

Argument `i` of a call with result dest `D` uses `$temp{D+i+1}`.

High-level:

```
sub5622($param0, $param1, 0x00000008h | 0x00000004h);
```

Low-level:

```
	$temp4 = 0x00000008h | 0x00000004h;
	$temp1 = sub5622($param0, $param1, $temp4);
```

### Binaries / logicals with a temp result: positional holes

Left dest `max(dest+1, reservedEnd+1)`, right is the next slot.

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

### Binaries / logicals with a named dest: pack complex operands only

No hole for a leaf left-hand side, unless dest 1 is reserved after a discarded call.

### Unary `not` / casts

Child dest is `dest+1`.

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

Index `i` uses `$temp{dest+i+1}`. Complex array-store RHS spills first. Compound assigns always spill a value RHS.

### Chain assignment

Only `=`. Inner assign first, then copy through dest 1.

### Switch expression

Switched value uses `FirstOperandDest` (typically dest+1), then the switch writes dest.

### `yield` / `exit` / `return`

Pass through. `return <expr>` evaluates into `$temp1`.
