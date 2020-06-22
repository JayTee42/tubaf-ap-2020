using System;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp;

namespace Kaleidoscope.Ast
{
	internal class CodeGen: Visitor
	{
		// The module to generate code into
		private Module _module;

		// A dictionary to map identifiers to LLVM values for the current scope:
		private Dictionary<string, LLVMValueRef> _namedValues = new Dictionary<string, LLVMValueRef>();

		// The resulting LLVM value of the last code generation
		private LLVMValueRef? _result = null;

		// A helper flag to tell declaration / definition prototypes apart
		private bool _prototypeIsDefinition;

		internal LLVMValueRef Result
		{
			get
			{
				if (this._result is LLVMValueRef result)
				{
					return result;
				}

				throw new InvalidOperationException("No result has been generated.");
			}

			private set => this._result = value;
		}

		// Create a new code generator with the given module to gen into.
		internal CodeGen(Module module)
		{
			this._module = module;
		}

		public void VisitLiteralExpression(LiteralExpression expr)
		{
			// Create a literal value.
			this.Result = LLVM.ConstReal(LLVM.DoubleType(), expr.Value);
		}

		public void VisitParameterExpression(ParameterExpression expr)
		{
			// Perform a lookup for the parameter value.
			if (!this._namedValues.TryGetValue(expr.Name, out var val))
			{
				throw new FormatException($"Invalid identifier: '{ expr.Name }'");
			}

			this.Result = val;
		}

		public void VisitBinaryOperatorExpression(BinaryOperatorExpression expr)
		{
			// TODO
		}

		public void VisitCallExpression(CallExpression expr)
		{
			// TODO
		}

		public void VisitConditionalExpression(ConditionalExpression expr)
		{
			// TODO
		}

		public void VisitFunctionPrototype(FunctionPrototype prototype)
		{
			// Try to fetch the existing function declaration.
			var func = LLVM.GetNamedFunction(this._module.Mod, prototype.Name);

			if (func.Pointer != IntPtr.Zero)
			{
				// If this is a definition, the function must not have been defined before.
				// In opposition, we allow an arbitrary number of compatible declarations
				// before and after the definition.
				if (this._prototypeIsDefinition && (LLVM.CountBasicBlocks(func) > 0))
				{
					throw new FormatException($"Redefinition of function '{ prototype.Name }' is not allowed.");
				}

				// Okay, the function has been declared / defined before.
				// Validate the number of parameters.
				if (LLVM.CountParams(func) != prototype.ParameterNames.Count)
				{
					throw new FormatException($"Parameter count of function '{ prototype.Name }' varies across declarations / definition.");
				}
			}
			else
			{
				// The prototype does not exist yet, we have create it.
				// The "false" parameter indicates that the function is not variadic.
				var parameterTypes = Enumerable.Repeat(LLVM.DoubleType(), prototype.ParameterNames.Count).ToArray();
				var functionType = LLVM.FunctionType(LLVM.DoubleType(), parameterTypes, false);

				// Create the function and mark it for external linkage.
				func = LLVM.AddFunction(this._module.Mod, prototype.Name, functionType);
				LLVM.SetLinkage(func, LLVMLinkage.LLVMExternalLinkage);

				// Assign our names to the parameters (only for nicer IR, we never query them again).
				for (int i = 0; i < prototype.ParameterNames.Count; i++)
				{
					var parameter = LLVM.GetParam(func, (uint)i);
					LLVM.SetValueName(parameter, prototype.ParameterNames[i]);
				}
			}

			this.Result = func;
		}

		public void VisitFunctionDeclaration(FunctionDeclaration declaration)
		{
			// Generate the declaration's prototype value if not yet present.
			// If it is already present, this is a pure validation.
			this._prototypeIsDefinition = false;
			declaration.Prototype.Accept(this);
		}

		public void VisitFunctionDefinition(FunctionDefinition definition)
		{
			// First, generate (resp. fetch) the value for the prototype.
			this._prototypeIsDefinition = true;
			definition.Prototype.Accept(this);
			var func = this.Result;

			// Bring the function parameters into scope.
			this._namedValues.Clear();

			for (int i = 0; i < definition.Prototype.ParameterNames.Count; i++)
			{
				var parameter = LLVM.GetParam(func, (uint)i);
				var parameterName = definition.Prototype.ParameterNames[i];

				// Note: `FunctionPrototype` enforces uniqueness.
				this._namedValues[parameterName] = parameter;
			}

			// Now we can create a basic block for the function.
			var block = LLVM.AppendBasicBlock(func, "body");
			LLVM.PositionBuilderAtEnd(this._module.Builder, block);

			// Generate the code for the body and grab the return value.
			definition.Body.Accept(this);
			var retVal = this.Result;

			// Create a return instruction with the return value.
			LLVM.BuildRet(this._module.Builder, retVal);

			// Ask LLVM to verify the generated function.
			if (LLVM.VerifyFunction(func, LLVMVerifierFailureAction.LLVMReturnStatusAction))
			{
				throw new FormatException($"LLVM failed to verify the function '{ definition.Prototype.Name }'.");
			}

			this.Result = func;
		}
	}

	public static class CodeGenParserExt
	{
		public static void EmitAll(this Parser parser, Module module)
		{
			// Create a new code generator.
			var codeGen = new CodeGen(module);

			// Parse toplevel elements until we hit the end.
			TopLevelElement topLevelElement;

			while ((topLevelElement = parser.Parse()) != null)
			{
				// Visit the toplevel element.
				topLevelElement.Accept(codeGen);
			}
		}
	}
}
