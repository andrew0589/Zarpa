using Xunit;
using Zarpa.Api.Data.Entities;
using Zarpa.Api.Services;
using Zarpa.Shared.Dtos;

namespace Zarpa.Api.Tests
{
    // Grading rules under test (official PER correction):
    //  - at most MaxTotalErrors errors in total (PER: 13, PNB: 10); unanswered = error;
    //  - ONLY the topics with an own MaxErrors in the blueprint (PER: Balizamiento 2,
    //    RIPA 5, Carta 2) can fail the exam by themselves; the rest only feed the total.
    // Correct answer is always index 1 in the fixture; "wrong" answers submit index 2.
    public class ExamSessionServiceTests
    {
        private const long UserId = 42;
        private const long PerExamId = 1;
        private const long PnbExamId = 2;

        // PER positions per topic: 1-4 T1, 5-6 T2, 7-10 T3, 11-12 T4, 13-17 T5(max 2),
        // 18-27 T6(max 5), 28-29 T7, 30-32 T8, 33-36 T9, 37-41 T10, 42-45 T11(max 2).
        private static readonly (int Topic, int Count)[] PerBlueprint =
            [(1, 4), (2, 2), (3, 4), (4, 2), (5, 5), (6, 10), (7, 2), (8, 3), (9, 4), (10, 5), (11, 4)];

        private static readonly (int Topic, int Count)[] PnbBlueprint =
            [(1, 4), (2, 2), (3, 4), (4, 2), (5, 5), (6, 10)];

        private static (FakeExamSessionRepository Repo, ExamSessionService Service) CreateFixture()
        {
            var repo = new FakeExamSessionRepository();

            var per = new LicenseEntity { ID = 2, Code = "PER", Name = "PER", ExamMinutes = 90, MaxTotalErrors = 13 };
            var pnb = new LicenseEntity { ID = 1, Code = "PNB", Name = "PNB", ExamMinutes = 45, MaxTotalErrors = 10 };

            var topics = Enumerable.Range(1, 11)
                .ToDictionary(n => n, n => new TopicEntity { ID = n, Number = n, Name = $"Tema {n}" });

            AddExam(repo, PerExamId, per, PerBlueprint, topics, questionIdBase: 100);
            AddExam(repo, PnbExamId, pnb, PnbBlueprint, topics, questionIdBase: 500);

            // PER: only Balizamiento (5), RIPA (6) and Carta (11) have own limits.
            repo.TopicLimitsByLicense[per.ID] = Enumerable.Range(1, 11)
                .ToDictionary(n => (long)n, n => n switch { 5 => (int?)2, 6 => 5, 11 => 2, _ => null });

            // PNB: no per-topic limits at all.
            repo.TopicLimitsByLicense[pnb.ID] = Enumerable.Range(1, 6)
                .ToDictionary(n => (long)n, _ => (int?)null);

            return (repo, new ExamSessionService(repo));
        }

        private static void AddExam(
            FakeExamSessionRepository repo, long examId, LicenseEntity license,
            (int Topic, int Count)[] blueprint, Dictionary<int, TopicEntity> topics, long questionIdBase)
        {
            repo.Exams.Add(new ExamEntity
            {
                ID = examId,
                LicenseID = license.ID,
                License = license,
                ComunidadAutonomaID = 9,
                Year = 2019,
                Month = 12,
                Model = "A",
            });

            var position = 0;
            foreach (var (topic, count) in blueprint)
            {
                for (var i = 0; i < count; i++)
                {
                    position++;
                    repo.Questions.Add(new ExamQuestionEntity
                    {
                        ID = questionIdBase + position,
                        ExamID = examId,
                        Position = position,
                        TopicID = topics[topic].ID,
                        Topic = topics[topic],
                        Text = $"Pregunta {position}",
                        Answer1 = "Correcta",
                        Answer2 = "Mala",
                        Answer3 = "Mala",
                        Answer4 = "Mala",
                        CorrectIndex = 1,
                    });
                }
            }
        }

        // Answers every question with the correct option (1) except the given
        // positions: `wrong` submits option 2, `unanswered` submits nothing.
        private static async Task<ExamSessionResultDto> RunExamAsync(
            ExamSessionService service, long examId, int[]? wrong = null, int[]? unanswered = null)
        {
            var start = await service.StartAsync(UserId, examId);
            Assert.NotNull(start);

            foreach (var question in start.Questions)
            {
                if (unanswered is not null && unanswered.Contains(question.Position))
                    continue;

                var chosen = wrong is not null && wrong.Contains(question.Position) ? 2 : 1;
                Assert.True(await service.SubmitAnswerAsync(UserId, start.SessionId,
                    new SubmitExamAnswerRequestDto(question.Id, chosen)));
            }

            var result = await service.FinishAsync(UserId, start.SessionId);
            Assert.NotNull(result);
            return result;
        }

