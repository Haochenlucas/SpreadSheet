/****************************************************************************
* dll - Formular
* Author:   Benwei Shi (u1088102), Student of University of Utah
* Date:     04/23/2017
* Purpose:  Project 2 of CS 3505
* Usage:    a Formula object must store the expression to evaluate (e.g., 
*           "1+2") along with any other needed information (e.g., dependencies), 
*           and can then be used to "re-evaluate" the expression whenever 
*           necessary (e.g., when other cells get changed).
****************************************************************************/

// Skeleton written by Joe Zachary for CS 3500, September 2013
// Read the entire skeleton carefully and completely before you
// do anything else!

// Version 1.1 (9/22/13 11:45 a.m.)

// Change log:
//  (Version 1.1) Repaired mistake in GetTokens
//  (Version 1.1) Changed specification of second constructor to
//                clarify description of how validation works

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence
    /// rules.  The allowed symbols are non-negative numbers written using double-precision 
    /// floating-point syntax; variables that consist of a letter or underscore followed by 
    /// zero or more letters, underscores, or digits; parentheses; and the four operator 
    /// symbols +, -, *, and /.  
    /// 
    /// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
    /// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable; 
    /// and "x 23" consists of a variable "x" and a number "23".
    /// 
    /// Associated with every formula are two delegates:  a normalizer and a validator.  The
    /// normalizer is used to convert variables into a canonical form, and the validator is used
    /// to add extra restrictions on the validity of a variable (beyond the standard requirement 
    /// that it consist of a letter or underscore followed by zero or more letters, underscores,
    /// or digits.)  Their use is described in detail in the constructor and method comments.
    /// </summary>
    public class Formula
    {
        // The token type, 0:lp, 1:rp, 2:op_as, 3:op_md, 4:var, 5:double   
        enum tokenType { NoneToken, Left_P, Right_P, Op_AS, Op_MD, Var, Double };

        // Store all the tokens.
        private LinkedList<Token> tokens;

        // Save the error message when failed to create the formula
        private string errorMessage = "";

        /// <summary>
        /// Error message of this Formula if any. Empyt means no error.
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }
            set
            {
                errorMessage = value;
            }
        }

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer is the identity function, and the associated validator
        /// maps every string to true.  
        /// </summary>
        /// <param name="formula">The infix expression of the formula.</param>
        public Formula(String formula) :
            this(formula, s => s, s => true)
        {
        }

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically incorrect,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer and validator are the second and third parameters,
        /// respectively.  
        /// 
        /// If the formula contains a variable v such that normalize(v) is not a legal variable, 
        /// throws a FormulaFormatException with an explanatory message. 
        /// 
        /// If the formula contains a variable v such that isValid(normalize(v)) is false,
        /// throws a FormulaFormatException with an explanatory message.
        /// 
        /// Suppose that N is a method that converts all the letters in a string to upper case, and
        /// that V is a method that returns true only if a string consists of one letter followed
        /// by one digit.  Then:
        /// 
        /// new Formula("x2+y3", N, V) should succeed
        /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
        /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
        /// </summary>
        /// <param name="formula">The infix expression of the formula.</param>
        /// <param name="normalize">A function to normalize the variable name.</param>
        /// <param name="isValid">A function to judge if a variable name is valid.</param>
        public Formula(String formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            tokens = new LinkedList<Token>();
            // Check syntax error and store tokens into tokens LinkedList.
            // If succeed, tokens LinkedList will contains all the normalized tokens, 
            // of a valid formula, in order.
            PreEvaluation(formula, normalize, isValid);
        }

        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  When a variable symbol v needs to be determined, it should be looked up
        /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
        /// the constructor.)
        /// 
        /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
        /// in a string to upper case:
        /// 
        /// new Formula("x+7", N, s => true).Evaluate(L) is 11
        /// new Formula("x+7").Evaluate(L) is 9
        /// 
        /// Given a variable symbol as its parameter, lookup returns the variable's value 
        /// (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        ///
        /// This method should never throw an exception.
        /// </summary>
        /// <param name="lookup">A function to return the value for each variable.</param>
        public object Evaluate(Func<string, double> lookup)
        {
            if (errorMessage.Length > 0)
            {
                return new FormulaError(errorMessage);
            }
            // 1. Begin with two empty stacks: a value stack and an operator stack. 
            Stack<double> operands = new Stack<double>();
            Stack<char> operators = new Stack<char>();

            // 2. Process each token : ...
            foreach (Token token in tokens)
            {
                switch (token.type)
                {
                    case tokenType.Left_P:
                        operators.Push(token.value[0]);
                        break;
                    case tokenType.Right_P:
                        if (isOnTop(operators, new char[] { '+', '-' }))
                            calLast(operands, operators);
                        if (isOnTop(operators, new char[] { '(' }))
                            operators.Pop();
                        if (isOnTop(operators, new char[] { '*', '/' }))
                            calLast(operands, operators);
                        break;
                    case tokenType.Op_AS:
                        if (isOnTop(operators, new char[] { '+', '-' }))
                            calLast(operands, operators);
                        operators.Push(token.value[0]);
                        break;
                    case tokenType.Op_MD:
                        operators.Push(token.value[0]);
                        break;
                    case tokenType.Var:
                        double v;
                        try { v = lookup(token.value); }
                        catch (Exception)
                        {
                            return new FormulaError(token.value + " Invalid");
                        }
                        operands.Push(v);
                        if (isOnTop(operators, new char[] { '*', '/' }))
                            calLast(operands, operators);
                        break;
                    case tokenType.Double:
                        operands.Push(double.Parse(token.value));
                        if (isOnTop(operators, new char[] { '*', '/' }))
                            calLast(operands, operators);
                        break;
                }
            }
            // 3. When the last token has been processed: ...
            // if + or - is the latest existing operator, calculate that. 
            if (isOnTop(operators, new char[] { '+', '-' }))
            {
                calLast(operands, operators);
            }
            // Check if everything is gone except the result. Then return the result.
            if (operators.Count == 0 && operands.Count == 1)
            {
                double result = operands.Pop();
                if (double.IsInfinity(result))
                    return new FormulaError("Divide by 0."); 
                else
                    return result;
            }
            return new FormulaError();
        }

        /// <summary>
        /// Enumerates the normalized versions of all of the variables that occur in this 
        /// formula.  No normalization may appear more than once in the enumeration, even 
        /// if it appears more than once in this Formula.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
        /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
        /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
        /// </summary>
        public IEnumerable<String> GetVariables()
        {
            HashSet<String> vars = new HashSet<string>();
            foreach (Token token in tokens)
                if (token.type == tokenType.Var)
                    vars.Add(token.value);
            return vars;
        }

        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).  All of the
        /// variables in the string should be normalized.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
        /// new Formula("x + Y").ToString() should return "x+Y"
        /// </summary>
        public override string ToString()
        {
            StringBuilder formula = new StringBuilder();
            foreach (Token token in tokens)
            {
                formula.Append(token.value);
            }
            return formula.ToString();
        }

        /// <summary>
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  To determine token equality, all tokens are compared as strings 
        /// except for numeric tokens, which are compared as doubles, and variable tokens,
        /// whose normalized forms are compared as strings.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        ///  
        /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
        /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
        /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        /// <param name="obj">The object to be compared with.</param>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            Formula f = (Formula)obj;
            return f.ToString().Equals(this.ToString());
        }

        /// <summary>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return true.  If one is
        /// null and one is not, this method should return false.
        /// </summary>
        /// <param name="f1">One operand of "==".</param>
        /// <param name="f2">Another operand of "==".</param>
        public static bool operator ==(Formula f1, Formula f2)
        {
            if (ReferenceEquals(f1, null))
                if (ReferenceEquals(f2, null))
                    return true;
                else return false;
            else
                return f1.Equals(f2);
        }

        /// <summary>
        /// Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return false.  If one is
        /// null and one is not, this method should return true.
        /// </summary>
        /// <param name="f1">One operand of "!=".</param>
        /// <param name="f2">Another operand of "!=".</param>
        public static bool operator !=(Formula f1, Formula f2)
        {
            if (ReferenceEquals(f1, null))
                if (ReferenceEquals(f2, null))
                    return false;
                else return true;
            else
                return !f1.Equals(f2);
        }

        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Represent each token with it's type.
        /// </summary>
        private struct Token
        {
            // token string.
            public string value;
            // The token type, 0:lp, 1:rp, 2:op_as(+-), 3:op_md(*/), 4:var, 5:double            
            public tokenType type;
            public Token(string value, tokenType type)
            {
                this.value = value;
                this.type = type;
            }
        }
        
        /// <summary>
        /// Calculate with the latest two operands and one perator.
        /// More specifically, pop the value stack twice and the operator stack once. 
        /// Apply the popped operator to the popped operands. Push the result back to the 
        /// operands stack.
        /// </summary>
        private void calLast(Stack<double> operands, Stack<char> operators)
        {
            if (operands.Count >= 2 && operators.Count >= 1)
            {
                double v2 = operands.Pop();
                double v1 = operands.Pop();
                double result = 0;
                char op = operators.Pop();
                switch (op)
                {
                    case '+':
                        result = v1 + v2;
                        break;
                    case '-':
                        result = v1 - v2;
                        break;
                    case '*':
                        result = v1 * v2;
                        break;
                    case '/':
                        result = v1 / v2;
                        break;
                }
                operands.Push(result);
            }
        }

        /// <summary>
        /// This method is used in Formula constructor to check syntax errors and store
        /// tokens into tokens stack.
        /// </summary>
        /// <param name="formula"></param>
        /// <param name="normalize"></param>
        /// <param name="isValid"></param>
        private void PreEvaluation(String formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            // Stack caledTokens is used to store the result of previous tokens.
            Stack<Token> caledTokens = new Stack<Token>();
            Token previousToken = new Token();
            Token currentToken = new Token();
            foreach (Token token in GetTokens(formula))
            {
                currentToken = token;
                switch (currentToken.type)
                {
                    case tokenType.Left_P:
                        if (caledTokens.Count != 0 && caledTokens.Peek().type == tokenType.Double)
                            errorMessage += (previousToken.value +
                                " cannot followed by a left parenthesis \"(\".");
                        else caledTokens.Push(currentToken);
                        break;
                    case tokenType.Right_P:
                        if (caledTokens.Count == 0)
                            errorMessage += ("Can not start with \")\"");
                        else if (caledTokens.Peek().type == tokenType.Left_P)
                            errorMessage += ("Empty \"()\" is not allowed.");
                        else if (caledTokens.Peek().type == tokenType.Double)
                        {
                            Token t = caledTokens.Pop();
                            if (caledTokens.Count == 0)
                                errorMessage += ("Parentheses does not pair up");
                            else // lp
                            {
                                caledTokens.Pop();
                                if (caledTokens.Count == 0 || caledTokens.Peek().type == tokenType.Left_P)
                                    caledTokens.Push(t);
                                else // op
                                    caledTokens.Pop();
                            }
                        }
                        else // op
                            errorMessage += (previousToken.value + "can not followed by a \")\".");
                        break;
                    case tokenType.Op_AS:
                    case tokenType.Op_MD:
                        if (caledTokens.Count == 0)
                            errorMessage += ("Can not start with " + currentToken.value);
                        else if (caledTokens.Peek().type == tokenType.Double)
                            caledTokens.Push(currentToken);
                        else // op or lp
                            errorMessage += (previousToken.value + "can not followed by" + 
                                currentToken.value);
                        break;
                    case tokenType.Var:
                        currentToken.value = normalize(currentToken.value);
                        if (!isValid(currentToken.value))
                            errorMessage += (currentToken.value + " is not a valid variable.");
                            //throw new FormulaFormatException(currentToken.value + " is not a valid variable.");
                        if (caledTokens.Count == 0 || caledTokens.Peek().type == tokenType.Left_P)
                            // use a empty double instead the value of the variable.
                            caledTokens.Push(new Token("", tokenType.Double));
                        else if (caledTokens.Peek().type == tokenType.Op_AS || caledTokens.Peek().type == tokenType.Op_MD)
                            caledTokens.Pop();
                        else // double
                            errorMessage += ("There must be a \"(\" or operator in front of operand: " + currentToken.value);
                        break;
                    case tokenType.Double:
                        // tryParse to double
                        double v;
                        if (!double.TryParse(currentToken.value, out v))
                            errorMessage += ("Can not convert " + currentToken.value + " to double value.");
                        currentToken.value = v.ToString();
                        if (caledTokens.Count == 0 || caledTokens.Peek().type == tokenType.Left_P)
                            caledTokens.Push(currentToken);
                        else if (caledTokens.Peek().type == tokenType.Op_AS || caledTokens.Peek().type == tokenType.Op_MD)
                            caledTokens.Pop();
                        else // double
                            errorMessage += ("There must be a \"(\" or operator in front of operand: " + currentToken.value);
                        break;
                }
                tokens.AddLast(currentToken);
                previousToken = currentToken;
            }
            if (caledTokens.Count == 0) errorMessage += ("Formula can not be empty.");
            else if (caledTokens.Count != 1 || caledTokens.Pop().type != tokenType.Double)
                errorMessage += ("Formula is invalid");
        }

        /// <summary>
        /// Check if the chars in char array is on top of the char Stack.
        /// </summary>
        /// <param name="st">The stack object</param>
        /// <param name="chs">chars to be checked</param>
        /// <returns>Return true if one of the chars is found at the top of the 
        /// stack.</returns>
        private static bool isOnTop(Stack<char> st, char[] chs)
        {
            if (st.Count > 0)
            {
                foreach (char ch in chs)
                {
                    if (st.Peek() == ch) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
        /// right paren; one of the four operator symbols; a string consisting of a letter or underscore
        /// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        private static IEnumerable<Token> GetTokens(String formula)
        {
            // Patterns for individual tokens
            String lpPattern = @"\(";   //0
            String rpPattern = @"\)";   //1
            String op_asPattern = @"[\+\-]";    //2
            String op_mdPattern = @"[*/]";      //3
            String varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*"; //4
            String doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";  //5
            String spacePattern = @"\s+";

            // Overall pattern
            String pattern = String.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5}) | {6}",
                                            lpPattern, rpPattern, op_asPattern, op_mdPattern, varPattern, doublePattern, spacePattern);

            // Enumerate matching tokens and decide the token type.
            foreach (String s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
            {
                tokenType type = tokenType.NoneToken;
                if      (Regex.IsMatch(s, "^" + lpPattern + "$",    RegexOptions.Singleline)) type = tokenType.Left_P;
                else if (Regex.IsMatch(s, "^" + rpPattern + "$",    RegexOptions.Singleline)) type = tokenType.Right_P;
                else if (Regex.IsMatch(s, "^" + op_asPattern + "$", RegexOptions.Singleline)) type = tokenType.Op_AS;
                else if (Regex.IsMatch(s, "^" + op_mdPattern + "$", RegexOptions.Singleline)) type = tokenType.Op_MD;
                else if (Regex.IsMatch(s, "^" + varPattern + "$",   RegexOptions.IgnorePatternWhitespace)) type = tokenType.Var;
                else if (Regex.IsMatch(s, "^" + doublePattern + "$",RegexOptions.IgnorePatternWhitespace)) type = tokenType.Double;
                // return if the type is one of the above
                if (type != tokenType.NoneToken)
                {
                    yield return new Token(s, type);
                }
            }
        }
    }

    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(String reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }
    }
}
