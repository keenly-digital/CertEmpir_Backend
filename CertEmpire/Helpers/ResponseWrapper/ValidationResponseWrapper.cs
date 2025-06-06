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

            // Extract correct answer between [ and ]
            var optionStart = input.IndexOf("The correct answer is [");
            var optionEnd = input.IndexOf("]", optionStart);
            if (optionStart >= 0 && optionEnd > optionStart)
            {
                var answer = input.Substring(optionStart + "The correct answer is [".Length,
                                             optionEnd - optionStart - "The correct answer is [".Length);
                result.Option = $"The correct answer is {answer}";
            }

            // Extract explanation
            var explanationIndex = input.IndexOf("Explanation:");
            if (explanationIndex >= 0)
            {
                result.Explanation = input.Substring(explanationIndex).Trim();
            }

            return result;

        }
    }
}