        private static ExamTopicResultDto Topic(ExamSessionResultDto result, int number) =>
            result.Topics.Single(t => t.TopicNumber == number);

        // ---------- A. plafonul total (max 13) ----------

        [Fact]
        public async Task A1_TodoCorrecto_Apto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId);

            Assert.True(result.Passed);
            Assert.Equal(45, result.Correct);
            Assert.Equal(0, result.TotalErrors);
        }

        [Fact]
        public async Task A2_PrimerasCuatroMal_Apto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId, wrong: [1, 2, 3, 4]);

            Assert.True(result.Passed);
            Assert.Equal(41, result.Correct);
            Assert.Equal(4, Topic(result, 1).Errors);
            Assert.True(result.Topics.All(t => t.WithinLimit));
        }

        [Fact]
        public async Task A3_TreceErroresFueraDeLosLimites_Apto()
        {
            var (_, service) = CreateFixture();
            // 13 errors, none in the limited topics (5, 6, 11).
            var result = await RunExamAsync(service, PerExamId,
                wrong: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 33]);

            Assert.True(result.Passed);
            Assert.Equal(13, result.TotalErrors);
        }

        [Fact]
        public async Task A4_CatorceErrores_NoApto_SinTemaEnRojo()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId,
                wrong: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 33, 34]);

            Assert.False(result.Passed);
            Assert.Equal(14, result.TotalErrors);
            // Falls ONLY on the total — every topic stays within its own limit.
            Assert.True(result.Topics.All(t => t.WithinLimit));
        }

        // ---------- B. limitele proprii (2 / 5 / 2) ----------

        [Fact]
        public async Task B1_BalizamientoDosErrores_Apto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId, wrong: [13, 14]);

            Assert.True(result.Passed);
            Assert.True(Topic(result, 5).WithinLimit);
            Assert.Equal(2, Topic(result, 5).Errors);
        }

        [Fact]
        public async Task B2_BalizamientoTresErrores_NoApto_ConSolo3ErroresTotales()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId, wrong: [13, 14, 15]);

            // The classic "42 de 45 pero suspenso".
            Assert.False(result.Passed);
            Assert.Equal(42, result.Correct);
            Assert.Equal(3, result.TotalErrors);
            Assert.False(Topic(result, 5).WithinLimit);
        }

        [Fact]
        public async Task B3_RipaCincoErrores_Apto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId, wrong: [18, 19, 20, 21, 22]);

            Assert.True(result.Passed);
            Assert.True(Topic(result, 6).WithinLimit);
        }

        [Fact]
        public async Task B4_RipaSeisErrores_NoApto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId, wrong: [18, 19, 20, 21, 22, 23]);

            Assert.False(result.Passed);
            Assert.False(Topic(result, 6).WithinLimit);
        }

        [Fact]
        public async Task B5_CartaDosErrores_Apto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId, wrong: [42, 43]);

            Assert.True(result.Passed);
            Assert.True(Topic(result, 11).WithinLimit);
        }

        [Fact]
        public async Task B6_CartaTresErrores_NoApto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId, wrong: [42, 43, 44]);

            Assert.False(result.Passed);
            Assert.False(Topic(result, 11).WithinLimit);
        }

        [Fact]
        public async Task B7_TodoAlLimiteSimultaneamente_Apto()
        {
            var (_, service) = CreateFixture();
            // Balizamiento 2 + RIPA 5 + Carta 2 + otras 4 = 13 errors, everything at max.
            var result = await RunExamAsync(service, PerExamId,
                wrong: [13, 14, 18, 19, 20, 21, 22, 42, 43, 1, 2, 3, 4]);

            Assert.True(result.Passed);
            Assert.Equal(13, result.TotalErrors);
            Assert.True(result.Topics.All(t => t.WithinLimit));
        }

        // ---------- C. fara raspuns = eroare ----------

        [Fact]
        public async Task C1_SinResponderEnCarta_NoApto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId, unanswered: [42, 43, 44]);

            Assert.False(result.Passed);
            Assert.Equal(3, result.Unanswered);
            Assert.Equal(0, result.Wrong);
            Assert.False(Topic(result, 11).WithinLimit);
        }

        [Fact]
        public async Task C2_EntregarEnBlanco_NoApto_45Errores()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId,
                unanswered: Enumerable.Range(1, 45).ToArray());

            Assert.False(result.Passed);
            Assert.Equal(45, result.TotalErrors);
            Assert.Equal(45, result.Unanswered);
            Assert.False(Topic(result, 5).WithinLimit);
            Assert.False(Topic(result, 6).WithinLimit);
            Assert.False(Topic(result, 11).WithinLimit);
        }

        // ---------- D. mecanica sesiunii ----------

        [Fact]
        public async Task D1_CambiarRespuesta_CuentaLaFinal()
        {
            var (_, service) = CreateFixture();
            var start = await service.StartAsync(UserId, PerExamId);
            var first = start!.Questions[0];

            // Wrong first, then changed to the correct option before handing in.
            Assert.True(await service.SubmitAnswerAsync(UserId, start.SessionId, new SubmitExamAnswerRequestDto(first.Id, 2)));
            Assert.True(await service.SubmitAnswerAsync(UserId, start.SessionId, new SubmitExamAnswerRequestDto(first.Id, 1)));

            foreach (var question in start.Questions.Skip(1))
                await service.SubmitAnswerAsync(UserId, start.SessionId, new SubmitExamAnswerRequestDto(question.Id, 1));

            var result = await service.FinishAsync(UserId, start.SessionId);

            Assert.True(result!.Passed);
            Assert.Equal(45, result.Correct);
        }

        [Fact]
        public async Task D2_StartReanudaSesionAbierta_ConRespuestas()
        {
            var (_, service) = CreateFixture();
            var start = await service.StartAsync(UserId, PerExamId);
            await service.SubmitAnswerAsync(UserId, start!.SessionId,
                new SubmitExamAnswerRequestDto(start.Questions[0].Id, 3));

            var resumed = await service.StartAsync(UserId, PerExamId);

            Assert.Equal(start.SessionId, resumed!.SessionId);
            Assert.Equal(3, resumed.Questions[0].ChosenIndex);
        }

        [Fact]
        public async Task D3_Abandonar_BorraSesionYRespuestas()
        {
            var (repo, service) = CreateFixture();
            var start = await service.StartAsync(UserId, PerExamId);
            await service.SubmitAnswerAsync(UserId, start!.SessionId,
                new SubmitExamAnswerRequestDto(start.Questions[0].Id, 1));

            Assert.True(await service.AbandonAsync(UserId, start.SessionId));

            Assert.Empty(repo.Sessions);
            Assert.Empty(repo.Answers);
            // A second abandon has nothing left to delete.
            Assert.False(await service.AbandonAsync(UserId, start.SessionId));
        }

        [Fact]
        public async Task D4_NoSePuedeAbandonarUnExamenEntregado()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId);

            Assert.False(await service.AbandonAsync(UserId, result.SessionId));
        }

        [Fact]
        public async Task D5_ReentrarEnExamenTerminado_ReemplazaLaSesion()
        {
            var (repo, service) = CreateFixture();
            var finished = await RunExamAsync(service, PerExamId);

            var fresh = await service.StartAsync(UserId, PerExamId);

            Assert.NotEqual(finished.SessionId, fresh!.SessionId);
            var only = Assert.Single(repo.Sessions);
            Assert.Equal(fresh.SessionId, only.ID);
            Assert.Null(only.FinishedAt);
            // The old attempt's answers were cleaned up with it.
            Assert.Empty(repo.Answers);
        }

        [Fact]
        public async Task D6_FinishDosVeces_DevuelveElMismoVeredicto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PerExamId, wrong: [13, 14, 15]);

            var again = await service.FinishAsync(UserId, result.SessionId);

            Assert.False(again!.Passed);
            Assert.Equal(result.TotalErrors, again.TotalErrors);
        }

        // ---------- E. PNB (27 preguntas, max 10, sin limites por tema) ----------

        [Fact]
        public async Task E1_Pnb_DiezErrores_Apto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PnbExamId,
                wrong: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

            Assert.True(result.Passed);
            Assert.Equal(10, result.TotalErrors);
        }

        [Fact]
        public async Task E2_Pnb_OnceErrores_NoApto()
        {
            var (_, service) = CreateFixture();
            var result = await RunExamAsync(service, PnbExamId,
                wrong: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11]);

            Assert.False(result.Passed);
            Assert.Equal(11, result.TotalErrors);
            Assert.True(result.Topics.All(t => t.WithinLimit));
        }
    }
}
