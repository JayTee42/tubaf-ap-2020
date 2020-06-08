# The Kaleidoscope language and compiler

## Introduction

_Kaleidoscope_ is a simple toy language for teaching purposes. It illustrates the basics of the [LLVM compiler framework](https://llvm.org/) to separate compiling into a frontend phase (lexing, parsing) and a backend phase (optimizing, emitting machine code).

The C# application in this repo is an implementation of the [official LLVM tutorial](https://llvm.org/docs/tutorial/MyFirstLanguageFrontend/index.html) on this topic. It allows to output the intermediate results of compiling a Kaleidoscope program in six incremental steps:

 - Lexing
 - Parsing
 - [LLVM IR](http://llvm.org/docs/LangRef.html) (_intermediate representation_), a kind of architecture-independent assembly
 - Assembly for the target platform
 - Object code as a linkable module
 - A fully-fledged binary

In addition, LLVM's optimizer passes can be enabled to generate not only correct, but also efficient machine code. Another command line switch allows basic cross-compiling for foreign architectures.

## Short language tutorial for Kaleidoscope

Due to the limitations of this project, Kaleidoscope is a rather simple programming language with some unusual rules:

1. First, there is only one data type: _Everything is a double_. This drastic simplification allows us to omit the type specifiers of functions and parameters as we know them from more complex languages. It also reduces the type of a function to a single number - its parameter count.

2. There are boolean operator equivalents like `<`, `>=` and `==`, but they return a `double` (pretty similar to C returning an integer): `0.0` corresponds to `false` and everything else to `true`.

3. There are no statements. Everything, especially the body of functions, is an expression (which can still be deeply nested).

4. Because of the absence of statements, there is no concept of variables. Kaleidoscope expressions only consist of literals, function parameters and function calls that can be combined with operators.

Let's take a look at some examples. First, we define a function that returns a literal.
```
def one()
   1
```

`def` is one of the few Kaleidoscope keywords. It introduces a function definition that consists of a _prototype_ (name and parameters) and a _body expression_. All functions return a single double value, there is no `void` type (and no tuples or arrays).

The name of the function is an arbitrary identifier that starts with a letter or an underscore. Starting at the second position, there may also appear digits.

In the example above, we put the body expression of the function on a new line and indented it with some spaces. That might be considered good programming style, but is completely optional: Kaleidoscope ignores whitespaces between tokens, allowing us to write our function as `def one() 1` or even `def one()1` if we wanted to.

A slightly more complex example illustrates the usage of function parameters, binary operators and brackets:
```
# Calculate the average of two numbers.
def avg(a, b)
   0.5 * (a + b)
```

Kaleidoscope supports basic algebraic operators, i.e. addition (`+`), subtraction (`-`), multiplication (`*`) and division (`/`). All of them behave in accordance with IEEE 754. Operator precedence is the same as in C (`*`, `/` before `+`, `-` before comparison operators). Furthermore, the example illustrates the usage of comments, starting with `#`. All characters until the next line break will be ignored by the parser.

Kaleidoscope function calls are only allowed if the callee has been defined previously. For example, this call is valid:
```
# Calculate the average of two numbers.
def avg(a, b)
   0.5 * (a + b)

# Call the avg() function with two literals.
def run()
   avg(3.142, 2.718)
```

But inverting the order of functions would result in a compiler error. In some cases, a *forward declaration* might be a better solution to this (e. g. if two functions need to call each other). Kaleidoscope's forward declarations are carried out using the keyword `dec` (instead of `def` for definitions) and omitting the body expression of the function. By the way, the parameter names of declaration and definition don't have to match up (though it is a good idea to keep them consistent). Only the _number_ of parameters is relevant.

Let's rewrite the example above using a forward declaration:
```
# Calculate the average of two numbers.
dec avg(a, b)

# Call the avg() function with two literals.
def run()
   avg(3.142, 2.718)

# The actual definition of avg() is down here.
def avg(a, b)
   0.5 * (a + b)
```

Kaleidoscope even includes conditional expressions using the keywords `if`, `then` and `else`. As described above, the positive branch (behind `then`) is executed if the condition does evaluate to anything else except `0.0`. Because every expression needs to return a value, the `else` branch is _not_ optional.

A more complex example shows the usage of conditional expressions and also includes recursion:
```
# Compute Ackermann's function for two parameters a and b.
def ack(a, b)
   if a == 0 then
      b + 1
   else if b == 0 then
      ack(a - 1, 1)
   else
      ack(a - 1, ack(a, b - 1))
```

A last cool feature of Kaleidoscope is the fact that compatible functions from glibc and libm can be forward-declared and linked. Consider this example:
```
# Reference three functions from libm.
dec sin(x)
dec cos(x)
dec pow(base, exp)

# Demonstrate the Pythagorean trigonometric identity.
def trig(x)
   pow(sin(x), 2) + pow(cos(x), 2)

```

## Building the compiler

As a .NET core project, the Kaleidoscope compiler (simply named _kalc_) can be build with the following CI command, executed inside the project directory:
```
dotnet build
```

The [C# LLVM bindings](https://github.com/Microsoft/LLVMSharp) are pulled in as a [NuGet package](https://www.nuget.org/packages/LLVMSharp/5.0.0). Building has been tested on Linux (.NET 3.1.103) and macOS (.NET 3.1.201).

## Running the compiler

The easiest way to run the compiler is via
```
dotnet run -- <arguments>
```

The command line arguments are listed and explained below.

 - `-h` / `--help`: Print a short manual.
 - `-o` / `--output-path`: Define the directory where output files shall be written to. By default, they land in the same directory as the source file.
 - `-s` / `--stage`: Define up to which stage the compiler shall proceed and which kind of output shall be generated. The six stages are:
   - `lex`: Run the lexer on the source file and dump the resulting token stream into a `.lex` file.
   - `ast`: Run the parser on the output of the lexer and dump the resulting abstract syntax tree (AST) into a `.ast` file.
   - `ir`: Transform the AST to LLVM's object model, generate IR code from it and dump it into a `.ll` file.
   - `asm`: Translate the IR into the platform's native assembly syntax and dump it into a `.s` file.
   - `obj`: Assemble to native machine code and dump it into a `.o` file.
   - `exe`: Create a Makefile project including the object code file and a light C shim to provide an entry point for the operating system.
 - `-O` / `--optimize`: Run a bunch of LLVM's optimizer passes on the resulting IR to generate more efficient code.
 - `-t` / `--target`: By passing a [target triple](https://clang.llvm.org/docs/CrossCompilation.html#target-triple) with this option, `kalc` supports a basic cross-compilation mode. Use with care, this does not work well with the `exe` stage.

In addition to the described arguments, a non-optional input file path has to be specified. Its file name determines the name of the output (and is assigned to the generated module).

An example call:
```
dotnet run -- --stage asm --target "armv7a-none-eabi" --optimize --output-path "/home/jaytee/my_awesome_proj/" samples/fib.kal
```

This will output an optimized ARM assembly file at `/home/jaytee/my_awesome_proj/fib.s` if compilation succeeds.

## The `exe` stage

To generate a fully-fledged executable file, we have to define an entry point. The classical `int main(int argc, char** argv)` is obviously not compatible to Kaleidoscope because of the parameter types. To provide a comfortable way for actually _running_ the self-defined Kaleidoscope programs, we take the following steps on the `exe` stage:

 - First, ensure that the Kaleidoscope program contains a function named `run`. It may specify an arbitrary number of parameters.
 - Generate a small shim in C that contains a `main` function as entry point, forward-declares `run`, parses the command line parameters to double and passes them to Kaleidoscope.
 - Generate a Makefile that describes how to build the final binary by compiling the C shim and linking it against the Kaleidoscope module.

## Contributions

... are welcome :)
