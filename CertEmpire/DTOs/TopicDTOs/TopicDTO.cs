namespace CertEmpire.DTOs.TopicDTOs
{
    public class AddCaseStudyDTOResponse
    {
        public Guid? TopicId { get; set; }
        public Guid FileId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public string CaseStudy { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? CaseStudyId { get; set; }

    }
    public class AddTopicDTO
    {
        public string TopicName { get; set; } = string.Empty;
        public Guid FileId { get; set; }
    }
    public class EditTopicDTO
    {
        public Guid TopicId { get; set; }
        public string TopicName { get; set; } = string.Empty;
    }
    public class AddCaseStudyDTO
    {
        public Guid? TopicId { get; set; }
        public Guid FileId { get; set; }
        public string CaseStudy { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
    public class EditCaseStudyDTO
    {
        public Guid CaseStudyId { get; set; }
        public string CaseStudy { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}   
