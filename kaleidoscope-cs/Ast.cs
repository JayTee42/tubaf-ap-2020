using System;
using System.Collections.Generic;
using System.Linq;

namespace Kaleidoscope.Ast
{
	// A basic interface that is implemented by every AST element
	public interface Element
	{
		void Accept(Visitor visitor);
	}

	// A basic interface that is implemented by those AST elements that are expressions
	public interface Expression: Element { }

	// A basic interface that is implemented by those AST elements that are top-level
	public interface TopLevelElement: Element { }

	public interface Visitor
	{
		void VisitLiteralExpression(LiteralExpression expr);
		void VisitParameterExpression(ParameterExpression expr);
		void VisitBinaryOperatorExpression(BinaryOperatorExpression expr);
		void VisitCallExpression(CallExpression expr);
		void VisitConditionalExpression(ConditionalExpression expr);
		void VisitFunctionPrototype(FunctionPrototype prototype);
		void VisitFunctionDeclaration(FunctionDeclaration declaration);
		void VisitFunctionDefinition(FunctionDefinition definition);
	}

	public class LiteralExpression: Expression
	{
		// The value of the literal
		public double Value { get; private set; }

		internal LiteralExpression(double value)
		{
			this.Value = value;
		}

		public void Accept(Visitor visitor)
		{
			visitor.VisitLiteralExpression(this);
		}
	}

	public class ParameterExpression: Expression
	{
		// The name of the parameter
		public string Name { get; private set; }

		internal ParameterExpression(string name)
		{
			this.Name = name;
		}

		public void Accept(Visitor visitor)
		{
			visitor.VisitParameterExpression(this);
		}
	}

	public class BinaryOperatorExpression: Expression
	{
		// The left expression
		public Expression Left { get; private set; }

		// The operator between left and right expression
		public Operator Operator { get; private set; }

		// The right expression
		public Expression Right { get; private set; }

		internal BinaryOperatorExpression(Expression lhs, Operator op, Expression rhs)
		{
			this.Left = lhs;
			this.Operator = op;
			this.Right = rhs;
		}

		public void Accept(Visitor visitor)
		{
			visitor.VisitBinaryOperatorExpression(this);
		}
	}

	public class CallExpression: Expression
	{
		// The name of the called function
		public string Name { get; private set; }

		// The parameter list
		public List<Expression> Parameters { get; private set; }

		internal CallExpression(string name, List<Expression> parameters)
		{
			this.Name = name;
			this.Parameters = parameters;
		}

		public void Accept(Visitor visitor)
		{
			visitor.VisitCallExpression(this);
		}
	}

	public class ConditionalExpression: Expression
	{
		// The condition
		public Expression Condition { get; private set; }

		// The "then" path
		public Expression Then { get; private set; }

		// The "else" path
		public Expression Else { get; private set; }

		internal ConditionalExpression(Expression condition, Expression thenExpr, Expression elseExpr)
		{
			this.Condition = condition;
			this.Then = thenExpr;
			this.Else = elseExpr;
		}

		public void Accept(Visitor visitor)
		{
			visitor.VisitConditionalExpression(this);
		}
	}

	public class FunctionPrototype: Element
	{
		// The function name
		public string Name { get; private set; }

		// The function parameter names
		public List<string> ParameterNames { get; private set; }

		internal FunctionPrototype(string name, List<string> parameterNames)
		{
			this.Name = name;
			this.ParameterNames = parameterNames;

			// Validate that the parameter names are unique.
			if (parameterNames.Distinct().Count() != parameterNames.Count)
			{
				throw new FormatException("Parameter names must be unique.");
			}
		}

		public void Accept(Visitor visitor)
		{
			visitor.VisitFunctionPrototype(this);
		}
	}

	public class FunctionDeclaration: TopLevelElement
	{
		// The function prototype
		public FunctionPrototype Prototype { get; private set; }

		internal FunctionDeclaration(FunctionPrototype prototype)
		{
			this.Prototype = prototype;
		}

		public void Accept(Visitor visitor)
		{
			visitor.VisitFunctionDeclaration(this);
		}
	}

	public class FunctionDefinition: TopLevelElement
	{
		// The function prototype
		public FunctionPrototype Prototype { get; private set; }

		// The function body
		public Expression Body { get; private set; }

		internal FunctionDefinition(FunctionPrototype prototype, Expression body)
		{
			this.Prototype = prototype;
			this.Body = body;
		}

		public void Accept(Visitor visitor)
		{
			visitor.VisitFunctionDefinition(this);
		}
	}
}
