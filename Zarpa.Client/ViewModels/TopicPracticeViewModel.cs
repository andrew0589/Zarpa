using CommunityToolkit.Mvvm.Input;
using Zarpa.Client.Resources.Languages;

namespace Zarpa.Client.ViewModels
{
    public partial class TopicPracticeViewModel : BaseViewModel
    {
        public IReadOnlyList<TopicProgressItem> Topics { get; }

        // Completed questions over total questions, across all topics.
        public double GlobalProgress { get; }
        public string GlobalProgressText { get; }

        public TopicPracticeViewModel()
        {
            // TODO: sample data — replace with the real topic list, question counts and the
            // user's progress from the API once the question bank exists in the database.
            Topics =
            [
                new(1, "Nomenclatura náutica", 45, 0.80),
                new(2, "Elementos de amarre y fondeo", 28, 0.65),
                new(3, "Seguridad", 45, 0.50),
                new(4, "Legislación", 38, 0.30),
                new(5, "Balizamiento", 60, 0.45),
                new(6, "Reglamento (RIPA)", 90, 0.25),
                new(7, "Maniobra y navegación", 32, 0.10),
                new(8, "Emergencias en la mar", 30, 0.0),
                new(9, "Meteorología", 42, 0.0),
                new(10, "Teoría de la navegación", 55, 0.0),
                new(11, "Carta de navegación", 40, 0.0),
            ];

            var totalQuestions = Topics.Sum(t => t.QuestionCount);
            GlobalProgress = totalQuestions == 0
                ? 0
                : Topics.Sum(t => t.QuestionCount * t.Progress) / totalQuestions;
            GlobalProgressText = $"{(int)Math.Round(GlobalProgress * 100)}%";
        }

        [RelayCommand]
        private void SelectTopic(TopicProgressItem topic)
        {
            // TODO: navigate to the per-topic practice session once the question flow exists.
        }
    }

    public class TopicProgressItem(int number, string name, int questionCount, double progress)
    {
        public int Number { get; } = number;
        public string Name { get; } = name;
        public int QuestionCount { get; } = questionCount;
        public double Progress { get; } = progress;

        public string QuestionCountText => string.Format(AppResources.QuestionsCountFormat, QuestionCount);
        public string ProgressText => $"{(int)Math.Round(Progress * 100)}%";
    }
}
