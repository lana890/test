using EskaCMS.Infrastructure.Data;
using EskaCMS.Survey.Models;
using Microsoft.EntityFrameworkCore;

using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.Survey;

namespace EskaCMS.Survey.Data
{
    public class SurveyCustomModelBuilder : ICustomModelBuilder
    {
        public void Build(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Section>(b =>
            //{
            //    b.HasOne(s => s.Questionnaire).WithMany(q => q.Sections).HasForeignKey(s => s.QuestionnaireId).OnDelete(DeleteBehavior.Cascade);
            //    b.ToTable("Survey_Section");
            //});

            //modelBuilder.Entity<Question>(b =>
            //{
            //    b.HasOne(q => q.Section).WithMany(s => s.Questions).HasForeignKey(q => q.SectionId);
            //    b.ToTable("Survey_Question");
            //});

            //modelBuilder.Entity<Answer>(b =>
            //{
            //    b.HasOne(a => a.Question).WithMany(q => q.MultipleChoiceAnswers).HasForeignKey(a => a.QuestionId);
            //    b.ToTable("Survey_Answer");
            //});

            //modelBuilder.Entity<FilledQuestionnaire>(b =>
            //{
            //    b.HasOne(f => f.Questionnaire).WithMany(q => q.FilledQuestionnaires).HasForeignKey(f => f.QuestionnaireId);
            //    b.ToTable("Survey_FilledQuestionnaire");
            //});

            //modelBuilder.Entity<FilledAnswer>(b =>
            //{
            //    b.HasOne(a => a.FilledQuestionnaire).WithMany(q => q.FilledAnswers).HasForeignKey(a => a.FilledQuestionnaireId);
            //    b.HasOne(a => a.Question).WithMany(q => q.FilledAnswers).HasForeignKey(a => a.QuestionId);
            //    b.HasOne(a => a.Answer).WithMany(a => a.FilledAnswers).HasForeignKey(a => a.AnswerId);
            //    b.ToTable("Survey_FilledAnswer");
            //});


            //modelBuilder.Entity<Section>(b =>
            //{
            //    b.HasOne(s => s.Questionnaire).WithMany(q => q.Sections).HasForeignKey(s => s.QuestionnaireId).OnDelete(DeleteBehavior.Cascade);
            //    b.ToTable("Survey_Section");
            //});

            //modelBuilder.Entity<Question>(b =>
            //{
            //    b.HasOne(q => q.Section).WithMany(s => s.Questions).HasForeignKey(q => q.SectionId);
            //    b.ToTable("Survey_Question");
            //});

            //modelBuilder.Entity<Answer>(b =>
            //{
            //    b.HasOne(a => a.Question).WithMany(q => q.MultipleChoiceAnswers).HasForeignKey(a => a.QuestionId);
            //    b.ToTable("Survey_Answer");
            //});

            //modelBuilder.Entity<FilledQuestionnaire>(b =>
            //{
            //    b.HasOne(f => f.Questionnaire).WithMany(q => q.FilledQuestionnaires).HasForeignKey(f => f.QuestionnaireId);
            //    b.ToTable("Survey_FilledQuestionnaire");
            //});

            //modelBuilder.Entity<FilledAnswer>(b =>
            //{
            //    b.HasOne(a => a.FilledQuestionnaire).WithMany(q => q.FilledAnswers).HasForeignKey(a => a.FilledQuestionnaireId);
            //    b.HasOne(a => a.Question).WithMany(q => q.FilledAnswers).HasForeignKey(a => a.QuestionId);
            //    b.HasOne(a => a.Answer).WithMany(a => a.FilledAnswers).HasForeignKey(a => a.AnswerId);
            //    b.ToTable("Survey_FilledAnswer");
            //});

        }
    }
}
