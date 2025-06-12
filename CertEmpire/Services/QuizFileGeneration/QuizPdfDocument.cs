using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CertEmpire.Services.QuizFileGeneration
{
    public class QuizPdfDocument : IDocument
    {
        private readonly string _title;
        private readonly List<QuizQuestion> _questions;

        public QuizPdfDocument(string title, List<QuizQuestion> questions)
        {
            _title = title;
            _questions = questions;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.PageColor(Colors.Blue.Darken2);
                page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontColor(Colors.White));

                page.Content().PaddingVertical(50).Column(col =>
                {
                    col.Item().Text(_title)
                        .FontSize(22).Bold();

                    col.Item().PaddingTop(15).Text("Microsoft Dynamics 365\nBusiness Central Developer")
                        .FontSize(16).Bold();

                    col.Item().PaddingTop(10).Text("Exam Question & Answers")
                        .FontSize(14);

                    col.Item().PaddingTop(100);

                    col.Item().AlignCenter().Text("Thank You for your purchase")
                        .FontSize(10).FontColor(Colors.White); // Optional gray

                    col.Item().AlignCenter().Text("CERT EMPIRE")
                        .FontSize(12).Bold();
                });
            });

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(12).FontColor(Colors.Black));

                page.Content().Column(col =>
                {
                    col.Item().Text($"Quiz Title: {_title}").Bold();
                    col.Item().Text($"Export Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                    col.Item().Text($"Questions Count: {_questions.Count}");
                    col.Item().Text("Version: 6.6");
                });
            });

            foreach (var question in _questions)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(11.5f));

                    page.Content().Column(col =>
                    {
                        col.Spacing(6);

                        col.Item().Text($"Question {question.Number}").FontSize(16).Bold();

                        col.Item().Text(question.Statement);

                        if (question.Options.Any())
                        {
                            foreach (var opt in question.Options)
                            {
                                col.Item().PaddingLeft(20).Text(opt);
                            }
                        }

                        col.Item().Text("Correct Answer:").Bold();
                        col.Item().PaddingLeft(20).Text(question.CorrectAnswer);

                        col.Item().Text("Explanation:").Bold();
                        col.Item().PaddingLeft(20).Text(question.Explanation);

                        col.Item().Text("Why Incorrect Options are Wrong:").Bold();
                        col.Item().PaddingLeft(20).Text(question.WhyIncorrect);

                        col.Item().Text("References:").Bold();
                        col.Item().PaddingLeft(20).Text(question.References);
                    });
                });
            }
        }
    }
    public class QuizQuestion
    {
        public int Number { get; set; }
        public string Statement { get; set; }
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; }
        public string Explanation { get; set; }
        public string WhyIncorrect { get; set; }
        public string References { get; set; }
    }
}
