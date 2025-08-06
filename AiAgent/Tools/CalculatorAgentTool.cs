using LangChain.Chains.StackableChains.Agents.Tools;
using Microsoft.Extensions.Logging;
using System.Data;

namespace AiAgent.Tools;

/// <summary>
/// Calculator tool for LangChain agent
/// </summary>
public class CalculatorAgentTool : AgentTool
{
    private readonly ILogger<CalculatorAgentTool> _logger;

    public CalculatorAgentTool(ILogger<CalculatorAgentTool> logger) 
        : base("calculator", "Perform mathematical calculations. Input should be a mathematical expression like '2 + 2' or '15 * 23 + sqrt(144)'.")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<string> ToolTask(string input, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating: {Expression}", input);

            var result = EvaluateExpression(input);
            var response = $"Result of '{input}' = {result}";
            
            _logger.LogInformation("Calculation result: {Result}", response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating expression: {Expression}", input);
            return $"Error calculating '{input}': {ex.Message}";
        }
    }

    private string EvaluateExpression(string expression)
    {
        try
        {
            // Clean and prepare the expression
            var cleanExpression = CleanExpression(expression);
            
            // Handle special functions
            cleanExpression = HandleSpecialFunctions(cleanExpression);
            
            // Use DataTable.Compute for evaluation
            var table = new DataTable();
            var result = table.Compute(cleanExpression, "");
            
            if (result == DBNull.Value)
            {
                throw new ArgumentException("Invalid expression");
            }
            
            // Format the result nicely
            if (result is double doubleResult)
            {
                return Math.Abs(doubleResult % 1) < 0.0000001 
                    ? ((long)doubleResult).ToString() 
                    : doubleResult.ToString("G15");
            }
            
            return result.ToString() ?? "0";
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Cannot evaluate expression: {ex.Message}", ex);
        }
    }

    private string CleanExpression(string expression)
    {
        // Remove whitespace and normalize
        expression = expression.Trim();
        
        // Replace common mathematical symbols
        expression = expression.Replace("×", "*");
        expression = expression.Replace("÷", "/");
        expression = expression.Replace("^", "**"); // Power operator for later handling
        
        return expression;
    }

    private string HandleSpecialFunctions(string expression)
    {
        // Handle square root
        while (expression.Contains("sqrt("))
        {
            var startIndex = expression.IndexOf("sqrt(");
            var openParenIndex = startIndex + 4; // Position after "sqrt"
            var parenCount = 1;
            var endIndex = openParenIndex;
            
            // Find matching closing parenthesis
            for (int i = openParenIndex + 1; i < expression.Length && parenCount > 0; i++)
            {
                if (expression[i] == '(') parenCount++;
                else if (expression[i] == ')') parenCount--;
                endIndex = i;
            }
            
            if (parenCount == 0)
            {
                var innerExpression = expression.Substring(openParenIndex + 1, endIndex - openParenIndex - 1);
                var innerResult = EvaluateExpression(innerExpression);
                var sqrtResult = Math.Sqrt(double.Parse(innerResult));
                
                expression = expression.Substring(0, startIndex) + 
                           sqrtResult.ToString("G15") + 
                           expression.Substring(endIndex + 1);
            }
            else
            {
                throw new ArgumentException("Mismatched parentheses in sqrt function");
            }
        }
        
        // Handle power operations (** converted from ^)
        while (expression.Contains("**"))
        {
            var powerIndex = expression.IndexOf("**");
            
            // Find the base (number before **)
            var baseStart = powerIndex - 1;
            while (baseStart >= 0 && (char.IsDigit(expression[baseStart]) || expression[baseStart] == '.' || expression[baseStart] == '-'))
            {
                baseStart--;
            }
            baseStart++;
            
            // Find the exponent (number after **)
            var exponentStart = powerIndex + 2;
            var exponentEnd = exponentStart;
            if (exponentStart < expression.Length && expression[exponentStart] == '-')
            {
                exponentEnd++; // Include negative sign
            }
            while (exponentEnd < expression.Length && (char.IsDigit(expression[exponentEnd]) || expression[exponentEnd] == '.'))
            {
                exponentEnd++;
            }
            
            if (baseStart < powerIndex && exponentEnd > exponentStart)
            {
                var baseStr = expression.Substring(baseStart, powerIndex - baseStart);
                var exponentStr = expression.Substring(exponentStart, exponentEnd - exponentStart);
                
                var baseValue = double.Parse(baseStr);
                var exponentValue = double.Parse(exponentStr);
                var result = Math.Pow(baseValue, exponentValue);
                
                expression = expression.Substring(0, baseStart) + 
                           result.ToString("G15") + 
                           expression.Substring(exponentEnd);
            }
            else
            {
                throw new ArgumentException("Invalid power expression");
            }
        }
        
        return expression;
    }
}
