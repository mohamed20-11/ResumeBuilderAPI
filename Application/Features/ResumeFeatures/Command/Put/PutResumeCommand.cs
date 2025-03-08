namespace Application.Features.ResumeFeatures.Command.Put
{
    public class PutResumeCommand : IRequest<ResponseDTO>
    {
        public Guid ResumeId { get; set; }
        public PutResumeDto Resume { get; set; }
    }
}
