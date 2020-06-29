using System;
using System.IO;
using System.Linq;
using LLVMSharp;
using Kaleidoscope.Ast;

namespace Kaleidoscope
{
    // Up to which state do we compile?
    enum Stage
    {
        Lexer,
        Parser,
        IntermediateRepresentation,
        Assembly,
        ObjectCode,
        MakefileProject,
    }

	class Compiler
	{
        static bool ParseArgs(string[] args, out string inputFile, out string outputPath, out Stage stage, out bool useOptimizations, out string targetTriple)
        {
            // Initially, assume default args.
            inputFile = null;
            outputPath = null;
            stage = Stage.ObjectCode;
            useOptimizations = false;
            targetTriple = null;

            // Look at the provided arguments.
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-h":
                    case "--help":
                    {
                        Console.WriteLine("Usage: kalc [--output-path <output path>] [--stage lex | ast | ir | asm | obj | exe] [--optimize] [--target <target triple>] <input path>");
                        return false;
                    }

                    case "-o":
                    case "--output-path":
                    {
                        if (i == (args.Length - 1))
                        {
                            Console.WriteLine("Missing output path argument!");
                            return false;
                        }

                        outputPath = args[++i];
                        break;
                    }

                    case "-s":
                    case "--stage":
                    {
                        if (i == (args.Length - 1))
                        {
                            Console.WriteLine("Missing stage argument!");
                            Console.WriteLine("Valid stages are: lex | ast | ir | asm | obj | exe");

                            return false;
                        }

                        switch (args[++i])
                        {
                            case "lex": stage = Stage.Lexer; break;
                            case "ast": stage = Stage.Parser; break;
                            case "ir": stage = Stage.IntermediateRepresentation; break;
                            case "asm": stage = Stage.Assembly; break;
                            case "obj": stage = Stage.ObjectCode; break;
                            case "exe": stage = Stage.MakefileProject; break;

                            default:
                            {
                                Console.WriteLine("Invalid stage argument!");
                                Console.WriteLine("Valid stages are: lex | ast | ir | asm | obj | exe");

                                return false;
                            }
                        }

                        break;
                    }

                    case "-O":
                    case "--optimize":
                    {
                        useOptimizations = true;
                        break;
                    }

                    case "-t":
                    case "--target":
                    {
                        if (i == (args.Length - 1))
                        {
                            Console.WriteLine("Missing target argument!");
                            return false;
                        }

                        targetTriple = args[++i];
                        break;
                    }

                    default:
                    {
                        if (inputFile == null)
                        {
                            inputFile = args[i];
                        }
                        else
                        {
                            Console.WriteLine($"Invalid argument: { args[i] }");
                            return false;
                        }

                        break;
                    }
                }
            }

            if (inputFile == null)
            {
                Console.WriteLine("Missing input path!");
                return false;
            }

