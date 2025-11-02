using System;
using System.Collections.Generic;

namespace CourseWork_Shchegol.Domain
{
    public class User
    {
        public string Username { get; set; } = "";
        public string PasswordPlain { get; set; } = "";   
        public string PasswordHash { get; set; } = "";   
        public Dictionary<char, string> DiskRights { get; set; } = new()
        {
            ['A'] = "",
            ['B'] = "",
            ['C'] = "",
            ['D'] = "",
            ['E'] = ""
        };

        public DateTime? PasswordChangedUtc { get; set; } = null; 
        public int PasswordTtlDays { get; set; } = 0;         

        public bool IsPasswordExpiredUtc(DateTime nowUtc) =>
            PasswordTtlDays > 0 && PasswordChangedUtc.HasValue &&
            nowUtc > PasswordChangedUtc.Value.AddDays(PasswordTtlDays);
        public string AccessRights
        {
            get
            {
                string rights = "";
                foreach (var kv in DiskRights)
                {
                    if (!string.IsNullOrEmpty(kv.Value))
                        rights += kv.Value;
                }
                return rights;
            }
        }

    }
}
