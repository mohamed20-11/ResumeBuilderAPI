
using Application.DTOs;
using Domain.Enums;
using Domain.Models;
using Infrastructure.UnitOfWork;
using Infrastrucure.Context;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Application.Features.ResumeFeatures.Command.Put
{
    public class PutResumeCommandHandler : IRequestHandler<PutResumeCommand, ResponseDTO>
    {
        private readonly IUnitOfWork _unitOfWork;
        private AppDBContext _dbContext;
        public ResponseDTO _responseDto;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PutResumeCommandHandler(IHttpContextAccessor httpContextAccessor, IUnitOfWork unitOfWork, AppDBContext dbContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _responseDto = new ResponseDTO();
        }

        public async Task<ResponseDTO> Handle(PutResumeCommand request, CancellationToken cancellationToken)
        {
            var resumedto = request.Resume;
            var userName = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = _dbContext.Users.FirstOrDefault(u => u.UserName == userName);
            var resume = _unitOfWork.Repository<Resume>().GetAllAsNoTracking(r => r.UserId == userId.Id
            &&(r.Id==request.ResumeId)
            ).FirstOrDefault();

            if (string.IsNullOrEmpty(userName))
            {
                return new ResponseDTO { Result = false, Message = "Invalid token: UserId not found" };
            }
            var attachmentDto = new Attachment();

            var file = request.Resume.Attachment.File;
            if (file != null && file.Length != 0)
            {
                var uploadFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadFolderPath))
                {
                    Directory.CreateDirectory(uploadFolderPath);
                }
                var filename = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadFolderPath, filename);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                var attachment = new Attachment
                {
                    FileName = filename,
                    FilePath = filePath,
                };
                attachmentDto = attachment;
            }
            resume.FullName = resumedto.FullName?? resume.FullName;
            resume.Email = resumedto.Email ?? resume.Email;
            resume.PhoneNumber = resumedto.PhoneNumber ?? resume.PhoneNumber;
            resume.Address = resumedto.Address ?? resume.Address;
            resume.Summary = resumedto.Summary ?? resume.Summary;
            if (resumedto.Educations != null)
            {
                //resume.EducationHistory.Clear();
                foreach(var education in resume.EducationHistory)
                {
                    education.State = State.Deleted;
                }
                resume.EducationHistory = resumedto.Educations.Select(e => new Education
                {
                    InstitutionName = e.InstitutionName,
                    Degree = e.Degree,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate
                }).ToList();
            }
            if (resumedto.Experiences != null)
            {
                //resume.WorkExperience.Clear();
                foreach(var e in resume.WorkExperience)
                {
                    e.State = State.Deleted;
                }
                resume.WorkExperience = resumedto.Experiences.Select(w => new Experience
                {
                    CompanyName = w.CompanyName,
                    JobTitle = w.JobTitle,
                    StartDate = w.StartDate,
                    EndDate = w.EndDate,
                    Description = w.Description
                }).ToList();
            }
            if (resumedto.Skills != null)
            {
                foreach (var skill in resume.Skills)
                {
                    skill.State = State.Deleted;
                }
                resume.Skills = resumedto.Skills.Select(s => new Skill
                {
                    SkillName = s.SkillName
                }).ToList();
            }

            // تحديث Attachments
            if (resumedto.Attachment != null)
            {
                if (resume.Attachments == null)
                {
                    resume.Attachments = new Attachment();
                }
                resume.Attachments.FileName = attachmentDto.FileName;
                resume.Attachments.FilePath = attachmentDto.FilePath;
            }

            _dbContext.Resumes.Update(resume);
            await _dbContext.SaveChangesAsync();
            _responseDto.Message = "Resume Saved!";
            return _responseDto;
        }
    }
}
