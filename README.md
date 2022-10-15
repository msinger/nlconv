Netlist converter
=================

CSharp application for converting a custom netlist format to HTML files or PNG images. It was made for documenting
all the cells and traces on the Game Boy chip DMG-CPU B.

Read [INSTALL](INSTALL) for build/install instructions.


Usage
-----

Generate HTML file containing all types, cells and wires:
```
nlconv --html <input.nl >output.html
```

Generate PNG image containing all cells:
```
nlconv --png-wires <input.nl >output.png
```

Generate PNG image containing all wires:
```
nlconv --png-wires <input.nl >output.png
```

Generate PNG image containing everything:
```
nlconv --png <input.nl >output.png
```


TODO
----

Not yet implemented, but planned:
* Generate PNG image containing all labels.
* Generate Java Script code containing the coordinates for cells and wires to be used by the
  [map](https://github.com/msinger/dmg_cpu_b_map).
* Generate Verilog code containing all cells and wires.


Input format
------------

All keywords are case-insensitive. User defined names are case-sensitive.
The netlist that gets fed into STDIN of this application can contain type, cell and wire definitions in any order.

### Type definition

The `TYPE` keyword is used to define cell types. Those cell types can then be used for defining instances of cells using
the `CELL` keyword.

```
TYPE <type-name>[:<color>] <port-name>[:<port-dir>]
                           [<port-name>[:<port-dir>]...]
     [@<cell-coordinates>]
     [<port-name>@<port-coordinates>...]
     ["<description-string>" [DOC "<documentation-url>"]];
```

<dl>
  <dt>&lt;type-name&gt;</dt>
  <dd>Unique name of the cell type, like <code>nand2</code> for example.</dd>

  <dt>&lt;color&gt;</dt>
  <dd>
    Color that will be used when drawing cell instances of this type.
    Can be one of: <code>BLACK</code>, <code>BLUE</code>, <code>CYAN</code>, <code>GREEN</code>, <code>LIME</code>,
    <code>MAGENTA</code>, <code>ORANGE</code>, <code>PURPLE</code>, <code>RED</code>, <code>TURQUOISE</code> or
    <code>YELLOW</code>. Defaults to <code>BLACK</code> if not given.
  </dd>

  <dt>&lt;port-name&gt;</dt>
  <dd>
    Name of a port that cells of this type have. Can occur multiple times. When followed by a space or
    colon, it defines the port itself. When followed by a <code>@</code>, it specifies the coordinates of that port.
  </dd>

  <dt>&lt;port-dir&gt;</dt>
  <dd>
    Port direction. Can be one of: <code>IN</code>, <code>OUT</code>, <code>TRI</code>, <code>INOUT</code>,
    <code>OUT0</code>, <code>OUT1</code> or <code>NC</code>. Defaults to <code>IN</code>.
  </dd>

  <dt>&lt;cell-coordinates&gt;</dt>
  <dd>
    Two two-dimensional vectors, describing the bounding box of a cell of this type.
    This is used to convert the absolute <b>&lt;port-coordinates&gt;</b> to coordinates relative to the
    center of this bounding box, so they can be used as default coordinates for all cells of this type that
    do not have them explicitly specified.
  </dd>

  <dt>&lt;port-coordinates&gt;</dt>
  <dd>
    One or two two-dimensional vectors, describing either a point or line, where this port is located. Those port
    coordinates will be used for all cells of this type that do not have port coordinates explicitely specified
    alongside their own <code>CELL</code> definition.
  </dd>

  <dt>&lt;description-string&gt;</dt>
  <dd>A string in double quotes that describes the function of cells of this type.</dd>

  <dt>&lt;documentation-url&gt;</dt>
  <dd>An URL in double quotes that contains more detailed information about cells of this type.</dd>
</dl>


### Cell definition

The `CELL` keyword is used to define cell instances.

```
CELL <cell-name>:<type-name> [<orientation>[,FLIP]]
     [@<cell-coordinates>] [<port-name>@<port-coordinates>...]
     [<flags>...] ["<description-string>"];
```

<dl>
  <dt>&lt;cell-name&gt;</dt>
  <dd>Unique name of the cell instance.</dd>

  <dt>&lt;type-name&gt;</dt>
  <dd>Cell type being instanciated.</dd>

  <dt>&lt;orientation&gt;</dt>
  <dd>
    Defines the clock-wise rotation of the cell. Can be one of: <code>ROT0</code>, <code>ROT90</code>,
    <code>ROT180</code> or <code>ROT270</code>. If not given, then orientation will be undefined and the
    cell may not be drawn in images. Can be appended with <code>,FLIP</code>, which means that the cell is
    flipped horizontally (reflected along the central vertical axis) before the rotation is applied.
  </dd>

  <dt>&lt;cell-coordinates&gt;</dt>
  <dd>Two two-dimensional vectors, describing the bounding box of the cell instance.</dd>

  <dt>&lt;port-coordinates&gt;</dt>
  <dd>One or two two-dimensional vectors, describing either a point or line, where this port is located.</dd>

  <dt>&lt;flags&gt;</dt>
  <dd>
    Any combination of the following flags may be applied to the cell:
    <ul>
      <li><code>SPARE</code>:   This is a spare cell that has no relevant function.</li>
      <li><code>VIRTUAL</code>: This cell does not actually exist in the circuit. May just be used to please the
                                electrical rules checker.</li>
      <li><code>COMP</code>:    This cell may not be drawn in the schematics, because it just provides a complement
                                clock for some latches or flip-flops.</li>
      <li><code>TRIVIAL</code>: This cell is not drawn in the schematics, because it is basically just wiring.</li>
    </ul>
  </dd>

  <dt>&lt;description-string&gt;</dt>
  <dd>A string in double quotes that describes the function of this cell instance within the circuit.</dd>
</dl>

Cells can have alias names. This is useful for example when a cell got renamed at some point. Then it can still
have an alias with its old name, so it can still be found in the HTML document when someone searches for it by
that old name. Aliases are specified with separate statements that must come somewhere after the cell definition:

```
ALIAS CELL <alias>... -> <cell-name>;
```

<dl>
  <dt>&lt;alias&gt;</dt>
  <dd>One or more alias names for the cell. Each of them must be unique within the cell namespace.</dd>

  <dt>&lt;cell-name&gt;</dt>
  <dd>Name of the cell instance.</dd>
</dl>

### Wire definition

The `WIRE` keyword is used to define connections between cell ports.

```
WIRE <wire-name>[:<wire-class>] <source-ports>... [-> <drain-ports>...]
     [@<wire-coordinates>...] ["<description-string>"];
```

<dl>
  <dt>&lt;wire-name&gt;</dt>
  <dd>Unique name of the wire.</dd>

  <dt>&lt;wire-class&gt;</dt>
  <dd>
    Optional wire class, describing the type of signal. Can be one of: <code>GND</code>, <code>PWR</code>,
    <code>DEC</code>, <code>CTL</code>, <code>CLK</code>, <code>DATA</code>, <code>ADR</code>, <code>RST</code>
    or <code>ANALOG</code>.
  </dd>

  <dt>&lt;source-ports&gt;</dt>
  <dd>
    One or more source ports in the form <code>&lt;cell&gt;.&lt;port&gt;</code> that are driving this wire. Only ports
    with the following directions are allowed as source ports: <code>OUT</code>, <code>TRI</code>, <code>INOUT</code>,
    <code>OUT0</code> or <code>OUT1</code>. If direction is <code>OUT</code>, then the wire must have only one source port.
    if one port has the direction <code>OUT0</code> or <code>OUT1</code>, then all source ports must have the same
    direction. Only <code>TRI</code> and <code>INOUT</code> ports can be intermixed.
  </dd>

  <dt>&lt;drain-ports&gt;</dt>
  <dd>
    Zero or more input ports in the form <code>&lt;cell&gt;.&lt;port&gt;</code> that are receiving the signal from
    this wire. Only ports with direction <code>IN</code> are allowed as drain ports. <code>INOUT</code> ports are
    technically inputs too, but they have to be listed under source ports, because they can also drive the wire.
  </dd>

  <dt>&lt;wire-coordinates&gt;</dt>
  <dd>List of two-dimensional vectors, describing a continuous strip of lines fitting the shape of the wire.</dd>

  <dt>&lt;description-string&gt;</dt>
  <dd>A string in double quotes that describes the signal carried by this wire within the circuit.</dd>
</dl>