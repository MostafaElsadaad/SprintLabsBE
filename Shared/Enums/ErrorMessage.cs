using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Enums
{
    public class ErrorMessage
    {
        public const string Success = "Success";
        public const string GeneralError = "Something went wrong";
        public const string DeleteSuccess = "Record Deleted Successfully";
        public const string InvalidFile = "Invalid File";
        public const string InvalidAccessToken = "InvalidAccessToken";
        public const string NotFound = "Resource Not Found";
        public const string IdNotFound = "Id Not Found";
        public const string UserCreationFailed = "User Creating Failed";
        public const string MemberOtherSquad = "Member in Another Squad";
        public const string ProjectOtherSquad = "Project Assigned to Another Squad";
        public const string CreationFailed = "Failed to Create Record";
        public const string DeletionFailed = "Failed to Delete Record";
        public const string ExistingRecord = "Record Already Exists";
        public const string NotModified = "Record is Not Modified";
        public const string InvalidInput = "Invalid Data";
        public const string TeacherAlreadyBelongsToAnotherCommunity = "TeacherAlreadyBelongsToAnotherCommunity";
        public const string InvalidTeacherInvitation = "InvalidTeacherInvitation";
        public const string InvalidOrExpiredPasswordResetToken = "InvalidOrExpiredPasswordResetToken";



    }
}
