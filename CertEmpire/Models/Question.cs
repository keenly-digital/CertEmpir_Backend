﻿using CertEmpire.Models.CommonModel;
using System.ComponentModel.DataAnnotations;

namespace CertEmpire.Models
{
    public class Question : AuditableBaseEntity
    {

        [Key]
        public Guid QuestionId { get; set; }
        public Guid FileId { get; set; }
        public string? QuestionText { get; set; }
        public string? QuestionDescription { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public List<int> CorrectAnswerIndices { get; set; } = new List<int>();
        public string? AnswerDescription { get; set; }
        public string? Explanation { get; set; }
        public string? questionImageURL { get; set; }
        public string? answerImageURL { get; set; }
        public bool ShowAnswer { get; set; } = false;
        public bool IsVerified {  get; set; }
        public string Verification { get; set; } = string.Empty;
        public Guid? TopicId { get; set; }
        public Guid? CaseStudyId { get; set; }
    }
}
