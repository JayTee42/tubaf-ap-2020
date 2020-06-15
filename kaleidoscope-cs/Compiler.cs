using System;
using System.IO;

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

                    // TODO
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: { ex.Message }");
            }
        }
	}
}
