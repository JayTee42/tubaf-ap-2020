using System;
using System.IO;
using System.Runtime.InteropServices;
using LLVMSharp;

public class Module: IDisposable
{
	// The LLVM module reference
	public LLVMModuleRef Mod { get; private set; }

	// The builder reference
	public LLVMBuilderRef Builder { get; private set; }

	// The target machine
	private LLVMTargetMachineRef _targetMachine;

	// Avoid double-dispose
	private bool _isDisposed = false;

	// The static constructor is executed once to init LLVM itself.
	static Module()
	{
		// Init the x86 target.
		LLVM.InitializeX86TargetInfo();
		LLVM.InitializeX86Target();
		LLVM.InitializeX86TargetMC();
		LLVM.InitializeX86AsmPrinter();

		// Init the ARM target.
		LLVM.InitializeARMTargetInfo();
		LLVM.InitializeARMTarget();
		LLVM.InitializeARMTargetMC();
		LLVM.InitializeARMAsmPrinter();

		// Init the MIPS target.
		LLVM.InitializeMipsTargetInfo();
		LLVM.InitializeMipsTarget();
		LLVM.InitializeMipsTargetMC();
		LLVM.InitializeMipsAsmPrinter();

		// TODO: Maybe add more platforms?
	}

	// Create a new module with the given name.
	// The passed target triple can be used for cross compilation.
	// If it is null, the current platform will be used.
	public Module(string name, string targetTriple)
	{
		// Create the module and the builder.
		this.Mod = LLVM.ModuleCreateWithName(name);
		this.Builder = LLVM.CreateBuilder();

		// Get the default target triple if necessary:
		if (targetTriple == null)
		{
			targetTriple = Marshal.PtrToStringAnsi(LLVM.GetDefaultTargetTriple());
		}

		// Select the target based on the triple.
		if (LLVM.GetTargetFromTriple(targetTriple, out var target, out var error))
		{
			throw new ArgumentException($"Failed to obtain target from triple '{ targetTriple }': { error }");
		}

		LLVM.SetTarget(this.Mod, targetTriple);

		// Create a target machine.
		this._targetMachine = LLVM.CreateTargetMachine(target, targetTriple, "generic", "",
			LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocPIC, LLVMCodeModel.LLVMCodeModelDefault);

		// Create a data layout from the machine and assign it to the module.
		var dataLayout = LLVM.CreateTargetDataLayout(this._targetMachine);
		LLVM.SetModuleDataLayout(this.Mod, dataLayout);
	}

	// Ensure that our LLVM references are cleaned up properly.
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (this._isDisposed)
		{
			return;
		}

		if (disposing)
		{
			// Dispose builder and module.
			LLVM.DisposeBuilder(this.Builder);
			LLVM.DisposeModule(this.Mod);
		}

		this._isDisposed = true;
	}

	public void PrintIRToFile(string filePath)
	{
		if (LLVM.PrintModuleToFile(this.Mod, filePath, out string error))
		{
			throw new InvalidOperationException($"Failed to print IR to file: { error }");
		}
	}

	public void PrintAssemblyToFile(string filePath)
	{
		if (LLVM.TargetMachineEmitToFile(this._targetMachine, this.Mod, Marshal.StringToHGlobalAnsi(filePath), LLVMCodeGenFileType.LLVMAssemblyFile, out string error))
		{
			throw new InvalidOperationException($"Failed to print assembly to file: { error }");
		}
	}

	public void PrintObjectCodeToFile(string filePath)
	{
		if (LLVM.TargetMachineEmitToFile(this._targetMachine, this.Mod, Marshal.StringToHGlobalAnsi(filePath), LLVMCodeGenFileType.LLVMObjectFile, out string error))
		{
			throw new InvalidOperationException($"Failed to print object code to file: { error }");
		}
	}
}
