using System.Text.RegularExpressions;

namespace CertEmpire.Helpers.ResponseWrapper
{
    public class ValidationResponseWrapper
    {
        public string Option { get; set; }
        public string Explanation { get; set; }

        public static ValidationResponseWrapper Parse(string input)
        {
            var result = new ValidationResponseWrapper();

            // Default case: If "The correct answer is" is missing but it says "The provided answer is correct."
            if (input.Contains("The provided answer is correct."))
            {
                result.Option = "The provided answer is correct.";
            }
            else
            {
                // Try to find the correct answer in format: The correct answer is [C, D]
                var optionStart = input.IndexOf("The correct answer is [");
                var optionEnd = input.IndexOf("]", optionStart);
                if (optionStart >= 0 && optionEnd > optionStart)
                {
                    string answer = input.Substring(optionStart + "The correct answer is [".Length,
                                                    optionEnd - optionStart - "The correct answer is [".Length);
                    result.Option = $"The correct answer is {answer}";
                }
                else
                {
                    result.Option = "Option not found";
                }
            }

            // Extract explanation
            var explanationIndex = input.IndexOf("Explanation:");
            if (explanationIndex >= 0)
            {
                result.Explanation = input.Substring(explanationIndex).Trim();
            }
            else
            {
                result.Explanation = "Explanation not found";
            }

            return result;

        }
    }
}