            return true;
        }

        private static void LexToOutputFile(Lexer lexer, string fileName, string outputPath)
        {
            // Create a stream writer around the output path.
            using (var streamWriter = new StreamWriter(Path.Combine(outputPath, fileName + ".lex")))
            {
                // Read tokens from the lexer until we hit the end of the file.
                Token token;

                do
                {
                    token = lexer.Lex();
                    streamWriter.WriteLine(token);
                } while (token.Type != TokenType.EndOfFile);
            }
        }

        private static void ParseToOutputFile(Parser parser, string fileName, string outputPath)
        {
            // Create a stream writer around the output path.
            using (var streamWriter = new StreamWriter(Path.Combine(outputPath, fileName + ".ast")))
            {
                // Read toplevel elements from the lexer until we hit the end of the file.
                TopLevelElement element;

                while ((element = parser.Parse()) != null)
                {
                    element.Print(streamWriter);
                    streamWriter.WriteLine();
                }
            }
        }

        private static void CreateMakefileProject(Module module, string fileName, string outputPath)
        {
            // Dump the module as object file to the given path.
            module.PrintObjectCodeToFile(Path.Combine(outputPath, fileName + ".o"));

            // Get the number of parameters of the run function.
            var runFunc = LLVM.GetNamedFunction(module.Mod, "run");

            if (runFunc.Pointer == IntPtr.Zero)
            {
                throw new FormatException("An executable needs a run function.");
            }

            var parameterCount = (int)LLVM.CountParams(runFunc);

            // Create a C shim file that invokes the run function.
            var parameterNames = string.Join(",", Enumerable.Range(0, parameterCount).Select(n => $"double p{ n }"));
            var parameters = string.Join(",", Enumerable.Range(0, parameterCount).Select(n => $"p[{ n }]"));
            var shim = $@"#include <stdio.h>" + "\n"
                + $@"#include <stdlib.h>" + "\n"
                + $@"#include <errno.h>" + "\n"
                + $@"double run({ parameterNames });" + "\n"
                + $@"double f(const char*a){{errno=0;char*e;double p=strtod(a,&e);if(errno||(a==e)){{printf(""Invalid argument: \""%s\""\n"",a);exit(EXIT_FAILURE);}}return p;}}" + "\n"
                + $@"int main(int argc,char**argv){{if(argc!={ parameterCount + 1 }){{printf(""Invalid number of arguments ({ parameterCount } expected).\n"");exit(EXIT_FAILURE);}}" + "\n"
                + $@"double p[{ parameterCount + 1 }];for(int i=0;i<{ parameterCount };i++)p[i]=f(argv[1+i]);printf(""Result: %lf\n"",run({ parameters }));}}";

            // Write the shim to a C file.
            var shimName = (fileName == "shim" ? "shim2" : "shim");
            File.WriteAllText(Path.Combine(outputPath, shimName + ".c"), shim);

            //Write a Makefile.
            var makefile = $@"{ fileName }: { fileName }.o { shimName }.o" + "\n"
                + "\t" + $@"gcc -o { fileName } { fileName }.o { shimName }.o -lm" + "\n\n"
                + $@"{ shimName }.o: { shimName }.c" + "\n"
                + "\t" + $@"gcc -O3 -c -o { shimName }.o { shimName }.c" + "\n";

            File.WriteAllText(Path.Combine(outputPath, "Makefile"), makefile);
        }

		static void Main(string[] args)
		{
            try
            {
                // Parse the arguments.
                if (!Compiler.ParseArgs(args, out var inputFile, out var outputPath, out var stage, out var useOptimizations, out var targetTriple))
                {
                    return;
                }

                // Get the file name from the input file path.
                var fileName = Path.GetFileNameWithoutExtension(inputFile);

                // If there is no output path, we use the containing directory of the input file.
                if (outputPath == null)
                {
                    outputPath = Path.GetDirectoryName(inputFile);
                }

                // Create a stream reader around the input file.
                using (var inputReader = new StreamReader(inputFile))
                {
                    // Wrap it into a lexer.
                    var lexer = new Lexer(inputReader);

                    // If we only want the lexing stage, lex into the output file.
                    if (stage == Stage.Lexer)
                    {
                        Compiler.LexToOutputFile(lexer, fileName, outputPath);
                        return;
                    }

                    // Wrap the lexer into a parser.
                    var parser = new Parser(lexer);

                    // If we only want the parser stage, parse into the output file.
                    if (stage == Stage.Parser)
                    {
                        Compiler.ParseToOutputFile(parser, fileName, outputPath);
                        return;
                    }

                    // Create a new module.
                    using (var module = new Module(fileName, targetTriple, useOptimizations))
                    {
                        // Parse everything into the module.
                        parser.EmitAll(module);

                        // What do we want to output?
                        switch (stage)
                        {
                            case Stage.IntermediateRepresentation: module.PrintIRToFile(Path.Combine(outputPath, fileName + ".ll")); break;
                            case Stage.Assembly: module.PrintAssemblyToFile(Path.Combine(outputPath, fileName + ".s")); break;
                            case Stage.ObjectCode: module.PrintObjectCodeToFile(Path.Combine(outputPath, fileName + ".o")); break;
                            case Stage.MakefileProject: Compiler.CreateMakefileProject(module, fileName, outputPath); break;

                            default: throw new InvalidOperationException("Invalid stage.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: { ex.Message }");
            }
        }
	}
}
